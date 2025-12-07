using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace UXUnit;

/// <summary>
/// Represents the status of a test execution.
/// </summary>
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

    public string? ErrorType { get; init; }

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
            ErrorType = exception.GetType().FullName,
            StackTrace = exception.StackTrace,
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

    public string? DisplayName { get; init; }

    public string? Category { get; init; }

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
        /// <summary>
        /// Gets the test body delegate for this fact.
        /// The delegate contains the actual test code.
        /// </summary>
        public Func<CancellationToken, Task>? Body { get; init; }
    }

    /// <summary>
    /// Represents a Theory test - a parameterized test method executed multiple times with different arguments.
    /// </summary>
    public sealed class Theory : TestMethodMetadata
    {
        /// <summary>
        /// Gets the test body delegate for this theory.
        /// The delegate accepts arguments and contains the actual test code.
        /// </summary>
        public Func<object?[], CancellationToken, Task>? ParameterizedBody { get; init; }

        /// <summary>
        /// Gets the test cases for this theory.
        /// Each test case provides arguments for one execution of the test.
        /// </summary>
        public IReadOnlyList<TestCaseMetadata> TestCases { get; init; } =
            Array.Empty<TestCaseMetadata>();
    }
}

/// <summary>
/// Represents metadata for a test case (parameterized test data).
/// </summary>
public sealed class TestCaseMetadata
{
    public object?[] Arguments { get; init; } = Array.Empty<object?>();

    public string? DisplayName { get; init; }

    public bool Skip { get; init; }

    public string? SkipReason { get; init; }
}

/// <summary>
/// Provides summary statistics for a test run.
/// </summary>
public sealed class TestRunSummary
{
    public int TotalTests { get; init; }

    public int PassedTests { get; init; }

    public int FailedTests { get; init; }

    public int SkippedTests { get; init; }

    public TimeSpan TotalDuration { get; init; }

    public double PassRate => TotalTests > 0 ? (double)PassedTests / TotalTests : 0.0;

    public bool AllPassed => FailedTests == 0;
}