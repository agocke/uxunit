using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace UXUnit.Runtime;

/// <summary>
/// Provides context and utilities during test execution.
/// </summary>
public sealed class TestContext : ITestContext
{
    private readonly ConcurrentDictionary<string, object?> _properties = new();

    /// <summary>
    /// Initializes a new instance of the TestContext.
    /// </summary>
    /// <param name="testName">The name of the current test.</param>
    /// <param name="className">The name of the test class.</param>
    /// <param name="assemblyName">The name of the test assembly.</param>
    /// <param name="output">The test output writer.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public TestContext(
        string testName,
        string className,
        string assemblyName,
        ITestOutput output,
        CancellationToken cancellationToken)
    {
        TestName = testName;
        ClassName = className;
        AssemblyName = assemblyName;
        Output = output;
        CancellationToken = cancellationToken;
    }

    /// <summary>
    /// Initializes a new instance of the TestContext for a class context.
    /// </summary>
    /// <param name="className">The name of the test class.</param>
    /// <param name="assemblyName">The name of the test assembly.</param>
    /// <param name="output">The test output writer.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public TestContext(
        string className,
        string assemblyName,
        ITestOutput output,
        CancellationToken cancellationToken)
        : this(string.Empty, className, assemblyName, output, cancellationToken)
    {
    }

    /// <summary>
    /// Gets the name of the current test.
    /// </summary>
    public string TestName { get; }

    /// <summary>
    /// Gets the name of the test class.
    /// </summary>
    public string ClassName { get; }

    /// <summary>
    /// Gets the name of the test assembly.
    /// </summary>
    public string AssemblyName { get; }

    /// <summary>
    /// Gets the cancellation token for the test execution.
    /// </summary>
    public CancellationToken CancellationToken { get; }

    /// <summary>
    /// Gets the test output writer.
    /// </summary>
    public ITestOutput Output { get; }

    /// <summary>
    /// Gets all properties from the test context.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Properties => _properties;

    /// <summary>
    /// Writes a message to the test output.
    /// </summary>
    /// <param name="message">The message to write.</param>
    public void WriteLine(string message)
    {
        Output.WriteLine(message);
    }

    /// <summary>
    /// Writes a formatted message to the test output.
    /// </summary>
    /// <param name="format">The format string.</param>
    /// <param name="args">The format arguments.</param>
    public void WriteLine(string format, params object[] args)
    {
        Output.WriteLine(format, args);
    }

    /// <summary>
    /// Adds a property to the test context.
    /// </summary>
    /// <param name="name">The property name.</param>
    /// <param name="value">The property value.</param>
    public void AddProperty(string name, object? value)
    {
        _properties[name] = value;
    }

    /// <summary>
    /// Gets a property from the test context.
    /// </summary>
    /// <typeparam name="T">The property type.</typeparam>
    /// <param name="name">The property name.</param>
    /// <returns>The property value, or default if not found.</returns>
    public T? GetProperty<T>(string name)
    {
        return _properties.TryGetValue(name, out var value) ? (T?)value : default;
    }

    /// <summary>
    /// Creates a new test context for a specific test method.
    /// </summary>
    /// <param name="testName">The name of the test method.</param>
    /// <returns>A new test context with the specified test name.</returns>
    public TestContext ForTest(string testName)
    {
        var context = new TestContext(testName, ClassName, AssemblyName, Output, CancellationToken);

        // Copy existing properties
        foreach (var property in _properties)
        {
            context._properties[property.Key] = property.Value;
        }

        return context;
    }
}