using System;
using System.Collections.Generic;
using System.Diagnostics;
using NXTest;

namespace NXTest.Runtime;

internal static class BenchmarkAnalysis
{
    internal const int MinimumSampleCount = 10;
    internal const double TargetRelativeMarginOfError = 0.02;
    internal const double InstabilityThreshold = 0.10;

    // Scales the median absolute deviation to a standard-deviation-equivalent
    // for normally distributed data, giving a robust dispersion estimate.
    private const double RobustStandardDeviationConstant = 1.4826;

    private static readonly double[] StudentTCriticalValues95 =
    [
        12.706,
        4.303,
        3.182,
        2.776,
        2.571,
        2.447,
        2.365,
        2.306,
        2.262,
        2.228,
        2.201,
        2.179,
        2.160,
        2.145,
        2.131,
        2.120,
        2.110,
        2.101,
        2.093,
        2.086,
        2.080,
        2.074,
        2.069,
        2.064,
        2.060,
        2.056,
        2.052,
        2.048,
        2.045,
        2.042,
    ];

    internal static bool HasMetPrecision(IReadOnlyList<double> samples)
    {
        if (samples.Count < MinimumSampleCount)
            return false;

        var median = Median(samples);
        if (median <= 0)
            return false;

        // Base convergence on a robust dispersion estimate rather than the
        // sample standard deviation. A few retained outliers inflate the
        // variance enough to keep an otherwise-tight benchmark from ever
        // meeting its precision target, even when the median and MAD are
        // stable. The MAD, scaled to a standard-deviation equivalent, is
        // insensitive to those outliers while preserving the "2% relative
        // precision" interpretation (now relative to the median).
        var robustStandardDeviation =
            RobustStandardDeviationConstant * MedianAbsoluteDeviation(samples, median);
        var standardError = robustStandardDeviation / Math.Sqrt(samples.Count);
        var marginOfError = GetCriticalValue95(samples.Count - 1) * standardError;
        return marginOfError <= median * TargetRelativeMarginOfError;
    }

    internal static BenchmarkStatistics Calculate(
        double[] samples,
        int operationsPerIteration,
        bool calibrationTargetReached,
        int warmupIterations,
        bool measurementConverged,
        long totalMeasurementTimestampTicks,
        TestExecutionEngine.BenchmarkGcStatistics gcStatistics = default
    )
    {
        if (samples.Length == 0)
            throw new ArgumentException("At least one benchmark sample is required.", nameof(samples));

        var sortedSamples = (double[])samples.Clone();
        Array.Sort(sortedSamples);

        var summary = CalculateMeanAndVariance(samples);
        var standardDeviation = Math.Sqrt(summary.SampleVariance);
        var standardError = standardDeviation / Math.Sqrt(samples.Length);
        var marginOfError = GetCriticalValue95(samples.Length - 1) * standardError;

        var median = Percentile(sortedSamples, 0.5);
        var medianAbsoluteDeviation = MedianAbsoluteDeviation(samples, median);

        var firstQuartile = Percentile(sortedSamples, 0.25);
        var thirdQuartile = Percentile(sortedSamples, 0.75);
        var interquartileRange = thirdQuartile - firstQuartile;
        var lowerOutlierFence = firstQuartile - 1.5 * interquartileRange;
        var upperOutlierFence = thirdQuartile + 1.5 * interquartileRange;
        var outlierCount = 0;
        foreach (var sample in samples)
        {
            if (sample < lowerOutlierFence || sample > upperOutlierFence)
                outlierCount++;
        }

        var isStable = IsStable(samples);

        var retainedSamples = Array.AsReadOnly((double[])samples.Clone());
        return new BenchmarkStatistics(
            samples.Length,
            operationsPerIteration,
            calibrationTargetReached,
            warmupIterations,
            measurementConverged,
            TimeSpan.FromSeconds(
                (double)totalMeasurementTimestampTicks / Stopwatch.Frequency
            ),
            retainedSamples,
            summary.Mean,
            median,
            sortedSamples[0],
            sortedSamples[^1],
            standardDeviation,
            standardError,
            Math.Max(0, summary.Mean - marginOfError),
            summary.Mean + marginOfError,
            outlierCount,
            medianAbsoluteDeviation,
            isStable,
            gcStatistics.Gen0Collections,
            gcStatistics.Gen1Collections,
            gcStatistics.Gen2Collections,
            gcStatistics.AllocatedBytes
        );
    }

    /// <summary>
    /// Detects non-stationary execution by comparing the median of the first
    /// half of the (chronologically ordered) samples with the median of the
    /// second half. A material difference signals distinct timing regimes,
    /// so the run is reported as unstable rather than as a deceptively precise
    /// mean. Comparing medians of sample groups also blunts the effect of
    /// autocorrelation between adjacent samples.
    /// </summary>
    internal static bool IsStable(IReadOnlyList<double> samples)
    {
        if (samples.Count < 4)
            return true;

        var midpoint = samples.Count / 2;
        var firstHalf = new double[midpoint];
        var secondHalf = new double[samples.Count - midpoint];
        for (var i = 0; i < midpoint; i++)
            firstHalf[i] = samples[i];
        for (var i = midpoint; i < samples.Count; i++)
            secondHalf[i - midpoint] = samples[i];

        Array.Sort(firstHalf);
        Array.Sort(secondHalf);
        var firstMedian = Percentile(firstHalf, 0.5);
        var secondMedian = Percentile(secondHalf, 0.5);
        if (firstMedian <= 0)
            return true;

        return Math.Abs(secondMedian - firstMedian) / firstMedian <= InstabilityThreshold;
    }

    private static double Median(IReadOnlyList<double> samples)
    {
        var sorted = new double[samples.Count];
        for (var i = 0; i < samples.Count; i++)
            sorted[i] = samples[i];
        Array.Sort(sorted);
        return Percentile(sorted, 0.5);
    }

    private static double MedianAbsoluteDeviation(IReadOnlyList<double> samples, double median)
    {
        var deviations = new double[samples.Count];
        for (var i = 0; i < samples.Count; i++)
            deviations[i] = Math.Abs(samples[i] - median);
        Array.Sort(deviations);
        return Percentile(deviations, 0.5);
    }

    private static (double Mean, double SampleVariance) CalculateMeanAndVariance(
        IReadOnlyList<double> samples
    )
    {
        double mean = 0;
        double sumOfSquaredDifferences = 0;

        for (var i = 0; i < samples.Count; i++)
        {
            var difference = samples[i] - mean;
            mean += difference / (i + 1);
            sumOfSquaredDifferences += difference * (samples[i] - mean);
        }

        var sampleVariance =
            samples.Count > 1 ? sumOfSquaredDifferences / (samples.Count - 1) : 0;
        return (mean, sampleVariance);
    }

    private static double GetCriticalValue95(int degreesOfFreedom)
    {
        if (degreesOfFreedom <= 0)
            return double.PositiveInfinity;
        if (degreesOfFreedom <= StudentTCriticalValues95.Length)
            return StudentTCriticalValues95[degreesOfFreedom - 1];

        return 1.96;
    }

    private static double Percentile(double[] sortedSamples, double percentile)
    {
        var position = (sortedSamples.Length - 1) * percentile;
        var lowerIndex = (int)Math.Floor(position);
        var upperIndex = (int)Math.Ceiling(position);
        if (lowerIndex == upperIndex)
            return sortedSamples[lowerIndex];

        var fraction = position - lowerIndex;
        return sortedSamples[lowerIndex]
            + (sortedSamples[upperIndex] - sortedSamples[lowerIndex]) * fraction;
    }
}
