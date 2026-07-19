using System;
using System.Collections.Generic;
using System.Diagnostics;
using NXTest;

namespace NXTest.Runtime;

internal static class BenchmarkAnalysis
{
    internal const int MinimumSampleCount = 10;
    internal const double TargetRelativeMarginOfError = 0.02;

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

        var summary = CalculateMeanAndVariance(samples);
        if (summary.Mean <= 0)
            return false;

        var standardError = Math.Sqrt(summary.SampleVariance / samples.Count);
        var marginOfError = GetCriticalValue95(samples.Count - 1) * standardError;
        return marginOfError <= summary.Mean * TargetRelativeMarginOfError;
    }

    internal static BenchmarkStatistics Calculate(
        double[] samples,
        int operationsPerIteration,
        bool calibrationTargetReached,
        int warmupIterations,
        bool measurementConverged,
        long totalMeasurementTimestampTicks
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
            Percentile(sortedSamples, 0.5),
            sortedSamples[0],
            sortedSamples[^1],
            standardDeviation,
            standardError,
            Math.Max(0, summary.Mean - marginOfError),
            summary.Mean + marginOfError,
            outlierCount
        );
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
