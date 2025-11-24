using System;
using System.Collections.Generic;
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

        if (options.ParallelExecution && allTests.Count > 1)
        {
            return await ExecuteInParallelAsync(allTests, options, cancellationToken);
        }
        else
        {
            return await ExecuteSequentiallyAsync(allTests, options, cancellationToken);
        }
    }

    private static List<TestDescriptor> CollectAllTests(
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
                                Metadata = fact,
                                TestCase = null,
                                TestCaseIndex = -1,
                                ClassName = testClass.ClassName,
                                AssemblyName = testClass.AssemblyName,
                            }
                        );
                        break;

                    case TestMethodMetadata.Theory theory:
                        // Theory: Execute once per test case
                        for (int i = 0; i < theory.TestCases.Count; i++)
                        {
                            var testCase = theory.TestCases[i];
                            tests.Add(
                                new TestDescriptor
                                {
                                    Metadata = theory,
                                    TestCase = testCase,
                                    TestCaseIndex = i,
                                    ClassName = testClass.ClassName,
                                    AssemblyName = testClass.AssemblyName,
                                }
                            );
                        }
                        break;
                }
            }
        }

        return tests;
    }

    private static async Task<TestResult[]> ExecuteSequentiallyAsync(
        List<TestDescriptor> tests,
        TestExecutionOptions options,
        CancellationToken cancellationToken
    )
    {
        var results = new List<TestResult>(tests.Count);

        foreach (var test in tests)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            var result = await ExecuteTestAsync(test, options, cancellationToken);
            results.Add(result);

            if (options.StopOnFirstFailure && result.Status == TestStatus.Failed)
                break;
        }

        return results.ToArray();
    }

    private static async Task<TestResult[]> ExecuteInParallelAsync(
        List<TestDescriptor> tests,
        TestExecutionOptions options,
        CancellationToken cancellationToken
    )
    {
        var results = new System.Collections.Concurrent.ConcurrentBag<TestResult>();
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

                var result = await ExecuteTestAsync(test, options, ct);
                results.Add(result);

                if (options.StopOnFirstFailure && result.Status == TestStatus.Failed)
                    shouldStop = true;
            }
        );

        return results.OrderBy(r => r.TestId).ToArray();
    }

    private static async Task<TestResult> ExecuteTestAsync(
        TestDescriptor test,
        TestExecutionOptions options,
        CancellationToken cancellationToken
    )
    {
        // Check if test case should be skipped
        if (test.TestCase?.Skip == true)
        {
            return TestResult.Skipped(
                GenerateTestId(test),
                test.Metadata.MethodName,
                test.TestCase.SkipReason ?? "Test case marked as skipped",
                test.ClassName,
                test.AssemblyName
            );
        }

        // Check if test method should be skipped
        if (test.Metadata.Skip)
        {
            return TestResult.Skipped(
                GenerateTestId(test),
                test.Metadata.MethodName,
                test.Metadata.SkipReason ?? "Test marked as skipped",
                test.ClassName,
                test.AssemblyName
            );
        }

        var testId = GenerateTestId(test);
        var startTime = DateTime.UtcNow;

        try
        {
            // Pattern match on Fact vs Theory
            switch (test.Metadata)
            {
                case TestMethodMetadata.Fact fact:
                    if (fact.Body != null)
                    {
                        await fact.Body(cancellationToken);
                    }
                    // else: No body - test passes (placeholder for generator)
                    break;

                case TestMethodMetadata.Theory theory:
                    if (test.TestCase == null)
                    {
                        throw new InvalidOperationException(
                            "Theory test descriptor must have a TestCase"
                        );
                    }
                    if (theory.ParameterizedBody != null)
                    {
                        await theory.ParameterizedBody(test.TestCase.Arguments, cancellationToken);
                    }
                    // else: No body - test passes (placeholder for generator)
                    break;

                default:
                    throw new InvalidOperationException(
                        $"Unknown test method type: {test.Metadata.GetType()}"
                    );
            }

            // If we get here, test passed
            var endTime = DateTime.UtcNow;
            return TestResult.Success(
                testId,
                test.Metadata.MethodName,
                endTime - startTime,
                test.ClassName,
                test.AssemblyName
            );
        }
        catch (Exception ex)
        {
            // If the delegate throws, create a failure result
            var endTime = DateTime.UtcNow;
            return TestResult.Failure(
                testId,
                test.Metadata.MethodName,
                ex,
                endTime - startTime,
                test.ClassName,
                test.AssemblyName
            );
        }
    }

    private static string GenerateTestId(TestDescriptor test)
    {
        var baseId = $"{test.ClassName}.{test.Metadata.MethodName}";

        // If this is a test case with a display name, use it
        if (test.TestCase?.DisplayName != null)
        {
            return $"{baseId}({test.TestCase.DisplayName})";
        }

        // If this is a test case with an index, include it
        if (test.TestCaseIndex >= 0)
        {
            return $"{baseId}[{test.TestCaseIndex}]";
        }

        // Regular test without cases
        return baseId;
    }

    private class TestDescriptor
    {
        public required TestMethodMetadata Metadata { get; init; }
        public required string ClassName { get; init; }
        public required string AssemblyName { get; init; }
        public TestCaseMetadata? TestCase { get; init; }
        public int TestCaseIndex { get; init; } = -1;
    }
}
