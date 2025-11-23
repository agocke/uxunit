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
    /// <summary>
    /// Gets or sets the total number of tests to run.
    /// </summary>
    public int TotalTests { get; init; }

    /// <summary>
    /// Gets or sets the test run ID.
    /// </summary>
    public string RunId { get; init; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the start time of the test run.
    /// </summary>
    public DateTime StartTime { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Summary statistics for a test run.
/// </summary>
public sealed class TestRunSummary
{
    /// <summary>
    /// Gets or sets the total number of tests.
    /// </summary>
    public int TotalTests { get; init; }

    /// <summary>
    /// Gets or sets the number of passed tests.
    /// </summary>
    public int PassedTests { get; init; }

    /// <summary>
    /// Gets or sets the number of failed tests.
    /// </summary>
    public int FailedTests { get; init; }

    /// <summary>
    /// Gets or sets the number of skipped tests.
    /// </summary>
    public int SkippedTests { get; init; }

    /// <summary>
    /// Gets or sets the total duration of all tests.
    /// </summary>
    public TimeSpan TotalDuration { get; init; }

    /// <summary>
    /// Gets the pass rate as a percentage (0.0 to 1.0).
    /// </summary>
    public double PassRate => TotalTests > 0 ? (double)PassedTests / TotalTests : 0.0;

    /// <summary>
    /// Gets whether all tests passed.
    /// </summary>
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
            TotalDuration = TimeSpan.FromTicks(results.Sum(r => r.Duration.Ticks))
        };
    }
}
