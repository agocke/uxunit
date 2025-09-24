using System;
using System.Collections.Generic;
using System.Threading;

namespace UXUnit;

/// <summary>
/// Provides context and utilities during test execution.
/// </summary>
public interface ITestContext
{
    /// <summary>
    /// Gets the name of the current test.
    /// </summary>
    string TestName { get; }

    /// <summary>
    /// Gets the name of the test class.
    /// </summary>
    string ClassName { get; }

    /// <summary>
    /// Gets the name of the test assembly.
    /// </summary>
    string AssemblyName { get; }

    /// <summary>
    /// Gets the cancellation token for the test execution.
    /// </summary>
    CancellationToken CancellationToken { get; }

    /// <summary>
    /// Writes a message to the test output.
    /// </summary>
    /// <param name="message">The message to write.</param>
    void WriteLine(string message);

    /// <summary>
    /// Writes a formatted message to the test output.
    /// </summary>
    /// <param name="format">The format string.</param>
    /// <param name="args">The format arguments.</param>
    void WriteLine(string format, params object[] args);

    /// <summary>
    /// Adds a property to the test context.
    /// </summary>
    /// <param name="name">The property name.</param>
    /// <param name="value">The property value.</param>
    void AddProperty(string name, object? value);

    /// <summary>
    /// Gets a property from the test context.
    /// </summary>
    /// <typeparam name="T">The property type.</typeparam>
    /// <param name="name">The property name.</param>
    /// <returns>The property value, or default if not found.</returns>
    T? GetProperty<T>(string name);

    /// <summary>
    /// Gets all properties from the test context.
    /// </summary>
    IReadOnlyDictionary<string, object?> Properties { get; }
}

/// <summary>
/// Interface for custom test method attributes that need lifecycle hooks.
/// </summary>
public interface ITestMethodAttribute
{
    /// <summary>
    /// Called before the test method is executed.
    /// </summary>
    /// <param name="context">The test context.</param>
    void OnBeforeTest(ITestContext context);

    /// <summary>
    /// Called after the test method is executed.
    /// </summary>
    /// <param name="context">The test context.</param>
    /// <param name="result">The test result.</param>
    void OnAfterTest(ITestContext context, TestResult result);
}

/// <summary>
/// Interface for custom test class attributes that need lifecycle hooks.
/// </summary>
public interface ITestClassAttribute
{
    /// <summary>
    /// Called before any tests in the class are executed.
    /// </summary>
    /// <param name="context">The test context.</param>
    void OnBeforeClass(ITestContext context);

    /// <summary>
    /// Called after all tests in the class are executed.
    /// </summary>
    /// <param name="context">The test context.</param>
    /// <param name="results">All test results from the class.</param>
    void OnAfterClass(ITestContext context, TestResult[] results);
}

/// <summary>
/// Interface for test data sources that provide data at runtime.
/// </summary>
public interface ITestDataSource
{
    /// <summary>
    /// Gets the test data for the specified method.
    /// </summary>
    /// <param name="methodInfo">The test method information.</param>
    /// <returns>An enumerable of test data arrays.</returns>
    IEnumerable<object?[]> GetTestData(System.Reflection.MethodInfo methodInfo);
}

/// <summary>
/// Interface for test output writers.
/// </summary>
public interface ITestOutput
{
    /// <summary>
    /// Writes a message to the output.
    /// </summary>
    /// <param name="message">The message to write.</param>
    void WriteLine(string message);

    /// <summary>
    /// Writes a formatted message to the output.
    /// </summary>
    /// <param name="format">The format string.</param>
    /// <param name="args">The format arguments.</param>
    void WriteLine(string format, params object[] args);
}

/// <summary>
/// Interface for test result processors that handle test results.
/// </summary>
public interface ITestResultProcessor
{
    /// <summary>
    /// Processes a test result.
    /// </summary>
    /// <param name="result">The test result to process.</param>
    void ProcessResult(TestResult result);
}

/// <summary>
/// Interface for test runners that execute tests.
/// </summary>
public interface ITestRunner
{
    /// <summary>
    /// Runs all tests in the provided test class runners.
    /// </summary>
    /// <param name="testClassRunners">The test class runners to execute.</param>
    /// <param name="configuration">The test run configuration.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The aggregate test run result.</returns>
    System.Threading.Tasks.Task<TestRunResult> RunTestsAsync(
        IEnumerable<ITestClassRunner> testClassRunners,
        TestRunConfiguration configuration,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for generated test class runners.
/// </summary>
public interface ITestClassRunner
{
    /// <summary>
    /// Gets the metadata for this test class.
    /// </summary>
    TestClassMetadata Metadata { get; }

    /// <summary>
    /// Runs all tests in this class.
    /// </summary>
    /// <param name="context">The test context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The test results for all methods in the class.</returns>
    System.Threading.Tasks.Task<TestResult[]> RunAllTestsAsync(
        ITestContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs a specific test method.
    /// </summary>
    /// <param name="methodName">The name of the test method to run.</param>
    /// <param name="context">The test context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The test result for the specified method.</returns>
    System.Threading.Tasks.Task<TestResult> RunTestAsync(
        string methodName,
        ITestContext context,
        CancellationToken cancellationToken = default);
}