using System.Globalization;

namespace NXTest.Runtime;

internal static class BenchmarkResultFormatter
{
    public static string Format(BenchmarkStatistics statistics) =>
        $"Mean: {FormatNanoseconds(statistics.MeanNanoseconds)}; "
        + $"Median: {FormatNanoseconds(statistics.MedianNanoseconds)}; "
        + $"Min: {FormatNanoseconds(statistics.MinimumNanoseconds)}; "
        + $"Max: {FormatNanoseconds(statistics.MaximumNanoseconds)}; "
        + $"StdDev: {FormatNanoseconds(statistics.StandardDeviationNanoseconds)}; "
        + $"95% CI: [{FormatNanoseconds(statistics.ConfidenceIntervalLowerNanoseconds)}, "
        + $"{FormatNanoseconds(statistics.ConfidenceIntervalUpperNanoseconds)}]; "
        + $"Samples: {statistics.Iterations}; "
        + $"Outliers: {statistics.OutlierCount}; "
        + $"Operations/iteration: {statistics.OperationsPerIteration}"
        + (
            statistics.CalibrationTargetReached
                ? ""
                : "; Warning: calibration reached the operation limit before the target duration"
        )
        + (
            statistics.MeasurementConverged
                ? ""
                : "; Warning: measurement did not reach the target precision"
        );

    public static string FormatNanoseconds(double nanoseconds)
    {
        if (nanoseconds < 1_000)
            return nanoseconds.ToString("F2", CultureInfo.InvariantCulture) + " ns";
        if (nanoseconds < 1_000_000)
            return (nanoseconds / 1_000).ToString("F2", CultureInfo.InvariantCulture) + " us";
        if (nanoseconds < 1_000_000_000)
            return (nanoseconds / 1_000_000).ToString("F2", CultureInfo.InvariantCulture) + " ms";

        return (nanoseconds / 1_000_000_000).ToString("F2", CultureInfo.InvariantCulture) + " s";
    }
}
