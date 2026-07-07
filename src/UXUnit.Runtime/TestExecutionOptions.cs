using System;

namespace UXUnit.Runtime;

/// <summary>
/// Controls how tests are parallelized during execution.
/// </summary>
public enum ParallelMode
{
    /// <summary>
    /// Everything runs sequentially.
    /// </summary>
    None,

    /// <summary>
    /// Test classes run in parallel with one another, but the tests within a
    /// single class run sequentially.
    /// </summary>
    Classes,

    /// <summary>
    /// Individual tests run in parallel, regardless of which class they belong
    /// to. This is the default.
    /// </summary>
    Tests,
}

/// <summary>
/// Configuration options for test execution.
/// </summary>
public sealed class TestExecutionOptions
{
    /// <summary>
    /// Controls parallelization. Defaults to running every test in parallel.
    /// Regardless of mode, tests are randomly permuted on each run to surface
    /// accidental ordering dependencies.
    /// </summary>
    public ParallelMode Mode { get; init; } = ParallelMode.Tests;

    public int MaxDegreeOfParallelism { get; init; } = Environment.ProcessorCount;

    public bool StopOnFirstFailure { get; init; } = false;

    public TimeSpan? GlobalTimeout { get; init; }

    /// <summary>
    /// Creates default execution options.
    /// </summary>
    public static TestExecutionOptions Default => new();
}
