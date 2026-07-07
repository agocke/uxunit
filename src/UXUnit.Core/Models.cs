using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using StaticCs;

namespace UXUnit;

/// <summary>
/// Represents the status of a test execution.
/// </summary>
[Closed]
public enum TestStatus
{
    /// <summary>
    /// The test passed successfully.
    /// </summary>
    Passed,

    /// <summary>
    /// The test failed with an error or assertion failure.
    /// </summary>
    Failed,

    /// <summary>
    /// The test was skipped.
    /// </summary>
    Skipped,

    /// <summary>
    /// The test was not executed due to a fault in the test infrastructure.
    /// </summary>
    Faulted,
}

/// <summary>
/// Represents the result of a test execution.
/// </summary>
public sealed class TestResult
{
    public required string TestId { get; init; }

    public required string TestName { get; init; }

    public string? TestDisplayName { get; init; }

    /// <summary>
    /// Fully qualified name of the test class.
    /// </summary>
    public required string ClassName { get; init; }

    public string? ClassDisplayName { get; init; }

    public required string AssemblyName { get; init; }

    public TestStatus Status { get; init; }

    public TimeSpan Duration { get; init; }

    public string? ErrorMessage { get; init; }

    public string? StackTrace { get; init; }

    public string? SkipReason { get; init; }

    public IReadOnlyList<string> OutputLines { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets the arguments used for parameterized tests.
    /// </summary>
    public object?[]? TestCaseArguments { get; init; }

    /// <summary>
    /// Creates a successful test result.
    /// </summary>
    public static TestResult Success(
        string testId,
        string testName,
        TimeSpan duration,
        string className,
        string assemblyName
    ) =>
        new()
        {
            TestId = testId,
            TestName = testName,
            ClassName = className,
            AssemblyName = assemblyName,
            Status = TestStatus.Passed,
            Duration = duration,
        };

    /// <summary>
    /// Creates a failed test result.
    /// </summary>
    public static TestResult Failure(
        string testId,
        string testName,
        Exception exception,
        TimeSpan duration,
        string className,
        string assemblyName
    ) =>
        new()
        {
            TestId = testId,
            TestName = testName,
            ClassName = className,
            AssemblyName = assemblyName,
            Status = TestStatus.Failed,
            Duration = duration,
            ErrorMessage = exception.Message,
            StackTrace = exception.StackTrace,
        };

    /// <summary>
    /// Represents a fault in the test infrastructure, such as an exception thrown during
    /// test discovery or setup, rather than a failure of the test itself.
    /// </summary>
    public static TestResult Fault(
        string testId,
        string testName,
        string errorMessage,
        string? stackTrace,
        string className,
        string assemblyName
    ) =>
        new()
        {
            TestId = testId,
            TestName = testName,
            ClassName = className,
            AssemblyName = assemblyName,
            Status = TestStatus.Failed,
            ErrorMessage = errorMessage,
            StackTrace = stackTrace,
        };

    /// <summary>
    /// Creates a skipped test result.
    /// </summary>
    public static TestResult Skipped(
        string testId,
        string testName,
        string reason,
        string className,
        string assemblyName
    ) =>
        new()
        {
            TestId = testId,
            TestName = testName,
            ClassName = className,
            AssemblyName = assemblyName,
            Status = TestStatus.Skipped,
            SkipReason = reason,
        };
}

/// <summary>
/// Represents metadata for a test class.
/// </summary>
public sealed class TestClassMetadata
{
    public required string ClassName { get; init; }

    public required string AssemblyName { get; init; }

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