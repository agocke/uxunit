namespace UXUnit.Runtime.Reporters;

/// <summary>
/// Test result reporter that produces no output.
/// Useful for testing or when output is not desired.
/// </summary>
public sealed class NullTestReporter : ITestResultReporter
{
    /// <summary>
    /// Singleton instance.
    /// </summary>
    public static readonly NullTestReporter Instance = new();

    private NullTestReporter() { }

    /// <summary>
    /// Does nothing.
    /// </summary>
    public void ReportTestRunStart(TestRunInfo info) { }

    /// <summary>
    /// Does nothing.
    /// </summary>
    public void ReportTestComplete(TestResult result) { }

    /// <summary>
    /// Does nothing.
    /// </summary>
    public void ReportTestRunComplete(TestRunSummary summary) { }
}
