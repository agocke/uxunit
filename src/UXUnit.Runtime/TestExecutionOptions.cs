using System;

namespace UXUnit.Runtime;

/// <summary>
/// Configuration options for test execution.
/// </summary>
public sealed class TestExecutionOptions
{
    /// <summary>
    /// Gets or sets whether to execute tests in parallel.
    /// </summary>
    public bool ParallelExecution { get; init; } = true;

    /// <summary>
    /// Gets or sets the maximum degree of parallelism.
    /// </summary>
    public int MaxDegreeOfParallelism { get; init; } = Environment.ProcessorCount;

    /// <summary>
    /// Gets or sets whether to stop execution on the first test failure.
    /// </summary>
    public bool StopOnFirstFailure { get; init; } = false;

    /// <summary>
    /// Gets or sets the global timeout for all tests.
    /// </summary>
    public TimeSpan? GlobalTimeout { get; init; }

    /// <summary>
    /// Creates default execution options.
    /// </summary>
    public static TestExecutionOptions Default => new();
}
