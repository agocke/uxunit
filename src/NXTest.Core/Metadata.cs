using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NXTest;

/// <summary>
/// Represents metadata for a test class.
/// </summary>
public sealed class TestClassMetadata
{
    public required string ClassName { get; init; }

    public string? DisplayName { get; init; }

    public string? Category { get; init; }

    public bool Skip { get; init; }

    public string? SkipReason { get; init; }

    public IReadOnlyList<TestMethodMetadata> TestMethods { get; init; } =
        Array.Empty<TestMethodMetadata>();

    public delegate Task DispatchFunc(object? receiver, string methodName, object? theoryArgs);

    public required Func<object?> CreateInstance { get; init; }
    public required DispatchFunc TestDispatch { get; init; }
}

/// <summary>
/// Represents metadata for a test method.
/// Use nested Fact or Theory types to create instances.
/// </summary>
public abstract class TestMethodMetadata
{
    /// <summary>
    /// Private constructor - use Fact or Theory nested types.
    /// </summary>
    private TestMethodMetadata() { }

    public required string MethodName { get; init; }

    public bool Skip { get; init; }

    public string? SkipReason { get; init; }

    public int TimeoutMs { get; init; } = 0;

    public bool IsAsync { get; init; }

    public bool IsStatic { get; init; }

    /// <summary>
    /// Represents a Fact test - a non-parameterized test method.
    /// </summary>
    public sealed class Fact : TestMethodMetadata
    {
    }

    /// <summary>
    /// Represents a Theory test - a parameterized test method executed multiple times with different arguments.
    /// </summary>
    public sealed class Theory : TestMethodMetadata
    {
        /// <summary>
        /// Gets the test cases for this theory.
        /// Each test case provides arguments for one execution of the test.
        /// </summary>
        public IReadOnlyList<TestCaseInfo> TestCases { get; init; } =
            Array.Empty<TestCaseInfo>();

           }
}

/// <summary>
/// Represents metadata for a test case (parameterized test data).
/// </summary>
public readonly struct TestCaseInfo
{
    public required object? Arguments { get; init; }

    public required string DisplayName { get; init; }
}