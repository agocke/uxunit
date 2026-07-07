using System;
using System.Collections.Generic;
using StaticCs;

namespace NXTest;

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
        string className
    ) =>
        new()
        {
            TestId = testId,
            TestName = testName,
            ClassName = className,
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
        string className
    ) =>
        new()
        {
            TestId = testId,
            TestName = testName,
            ClassName = className,
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
        string className
    ) =>
        new()
        {
            TestId = testId,
            TestName = testName,
            ClassName = className,
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
        string className
    ) =>
        new()
        {
            TestId = testId,
            TestName = testName,
            ClassName = className,
            Status = TestStatus.Skipped,
            SkipReason = reason,
        };
}
