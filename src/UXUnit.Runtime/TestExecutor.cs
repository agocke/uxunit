using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace UXUnit.Runtime;

/// <summary>
/// Handles the execution of individual test methods with proper lifecycle management,
/// timeout handling, exception handling, and result collection.
/// </summary>
public static class TestExecutor
{
    /// <summary>
    /// Executes a test method with full lifecycle management.
    /// </summary>
    /// <param name="testInstance">The test class instance.</param>
    /// <param name="methodInfo">The test method to execute.</param>
    /// <param name="metadata">The test method metadata.</param>
    /// <param name="context">The test context.</param>
    /// <param name="arguments">Arguments for parameterized tests.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The test result.</returns>
    public static async Task<TestResult> ExecuteTestAsync(
        object testInstance,
        MethodInfo methodInfo,
        TestMethodMetadata metadata,
        ITestContext context,
        object?[]? arguments = null,
        CancellationToken cancellationToken = default)
    {
        var testId = GenerateTestId(context.ClassName, metadata.MethodName, arguments);
        var startTime = DateTime.UtcNow;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Check if test should be skipped
            if (metadata.Skip)
            {
                return TestResult.Skipped(testId, metadata.MethodName, metadata.SkipReason ?? "Test marked as skipped");
            }

            // Execute pre-test hooks
            await ExecutePreTestHooks(testInstance, metadata, context);

            // Execute the test method with timeout handling
            var result = await ExecuteTestWithTimeout(testInstance, methodInfo, metadata, context, arguments, cancellationToken);

            // Execute post-test hooks
            await ExecutePostTestHooks(testInstance, metadata, context, result);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var endTime = DateTime.UtcNow;

            // Create failure result
            var failureResult = TestResult.Failure(testId, metadata.MethodName, ex, stopwatch.Elapsed, startTime, endTime);

            // Try to execute post-test hooks even on failure
            try
            {
                await ExecutePostTestHooks(testInstance, metadata, context, failureResult);
            }
            catch (Exception hookEx)
            {
                // Log hook exception but don't overwrite the original failure
                context.WriteLine($"Warning: Post-test hook failed: {hookEx.Message}");
            }

            return failureResult;
        }
    }

    /// <summary>
    /// Executes a test method with timeout handling.
    /// </summary>
    private static async Task<TestResult> ExecuteTestWithTimeout(
        object testInstance,
        MethodInfo methodInfo,
        TestMethodMetadata metadata,
        ITestContext context,
        object?[]? arguments,
        CancellationToken cancellationToken)
    {
        var testId = GenerateTestId(context.ClassName, metadata.MethodName, arguments);
        var startTime = DateTime.UtcNow;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Combine cancellation tokens (global + timeout)
            using var timeoutCts = CreateTimeoutCancellationToken(metadata.TimeoutMs);
            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, 
                timeoutCts?.Token ?? CancellationToken.None);

            var combinedToken = combinedCts.Token;

            // Execute the test method
            var result = methodInfo.Invoke(testInstance, arguments);

            // Handle async methods
            if (result is Task task)
            {
                await task.WaitAsync(combinedToken);
            }
            else if (result is ValueTask valueTask)
            {
                await valueTask.ConfigureAwait(false);
            }

            stopwatch.Stop();
            var endTime = DateTime.UtcNow;

            // Create success result
            return new TestResult
            {
                TestId = testId,
                TestName = metadata.MethodName,
                TestDisplayName = metadata.DisplayName,
                ClassName = context.ClassName,
                AssemblyName = context.AssemblyName,
                Status = TestStatus.Passed,
                Duration = stopwatch.Elapsed,
                StartTime = startTime,
                EndTime = endTime,
                TestCaseArguments = arguments,
                OutputLines = ExtractOutputLines(context)
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Test was cancelled by external cancellation
            throw;
        }
        catch (OperationCanceledException)
        {
            // Test timed out
            stopwatch.Stop();
            var timeoutException = new TimeoutException($"Test '{metadata.MethodName}' timed out after {metadata.TimeoutMs}ms");
            return TestResult.Failure(testId, metadata.MethodName, timeoutException, stopwatch.Elapsed, startTime, DateTime.UtcNow);
        }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        {
            // Unwrap reflection exception
            stopwatch.Stop();
            return TestResult.Failure(testId, metadata.MethodName, ex.InnerException, stopwatch.Elapsed, startTime, DateTime.UtcNow);
        }
    }

    /// <summary>
    /// Executes pre-test lifecycle hooks.
    /// </summary>
    private static async Task ExecutePreTestHooks(object testInstance, TestMethodMetadata metadata, ITestContext context)
    {
        // Execute setup methods if they exist
        var setupMethods = testInstance.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(m => m.GetCustomAttributes().Any(a => a.GetType().Name == "SetupAttribute" || a.GetType().Name == "TestInitializeAttribute"))
            .ToArray();

        foreach (var setupMethod in setupMethods)
        {
            try
            {
                var result = setupMethod.Invoke(testInstance, null);
                if (result is Task setupTask)
                {
                    await setupTask;
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Setup method '{setupMethod.Name}' failed: {ex.Message}", ex);
            }
        }

        // Execute custom attribute hooks
        await ExecuteAttributeHooks(testInstance, metadata, context, isPreTest: true);
    }

    /// <summary>
    /// Executes post-test lifecycle hooks.
    /// </summary>
    private static async Task ExecutePostTestHooks(object testInstance, TestMethodMetadata metadata, ITestContext context, TestResult result)
    {
        // Execute custom attribute hooks
        await ExecuteAttributeHooks(testInstance, metadata, context, isPreTest: false, result);

        // Execute cleanup methods if they exist
        var cleanupMethods = testInstance.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(m => m.GetCustomAttributes().Any(a => a.GetType().Name == "CleanupAttribute" || a.GetType().Name == "TestCleanupAttribute"))
            .ToArray();

        foreach (var cleanupMethod in cleanupMethods)
        {
            try
            {
                var methodResult = cleanupMethod.Invoke(testInstance, null);
                if (methodResult is Task cleanupTask)
                {
                    await cleanupTask;
                }
            }
            catch (Exception ex)
            {
                context.WriteLine($"Warning: Cleanup method '{cleanupMethod.Name}' failed: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Executes custom attribute lifecycle hooks.
    /// </summary>
    private static async Task ExecuteAttributeHooks(
        object testInstance, 
        TestMethodMetadata metadata, 
        ITestContext context, 
        bool isPreTest, 
        TestResult? result = null)
    {
        // Get the actual method to inspect its attributes
        var method = testInstance.GetType().GetMethod(metadata.MethodName);
        if (method == null) return;

        var attributes = method.GetCustomAttributes().OfType<ITestMethodAttribute>().ToArray();

        foreach (var attribute in attributes)
        {
            try
            {
                if (isPreTest)
                {
                    attribute.OnBeforeTest(context);
                }
                else if (result != null)
                {
                    attribute.OnAfterTest(context, result);
                }
            }
            catch (Exception ex)
            {
                var hookType = isPreTest ? "pre-test" : "post-test";
                var message = $"{hookType} hook for attribute '{attribute.GetType().Name}' failed: {ex.Message}";
                
                if (isPreTest)
                {
                    throw new InvalidOperationException(message, ex);
                }
                else
                {
                    context.WriteLine($"Warning: {message}");
                }
            }
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Creates a cancellation token source for timeout handling.
    /// </summary>
    private static CancellationTokenSource? CreateTimeoutCancellationToken(int timeoutMs)
    {
        return timeoutMs > 0 ? new CancellationTokenSource(TimeSpan.FromMilliseconds(timeoutMs)) : null;
    }

    /// <summary>
    /// Generates a unique test ID.
    /// </summary>
    private static string GenerateTestId(string className, string methodName, object?[]? arguments)
    {
        var baseId = $"{className}.{methodName}";
        
        if (arguments != null && arguments.Length > 0)
        {
            var argString = string.Join(",", arguments.Select(a => a?.ToString() ?? "null"));
            baseId += $"({argString})";
        }

        return baseId;
    }

    /// <summary>
    /// Extracts output lines from the test context if it supports output capture.
    /// </summary>
    private static string[] ExtractOutputLines(ITestContext context)
    {
        // If the context uses a BufferedTestOutput, we could extract the lines
        // For now, return empty array as this would require more sophisticated output tracking
        return Array.Empty<string>();
    }
}