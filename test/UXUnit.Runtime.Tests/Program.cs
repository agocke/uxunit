using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UXUnit.Runtime;
using Xunit;

namespace UXUnit.Runtime.Tests;

/// <summary>
/// Entry point for running UXUnit.Runtime tests.
/// This demonstrates how a test project should set up its own executable entry point.
///
/// NOTE: This uses the OLD architecture and needs to be rewritten.
/// Temporarily disabled to allow the codebase to build.
/// </summary>
#if FALSE
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

            // First, run our validation tests (XUnit tests that validate the execution engine)
            Console.WriteLine(
                "🧪 Running Validation Tests (XUnit tests validating UXUnit engine)..."
            );
            Console.WriteLine(string.Empty);

            var validationResult = await RunValidationTests();

            Console.WriteLine(string.Empty);
            Console.WriteLine("=".PadLeft(60, '='));
            Console.WriteLine(string.Empty);

            // Then, run our demo tests (UXUnit tests demonstrating the execution)
            Console.WriteLine("🚀 Running Demo Tests (UXUnit execution engine demonstration)...");
            Console.WriteLine(string.Empty);

            var demoResult = await RunDemoTests();

            // Final summary
            Console.WriteLine(string.Empty);
            Console.WriteLine("📊 OVERALL SUMMARY");
            Console.WriteLine("==================");
            Console.WriteLine(
                $"Validation Tests: {(validationResult == 0 ? "✅ PASSED" : "❌ FAILED")}"
            );
            Console.WriteLine(
                $"Demo Tests: {(demoResult == 0 ? "✅ PASSED" : "❌ FAILED (expected due to intentional failing test)")}"
            );
            Console.WriteLine(string.Empty);

            if (validationResult != 0)
            {
                Console.WriteLine(
                    "❌ CRITICAL: Validation tests failed - the execution engine has issues!"
                );
                return validationResult;
            }
            else
            {
                Console.WriteLine(
                    "✅ SUCCESS: All validation tests passed - the execution engine is working correctly!"
                );
                return 0; // Return success even if demo has intentional failures
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fatal error: {ex.Message}");
            Console.WriteLine(ex.StackTrace ?? string.Empty);
            return -1;
        }
    }

    private static async Task<int> RunValidationTests()
    {
        // Create an instance of our validation test class and run the tests directly
        var testClass = new ExecutionEngineTests();
        var asyncTestMethods = new Dictionary<string, Func<Task>>
        {
            {
                nameof(ExecutionEngineTests.TestRunner_WithPassingTest_ShouldReturnPassedResult),
                testClass.TestRunner_WithPassingTest_ShouldReturnPassedResult
            },
            {
                nameof(ExecutionEngineTests.TestRunner_WithFailingTest_ShouldReturnFailedResult),
                testClass.TestRunner_WithFailingTest_ShouldReturnFailedResult
            },
            {
                nameof(ExecutionEngineTests.TestRunner_WithSkippedTest_ShouldReturnSkippedResult),
                testClass.TestRunner_WithSkippedTest_ShouldReturnSkippedResult
            },
            {
                nameof(ExecutionEngineTests.TestRunner_WithAsyncTest_ShouldExecuteCorrectly),
                testClass.TestRunner_WithAsyncTest_ShouldExecuteCorrectly
            },
            {
                nameof(ExecutionEngineTests.TestRunner_WithParameterizedTest_ShouldExecuteAllCases),
                testClass.TestRunner_WithParameterizedTest_ShouldExecuteAllCases
            },
            {
                nameof(ExecutionEngineTests.TestRunner_WithMixedResults_ShouldReturnCorrectSummary),
                testClass.TestRunner_WithMixedResults_ShouldReturnCorrectSummary
            },
            {
                nameof(
                    ExecutionEngineTests.TestRunner_WithStopOnFirstFailure_ShouldStopAfterFirstFailure
                ),
                testClass.TestRunner_WithStopOnFirstFailure_ShouldStopAfterFirstFailure
            },
        };

        var syncTestMethods = new Dictionary<string, Action>
        {
            {
                nameof(ExecutionEngineTests.TestDiscovery_WithManualRunners_ShouldFindRunners),
                testClass.TestDiscovery_WithManualRunners_ShouldFindRunners
            },
        };

        var totalTestCount = asyncTestMethods.Count + syncTestMethods.Count;
        Console.WriteLine($"Running {totalTestCount} validation tests...");
        Console.WriteLine(string.Empty);

        int passed = 0;
        int failed = 0;
        var failures = new List<string>();

        // Run async tests
        foreach (var (methodName, testMethod) in asyncTestMethods)
        {
            try
            {
                Console.Write($"  • {methodName}... ");

                await testMethod();

                Console.WriteLine("✅ PASSED");
                passed++;
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ FAILED");
                Console.WriteLine($"    Error: {ex.Message}");
                failures.Add($"{methodName}: {ex.Message}");
                failed++;
            }
        }

        // Run sync tests
        foreach (var (methodName, testMethod) in syncTestMethods)
        {
            try
            {
                Console.Write($"  • {methodName}... ");

                testMethod();

                Console.WriteLine("✅ PASSED");
                passed++;
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ FAILED");
                Console.WriteLine($"    Error: {ex.Message}");
                failures.Add($"{methodName}: {ex.Message}");
                failed++;
            }
        }

        Console.WriteLine(string.Empty);
        Console.WriteLine($"Validation Results: {passed} passed, {failed} failed");

        if (failures.Any())
        {
            Console.WriteLine(string.Empty);
            Console.WriteLine("Validation Failures:");
            foreach (var failure in failures)
            {
                Console.WriteLine($"  ❌ {failure}");
            }
        }

        return failed > 0 ? 1 : 0;
    }

    private static async Task<int> RunDemoTests()
    {
        try
        {
            // Create test runner with console output
            var output = new ConsoleTestOutput();
            var runner = new TestRunner(output);

            // Manually create test runners to simulate what source generator would produce
            var testRunners = new List<ITestClassRunner>
            {
                new ManualTestClassRunner(),
                new SecondManualTestClassRunner(),
            };

            Console.WriteLine(
                "Using manually created test runners to validate execution engine..."
            );
            Console.WriteLine(string.Empty);

            // Show discovery summary
            var summary = TestDiscovery.GetDiscoverySummary(testRunners);
            output.WriteLine(
                $"Discovered {summary.TotalClasses} test classes with {summary.TotalMethods} test methods"
            );
            output.WriteLine(string.Empty);

            // Create test configuration
            var configuration = new TestRunConfiguration
            {
                Output = output,
                ParallelExecution = true,
                MaxDegreeOfParallelism = Environment.ProcessorCount,
            };

            // Run the tests
            var result = await runner.RunTestsAsync(testRunners, configuration);

            // Return appropriate exit code
            return result.HasFailures ? 1 : 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fatal error in demo tests: {ex.Message}");
            Console.WriteLine(ex.StackTrace ?? string.Empty);
            return -1;
        }
    }
}
#endif
