namespace UXUnit.Runtime.Reporters;

/// <summary>
/// Test result reporter that produces no output.
/// Useful for testing or when output is not desired.
/// </summary>
public sealed class NullTestReporter : ITestResultReporter
{
    public static readonly NullTestReporter Instance = new();

    private NullTestReporter() { }

    public void ReportTestRunStart(TestRunInfo info) { }

    public void ReportTestComplete(TestResult result) { }

    public void ReportTestRunComplete(TestRunSummary summary) { }
}
