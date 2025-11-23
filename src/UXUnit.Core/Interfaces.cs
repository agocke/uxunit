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
    /// <param name="methodMetadata">The test method metadata.</param>
    /// <returns>An enumerable of test data arrays.</returns>
    IEnumerable<object?[]> GetTestData(TestMethodMetadata methodMetadata);
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

