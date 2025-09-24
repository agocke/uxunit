using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace UXUnit.Runtime;

/// <summary>
/// Runtime test execution engine for UXUnit.
/// Provides core test execution functionality for source-generated test runners.
/// </summary>
public class TestRunner : ITestRunner
{
    private readonly ITestOutput _output;
    private readonly IServiceProvider _services;

    /// <summary>
    /// Initializes a new instance of the TestRunner.
    /// </summary>
    /// <param name="output">Test output writer. If null, uses console output.</param>
    /// <param name="services">Service provider for dependency injection. If null, uses empty provider.</param>
    public TestRunner(ITestOutput? output = null, IServiceProvider? services = null)
    {
        _output = output ?? new ConsoleTestOutput();
        _services = services ?? EmptyServiceProvider.Instance;
    }

    /// <summary>
    /// Runs all tests in the provided test class runners.
    /// </summary>
    /// <param name="testClassRunners">The test class runners to execute.</param>
    /// <param name="configuration">The test run configuration.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The aggregate test run result.</returns>
    public async Task<TestRunResult> RunTestsAsync(
        IEnumerable<ITestClassRunner> testClassRunners,
        TestRunConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        var runId = Guid.NewGuid().ToString();
        var startTime = DateTime.UtcNow;
        var runners = testClassRunners.ToList();
        
        _output.WriteLine($"UXUnit Test Run Started - ID: {runId}");
        _output.WriteLine($"Discovered {runners.Count} test classes");
        _output.WriteLine(string.Empty);

        var allResults = new ConcurrentBag<TestResult>();
        var totalTests = runners.Sum(r => r.Metadata.TestMethods.Count);
        var executedTests = 0;

        try
        {
            // Run test classes based on parallel execution configuration
            if (configuration.ParallelExecution && runners.Count > 1)
            {
                await RunTestClassesInParallel(runners, configuration, allResults, cancellationToken,
                    (completed) => {
                        var newExecuted = Interlocked.Add(ref executedTests, completed);
                        ReportProgress(newExecuted, totalTests);
                    });
            }
            else
            {
                await RunTestClassesSequentially(runners, configuration, allResults, cancellationToken,
                    (completed) => {
                        executedTests += completed;
                        ReportProgress(executedTests, totalTests);
                    });
            }
        }
        catch (OperationCanceledException)
        {
            _output.WriteLine(string.Empty);
            _output.WriteLine("Test execution was cancelled.");
        }

        var endTime = DateTime.UtcNow;
        var testResults = allResults.OrderBy(r => r.ClassName).ThenBy(r => r.TestName).ToList();
        var summary = CreateSummary(testResults);

        var result = new TestRunResult
        {
            RunId = runId,
            StartTime = startTime,
            EndTime = endTime,
            TestResults = testResults,
            Summary = summary
        };

        ReportFinalResults(result);
        return result;
    }

    private async Task RunTestClassesSequentially(
        IReadOnlyList<ITestClassRunner> runners,
        TestRunConfiguration configuration,
        ConcurrentBag<TestResult> allResults,
        CancellationToken cancellationToken,
        Action<int> onProgress)
    {
        foreach (var runner in runners)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            var classResults = await RunTestClass(runner, configuration, cancellationToken);
            foreach (var result in classResults)
            {
                allResults.Add(result);
            }

            onProgress(classResults.Length);

            // Stop on first failure if configured
            if (configuration.StopOnFirstFailure && classResults.Any(r => r.Status == TestStatus.Failed))
            {
                _output.WriteLine("Stopping execution on first failure as requested.");
                break;
            }
        }
    }

    private async Task RunTestClassesInParallel(
        IReadOnlyList<ITestClassRunner> runners,
        TestRunConfiguration configuration,
        ConcurrentBag<TestResult> allResults,
        CancellationToken cancellationToken,
        Action<int> onProgress)
    {
        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = configuration.MaxDegreeOfParallelism,
            CancellationToken = cancellationToken
        };

        var stopOnFailure = configuration.StopOnFirstFailure;
        var hasFailure = false;

        await Parallel.ForEachAsync(runners, parallelOptions, async (runner, ct) =>
        {
            if (stopOnFailure && hasFailure)
                return;

            var classResults = await RunTestClass(runner, configuration, ct);
            foreach (var result in classResults)
            {
                allResults.Add(result);
            }

            onProgress(classResults.Length);

            if (stopOnFailure && classResults.Any(r => r.Status == TestStatus.Failed))
            {
                hasFailure = true;
                _output.WriteLine("Stopping execution on first failure as requested.");
            }
        });
    }

    private async Task<TestResult[]> RunTestClass(
        ITestClassRunner runner,
        TestRunConfiguration configuration,
        CancellationToken cancellationToken)
    {
        var context = CreateTestContext(runner.Metadata, configuration);

        try
        {
            // Check if the entire class should be skipped
            if (runner.Metadata.Skip)
            {
                _output.WriteLine($"Skipping test class {runner.Metadata.ClassName}: {runner.Metadata.SkipReason}");
                return runner.Metadata.TestMethods
                    .Select(m => TestResult.Skipped($"{runner.Metadata.ClassName}.{m.MethodName}", 
                        m.MethodName, runner.Metadata.SkipReason ?? "Class skipped"))
                    .ToArray();
            }

            _output.WriteLine($"Running test class: {runner.Metadata.DisplayName ?? runner.Metadata.ClassName}");

            // Execute class-level lifecycle hooks if the runner supports them
            await ExecuteClassSetupHooks(runner, context);

            var results = await runner.RunAllTestsAsync(context, cancellationToken);

            await ExecuteClassCleanupHooks(runner, context, results);

            return results;
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Error running test class {runner.Metadata.ClassName}: {ex.Message}");
            
            // Create failure results for all methods in the class
            return runner.Metadata.TestMethods
                .Select(m => TestResult.Failure($"{runner.Metadata.ClassName}.{m.MethodName}", 
                    m.MethodName, ex, TimeSpan.Zero, DateTime.UtcNow, DateTime.UtcNow))
                .ToArray();
        }
    }

    private TestContext CreateTestContext(TestClassMetadata classMetadata, TestRunConfiguration configuration)
    {
        return new TestContext(
            className: classMetadata.ClassName,
            assemblyName: GetAssemblyName(classMetadata),
            output: _output,
            services: configuration.Services,
            cancellationToken: CancellationToken.None);
    }

    private static string GetAssemblyName(TestClassMetadata classMetadata)
    {
        // Try to extract assembly name, fallback to a default
        return classMetadata.Properties.TryGetValue("AssemblyName", out var name) 
            ? name?.ToString() ?? "Unknown"
            : "Unknown";
    }

    private async Task ExecuteClassSetupHooks(ITestClassRunner runner, TestContext context)
    {
        // In a full implementation, this would execute any class-level setup methods
        // For now, we'll implement this as a no-op since it depends on the generated code structure
        await Task.CompletedTask;
    }

    private async Task ExecuteClassCleanupHooks(ITestClassRunner runner, TestContext context, TestResult[] results)
    {
        // In a full implementation, this would execute any class-level cleanup methods
        // For now, we'll implement this as a no-op since it depends on the generated code structure
        await Task.CompletedTask;
    }

    private void ReportProgress(int completed, int total)
    {
        var percentage = total > 0 ? (completed * 100) / total : 100;
        _output.WriteLine($"Progress: {completed}/{total} tests completed ({percentage}%)");
    }

    private TestRunSummary CreateSummary(IReadOnlyList<TestResult> results)
    {
        var totalDuration = TimeSpan.FromTicks(results.Sum(r => r.Duration.Ticks));

        return new TestRunSummary
        {
            TotalTests = results.Count,
            PassedTests = results.Count(r => r.Status == TestStatus.Passed),
            FailedTests = results.Count(r => r.Status == TestStatus.Failed),
            SkippedTests = results.Count(r => r.Status == TestStatus.Skipped),
            InconclusiveTests = results.Count(r => r.Status == TestStatus.Inconclusive),
            TotalDuration = totalDuration
        };
    }

    private void ReportFinalResults(TestRunResult result)
    {
        _output.WriteLine(string.Empty);
        _output.WriteLine("=== Test Run Summary ===");
        _output.WriteLine($"Run ID: {result.RunId}");
        _output.WriteLine($"Duration: {result.Duration.TotalSeconds:F2} seconds");
        _output.WriteLine($"Total Tests: {result.Summary.TotalTests}");
        _output.WriteLine($"Passed: {result.Summary.PassedTests}");
        _output.WriteLine($"Failed: {result.Summary.FailedTests}");
        _output.WriteLine($"Skipped: {result.Summary.SkippedTests}");
        
        if (result.Summary.InconclusiveTests > 0)
        {
            _output.WriteLine($"Inconclusive: {result.Summary.InconclusiveTests}");
        }

        _output.WriteLine($"Pass Rate: {result.Summary.PassRate:P1}");
        _output.WriteLine(string.Empty);

        // Report failed tests
        var failedTests = result.TestResults.Where(r => r.Status == TestStatus.Failed).ToList();
        if (failedTests.Any())
        {
            _output.WriteLine("=== Failed Tests ===");
            foreach (var failedTest in failedTests)
            {
                _output.WriteLine($"❌ {failedTest.ClassName}.{failedTest.TestName}");
                if (!string.IsNullOrEmpty(failedTest.ErrorMessage))
                {
                    _output.WriteLine($"   Error: {failedTest.ErrorMessage}");
                }
                    _output.WriteLine($"   Duration: {failedTest.Duration.TotalMilliseconds:F0}ms");
                _output.WriteLine(string.Empty);
            }
        }        // Final status
        if (result.HasFailures)
        {
            _output.WriteLine("❌ Test run FAILED");
        }
        else
        {
            _output.WriteLine("✅ Test run PASSED");
        }
    }
}