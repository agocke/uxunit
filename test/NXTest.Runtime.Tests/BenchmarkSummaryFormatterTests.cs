using System;
using NXTest.Runtime;
using static NXTest.RunResult;
using XunitAssert = Xunit.Assert;

namespace NXTest.Runtime.Tests;

public class BenchmarkSummaryFormatterTests
{
    private static BenchmarkResult.Completed Completed(
        string className,
        string name,
        double[] samples,
        TestExecutionEngine.BenchmarkGcStatistics gc = default
    )
    {
        var statistics = BenchmarkAnalysis.Calculate(
            samples,
            operationsPerIteration: 100,
            calibrationTargetReached: true,
            warmupIterations: 5,
            measurementConverged: true,
            totalMeasurementTimestampTicks: System.Diagnostics.Stopwatch.Frequency,
            gc
        );
        return new BenchmarkResult.Completed(name, name, className, TimeSpan.FromSeconds(1), statistics);
    }

    [Fact]
    public void FormatSummary_RendersAlignedTableWithHeaders()
    {
        double[] stable = [100, 100, 100, 100, 100, 100, 100, 100, 100, 100];
        var summary = BenchmarkSummaryFormatter.FormatSummary(
        [
            Completed("Bench", "Fast", stable),
            Completed("Bench", "Slow", stable),
        ]);

        XunitAssert.Contains("Benchmark summary", summary);
        XunitAssert.Contains("Median", summary);
        XunitAssert.Contains("Floor (P10)", summary);
        XunitAssert.Contains("Alloc/op", summary);
        XunitAssert.Contains("GC/1k op", summary);
        XunitAssert.Contains("Bench.Fast", summary);
        XunitAssert.Contains("Bench.Slow", summary);
        // A markdown-style separator row is present.
        XunitAssert.Contains("|---", summary);
    }

    [Fact]
    public void FormatSummary_MarksUnstableBenchmarksAndAddsNote()
    {
        double[] drifting = [100, 100, 100, 100, 200, 200, 200, 200, 200, 200];
        var summary = BenchmarkSummaryFormatter.FormatSummary(
        [
            Completed("Bench", "Drifty", drifting),
        ]);

        XunitAssert.Contains("Bench.Drifty*", summary);
        XunitAssert.Contains("Notes:", summary);
        XunitAssert.Contains("unstable", summary);
    }

    [Fact]
    public void FormatSummary_ListsFailedBenchmarks()
    {
        var failed = new BenchmarkResult.Failed(
            "Boom", "Boom", "Bench", TimeSpan.Zero, "kaboom\nsecond line"
        );
        var summary = BenchmarkSummaryFormatter.FormatSummary([failed]);

        XunitAssert.Contains("Failed benchmarks (1)", summary);
        XunitAssert.Contains("Bench.Boom: kaboom", summary);
        // Only the first line of the error message is shown.
        XunitAssert.DoesNotContain("second line", summary);
    }
}
