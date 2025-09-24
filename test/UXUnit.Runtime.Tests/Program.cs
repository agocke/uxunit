using System;
using System.Linq;
using System.Threading.Tasks;
using UXUnit.Runtime;

namespace UXUnit.Runtime.Tests;

/// <summary>
/// Entry point for running UXUnit.Runtime tests.
/// This demonstrates how a test project should set up its own executable entry point.
/// </summary>
public static class Program
{
    /// <summary>
    /// Main entry point for the test runner.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>Exit code: 0 for success, non-zero for failure.</returns>
    public static async Task<int> Main(string[] args)
    {
        try
        {
            Console.WriteLine("UXUnit Runtime Tests");
            Console.WriteLine("===================");
            Console.WriteLine(string.Empty);

            // Create test runner with console output
            var output = new ConsoleTestOutput();
            var runner = new TestRunner(output);

            // Discover test runners in this assembly
            var testRunners = TestDiscovery.DiscoverTestRunners().ToList();
            
            if (!testRunners.Any())
            {
                output.WriteLine("No test classes found. This is expected until the source generator is fully implemented.");
                return 0; // Not a failure - just no tests generated yet
            }

            // Show discovery summary
            var summary = TestDiscovery.GetDiscoverySummary(testRunners);
            output.WriteLine($"Discovered {summary.TotalClasses} test classes with {summary.TotalMethods} test methods");
            output.WriteLine(string.Empty);

            // Create test configuration
            var configuration = new TestRunConfiguration
            {
                Output = output,
                ParallelExecution = true,
                MaxDegreeOfParallelism = Environment.ProcessorCount
            };

            // Run the tests
            var result = await runner.RunTestsAsync(testRunners, configuration);

            // Return appropriate exit code
            return result.HasFailures ? 1 : 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fatal error: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            return -1;
        }
    }
}