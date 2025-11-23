using System;

namespace UXUnit.Runtime;

/// <summary>
/// Configuration options for test execution.
/// </summary>
public sealed class TestExecutionOptions
{
    public bool ParallelExecution { get; init; } = true;

    public int MaxDegreeOfParallelism { get; init; } = Environment.ProcessorCount;

    public bool StopOnFirstFailure { get; init; } = false;

    public TimeSpan? GlobalTimeout { get; init; }

    /// <summary>
    /// Creates default execution options.
    /// </summary>
    public static TestExecutionOptions Default => new();
}
