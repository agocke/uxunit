using System;
using System.Linq;
using static NXTest.RunResult;

namespace NXTest.Runtime.Reporters;

/// <summary>
/// Interface for reporting test and benchmark execution progress and results.
/// Presentation layer - completely separate from execution.
/// </summary>
public interface IRunResultReporter
{
    /// <summary>
    /// Called when a run starts.
    /// </summary>
    /// <param name="info">Information about the test run.</param>
    void ReportRunStart(RunInfo info);

    /// <summary>
    /// Called when a test or benchmark completes.
    /// </summary>
    /// <param name="result">The run result.</param>
    void ReportResult(RunResult result);

    /// <summary>
    /// Called when the run completes.
    /// </summary>
    /// <param name="summary">The test run summary.</param>
    void ReportRunComplete(RunSummary summary);
}

/// <summary>
/// Information about a test run.
/// </summary>
public sealed class RunInfo
{
    public int TotalTests { get; init; }

    public int TotalBenchmarks { get; init; }

    public string RunId { get; init; } = Guid.NewGuid().ToString();

    public DateTime StartTime { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Summary statistics for a test run.
/// </summary>
public sealed class RunSummary
{
    public int TotalTests { get; init; }

    public int PassedTests { get; init; }

    public int FailedTests { get; init; }

    public int SkippedTests { get; init; }

    public int CompletedBenchmarks { get; init; }

    public int FailedBenchmarks { get; init; }

    public int SkippedBenchmarks { get; init; }

    public TimeSpan TotalDuration { get; init; }

    public double PassRate => TotalTests > 0 ? (double)PassedTests / TotalTests : 0.0;

    public bool AllSucceeded => FailedTests == 0 && FailedBenchmarks == 0;

    /// <summary>
    /// Creates a summary from test results.
    /// </summary>
    public static RunSummary FromResults(RunResult[] results)
    {
        var testResults = results.OfType<TestResult>().ToArray();
        var benchmarkResults = results.OfType<BenchmarkResult>().ToArray();
        return new RunSummary
        {
            TotalTests = testResults.Length,
            PassedTests = testResults.Count(r => r is TestResult.Passed),
            FailedTests = testResults.Count(
                r => r is TestResult.Failed or TestResult.Faulted
            ),
            SkippedTests = testResults.Count(r => r is TestResult.Skipped),
            CompletedBenchmarks = benchmarkResults.Count(
                r => r is BenchmarkResult.Completed
            ),
            FailedBenchmarks = benchmarkResults.Count(
                r => r is BenchmarkResult.Failed
            ),
            SkippedBenchmarks = benchmarkResults.Count(
                r => r is BenchmarkResult.Skipped
            ),
            TotalDuration = TimeSpan.FromTicks(
                testResults.Sum(
                    r => r switch
                    {
                        TestResult.Passed passed => passed.Duration.Ticks,
                        TestResult.Failed failed => failed.Duration.Ticks,
                        _ => 0,
                    }
                )
                + benchmarkResults.Sum(
                    r => r switch
                    {
                        BenchmarkResult.Completed completed =>
                            completed.RunDuration.Ticks,
                        BenchmarkResult.Failed failed => failed.RunDuration.Ticks,
                        _ => 0,
                    }
                )
            ),
        };
    }
}
