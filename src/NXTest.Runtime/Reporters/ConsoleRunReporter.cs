using System;
using static NXTest.RunResult;

namespace NXTest.Runtime.Reporters;

/// <summary>
/// Console-based run result reporter.
/// </summary>
public sealed class ConsoleRunReporter(bool _verbose) : IRunResultReporter
{
    public void ReportRunStart(RunInfo info)
    {
        Console.WriteLine($"NXTest Test Run Started - ID: {info.RunId}");
        Console.WriteLine(
            $"Discovered {info.TotalTests} tests and {info.TotalBenchmarks} benchmarks"
        );
        Console.WriteLine();
    }

    public void ReportResult(RunResult result)
    {
        if (!_verbose)
            return;

        switch (result)
        {
            case TestResult testResult:
                ReportTestResult(testResult);
                break;
            case BenchmarkResult benchmarkResult:
                ReportBenchmarkResult(benchmarkResult);
                break;
        }
    }

    private static void ReportTestResult(TestResult result)
    {
        var symbol = result switch
        {
            TestResult.Passed => "✓",
            TestResult.Failed or TestResult.Faulted => "✗",
            TestResult.Skipped => "⊝",
            _ => throw new InvalidOperationException(
                $"Unknown test result type: {result.GetType()}"
            ),
        };

        var color = result switch
        {
            TestResult.Passed => ConsoleColor.Green,
            TestResult.Failed or TestResult.Faulted => ConsoleColor.Red,
            TestResult.Skipped => ConsoleColor.Yellow,
            _ => throw new InvalidOperationException(
                $"Unknown test result type: {result.GetType()}"
            ),
        };

        Console.ForegroundColor = color;
        Console.Write(symbol);
        Console.ResetColor();
        var duration = result switch
        {
            TestResult.Passed passed => passed.Duration,
            TestResult.Failed failed => failed.Duration,
            _ => TimeSpan.Zero,
        };
        Console.WriteLine($" {result.ClassName}.{result.Name} ({duration.TotalMilliseconds:F0}ms)");

        var errorMessage = result switch
        {
            TestResult.Failed failed => failed.ErrorMessage,
            TestResult.Faulted faulted => faulted.ErrorMessage,
            _ => null,
        };
        if (errorMessage is not null)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"   {errorMessage}");
            Console.ResetColor();
        }
    }

    private static void ReportBenchmarkResult(BenchmarkResult result)
    {
        var symbol = result switch
        {
            BenchmarkResult.Completed => "✓",
            BenchmarkResult.Failed => "✗",
            BenchmarkResult.Skipped => "⊝",
        };
        var color = result switch
        {
            BenchmarkResult.Completed => ConsoleColor.Green,
            BenchmarkResult.Failed => ConsoleColor.Red,
            BenchmarkResult.Skipped => ConsoleColor.Yellow,
        };

        Console.ForegroundColor = color;
        Console.Write(symbol);
        Console.ResetColor();

        if (result is BenchmarkResult.Completed completed)
        {
            Console.WriteLine(
                $" {result.ClassName}.{result.Name} "
                + $"(mean {BenchmarkResultFormatter.FormatNanoseconds(completed.Statistics.MeanNanoseconds)}, "
                + $"{completed.Statistics.OperationsPerIteration} operations/iteration)"
            );
        }
        else
        {
            Console.WriteLine($" {result.ClassName}.{result.Name}");
        }

        if (
            result is BenchmarkResult.Failed failed
            && !string.IsNullOrEmpty(failed.ErrorMessage)
        )
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"   {failed.ErrorMessage}");
            Console.ResetColor();
        }
    }

    public void ReportRunComplete(RunSummary summary)
    {
        Console.WriteLine();
        Console.WriteLine("=== Run Summary ===");
        Console.WriteLine($"Total Tests: {summary.TotalTests}");

        if (summary.PassedTests > 0)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Passed: {summary.PassedTests}");
            Console.ResetColor();
        }

        if (summary.FailedTests > 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Failed: {summary.FailedTests}");
            Console.ResetColor();
        }

        if (summary.SkippedTests > 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Skipped: {summary.SkippedTests}");
            Console.ResetColor();
        }

        if (
            summary.CompletedBenchmarks
            + summary.FailedBenchmarks
            + summary.SkippedBenchmarks > 0
        )
        {
            Console.WriteLine($"Completed Benchmarks: {summary.CompletedBenchmarks}");
            Console.WriteLine($"Failed Benchmarks: {summary.FailedBenchmarks}");
            Console.WriteLine($"Skipped Benchmarks: {summary.SkippedBenchmarks}");
        }

        Console.WriteLine($"Duration: {summary.TotalDuration.TotalSeconds:F2}s");
        Console.WriteLine($"Pass Rate: {summary.PassRate:P1}");
        Console.WriteLine();

        if (summary.AllSucceeded)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✅ Run SUCCEEDED");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("❌ Run FAILED");
            Console.ResetColor();
        }
    }
}
