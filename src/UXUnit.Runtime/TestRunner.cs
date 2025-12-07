using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace UXUnit.Runtime;

/// <summary>
/// Console test runner that executes tests and prints results.
/// </summary>
public static class TestRunner
{
    /// <summary>
    /// Runs tests and prints results to the console.
    /// </summary>
    /// <param name="testClasses">The test classes to execute.</param>
    /// <param name="options">Execution options. If null, uses default options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Exit code: 0 for success, 1 for test failures.</returns>
    public static async Task<int> RunAsync(
        TestClassMetadata[] testClasses,
        TestExecutionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new TestExecutionOptions();

        var startTime = DateTime.UtcNow;
        var results = await TestExecutionEngine.ExecuteTestsAsync(testClasses, options, cancellationToken);
        var duration = DateTime.UtcNow - startTime;

        // Count results
        int passed = 0, failed = 0, skipped = 0;
        foreach (var result in results)
        {
            switch (result.Status)
            {
                case TestStatus.Passed:
                    passed++;
                    break;
                case TestStatus.Failed:
                    failed++;
                    break;
                case TestStatus.Skipped:
                    skipped++;
                    break;
            }

            // Print failures
            if (result.Status == TestStatus.Failed && result.ErrorMessage != null)
            {
                Console.WriteLine($"Failed {result.ClassName}.{result.TestName}");
                Console.WriteLine($"  {result.ErrorMessage}");
                if (!string.IsNullOrEmpty(result.StackTrace))
                {
                    Console.WriteLine(result.StackTrace);
                }
                Console.WriteLine();
            }
        }

        PrintSummary(results.Length, passed, failed, skipped, duration);

        return failed > 0 ? 1 : 0;
    }

    /// <summary>
    /// Prints the test run summary.
    /// </summary>
    public static void PrintSummary(int total, int passed, int failed, int skipped, TimeSpan duration)
    {
        var statusText = failed > 0 ? "Failed!" : "Passed!";
        var assemblyPath = Environment.ProcessPath ?? string.Empty;
        var arch = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString().ToLower();

        // Extract TFM from framework description (e.g., ".NET 8.0.22" -> "net8.0")
        var tfm = "net8.0"; // Default assumption for now

        Console.WriteLine($"Test run summary: {statusText} - {assemblyPath} ({tfm}|{arch})");
        Console.WriteLine($"  total: {total}");
        Console.WriteLine($"  failed: {failed}");
        Console.WriteLine($"  succeeded: {passed}");
        Console.WriteLine($"  skipped: {skipped}");
        Console.WriteLine($"  duration: {duration.TotalMilliseconds:F0}ms");
    }
}
