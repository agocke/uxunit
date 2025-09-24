# UXUnit Data Model

## Overview

This document describes the internal data structures and models used by UXUnit for representing tests, execution context, and results. These models are used both at compile-time (by source generators) and at runtime (by the test execution engine).

## Core Data Models

### Test Metadata Models

#### TestAssemblyMetadata
Represents metadata for an entire test assembly.

```csharp
public sealed class TestAssemblyMetadata
{
    public string AssemblyName { get; init; } = string.Empty;
    public string? DisplayName { get; init; }
    public string? Category { get; init; }
    public IReadOnlyList<TestClassMetadata> TestClasses { get; init; } = Array.Empty<TestClassMetadata>();
    public AssemblyConfiguration Configuration { get; init; } = new();
    public IReadOnlyDictionary<string, object?> Properties { get; init; } = new Dictionary<string, object?>();
}

public sealed class AssemblyConfiguration
{
    public bool ParallelExecution { get; init; } = true;
    public int MaxDegreeOfParallelism { get; init; } = Environment.ProcessorCount;
    public int DefaultTimeoutMs { get; init; } = 0; // No timeout
    public bool StopOnFirstFailure { get; init; } = false;
    public string? SetupClassName { get; init; }
    public string? CleanupClassName { get; init; }
}
```

#### TestClassMetadata
Represents metadata for a test class.

```csharp
public sealed class TestClassMetadata
{
    public string ClassName { get; init; } = string.Empty;
    public string? DisplayName { get; init; }
    public string? Category { get; init; }
    public bool Skip { get; init; }
    public string? SkipReason { get; init; }
    public ParallelConfiguration ParallelConfig { get; init; } = new();
    public IReadOnlyList<TestMethodMetadata> TestMethods { get; init; } = Array.Empty<TestMethodMetadata>();
    public IReadOnlyList<LifecycleMethodMetadata> SetupMethods { get; init; } = Array.Empty<LifecycleMethodMetadata>();
    public IReadOnlyList<LifecycleMethodMetadata> CleanupMethods { get; init; } = Array.Empty<LifecycleMethodMetadata>();
    public IReadOnlyList<LifecycleMethodMetadata> ClassSetupMethods { get; init; } = Array.Empty<LifecycleMethodMetadata>();
    public IReadOnlyList<LifecycleMethodMetadata> ClassCleanupMethods { get; init; } = Array.Empty<LifecycleMethodMetadata>();
    public IReadOnlyDictionary<string, object?> Properties { get; init; } = new Dictionary<string, object?>();
    public TypeInfo ClassType { get; init; } = default!;
}

public sealed class ParallelConfiguration
{
    public ParallelExecution Execution { get; init; } = ParallelExecution.Enabled;
    public string? Group { get; init; }
}

public enum ParallelExecution
{
    Enabled,
    Disabled,
    Required
}
```

#### TestMethodMetadata
Represents metadata for a test method.

```csharp
public sealed class TestMethodMetadata
{
    public string MethodName { get; init; } = string.Empty;
    public string? DisplayName { get; init; }
    public string? Category { get; init; }
    public bool Skip { get; init; }
    public string? SkipReason { get; init; }
    public int TimeoutMs { get; init; } = 0;
    public Type? ExpectedExceptionType { get; init; }
    public ParallelConfiguration ParallelConfig { get; init; } = new();
    public RetryConfiguration RetryConfig { get; init; } = new();
    public IReadOnlyList<TestCaseMetadata> TestCases { get; init; } = Array.Empty<TestCaseMetadata>();
    public IReadOnlyList<CustomAttributeMetadata> CustomAttributes { get; init; } = Array.Empty<CustomAttributeMetadata>();
    public IReadOnlyDictionary<string, object?> Properties { get; init; } = new Dictionary<string, object?>();
    public MethodInfo Method { get; init; } = default!;
    public bool IsAsync { get; init; }
    public bool IsStatic { get; init; }
}

public sealed class RetryConfiguration
{
    public int MaxAttempts { get; init; } = 1;
    public int DelayMs { get; init; } = 0;
    public bool Enabled => MaxAttempts > 1;
}

public sealed class TestCaseMetadata
{
    public object?[] Arguments { get; init; } = Array.Empty<object?>();
    public string? DisplayName { get; init; }
    public bool Skip { get; init; }
    public string? SkipReason { get; init; }
    public IReadOnlyDictionary<string, object?> Properties { get; init; } = new Dictionary<string, object?>();
}

public sealed class CustomAttributeMetadata
{
    public string AttributeTypeName { get; init; } = string.Empty;
    public IReadOnlyDictionary<string, object?> Properties { get; init; } = new Dictionary<string, object?>();
}
```

#### LifecycleMethodMetadata
Represents metadata for setup/cleanup methods.

```csharp
public sealed class LifecycleMethodMetadata
{
    public string MethodName { get; init; } = string.Empty;
    public LifecycleMethodType Type { get; init; }
    public bool IsAsync { get; init; }
    public bool IsStatic { get; init; }
    public MethodInfo Method { get; init; } = default!;
}

public enum LifecycleMethodType
{
    Setup,
    Cleanup,
    ClassSetup,
    ClassCleanup
}
```

## Execution Models

### TestContext
Provides context and utilities during test execution.

```csharp
public sealed class TestContext : ITestContext
{
    public string TestName { get; }
    public string ClassName { get; }
    public string AssemblyName { get; }
    public CancellationToken CancellationToken { get; }
    public ITestOutput Output { get; }

    private readonly Dictionary<string, object?> _properties = new();

    public void WriteLine(string message) => Output.WriteLine(message);
    public void WriteLine(string format, params object[] args) => Output.WriteLine(format, args);
    public void AddProperty(string name, object? value) => _properties[name] = value;
    public T? GetProperty<T>(string name) => _properties.TryGetValue(name, out var value) ? (T?)value : default;
    public IReadOnlyDictionary<string, object?> Properties => _properties;
}

public interface ITestContext
{
    string TestName { get; }
    string ClassName { get; }
    string AssemblyName { get; }
    CancellationToken CancellationToken { get; }
    void WriteLine(string message);
    void WriteLine(string format, params object[] args);
    void AddProperty(string name, object? value);
    T? GetProperty<T>(string name);
    IReadOnlyDictionary<string, object?> Properties { get; }
}

public interface ITestOutput
{
    void WriteLine(string message);
    void WriteLine(string format, params object[] args);
}
```

### TestExecution
Represents the execution state and progress of tests.

```csharp
public sealed class TestExecution
{
    public string TestId { get; init; } = string.Empty;
    public TestMethodMetadata Metadata { get; init; } = default!;
    public TestExecutionState State { get; private set; } = TestExecutionState.Pending;
    public DateTime? StartTime { get; private set; }
    public DateTime? EndTime { get; private set; }
    public TimeSpan Duration => EndTime - StartTime ?? TimeSpan.Zero;
    public Exception? Exception { get; private set; }
    public string? SkipReason { get; private set; }
    public int AttemptNumber { get; private set; } = 1;
    public TestContext Context { get; init; } = default!;

    public void MarkStarted() => (State, StartTime) = (TestExecutionState.Running, DateTime.UtcNow);
    public void MarkCompleted() => (State, EndTime) = (TestExecutionState.Completed, DateTime.UtcNow);
    public void MarkFailed(Exception exception) => (State, EndTime, Exception) = (TestExecutionState.Failed, DateTime.UtcNow, exception);
    public void MarkSkipped(string reason) => (State, SkipReason) = (TestExecutionState.Skipped, reason);
    public void IncrementAttempt() => AttemptNumber++;
}

public enum TestExecutionState
{
    Pending,
    Running,
    Completed,
    Failed,
    Skipped,
    Cancelled
}
```

## Result Models

### TestResult
Represents the result of a test execution.

```csharp
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
    public IReadOnlyDictionary<string, object?> Properties { get; init; } = new Dictionary<string, object?>();
    public object?[]? TestCaseArguments { get; init; }

    public static TestResult Success(string testId, string testName, TimeSpan duration, DateTime startTime, DateTime endTime) =>
        new() { TestId = testId, TestName = testName, Status = TestStatus.Passed, Duration = duration, StartTime = startTime, EndTime = endTime };

    public static TestResult Failure(string testId, string testName, Exception exception, TimeSpan duration, DateTime startTime, DateTime endTime) =>
        new() {
            TestId = testId, TestName = testName, Status = TestStatus.Failed, Duration = duration, StartTime = startTime, EndTime = endTime,
            ErrorMessage = exception.Message, ErrorType = exception.GetType().FullName, StackTrace = exception.StackTrace
        };

    public static TestResult Skipped(string testId, string testName, string reason) =>
        new() { TestId = testId, TestName = testName, Status = TestStatus.Skipped, SkipReason = reason };
}

public enum TestStatus
{
    Passed,
    Failed,
    Skipped,
    Inconclusive
}
```

### TestRunResult
Represents the aggregate result of a test run.

```csharp
public sealed class TestRunResult
{
    public string RunId { get; init; } = Guid.NewGuid().ToString();
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
    public TimeSpan Duration => EndTime - StartTime;
    public IReadOnlyList<TestResult> TestResults { get; init; } = Array.Empty<TestResult>();
    public TestRunSummary Summary { get; init; } = new();
    public IReadOnlyDictionary<string, object?> Properties { get; init; } = new Dictionary<string, object?>();

    public int TotalTests => TestResults.Count;
    public int PassedTests => TestResults.Count(r => r.Status == TestStatus.Passed);
    public int FailedTests => TestResults.Count(r => r.Status == TestStatus.Failed);
    public int SkippedTests => TestResults.Count(r => r.Status == TestStatus.Skipped);
    public bool HasFailures => FailedTests > 0;
}

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
```

## Source Generation Models

### GenerationContext
Provides context and utilities during source generation.

```csharp
public sealed class GenerationContext
{
    public Compilation Compilation { get; }
    public INamedTypeSymbol[] TestClasses { get; }
    public GeneratorExecutionContext ExecutionContext { get; }
    public CancellationToken CancellationToken => ExecutionContext.CancellationToken;

    public void ReportDiagnostic(Diagnostic diagnostic) => ExecutionContext.ReportDiagnostic(diagnostic);
    public void AddSource(string fileName, string sourceText) => ExecutionContext.AddSource(fileName, sourceText);
}
```

### SymbolAnalysisResult
Represents the result of analyzing symbols during source generation.

```csharp
public sealed class SymbolAnalysisResult
{
    public IReadOnlyList<TestClassSymbol> TestClasses { get; init; } = Array.Empty<TestClassSymbol>();
    public IReadOnlyList<Diagnostic> Diagnostics { get; init; } = Array.Empty<Diagnostic>();
    public bool HasErrors => Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);
}

public sealed class TestClassSymbol
{
    public INamedTypeSymbol Symbol { get; init; } = default!;
    public string ClassName => Symbol.Name;
    public string FullyQualifiedName => Symbol.ToDisplayString();
    public IReadOnlyList<TestMethodSymbol> TestMethods { get; init; } = Array.Empty<TestMethodSymbol>();
    public IReadOnlyList<LifecycleMethodSymbol> LifecycleMethods { get; init; } = Array.Empty<LifecycleMethodSymbol>();
    public IReadOnlyList<AttributeData> Attributes { get; init; } = Array.Empty<AttributeData>();
}

public sealed class TestMethodSymbol
{
    public IMethodSymbol Symbol { get; init; } = default!;
    public string MethodName => Symbol.Name;
    public IReadOnlyList<IParameterSymbol> Parameters => Symbol.Parameters;
    public IReadOnlyList<AttributeData> Attributes { get; init; } = Array.Empty<AttributeData>();
    public IReadOnlyList<TestDataSymbol> TestData { get; init; } = Array.Empty<TestDataSymbol>();
    public bool IsAsync => Symbol.ReturnType.Name is "Task" or "ValueTask";
    public bool IsStatic => Symbol.IsStatic;
}

public sealed class TestDataSymbol
{
    public AttributeData Attribute { get; init; } = default!;
    public object?[] Arguments { get; init; } = Array.Empty<object?>();
    public string? DisplayName { get; init; }
    public bool Skip { get; init; }
    public string? SkipReason { get; init; }
}

public sealed class LifecycleMethodSymbol
{
    public IMethodSymbol Symbol { get; init; } = default!;
    public LifecycleMethodType Type { get; init; }
    public string MethodName => Symbol.Name;
    public bool IsAsync => Symbol.ReturnType.Name is "Task" or "ValueTask";
    public bool IsStatic => Symbol.IsStatic;
}
```

## Execution Engine Models

### TestRunner
Core interface for test execution.

```csharp
public interface ITestRunner
{
    Task<TestRunResult> RunTestsAsync(TestRunConfiguration configuration, CancellationToken cancellationToken = default);
    Task<TestResult> RunSingleTestAsync(TestExecution execution, CancellationToken cancellationToken = default);
}

public sealed class TestRunConfiguration
{
    public TestAssemblyMetadata Assembly { get; init; } = default!;
    public TestFilter Filter { get; init; } = TestFilter.All;
    public ParallelExecutionSettings ParallelSettings { get; init; } = new();
    public ITestOutput Output { get; init; } = NullTestOutput.Instance;
    public bool StopOnFirstFailure { get; init; } = false;
    public TimeSpan? GlobalTimeout { get; init; }
}

public sealed class ParallelExecutionSettings
{
    public bool Enabled { get; init; } = true;
    public int MaxDegreeOfParallelism { get; init; } = Environment.ProcessorCount;
    public TaskScheduler? TaskScheduler { get; init; }
}
```

### TestFilter
Defines criteria for filtering tests during execution.

```csharp
public abstract class TestFilter
{
    public static readonly TestFilter All = new AllTestsFilter();
    public static TestFilter ByName(string pattern) => new NamePatternFilter(pattern);
    public static TestFilter ByCategory(string category) => new CategoryFilter(category);
    public static TestFilter ByClass(string className) => new ClassFilter(className);

    public abstract bool ShouldRun(TestMethodMetadata test);

    public static TestFilter operator &(TestFilter left, TestFilter right) => new CompositeFilter(left, right, FilterOperator.And);
    public static TestFilter operator |(TestFilter left, TestFilter right) => new CompositeFilter(left, right, FilterOperator.Or);
    public static TestFilter operator !(TestFilter filter) => new NotFilter(filter);
}

internal sealed class AllTestsFilter : TestFilter
{
    public override bool ShouldRun(TestMethodMetadata test) => !test.Skip;
}

internal sealed class NamePatternFilter : TestFilter
{
    private readonly Regex _pattern;
    public NamePatternFilter(string pattern) => _pattern = new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
    public override bool ShouldRun(TestMethodMetadata test) => !test.Skip && _pattern.IsMatch(test.MethodName);
}

internal sealed class CategoryFilter : TestFilter
{
    private readonly string _category;
    public CategoryFilter(string category) => _category = category;
    public override bool ShouldRun(TestMethodMetadata test) =>
        !test.Skip && string.Equals(test.Category, _category, StringComparison.OrdinalIgnoreCase);
}
```

## Serialization Models

### TestResultJson
JSON-serializable representation of test results for reporting and storage.

```csharp
public sealed class TestResultJson
{
    public string TestId { get; set; } = string.Empty;
    public string TestName { get; set; } = string.Empty;
    public string? TestDisplayName { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public string? ClassDisplayName { get; set; }
    public string AssemblyName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public double DurationMs { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorType { get; set; }
    public string? StackTrace { get; set; }
    public string? SkipReason { get; set; }
    public int AttemptCount { get; set; } = 1;
    public string[] OutputLines { get; set; } = Array.Empty<string>();
    public Dictionary<string, object?> Properties { get; set; } = new();
    public object?[]? TestCaseArguments { get; set; }

    public static TestResultJson FromTestResult(TestResult result) => new()
    {
        TestId = result.TestId,
        TestName = result.TestName,
        TestDisplayName = result.TestDisplayName,
        ClassName = result.ClassName,
        ClassDisplayName = result.ClassDisplayName,
        AssemblyName = result.AssemblyName,
        Status = result.Status.ToString(),
        DurationMs = result.Duration.TotalMilliseconds,
        StartTime = result.StartTime,
        EndTime = result.EndTime,
        ErrorMessage = result.ErrorMessage,
        ErrorType = result.ErrorType,
        StackTrace = result.StackTrace,
        SkipReason = result.SkipReason,
        AttemptCount = result.AttemptCount,
        OutputLines = result.OutputLines.ToArray(),
        Properties = new Dictionary<string, object?>(result.Properties),
        TestCaseArguments = result.TestCaseArguments
    };

    public TestResult ToTestResult() => new()
    {
        TestId = TestId,
        TestName = TestName,
        TestDisplayName = TestDisplayName,
        ClassName = ClassName,
        ClassDisplayName = ClassDisplayName,
        AssemblyName = AssemblyName,
        Status = Enum.Parse<TestStatus>(Status),
        Duration = TimeSpan.FromMilliseconds(DurationMs),
        StartTime = StartTime,
        EndTime = EndTime,
        ErrorMessage = ErrorMessage,
        ErrorType = ErrorType,
        StackTrace = StackTrace,
        SkipReason = SkipReason,
        AttemptCount = AttemptCount,
        OutputLines = OutputLines,
        Properties = Properties,
        TestCaseArguments = TestCaseArguments
    };
}
```

## Diagnostic Models

### DiagnosticDescriptors
Predefined diagnostic descriptors for source generation warnings and errors.

```csharp
public static class UXUnitDiagnostics
{
    public static readonly DiagnosticDescriptor InvalidTestMethodSignature = new(
        "UX0001",
        "Invalid test method signature",
        "Test method '{0}' has an invalid signature. Test methods must be public and return void, Task, or ValueTask",
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor TestMethodMustNotBeGeneric = new(
        "UX0002",
        "Test method cannot be generic",
        "Test method '{0}' cannot be generic. Consider using TestData attributes for parameterized tests",
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidSetupMethodSignature = new(
        "UX0003",
        "Invalid setup method signature",
        "Setup method '{0}' has an invalid signature. Setup methods must be public and return void or Task",
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor TestDataParameterMismatch = new(
        "UX0004",
        "Test data parameter count mismatch",
        "TestData attribute provides {0} arguments but test method '{1}' expects {2} parameters",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DuplicateLifecycleMethod = new(
        "UX0005",
        "Duplicate lifecycle method",
        "Multiple {0} methods found in class '{1}'. Only one method per lifecycle type is supported",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor TestClassMustHaveDefaultConstructor = new(
        "UX0006",
        "Test class must have accessible constructor",
        "Test class '{0}' must have a public parameterless constructor",
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
```

This data model provides a comprehensive foundation for the UXUnit framework, enabling efficient test discovery, execution, and result reporting while maintaining type safety and performance through compile-time code generation.