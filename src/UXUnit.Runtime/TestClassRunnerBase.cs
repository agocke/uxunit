using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace UXUnit.Runtime;

/// <summary>
/// Base implementation for generated test class runners.
/// Provides common functionality that generated test runners can inherit from.
/// </summary>
public abstract class TestClassRunnerBase : ITestClassRunner
{
    /// <summary>
    /// Gets the metadata for this test class.
    /// </summary>
    public abstract TestClassMetadata Metadata { get; }

    /// <summary>
    /// Runs all tests in this class.
    /// </summary>
    /// <param name="context">The test context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The test results for all methods in the class.</returns>
    public virtual async Task<TestResult[]> RunAllTestsAsync(ITestContext context, CancellationToken cancellationToken = default)
    {
        var results = new TestResult[Metadata.TestMethods.Count];

        for (int i = 0; i < Metadata.TestMethods.Count; i++)
        {
            var method = Metadata.TestMethods[i];
            results[i] = await RunTestAsync(method.MethodName, context, cancellationToken);

            if (cancellationToken.IsCancellationRequested)
                break;
        }

        return results;
    }

    /// <summary>
    /// Runs a specific test method.
    /// </summary>
    /// <param name="methodName">The name of the test method to run.</param>
    /// <param name="context">The test context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The test result for the specified method.</returns>
    public virtual async Task<TestResult> RunTestAsync(string methodName, ITestContext context, CancellationToken cancellationToken = default)
    {
        var methodMetadata = Metadata.TestMethods.FirstOrDefault(m => m.MethodName == methodName);
        if (methodMetadata == null)
        {
            throw new InvalidOperationException($"Test method '{methodName}' not found in class '{Metadata.ClassName}'");
        }

        // Create test-specific context
        var testContext = CreateTestContext(context, methodName);

        // Handle parameterized tests
        if (methodMetadata.TestCases.Any())
        {
            return await RunParameterizedTest(methodMetadata, testContext, cancellationToken);
        }
        else
        {
            return await RunSingleTest(methodMetadata, testContext, cancellationToken);
        }
    }

    /// <summary>
    /// Runs a single test method without parameters.
    /// </summary>
    protected virtual async Task<TestResult> RunSingleTest(TestMethodMetadata metadata, ITestContext context, CancellationToken cancellationToken)
    {
        var testInstance = CreateTestInstance();
        var testMethodDelegate = GetTestMethodDelegate(metadata.MethodName);

        try
        {
            await ExecuteClassSetup(testInstance, context);
            var result = await TestExecutor.ExecuteTestAsync(testInstance, testMethodDelegate, metadata, context, null, cancellationToken);
            await ExecuteClassCleanup(testInstance, context);
            return result;
        }
        finally
        {
            DisposeTestInstance(testInstance);
        }
    }

    /// <summary>
    /// Runs a parameterized test with multiple test cases.
    /// </summary>
    protected virtual async Task<TestResult> RunParameterizedTest(TestMethodMetadata metadata, ITestContext context, CancellationToken cancellationToken)
    {
        var parameterizedTestMethodDelegate = GetParameterizedTestMethodDelegate(metadata.MethodName);
        var allResults = new TestResult[metadata.TestCases.Count];

        for (int i = 0; i < metadata.TestCases.Count; i++)
        {
            var testCase = metadata.TestCases[i];
            var testInstance = CreateTestInstance();

            try
            {
                await ExecuteClassSetup(testInstance, context);

                // Skip test case if marked for skipping
                if (testCase.Skip)
                {
                    var testId = GenerateTestId(context.ClassName, metadata.MethodName, testCase.Arguments);
                    allResults[i] = TestResult.Skipped(testId, metadata.MethodName, testCase.SkipReason ?? "Test case marked as skipped");
                }
                else
                {
                    allResults[i] = await TestExecutor.ExecuteParameterizedTestAsync(testInstance, parameterizedTestMethodDelegate, metadata, context, testCase.Arguments, cancellationToken);
                }

                await ExecuteClassCleanup(testInstance, context);
            }
            finally
            {
                DisposeTestInstance(testInstance);
            }

            if (cancellationToken.IsCancellationRequested)
                break;
        }

        // For parameterized tests, we return the first failure or the last result if all passed
        var firstFailure = allResults.FirstOrDefault(r => r.Status == TestStatus.Failed);
        return firstFailure ?? allResults.LastOrDefault() ?? TestResult.Skipped(
            GenerateTestId(context.ClassName, metadata.MethodName, null),
            metadata.MethodName,
            "No test cases executed");
    }

    /// <summary>
    /// Creates an instance of the test class. Override this method in generated classes.
    /// </summary>
    protected abstract object CreateTestInstance();

    /// <summary>
    /// Gets the test method delegate for the specified test method. Override this method in generated classes.
    /// </summary>
    protected abstract Func<object, Task> GetTestMethodDelegate(string methodName);

    /// <summary>
    /// Gets the parameterized test method delegate for the specified test method. Override this method in generated classes.
    /// </summary>
    protected abstract Func<object, object?[], Task> GetParameterizedTestMethodDelegate(string methodName);

    /// <summary>
    /// Executes class-level setup methods. Override in generated classes to call specific setup methods.
    /// </summary>
    protected virtual async Task ExecuteClassSetup(object testInstance, ITestContext context)
    {
        // No default setup - override in generated classes
        await Task.CompletedTask;
    }

    /// <summary>
    /// Executes class-level cleanup methods. Override in generated classes to call specific cleanup methods.
    /// </summary>
    protected virtual async Task ExecuteClassCleanup(object testInstance, ITestContext context)
    {
        // No default cleanup - override in generated classes
        await Task.CompletedTask;
    }

    /// <summary>
    /// Creates a test-specific context from the class context.
    /// </summary>
    protected virtual ITestContext CreateTestContext(ITestContext classContext, string testName)
    {
        if (classContext is TestContext testContext)
        {
            return testContext.ForTest(testName);
        }

        // Fallback: create a new context
        return new TestContext(testName, classContext.ClassName, classContext.AssemblyName,
            classContext.GetTestOutput(), classContext.CancellationToken);
    }

    /// <summary>
    /// Disposes the test instance if it implements IDisposable.
    /// </summary>
    protected virtual void DisposeTestInstance(object testInstance)
    {
        if (testInstance is IDisposable disposable)
        {
            try
            {
                disposable.Dispose();
            }
            catch (Exception ex)
            {
                // Log disposal errors but don't throw
                Debug.WriteLine($"Warning: Failed to dispose test instance: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Generates a unique test ID for a test case.
    /// </summary>
    protected static string GenerateTestId(string className, string methodName, object?[]? arguments)
    {
        var baseId = $"{className}.{methodName}";

        if (arguments != null && arguments.Length > 0)
        {
            var argString = string.Join(",", arguments.Select(a => a?.ToString() ?? "null"));
            baseId += $"({argString})";
        }

        return baseId;
    }
}

/// <summary>
/// Extension methods for ITestContext to provide additional functionality.
/// </summary>
public static class TestContextExtensions
{
    /// <summary>
    /// Gets the test output from the context.
    /// </summary>
    public static ITestOutput GetTestOutput(this ITestContext context)
    {
        return context is TestContext testContext ? testContext.Output : NullTestOutput.Instance;
    }
}