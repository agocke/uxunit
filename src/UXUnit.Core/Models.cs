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

    /// <summary>
    /// The test result is inconclusive.
    /// </summary>
    Inconclusive,
}

/// <summary>
/// Represents the result of a test execution.
/// </summary>
public sealed class TestResult
{
    public string TestId { get; init; } = string.Empty;

    public string TestName { get; init; } = string.Empty;

    public string? TestDisplayName { get; init; }

    public string ClassName { get; init; } = string.Empty;

    public string? ClassDisplayName { get; init; }

    public string AssemblyName { get; init; } = string.Empty;

    public TestStatus Status { get; init; }

    public TimeSpan Duration { get; init; }

    public DateTime StartTime { get; init; }

    public DateTime EndTime { get; init; }

    public string? ErrorMessage { get; init; }

    public string? ErrorType { get; init; }

    public string? StackTrace { get; init; }

    public string? SkipReason { get; init; }

    public int AttemptCount { get; init; } = 1;

    public IReadOnlyList<string> OutputLines { get; init; } = Array.Empty<string>();

    public IReadOnlyDictionary<string, object?> Properties { get; init; } =
        new Dictionary<string, object?>();

    public object?[]? TestCaseArguments { get; init; }

    /// <summary>
    /// Creates a successful test result.
    /// </summary>
    public static TestResult Success(
        string testId,
        string testName,
        TimeSpan duration,
        DateTime startTime,
        DateTime endTime,
        string? className = null
    ) =>
        new()
        {
            TestId = testId,
            TestName = testName,
            ClassName = className ?? string.Empty,
            Status = TestStatus.Passed,
            Duration = duration,
            StartTime = startTime,
            EndTime = endTime,
        };

    /// <summary>
    /// Creates a failed test result.
    /// </summary>
    public static TestResult Failure(
        string testId,
        string testName,
        Exception exception,
        TimeSpan duration,
        DateTime startTime,
        DateTime endTime,
        string? className = null
    ) =>
        new()
        {
            TestId = testId,
            TestName = testName,
            ClassName = className ?? string.Empty,
            Status = TestStatus.Failed,
            Duration = duration,
            StartTime = startTime,
            EndTime = endTime,
            ErrorMessage = exception.Message,
            ErrorType = exception.GetType().FullName,
            StackTrace = exception.StackTrace,
        };

    /// <summary>
    /// Creates a skipped test result.
    /// </summary>
    public static TestResult Skipped(string testId, string testName, string reason, string? className = null) =>
        new()
        {
            TestId = testId,
            TestName = testName,
            ClassName = className ?? string.Empty,
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

    public string? DisplayName { get; init; }

    public string? Category { get; init; }

    public bool Skip { get; init; }

    public string? SkipReason { get; init; }

    public IReadOnlyList<TestMethodMetadata> TestMethods { get; init; } =
        Array.Empty<TestMethodMetadata>();

    public IReadOnlyDictionary<string, object?> Properties { get; init; } =
        new Dictionary<string, object?>();
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

    public IReadOnlyDictionary<string, object?> Properties { get; init; } =
        new Dictionary<string, object?>();

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
        public IReadOnlyList<TestCaseMetadata> TestCases { get; init; } = Array.Empty<TestCaseMetadata>();
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

    public IReadOnlyDictionary<string, object?> Properties { get; init; } =
        new Dictionary<string, object?>();
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

    public int InconclusiveTests { get; init; }

    public TimeSpan TotalDuration { get; init; }

    public double PassRate => TotalTests > 0 ? (double)PassedTests / TotalTests : 0.0;

    public bool AllPassed => FailedTests == 0 && InconclusiveTests == 0;
}

// Helper implementations for null objects
public sealed class NullTestOutput : ITestOutput
{
    public static readonly NullTestOutput Instance = new();

    public void WriteLine(string message) { }

    public void WriteLine(string format, params object[] args) { }
}
