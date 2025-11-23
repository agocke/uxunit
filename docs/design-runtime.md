# UXUnit.Runtime Design

## Overview

UXUnit.Runtime is the execution engine that runs generated test code. It's **not an extensible framework** but rather a focused execution engine that takes test metadata as input and produces test results as output.

## Design Principles

1. **Simple Function**: Transform test metadata into test results
2. **No Abstractions**: No ITestRunner, ITestClassRunner interfaces
3. **Separation of Concerns**: Execution is separate from presentation
4. **Performance**: Minimal overhead, efficient parallel execution
5. **Stateless**: No global state, pure functional approach where possible

## Core Architecture

```
┌─────────────────────────────────────────────────────┐
│  Generated Code (from UXUnit.Generators)            │
│  ├─ TestClassMetadata[]                             │
│  ├─ Func<CancellationToken, Task<TestResult>>[]     │
│  └─ Entry point that calls ExecuteTestsAsync        │
└──────────────────┬──────────────────────────────────┘
                   │
                   ▼
┌─────────────────────────────────────────────────────┐
│  TestExecutionEngine.ExecuteTestsAsync()            │
│  ├─ Coordinate parallel/sequential execution        │
│  ├─ Invoke generated test delegates                 │
│  ├─ Aggregate results                               │
│  └─ Return TestResult[]                             │
└──────────────────┬──────────────────────────────────┘
                   │
                   ▼
┌─────────────────────────────────────────────────────┐
│  ITestResultReporter (separate)                     │
│  └─ Format and output results                       │
└─────────────────────────────────────────────────────┘
```

## Core Components

### 1. TestExecutionEngine

The heart of the runtime - a static class with a single primary method:

```csharp
public static class TestExecutionEngine
{
    public static async Task<TestResult[]> ExecuteTestsAsync(
        IReadOnlyList<TestClassMetadata> testClasses,
        TestExecutionOptions options,
        CancellationToken cancellationToken = default)
    {
        var allTests = CollectAllTests(testClasses);

        if (options.ParallelExecution)
        {
            return await ExecuteInParallelAsync(allTests, options, cancellationToken);
        }
        else
        {
            return await ExecuteSequentiallyAsync(allTests, options, cancellationToken);
        }
    }

    private static List<TestDescriptor> CollectAllTests(
        IReadOnlyList<TestClassMetadata> testClasses)
    {
        // Flatten all test methods from all classes
        // Each TestDescriptor contains:
        // - Metadata (name, timeout, etc.)
        // - Execution delegate (Func<CancellationToken, Task<TestResult>>)
    }
}
```

**What it does**:
- Takes metadata describing all tests
- Orchestrates execution (parallel or sequential)
- Invokes generated test delegates
- Returns aggregated results

**What it doesn't do**:
- ❌ No progress reporting (that's presentation)
- ❌ No console output (that's presentation)
- ❌ No result formatting (that's presentation)
- ❌ No extensibility hooks (generated code is the extension)

### 2. TestExecutionOptions

Configuration for test execution:

```csharp
public sealed class TestExecutionOptions
{
    public bool ParallelExecution { get; init; } = true;
    public int MaxDegreeOfParallelism { get; init; } = Environment.ProcessorCount;
    public bool StopOnFirstFailure { get; init; } = false;
    public TimeSpan? GlobalTimeout { get; init; }
    public ITestOutputSink? OutputSink { get; init; }
}
```

### 3. TestContext

Provides context during test execution:

```csharp
public sealed class TestContext : ITestContext
{
    private readonly List<string> _outputLines = new();
    private readonly Dictionary<string, object?> _properties = new();

    public string TestName { get; }
    public string ClassName { get; }
    public string AssemblyName { get; }
    public CancellationToken CancellationToken { get; }

    public void WriteLine(string message) => _outputLines.Add(message);

    public void AddProperty(string name, object? value) =>
        _properties[name] = value;

    public T? GetProperty<T>(string name) =>
        _properties.TryGetValue(name, out var value) ? (T?)value : default;

    public IReadOnlyList<string> OutputLines => _outputLines;
    public IReadOnlyDictionary<string, object?> Properties => _properties;
}
```

## Execution Patterns

### Sequential Execution

Simple loop through all tests:

```csharp
private static async Task<TestResult[]> ExecuteSequentiallyAsync(
    List<TestDescriptor> tests,
    TestExecutionOptions options,
    CancellationToken cancellationToken)
{
    var results = new List<TestResult>(tests.Count);

    foreach (var test in tests)
    {
        if (cancellationToken.IsCancellationRequested)
            break;

        var result = await test.ExecuteAsync(cancellationToken);
        results.Add(result);

        if (options.StopOnFirstFailure && result.Status == TestStatus.Failed)
            break;
    }

    return results.ToArray();
}
```

### Parallel Execution

Efficient parallel execution with degree of parallelism control:

```csharp
private static async Task<TestResult[]> ExecuteInParallelAsync(
    List<TestDescriptor> tests,
    TestExecutionOptions options,
    CancellationToken cancellationToken)
{
    var results = new ConcurrentBag<TestResult>();
    var shouldStop = false;

    await Parallel.ForEachAsync(
        tests,
        new ParallelOptions
        {
            MaxDegreeOfParallelism = options.MaxDegreeOfParallelism,
            CancellationToken = cancellationToken
        },
        async (test, ct) =>
        {
            if (options.StopOnFirstFailure && shouldStop)
                return;

            var result = await test.ExecuteAsync(ct);
            results.Add(result);

            if (options.StopOnFirstFailure && result.Status == TestStatus.Failed)
                shouldStop = true;
        });

    return results.OrderBy(r => r.TestId).ToArray();
}
```

## Result Reporting (Separate Layer)

Test result reporting is **completely separate** from execution:

```csharp
public interface ITestResultReporter
{
    void ReportTestRunStart(TestRunInfo info);
    void ReportTestComplete(TestResult result);
    void ReportTestRunComplete(TestRunSummary summary);
}
```

### Console Reporter

```csharp
public sealed class ConsoleTestReporter : ITestResultReporter
{
    public void ReportTestRunStart(TestRunInfo info)
    {
        Console.WriteLine($"Starting test run: {info.TotalTests} tests");
    }

    public void ReportTestComplete(TestResult result)
    {
        var symbol = result.Status switch
        {
            TestStatus.Passed => "✓",
            TestStatus.Failed => "✗",
            TestStatus.Skipped => "⊝",
            _ => "?"
        };

        Console.WriteLine($"{symbol} {result.ClassName}.{result.TestName}");
    }

    public void ReportTestRunComplete(TestRunSummary summary)
    {
        Console.WriteLine();
        Console.WriteLine($"Tests: {summary.TotalTests}");
        Console.WriteLine($"Passed: {summary.PassedTests}");
        Console.WriteLine($"Failed: {summary.FailedTests}");
        Console.WriteLine($"Skipped: {summary.SkippedTests}");
        Console.WriteLine($"Duration: {summary.TotalDuration.TotalSeconds:F2}s");
    }
}
```

### JUnit XML Reporter

```csharp
public sealed class JUnitXmlReporter : ITestResultReporter
{
    private readonly List<TestResult> _results = new();
    private readonly string _outputPath;

    public void ReportTestComplete(TestResult result) => _results.Add(result);

    public void ReportTestRunComplete(TestRunSummary summary)
    {
        var xml = GenerateJUnitXml(_results, summary);
        File.WriteAllText(_outputPath, xml);
    }
}
```

## Integration with Generated Code

The generated code creates the bridge between metadata and execution:

```csharp
// Generated by UXUnit.Generators
internal static class GeneratedTestRegistry
{
    // Metadata for discovery
    public static readonly TestClassMetadata[] AllTestClasses = ...;

    // Entry point
    public static async Task<int> Main(string[] args)
    {
        // Parse command line args
        var options = ParseOptions(args);

        // Execute tests (runtime engine)
        var results = await TestExecutionEngine.ExecuteTestsAsync(
            AllTestClasses,
            options);

        // Report results (separate concern)
        var reporter = CreateReporter(options);
        var summary = CreateSummary(results);

        reporter.ReportTestRunStart(new TestRunInfo { TotalTests = results.Length });
        foreach (var result in results)
            reporter.ReportTestComplete(result);
        reporter.ReportTestRunComplete(summary);

        // Exit code: 0 if all passed, 1 if any failed
        return summary.FailedTests > 0 ? 1 : 0;
    }
}
```

## What We Removed

The current implementation has unnecessary abstractions that should be removed:

### ❌ ITestRunner Interface

```csharp
// REMOVE: Only one implementation, no extensibility needed
public interface ITestRunner
{
    Task<TestRunResult> RunTestsAsync(...);
}
```

**Why**: The execution engine is a static function, not an extensible framework.

### ❌ ITestClassRunner Interface

```csharp
// REMOVE: Generated code creates delegates directly
public interface ITestClassRunner
{
    TestClassMetadata Metadata { get; }
    Task<TestResult[]> RunAllTestsAsync(...);
}
```

**Why**: Generated code produces test delegates; no need for runner abstraction.

### ❌ TestClassRunnerBase

```csharp
// REMOVE: No inheritance needed with source generation
public abstract class TestClassRunnerBase : ITestClassRunner
{
    // Helper methods for generated code
}
```

**Why**: Generated code can include any needed helpers inline.

### ❌ Presentation Logic in TestRunner

```csharp
// REMOVE: Progress bars, emoji, console output
private void ReportProgress(int completed, int total)
{
    var percentage = (completed * 100) / total;
    _output.WriteLine($"Progress: {completed}/{total} ({percentage}%)");
}
```

**Why**: Execution should be pure; presentation is separate.

## Simplified Class Structure

```
UXUnit.Runtime/
├── TestExecutionEngine.cs     # Core execution logic
├── TestExecutionOptions.cs    # Configuration
├── TestContext.cs              # Test execution context
├── Reporters/
│   ├── ITestResultReporter.cs
│   ├── ConsoleTestReporter.cs
│   ├── JUnitXmlReporter.cs
│   └── TrxReporter.cs
└── UXUnit.Runtime.csproj
```

## Performance Characteristics

### Memory Efficiency
- No dynamic allocations per test execution
- Results collected in pre-sized arrays when count is known
- Minimal framework overhead

### Execution Speed
- Direct method calls (generated code)
- Efficient parallel execution
- No reflection overhead
- Minimal synchronization

### Scalability
- Handles thousands of tests efficiently
- Parallel execution scales with CPU cores
- Constant memory overhead regardless of test count

## Future Considerations

- **Test Filtering**: Execute subset of tests based on criteria
- **Test Ordering**: Custom execution order strategies
- **Retry Logic**: Automatic retry for flaky tests
- **Resource Pooling**: Reuse expensive test fixtures
- **Distributed Execution**: Run tests across multiple machines
