using System;
using System.Linq;

namespace UXUnit.Runtime.Reporters;

/// <summary>
/// Console-based test result reporter.
/// </summary>
public sealed class ConsoleTestReporter : ITestResultReporter
{
    private readonly bool _verbose;
    private int _completedTests;

    /// <summary>
    /// Initializes a new instance of the ConsoleTestReporter.
    /// </summary>
    /// <param name="verbose">Whether to show verbose output.</param>
    public ConsoleTestReporter(bool verbose = false)
    {
        _verbose = verbose;
    }

    public void ReportTestRunStart(TestRunInfo info)
    {
        Console.WriteLine($"UXUnit Test Run Started - ID: {info.RunId}");
        Console.WriteLine($"Discovered {info.TotalTests} tests");
        Console.WriteLine();
    }

    public void ReportTestComplete(TestResult result)
    {
        _completedTests++;

        if (_verbose)
        {
            var symbol = result.Status switch
            {
                TestStatus.Passed => "✓",
                TestStatus.Failed => "✗",
                TestStatus.Skipped => "⊝",
                _ => "?"
            };

            var color = result.Status switch
            {
                TestStatus.Passed => ConsoleColor.Green,
                TestStatus.Failed => ConsoleColor.Red,
                TestStatus.Skipped => ConsoleColor.Yellow,
                _ => ConsoleColor.Gray
            };

            Console.ForegroundColor = color;
            Console.Write(symbol);
            Console.ResetColor();
            Console.WriteLine($" {result.ClassName}.{result.TestName} ({result.Duration.TotalMilliseconds:F0}ms)");

            if (result.Status == TestStatus.Failed && !string.IsNullOrEmpty(result.ErrorMessage))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"   {result.ErrorMessage}");
                Console.ResetColor();
            }
        }
    }

    public void ReportTestRunComplete(TestRunSummary summary)
    {
        Console.WriteLine();
        Console.WriteLine("=== Test Run Summary ===");
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

        Console.WriteLine($"Duration: {summary.TotalDuration.TotalSeconds:F2}s");
        Console.WriteLine($"Pass Rate: {summary.PassRate:P1}");
        Console.WriteLine();

        if (summary.AllPassed)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✅ Test run PASSED");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("❌ Test run FAILED");
            Console.ResetColor();
        }
    }
}
