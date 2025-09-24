using System;

namespace UXUnit;

/// <summary>
/// Marks a class as containing test methods.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class TestClassAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the display name for the test class.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Gets or sets the category for grouping tests.
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Gets or sets whether to skip all tests in this class.
    /// </summary>
    public bool Skip { get; set; }

    /// <summary>
    /// Gets or sets the reason for skipping tests (required if Skip = true).
    /// </summary>
    public string? SkipReason { get; set; }
}

/// <summary>
/// Marks a method as a test case.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class TestAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the display name for the test.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Gets or sets the category for grouping tests.
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Gets or sets whether to skip this test.
    /// </summary>
    public bool Skip { get; set; }

    /// <summary>
    /// Gets or sets the reason for skipping.
    /// </summary>
    public string? SkipReason { get; set; }

    /// <summary>
    /// Gets or sets the maximum execution time in milliseconds (0 = no timeout).
    /// </summary>
    public int Timeout { get; set; } = 0;
}

/// <summary>
/// Provides data for parameterized tests.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class TestDataAttribute : Attribute
{
    /// <summary>
    /// Gets the test data arguments.
    /// </summary>
    public object?[] Data { get; }

    /// <summary>
    /// Gets or sets the display name for this test case.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Gets or sets whether to skip this test case.
    /// </summary>
    public bool Skip { get; set; }

    /// <summary>
    /// Gets or sets the reason for skipping this test case.
    /// </summary>
    public string? SkipReason { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TestDataAttribute"/> class.
    /// </summary>
    /// <param name="data">The test data arguments.</param>
    public TestDataAttribute(params object?[] data)
    {
        Data = data;
    }
}

/// <summary>
/// References a method or property that provides test data.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class TestDataSourceAttribute : Attribute
{
    /// <summary>
    /// Gets the name of the member that provides test data.
    /// </summary>
    public string MemberName { get; }

    /// <summary>
    /// Gets or sets the type that contains the data source member.
    /// If not specified, uses the test class type.
    /// </summary>
    public Type? MemberType { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TestDataSourceAttribute"/> class.
    /// </summary>
    /// <param name="memberName">The name of the member that provides test data.</param>
    public TestDataSourceAttribute(string memberName)
    {
        MemberName = memberName;
    }
}

/// <summary>
/// Marks a method to run before each test in the class.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class SetupAttribute : Attribute
{
}

/// <summary>
/// Marks a method to run after each test in the class.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class CleanupAttribute : Attribute
{
}

/// <summary>
/// Marks a static method to run once before all tests in the class.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class ClassSetupAttribute : Attribute
{
}

/// <summary>
/// Marks a static method to run once after all tests in the class.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class ClassCleanupAttribute : Attribute
{
}

/// <summary>
/// Controls parallel execution behavior for tests.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class ParallelAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the parallel execution mode.
    /// </summary>
    public ParallelExecution Execution { get; set; } = ParallelExecution.Enabled;

    /// <summary>
    /// Gets or sets the group name for sequential execution within the group.
    /// Tests in the same group will run sequentially relative to each other.
    /// </summary>
    public string? Group { get; set; }
}

/// <summary>
/// Defines parallel execution modes.
/// </summary>
public enum ParallelExecution
{
    /// <summary>
    /// Can run in parallel with other tests.
    /// </summary>
    Enabled,

    /// <summary>
    /// Must run sequentially.
    /// </summary>
    Disabled,

    /// <summary>
    /// Must run in parallel (fails if not possible).
    /// </summary>
    Required
}

/// <summary>
/// Configures retry behavior for a test.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class RetryAttribute : Attribute
{
    /// <summary>
    /// Gets the maximum number of attempts (including the initial attempt).
    /// </summary>
    public int MaxAttempts { get; }

    /// <summary>
    /// Gets or sets the delay between retry attempts in milliseconds.
    /// </summary>
    public int DelayMs { get; set; } = 0;

    /// <summary>
    /// Initializes a new instance of the <see cref="RetryAttribute"/> class.
    /// </summary>
    /// <param name="maxAttempts">The maximum number of attempts (default: 3).</param>
    public RetryAttribute(int maxAttempts = 3)
    {
        if (maxAttempts <= 0) 
            throw new ArgumentException("MaxAttempts must be positive", nameof(maxAttempts));
        MaxAttempts = maxAttempts;
    }
}

/// <summary>
/// Repeats a test multiple times.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class RepeatAttribute : Attribute
{
    /// <summary>
    /// Gets the number of times to repeat the test.
    /// </summary>
    public int Count { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RepeatAttribute"/> class.
    /// </summary>
    /// <param name="count">The number of times to repeat the test.</param>
    public RepeatAttribute(int count)
    {
        if (count <= 0) 
            throw new ArgumentException("Count must be positive", nameof(count));
        Count = count;
    }
}

/// <summary>
/// Configures UXUnit behavior at the assembly level.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly)]
public sealed class UXUnitConfigurationAttribute : Attribute
{
    /// <summary>
    /// Gets or sets whether parallel execution is enabled by default.
    /// </summary>
    public bool ParallelExecution { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum degree of parallelism.
    /// </summary>
    public int MaxDegreeOfParallelism { get; set; } = -1; // Use Environment.ProcessorCount

    /// <summary>
    /// Gets or sets the default timeout for all tests in milliseconds.
    /// </summary>
    public int DefaultTimeout { get; set; } = 0; // No timeout

    /// <summary>
    /// Gets or sets whether to stop execution on the first test failure.
    /// </summary>
    public bool StopOnFirstFailure { get; set; } = false;
}

/// <summary>
/// Configures test assembly metadata.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly)]
public sealed class TestAssemblyAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the display name for the test assembly.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Gets or sets the category for the test assembly.
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Gets or sets the type containing assembly setup methods.
    /// </summary>
    public Type? SetupClass { get; set; }

    /// <summary>
    /// Gets or sets the type containing assembly cleanup methods.
    /// </summary>
    public Type? CleanupClass { get; set; }
}