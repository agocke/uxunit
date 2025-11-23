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
        CancellationToken cancellationToken = default)
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

    private static List<TestDescriptor> CollectAllTests(IReadOnlyList<TestClassMetadata> testClasses)
    {
        var tests = new List<TestDescriptor>();

        foreach (var testClass in testClasses)
        {
            foreach (var testMethod in testClass.TestMethods)
            {
                // For now, we'll need to update this when generators are ready
                // to provide execution delegates directly in metadata
                tests.Add(new TestDescriptor
                {
                    Metadata = testMethod,
                    ClassName = testClass.ClassName,
                    AssemblyName = testClass.Properties.TryGetValue("AssemblyName", out var asm)
                        ? asm?.ToString() ?? "Unknown"
                        : "Unknown"
                });
            }
        }

        return tests;
    }

    private static async Task<TestResult[]> ExecuteSequentiallyAsync(
        List<TestDescriptor> tests,
        TestExecutionOptions options,
        CancellationToken cancellationToken)
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
        CancellationToken cancellationToken)
    {
        var results = new System.Collections.Concurrent.ConcurrentBag<TestResult>();
        var shouldStop = false;

        await Parallel.ForEachAsync(
            tests,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = options.MaxDegreeOfParallelism,
                CancellationToken = cancellationToken
            },
            async (test, ct) =>
            {
                if (options.StopOnFirstFailure && shouldStop)
                    return;

                var result = await ExecuteTestAsync(test, options, ct);
                results.Add(result);

                if (options.StopOnFirstFailure && result.Status == TestStatus.Failed)
                    shouldStop = true;
            });

        return results.OrderBy(r => r.TestId).ToArray();
    }

    private static async Task<TestResult> ExecuteTestAsync(
        TestDescriptor test,
        TestExecutionOptions options,
        CancellationToken cancellationToken)
    {
        // Check if test should be skipped
        if (test.Metadata.Skip)
        {
            return TestResult.Skipped(
                GenerateTestId(test),
                test.Metadata.MethodName,
                test.Metadata.SkipReason ?? "Test marked as skipped",
                test.ClassName);
        }

        var testId = GenerateTestId(test);
        var startTime = DateTime.UtcNow;

        // If execution delegate is provided, invoke it
        if (test.Metadata.ExecuteAsync != null)
        {
            try
            {
                var result = await test.Metadata.ExecuteAsync(cancellationToken);
                // The delegate returns a complete TestResult, but we need to enrich it with ClassName
                // Since ClassName is init-only, we need to create a new result if it's not set
                if (string.IsNullOrEmpty(result.ClassName))
                {
                    // Re-create the result with the ClassName populated
                    return result.Status switch
                    {
                        TestStatus.Passed => TestResult.Success(
                            result.TestId,
                            result.TestName,
                            result.Duration,
                            result.StartTime,
                            result.EndTime,
                            test.ClassName),
                        TestStatus.Failed => TestResult.Failure(
                            result.TestId,
                            result.TestName,
                            new Exception(result.ErrorMessage ?? "Test failed"),
                            result.Duration,
                            result.StartTime,
                            result.EndTime,
                            test.ClassName),
                        TestStatus.Skipped => TestResult.Skipped(
                            result.TestId,
                            result.TestName,
                            result.SkipReason ?? "Skipped",
                            test.ClassName),
                        _ => result
                    };
                }
                return result;
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
                    startTime,
                    endTime,
                    test.ClassName);
            }
        }

        // No delegate provided - return placeholder success
        // (This will be used when generators provide the delegate)
        return TestResult.Success(
            testId,
            test.Metadata.MethodName,
            TimeSpan.Zero,
            startTime,
            DateTime.UtcNow,
            test.ClassName);
    }

    private static string GenerateTestId(TestDescriptor test)
    {
        return $"{test.ClassName}.{test.Metadata.MethodName}";
    }

    private class TestDescriptor
    {
        public required TestMethodMetadata Metadata { get; init; }
        public required string ClassName { get; init; }
        public required string AssemblyName { get; init; }
    }
}
