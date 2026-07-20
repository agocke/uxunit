using System.Globalization;

namespace NXTest.Runtime;

internal static class BenchmarkResultFormatter
{
    public static string Format(BenchmarkStatistics statistics) =>
        $"Median: {FormatNanoseconds(statistics.MedianNanoseconds)}; "
        + $"MAD: {FormatNanoseconds(statistics.MedianAbsoluteDeviationNanoseconds)}; "
        + $"Mean: {FormatNanoseconds(statistics.MeanNanoseconds)}; "
        + $"StdDev: {FormatNanoseconds(statistics.StandardDeviationNanoseconds)}; "
        + $"Min: {FormatNanoseconds(statistics.MinimumNanoseconds)}; "
        + $"Max: {FormatNanoseconds(statistics.MaximumNanoseconds)}; "
        + $"95% CI: [{FormatNanoseconds(statistics.ConfidenceIntervalLowerNanoseconds)}, "
        + $"{FormatNanoseconds(statistics.ConfidenceIntervalUpperNanoseconds)}]; "
        + $"Samples: {statistics.Iterations}; "
        + $"Outliers: {statistics.OutlierCount}; "
        + $"Operations/iteration: {statistics.OperationsPerIteration}; "
        + $"GC: {statistics.Gen0Collections}/{statistics.Gen1Collections}/{statistics.Gen2Collections}; "
        + $"Allocated: {FormatBytes(statistics.AllocatedBytes)}"
        + (
            statistics.CalibrationTargetReached
                ? ""
                : "; Warning: calibration reached the operation limit before the target duration"
        )
        + (
            statistics.MeasurementConverged
                ? ""
                : "; Warning: measurement did not reach the target precision"
        )
        + (
            statistics.IsStable
                ? ""
                : "; Warning: unstable timing detected (distinct regimes across samples); treat the mean with caution"
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

    public static string FormatBytes(long bytes)
    {
        if (bytes < 1_024)
            return bytes.ToString(CultureInfo.InvariantCulture) + " B";
        if (bytes < 1_024 * 1_024)
            return (bytes / 1_024d).ToString("F2", CultureInfo.InvariantCulture) + " KB";

        return (bytes / (1_024d * 1_024d)).ToString("F2", CultureInfo.InvariantCulture) + " MB";
    }
}
