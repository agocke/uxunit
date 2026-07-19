namespace NXTest.Runtime.Reporters;

/// <summary>
/// Run result reporter that produces no output.
/// Useful for testing or when output is not desired.
/// </summary>
public sealed class NullRunReporter : IRunResultReporter
{
    public static readonly NullRunReporter Instance = new();

    private NullRunReporter() { }

    public void ReportRunStart(RunInfo info) { }

    public void ReportResult(RunResult result) { }

    public void ReportRunComplete(RunSummary summary) { }
}
