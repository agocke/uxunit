using System;

namespace UXUnit.Runtime;

/// <summary>
/// Console-based test output writer that writes test output to the console.
/// </summary>
public sealed class ConsoleTestOutput : ITestOutput
{
    /// <summary>
    /// Writes a message to the console.
    /// </summary>
    /// <param name="message">The message to write.</param>
    public void WriteLine(string message)
    {
        Console.WriteLine(message);
    }

    /// <summary>
    /// Writes a formatted message to the console.
    /// </summary>
    /// <param name="format">The format string.</param>
    /// <param name="args">The format arguments.</param>
    public void WriteLine(string format, params object[] args)
    {
        Console.WriteLine(format, args);
    }
}

/// <summary>
/// A test output writer that buffers output in memory.
/// Useful for capturing test output for later analysis.
/// </summary>
public sealed class BufferedTestOutput : ITestOutput
{
    private readonly System.Text.StringBuilder _buffer = new();
    private readonly object _lock = new();

    /// <summary>
    /// Writes a message to the buffer.
    /// </summary>
    /// <param name="message">The message to write.</param>
    public void WriteLine(string message)
    {
        lock (_lock)
        {
            _buffer.AppendLine(message);
        }
    }

    /// <summary>
    /// Writes a formatted message to the buffer.
    /// </summary>
    /// <param name="format">The format string.</param>
    /// <param name="args">The format arguments.</param>
    public void WriteLine(string format, params object[] args)
    {
        WriteLine(string.Format(format, args));
    }

    /// <summary>
    /// Gets all buffered output as a string.
    /// </summary>
    /// <returns>The buffered output.</returns>
    public string GetOutput()
    {
        lock (_lock)
        {
            return _buffer.ToString();
        }
    }

    /// <summary>
    /// Clears the buffer.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _buffer.Clear();
        }
    }
}

/// <summary>
/// A composite test output writer that writes to multiple outputs.
/// </summary>
public sealed class CompositeTestOutput : ITestOutput
{
    private readonly ITestOutput[] _outputs;

    /// <summary>
    /// Initializes a new instance of the CompositeTestOutput.
    /// </summary>
    /// <param name="outputs">The outputs to write to.</param>
    public CompositeTestOutput(params ITestOutput[] outputs)
    {
        _outputs = outputs ?? throw new ArgumentNullException(nameof(outputs));
    }

    /// <summary>
    /// Writes a message to all outputs.
    /// </summary>
    /// <param name="message">The message to write.</param>
    public void WriteLine(string message)
    {
        foreach (var output in _outputs)
        {
            output.WriteLine(message);
        }
    }

    /// <summary>
    /// Writes a formatted message to all outputs.
    /// </summary>
    /// <param name="format">The format string.</param>
    /// <param name="args">The format arguments.</param>
    public void WriteLine(string format, params object[] args)
    {
        foreach (var output in _outputs)
        {
            output.WriteLine(format, args);
        }
    }
}
