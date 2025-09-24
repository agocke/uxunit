using System;
using System.Collections.Generic;
using System.Linq;

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
    Inconclusive
}

/// <summary>
/// Represents the result of a test execution.
/// </summary>
public sealed class TestResult
{
    /// <summary>
    /// Gets the unique identifier for this test execution.
    /// </summary>
    public string TestId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the name of the test method.
    /// </summary>
    public string TestName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the display name for the test (if different from TestName).
    /// </summary>
    public string? TestDisplayName { get; init; }

    /// <summary>
    /// Gets the name of the test class.
    /// </summary>
    public string ClassName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the display name for the test class (if different from ClassName).
    /// </summary>
    public string? ClassDisplayName { get; init; }

    /// <summary>
    /// Gets the name of the test assembly.
    /// </summary>
    public string AssemblyName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the test execution status.
    /// </summary>
    public TestStatus Status { get; init; }

    /// <summary>
    /// Gets the test execution duration.
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// Gets the test start time (UTC).
    /// </summary>
    public DateTime StartTime { get; init; }

    /// <summary>
    /// Gets the test end time (UTC).
    /// </summary>
    public DateTime EndTime { get; init; }

    /// <summary>
    /// Gets the error message if the test failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets the error type if the test failed.
    /// </summary>
    public string? ErrorType { get; init; }

    /// <summary>
    /// Gets the stack trace if the test failed.
    /// </summary>
    public string? StackTrace { get; init; }

    /// <summary>
    /// Gets the reason for skipping the test.
    /// </summary>
    public string? SkipReason { get; init; }

    /// <summary>
    /// Gets the number of execution attempts for this test.
    /// </summary>
    public int AttemptCount { get; init; } = 1;

    /// <summary>
    /// Gets the output lines produced during test execution.
    /// </summary>
    public IReadOnlyList<string> OutputLines { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets additional properties associated with the test result.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Properties { get; init; } = new Dictionary<string, object?>();

    /// <summary>
    /// Gets the arguments used for parameterized tests.
    /// </summary>
    public object?[]? TestCaseArguments { get; init; }

    /// <summary>
    /// Creates a successful test result.
    /// </summary>
    public static TestResult Success(string testId, string testName, TimeSpan duration, DateTime startTime, DateTime endTime) =>
        new()
        {
            TestId = testId,
            TestName = testName,
            Status = TestStatus.Passed,
            Duration = duration,
            StartTime = startTime,
            EndTime = endTime
        };

    /// <summary>
    /// Creates a failed test result.
    /// </summary>
    public static TestResult Failure(string testId, string testName, Exception exception, TimeSpan duration, DateTime startTime, DateTime endTime) =>
        new()
        {
            TestId = testId,
            TestName = testName,
            Status = TestStatus.Failed,
            Duration = duration,
            StartTime = startTime,
            EndTime = endTime,
            ErrorMessage = exception.Message,
            ErrorType = exception.GetType().FullName,
            StackTrace = exception.StackTrace
        };

    /// <summary>
    /// Creates a skipped test result.
    /// </summary>
    public static TestResult Skipped(string testId, string testName, string reason) =>
        new()
        {
            TestId = testId,
            TestName = testName,
            Status = TestStatus.Skipped,
            SkipReason = reason
        };
}

/// <summary>
/// Represents metadata for a test class.
/// </summary>
public sealed class TestClassMetadata
{
    /// <summary>
    /// Gets the class name.
    /// </summary>
    public string ClassName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the display name for the class.
    /// </summary>
    public string? DisplayName { get; init; }

    /// <summary>
    /// Gets the category for the class.
    /// </summary>
    public string? Category { get; init; }

    /// <summary>
    /// Gets whether the class should be skipped.
    /// </summary>
    public bool Skip { get; init; }

    /// <summary>
    /// Gets the reason for skipping the class.
    /// </summary>
    public string? SkipReason { get; init; }

    /// <summary>
    /// Gets the test methods in this class.
    /// </summary>
    public IReadOnlyList<TestMethodMetadata> TestMethods { get; init; } = Array.Empty<TestMethodMetadata>();

    /// <summary>
    /// Gets additional properties for the test class.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Properties { get; init; } = new Dictionary<string, object?>();
}

/// <summary>
/// Represents metadata for a test method.
/// </summary>
public sealed class TestMethodMetadata
{
    /// <summary>
    /// Gets the method name.
    /// </summary>
    public string MethodName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the display name for the method.
    /// </summary>
    public string? DisplayName { get; init; }

    /// <summary>
    /// Gets the category for the method.
    /// </summary>
    public string? Category { get; init; }

    /// <summary>
    /// Gets whether the method should be skipped.
    /// </summary>
    public bool Skip { get; init; }

    /// <summary>
    /// Gets the reason for skipping the method.
    /// </summary>
    public string? SkipReason { get; init; }

    /// <summary>
    /// Gets the timeout in milliseconds for the method.
    /// </summary>
    public int TimeoutMs { get; init; } = 0;

    /// <summary>
    /// Gets the test cases for this method (for parameterized tests).
    /// </summary>
    public IReadOnlyList<TestCaseMetadata> TestCases { get; init; } = Array.Empty<TestCaseMetadata>();

    /// <summary>
    /// Gets whether this is an async method.
    /// </summary>
    public bool IsAsync { get; init; }

    /// <summary>
    /// Gets whether this is a static method.
    /// </summary>
    public bool IsStatic { get; init; }

    /// <summary>
    /// Gets additional properties for the test method.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Properties { get; init; } = new Dictionary<string, object?>();
}

/// <summary>
/// Represents metadata for a test case (parameterized test data).
/// </summary>
public sealed class TestCaseMetadata
{
    /// <summary>
    /// Gets the arguments for this test case.
    /// </summary>
    public object?[] Arguments { get; init; } = Array.Empty<object?>();

    /// <summary>
    /// Gets the display name for this test case.
    /// </summary>
    public string? DisplayName { get; init; }

    /// <summary>
    /// Gets whether this test case should be skipped.
    /// </summary>
    public bool Skip { get; init; }

    /// <summary>
    /// Gets the reason for skipping this test case.
    /// </summary>
    public string? SkipReason { get; init; }

    /// <summary>
    /// Gets additional properties for this test case.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Properties { get; init; } = new Dictionary<string, object?>();
}

/// <summary>
/// Represents the aggregate result of a test run.
/// </summary>
public sealed class TestRunResult
{
    /// <summary>
    /// Gets the unique identifier for this test run.
    /// </summary>
    public string RunId { get; init; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets the test run start time (UTC).
    /// </summary>
    public DateTime StartTime { get; init; }

    /// <summary>
    /// Gets the test run end time (UTC).
    /// </summary>
    public DateTime EndTime { get; init; }

    /// <summary>
    /// Gets the total duration of the test run.
    /// </summary>
    public TimeSpan Duration => EndTime - StartTime;

    /// <summary>
    /// Gets all individual test results.
    /// </summary>
    public IReadOnlyList<TestResult> TestResults { get; init; } = Array.Empty<TestResult>();

    /// <summary>
    /// Gets the test run summary statistics.
    /// </summary>
    public TestRunSummary Summary { get; init; } = new();

    /// <summary>
    /// Gets additional properties for the test run.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Properties { get; init; } = new Dictionary<string, object?>();

    /// <summary>
    /// Gets the total number of tests executed.
    /// </summary>
    public int TotalTests => TestResults.Count;

    /// <summary>
    /// Gets the number of passed tests.
    /// </summary>
    public int PassedTests => TestResults.Count(r => r.Status == TestStatus.Passed);

    /// <summary>
    /// Gets the number of failed tests.
    /// </summary>
    public int FailedTests => TestResults.Count(r => r.Status == TestStatus.Failed);

    /// <summary>
    /// Gets the number of skipped tests.
    /// </summary>
    public int SkippedTests => TestResults.Count(r => r.Status == TestStatus.Skipped);

    /// <summary>
    /// Gets whether any tests failed.
    /// </summary>
    public bool HasFailures => FailedTests > 0;
}

/// <summary>
/// Provides summary statistics for a test run.
/// </summary>
public sealed class TestRunSummary
{
    /// <summary>
    /// Gets the total number of tests.
    /// </summary>
    public int TotalTests { get; init; }

    /// <summary>
    /// Gets the number of passed tests.
    /// </summary>
    public int PassedTests { get; init; }

    /// <summary>
    /// Gets the number of failed tests.
    /// </summary>
    public int FailedTests { get; init; }

    /// <summary>
    /// Gets the number of skipped tests.
    /// </summary>
    public int SkippedTests { get; init; }

    /// <summary>
    /// Gets the number of inconclusive tests.
    /// </summary>
    public int InconclusiveTests { get; init; }

    /// <summary>
    /// Gets the total duration of all tests.
    /// </summary>
    public TimeSpan TotalDuration { get; init; }

    /// <summary>
    /// Gets the pass rate as a percentage (0.0 to 1.0).
    /// </summary>
    public double PassRate => TotalTests > 0 ? (double)PassedTests / TotalTests : 0.0;

    /// <summary>
    /// Gets whether all tests passed.
    /// </summary>
    public bool AllPassed => FailedTests == 0 && InconclusiveTests == 0;
}

/// <summary>
/// Configuration for test execution.
/// </summary>
public sealed class TestRunConfiguration
{
    /// <summary>
    /// Gets or sets the test output writer.
    /// </summary>
    public ITestOutput Output { get; init; } = NullTestOutput.Instance;

    /// <summary>
    /// Gets or sets whether to stop execution on the first test failure.
    /// </summary>
    public bool StopOnFirstFailure { get; init; } = false;

    /// <summary>
    /// Gets or sets the global timeout for all tests.
    /// </summary>
    public TimeSpan? GlobalTimeout { get; init; }

    /// <summary>
    /// Gets or sets the maximum degree of parallelism.
    /// </summary>
    public int MaxDegreeOfParallelism { get; init; } = Environment.ProcessorCount;

    /// <summary>
    /// Gets or sets whether parallel execution is enabled.
    /// </summary>
    public bool ParallelExecution { get; init; } = true;
}

// Helper implementations for null objects
public sealed class NullTestOutput : ITestOutput
{
    public static readonly NullTestOutput Instance = new();
    public void WriteLine(string message) { }
    public void WriteLine(string format, params object[] args) { }
}