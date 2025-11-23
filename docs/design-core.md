# UXUnit.Core Design

## Overview

UXUnit.Core is the foundational package that defines the core data models, attributes, and interfaces used throughout the UXUnit testing framework. This package has **no runtime dependencies** on other UXUnit packages and serves as the contract between the source generator, runtime, and user code.

## Design Principles

1. **Minimal Dependencies**: Core has zero dependencies beyond .NET base libraries
2. **Pure Data Models**: No execution logic, only data structures
3. **Framework Contract**: Defines the interface between generator and runtime
4. **Compatibility Layer**: Provides xUnit-compatible attributes for migration

## Core Components

### Data Models

The data models represent the structure of tests and their results. These are **pure data classes** with no behavior.

#### TestResult

Represents the outcome of a single test execution:

```csharp
public sealed class TestResult
{
    // Identification
    public string TestId { get; init; }
    public string TestName { get; init; }
    public string ClassName { get; init; }
    public string AssemblyName { get; init; }

    // Outcome
    public TestStatus Status { get; init; }  // Passed, Failed, Skipped, Inconclusive

    // Timing
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
    public TimeSpan Duration { get; init; }

    // Error Information (for failures)
    public string? ErrorMessage { get; init; }
    public string? ErrorType { get; init; }
    public string? StackTrace { get; init; }

    // Additional Data
    public string? SkipReason { get; init; }
    public object?[]? TestCaseArguments { get; init; }
    public IReadOnlyList<string> OutputLines { get; init; }
    public IReadOnlyDictionary<string, object?> Properties { get; init; }

    // Factory methods
    public static TestResult Success(string testId, string testName, ...);
    public static TestResult Failure(string testId, string testName, Exception ex, ...);
    public static TestResult Skipped(string testId, string testName, string reason);
}
```

#### TestClassMetadata

Describes a test class discovered at compile time:

```csharp
public sealed class TestClassMetadata
{
    public string ClassName { get; init; }
    public string? DisplayName { get; init; }
    public string? Category { get; init; }
    public bool Skip { get; init; }
    public string? SkipReason { get; init; }

    public IReadOnlyList<TestMethodMetadata> TestMethods { get; init; }
    public IReadOnlyDictionary<string, object?> Properties { get; init; }
}
```

#### TestMethodMetadata

Describes a test method discovered at compile time:

```csharp
public sealed class TestMethodMetadata
{
    public string MethodName { get; init; }
    public string? DisplayName { get; init; }
    public string? Category { get; init; }
    public bool Skip { get; init; }
    public string? SkipReason { get; init; }
    public int TimeoutMs { get; init; }

    // Parameterized test support
    public IReadOnlyList<TestCaseMetadata> TestCases { get; init; }

    // Method characteristics
    public bool IsAsync { get; init; }
    public bool IsStatic { get; init; }

    public IReadOnlyDictionary<string, object?> Properties { get; init; }
}
```

#### TestCaseMetadata

Describes a single test case for parameterized tests:

```csharp
public sealed class TestCaseMetadata
{
    public object?[] Arguments { get; init; }
    public string? DisplayName { get; init; }
    public bool Skip { get; init; }
    public string? SkipReason { get; init; }
    public IReadOnlyDictionary<string, object?> Properties { get; init; }
}
```

### Attributes

Attributes are used to mark test classes and methods. These are recognized by the source generator.

#### Test Marking Attributes

```csharp
[AttributeUsage(AttributeTargets.Class)]
public sealed class TestClassAttribute : Attribute
{
    public string? DisplayName { get; set; }
    public string? Category { get; set; }
    public bool Skip { get; set; }
    public string? SkipReason { get; set; }
}

[AttributeUsage(AttributeTargets.Method)]
public sealed class TestAttribute : Attribute
{
    public string? DisplayName { get; set; }
    public int TimeoutMs { get; set; }
    public bool Skip { get; set; }
    public string? SkipReason { get; set; }
}
```

#### XUnit Compatibility Attributes

For seamless migration from xUnit:

```csharp
// These map to [Test] internally
[AttributeUsage(AttributeTargets.Method)]
public sealed class FactAttribute : TestAttribute { }

[AttributeUsage(AttributeTargets.Method)]
public sealed class TheoryAttribute : TestAttribute { }

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class InlineDataAttribute : Attribute
{
    public object?[] Data { get; }
    public InlineDataAttribute(params object?[] data) => Data = data;
}
```

#### Lifecycle Attributes

```csharp
[AttributeUsage(AttributeTargets.Method)]
public sealed class SetupAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Method)]
public sealed class CleanupAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Method)]
public sealed class ClassSetupAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Method)]
public sealed class ClassCleanupAttribute : Attribute { }
```

### Interfaces

Interfaces define extension points and contracts.

#### ITestContext

Provides context during test execution:

```csharp
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
```

#### ITestOutput

Abstraction for test output (console, file, etc.):

```csharp
public interface ITestOutput
{
    void WriteLine(string message);
    void WriteLine(string format, params object[] args);
}
```

#### Extensibility Interfaces

For custom test behaviors:

```csharp
public interface ITestMethodAttribute
{
    void OnBeforeTest(ITestContext context);
    void OnAfterTest(ITestContext context, TestResult result);
}

public interface ITestClassAttribute
{
    void OnBeforeClass(ITestContext context);
    void OnAfterClass(ITestContext context, TestResult[] results);
}

public interface ITestDataSource
{
    IEnumerable<object?[]> GetTestData(TestMethodMetadata methodMetadata);
}
```

## Package Structure

```
UXUnit.Core/
├── Attributes.cs           # All attribute definitions
├── Models.cs              # TestResult, TestClassMetadata, etc.
├── Interfaces.cs          # ITestContext, ITestOutput, etc.
└── UXUnit.Core.csproj
```

## Design Decisions

### Why Immutable Records?

All data models use `init` properties to ensure immutability:
- Thread-safe by default
- Easier to reason about
- Prevents accidental mutations during test execution
- Supports with-expressions for creating variants

### Why No Execution Logic?

Core contains **only data structures**, no execution logic:
- Clear separation of concerns
- Generator and Runtime can evolve independently
- Easier to version and maintain
- Reduces coupling between components

### Why Factory Methods on TestResult?

Factory methods like `TestResult.Success()` provide:
- Consistent object construction
- Better discoverability
- Type-safe result creation
- Extensibility through optional parameters

## Versioning Strategy

UXUnit.Core follows semantic versioning with strong compatibility guarantees:

- **Patch**: Bug fixes to documentation/comments only
- **Minor**: New optional properties or methods (backward compatible)
- **Major**: Breaking changes to data models or interfaces

Generator and Runtime packages declare minimum compatible Core version.

## Future Considerations

- **Serialization**: Add JSON serialization attributes for test result export
- **Extensibility**: Additional metadata properties for advanced scenarios
- **Performance**: Consider struct-based models for high-frequency scenarios
