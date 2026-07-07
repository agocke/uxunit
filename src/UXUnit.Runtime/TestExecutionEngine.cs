using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace UXUnit.Runtime;

/// <summary>
/// Core test execution engine. Takes test metadata and produces test results.
/// This is a pure function with no presentation logic.
/// </summary>
public static class TestExecutionEngine
{
    /// <summary>
    /// Executes all tests and returns results.
    /// </summary>
    /// <param name="testClasses">The test class metadata with execution delegates.</param>
    /// <param name="options">Execution options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Array of test results.</returns>
    public static async Task<TestResult[]> ExecuteTestsAsync(
        IReadOnlyList<TestClassMetadata> testClasses,
        TestExecutionOptions options,
        CancellationToken cancellationToken = default
    )
    {
        var allTests = CollectAllTests(testClasses);

        return options.Mode switch
        {
            ParallelMode.None => await ExecuteSequentiallyAsync(allTests, options, cancellationToken),
            ParallelMode.Classes => await ExecuteClassesInParallelAsync(allTests, options, cancellationToken),
            _ => await ExecuteTestsInParallelAsync(allTests, options, cancellationToken),
        };
    }

    internal static List<TestDescriptor> CollectAllTests(
        IReadOnlyList<TestClassMetadata> testClasses
    )
    {
        var tests = new List<TestDescriptor>();

        foreach (var testClass in testClasses)
        {
            foreach (var testMethod in testClass.TestMethods)
            {
                // Pattern match on Fact vs Theory
                switch (testMethod)
                {
                    case TestMethodMetadata.Fact fact:
                        // Fact: Execute once
                        tests.Add(
                            new TestDescriptor
                            {
                                Method = fact,
                                Class = testClass,
                            }
                        );
                        break;

                    case TestMethodMetadata.Theory theory:
                        // Theory: a single descriptor; ExecuteTestAsync expands the cases.
                        tests.Add(
                            new TestDescriptor
                            {
                                Method = theory,
                                Class = testClass,
                            }
                        );
                        break;
                }
            }
        }

        // Randomly permute tests on each run to surface accidental ordering dependencies.
        Shuffle(tests);

        return tests;
    }

    private static void Shuffle(List<TestDescriptor> tests)
    {
        // Fisher-Yates shuffle using a thread-safe shared RNG.
        for (int i = tests.Count - 1; i > 0; i--)
        {
            int j = Random.Shared.Next(i + 1);
            (tests[i], tests[j]) = (tests[j], tests[i]);
        }
    }

    private static async Task<TestResult[]> ExecuteSequentiallyAsync(
        List<TestDescriptor> tests,
        TestExecutionOptions options,
        CancellationToken cancellationToken
    )
    {
        var allResults = new List<TestResult>(tests.Count);

        foreach (var test in tests)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            var results = await ExecuteTestAsync(test, options, cancellationToken);
            allResults.AddRange(results);

            if (options.StopOnFirstFailure && results.Any(r => r.Status == TestStatus.Failed))
                break;
        }

        return allResults.ToArray();
    }

    private static async Task<TestResult[]> ExecuteTestsInParallelAsync(
        List<TestDescriptor> tests,
        TestExecutionOptions options,
        CancellationToken cancellationToken
    )
    {
        var allResults = new System.Collections.Concurrent.ConcurrentBag<TestResult>();
        var shouldStop = false;

        await Parallel.ForEachAsync(
            tests,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = options.MaxDegreeOfParallelism,
                CancellationToken = cancellationToken,
            },
            async (test, ct) =>
            {
                if (options.StopOnFirstFailure && shouldStop)
                    return;

                var results = await ExecuteTestAsync(test, options, ct);
                foreach (var result in results)
                {
                    allResults.Add(result);
                }

                if (options.StopOnFirstFailure && results.Any(r => r.Status == TestStatus.Failed))
                    shouldStop = true;
            }
        );

        return allResults.OrderBy(r => r.TestId).ToArray();
    }

    private static async Task<TestResult[]> ExecuteClassesInParallelAsync(
        List<TestDescriptor> tests,
        TestExecutionOptions options,
        CancellationToken cancellationToken
    )
    {
        // Classes run in parallel; tests within a class run sequentially.
        var classGroups = tests.GroupBy(t => t.ClassName, StringComparer.Ordinal);

        var allResults = new System.Collections.Concurrent.ConcurrentBag<TestResult>();
        var shouldStop = false;

        await Parallel.ForEachAsync(
            classGroups,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = options.MaxDegreeOfParallelism,
                CancellationToken = cancellationToken,
            },
            async (group, ct) =>
            {
                foreach (var test in group)
                {
                    if (ct.IsCancellationRequested)
                        break;

                    if (options.StopOnFirstFailure && shouldStop)
                        break;

                    var results = await ExecuteTestAsync(test, options, ct);
                    foreach (var result in results)
                    {
                        allResults.Add(result);

                        if (options.StopOnFirstFailure && result.Status == TestStatus.Failed)
                            shouldStop = true;
                    }
                }
            }
        );

        return allResults.OrderBy(r => r.TestId).ToArray();
    }

    internal static async Task<TestResult[]> ExecuteTestAsync(
        TestDescriptor test,
        TestExecutionOptions options,
        CancellationToken cancellationToken
    )
    {
        var testId = GenerateTestId(test);
        var methodName = test.Method.MethodName;


        // Check if test method should be skipped
        if (test.Method.Skip)
        {
            return
            [
                TestResult.Skipped(
                    testId,
                    methodName,
                    test.Method.SkipReason ?? "Test marked as skipped",
                    test.ClassName
                )
            ];
        }

        object? testClassInstance = null;
        try
        {
            testClassInstance = test.Class.CreateInstance();
        }
        catch (Exception ex)
        {
            // If the delegate throws, create a fault result
            return
            [
                TestResult.Fault(
                    testId,
                    methodName,
                    ex.Message,
                    ex.StackTrace,
                    test.ClassName
                )
            ];
        }

        // Pattern match on Fact vs Theory
        try
        {
            switch (test.Method)
            {
                case TestMethodMetadata.Fact fact:
                    {
                        return
                        [
                            await RunTest(
                                testId,
                                methodName,
                                test.Class.TestDispatch,
                                testClassInstance,
                                methodName,
                                null,
                                test.ClassName
                            )
                        ];
                    }

                case TestMethodMetadata.Theory theory:
                    var cases = theory.TestCases;
                    var results = new TestResult[cases.Count];
                    for (int i = 0; i < cases.Count; i++)
                    {
                        // Match xUnit's per-case naming: MethodName(displayName)
                        var caseName = $"{methodName}({cases[i].DisplayName})";
                        var caseId = $"{testId}({cases[i].DisplayName})";
                        results[i] = await RunTest(
                            caseId,
                            caseName,
                            test.Class.TestDispatch,
                            testClassInstance,
                            methodName,
                            cases[i].Arguments,
                            test.ClassName
                        );
                    }
                    return results;

                default:
                    throw new InvalidOperationException(
                        $"Unknown test method type: {test.Method.GetType()}"
                    );
            }
        }
        finally
        {
            if (testClassInstance is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        static async Task<TestResult> RunTest(
            string testId,
            string testName,
            TestClassMetadata.DispatchFunc dispatch,
            object? testClassInstance,
            string methodName,
            object? theoryArgs,
            string className
        )
        {
            var sw = new Stopwatch();
            sw.Start();
            try
            {
                await dispatch(testClassInstance, methodName, theoryArgs);
            }
            catch (Exception ex)
            {
                // If the delegate throws, create a failure result
                sw.Stop();
                return TestResult.Failure(
                        testId,
                        testName,
                        ex,
                        sw.Elapsed,
                        className
                    );
            }
            sw.Stop();
            return TestResult.Success(
                testId,
                testName,
                sw.Elapsed,
                className
            );
        }
    }

    private static string GenerateTestName(TestDescriptor test)
    {
        var baseName = test.Method.MethodName;

        // Regular test without cases
        return baseName;
    }

    private static string GenerateTestId(TestDescriptor test)
    {
        var baseId = test.DisplayName;

        // Regular test without cases
        return baseId;
    }

    internal class TestDescriptor
    {
        public required TestClassMetadata Class { get; init; }
        public required TestMethodMetadata Method { get; init; }

        public string ClassName => Class.ClassName;
        public string DisplayName => Class.DisplayName ?? Class.ClassName + "." + Method.MethodName;
    }
}
