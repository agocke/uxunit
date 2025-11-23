using System;
using System.Linq;

namespace UXUnit.Runtime.Reporters;

/// <summary>
/// Interface for reporting test execution progress and results.
/// Presentation layer - completely separate from execution.
/// </summary>
public interface ITestResultReporter
{
    /// <summary>
    /// Called when a test run starts.
    /// </summary>
    /// <param name="info">Information about the test run.</param>
    void ReportTestRunStart(TestRunInfo info);

    /// <summary>
    /// Called when a single test completes.
    /// </summary>
    /// <param name="result">The test result.</param>
    void ReportTestComplete(TestResult result);

    /// <summary>
    /// Called when the test run completes.
    /// </summary>
    /// <param name="summary">The test run summary.</param>
    void ReportTestRunComplete(TestRunSummary summary);
}

/// <summary>
/// Information about a test run.
/// </summary>
public sealed class TestRunInfo
{
    public int TotalTests { get; init; }

    public string RunId { get; init; } = Guid.NewGuid().ToString();

    public DateTime StartTime { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Summary statistics for a test run.
/// </summary>
public sealed class TestRunSummary
{
    public int TotalTests { get; init; }

    public int PassedTests { get; init; }

    public int FailedTests { get; init; }

    public int SkippedTests { get; init; }

    public TimeSpan TotalDuration { get; init; }

    public double PassRate => TotalTests > 0 ? (double)PassedTests / TotalTests : 0.0;

    public bool AllPassed => FailedTests == 0;

    /// <summary>
    /// Creates a summary from test results.
    /// </summary>
    public static TestRunSummary FromResults(TestResult[] results)
    {
        return new TestRunSummary
        {
            TotalTests = results.Length,
            PassedTests = results.Count(r => r.Status == TestStatus.Passed),
            FailedTests = results.Count(r => r.Status == TestStatus.Failed),
            SkippedTests = results.Count(r => r.Status == TestStatus.Skipped),
            TotalDuration = TimeSpan.FromTicks(results.Sum(r => r.Duration.Ticks)),
        };
    }
}
