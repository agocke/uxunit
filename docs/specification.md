# UXUnit Specification

## Overview

This document provides a comprehensive specification of the UXUnit testing framework APIs, behaviors, and conventions.

## Core Concepts

### Test Classes

A test class is a class marked with the `[TestClass]` attribute that contains one or more test methods.

#### Requirements
- Must be marked with `[TestClass]` attribute
- Must be public or internal
- Must have a parameterless constructor
- May contain setup and cleanup methods
- May inherit from base classes (inheritance is supported)

#### Example
```csharp
[TestClass]
public class CalculatorTests
{
    private Calculator _calculator;

    [ClassSetup]
    public void InitializeClass()
    {
        // Run once before all tests in the class
    }

    [Setup]
    public void Setup()
    {
        _calculator = new Calculator();
    }

    [Test]
    public void Add_TwoNumbers_ReturnsSum()
    {
        var result = _calculator.Add(2, 3);
        Assert.That(result).IsEqualTo(5);
    }

    [Cleanup]
    public void Cleanup()
    {
        _calculator?.Dispose();
    }

    [ClassCleanup]
    public void CleanupClass()
    {
        // Run once after all tests in the class
    }
}
```

### Test Methods

A test method is a method marked with the `[Test]` attribute within a test class.

#### Requirements
- Must be marked with `[Test]` attribute
- Must be public
- Must return `void`, `Task`, `ValueTask`, or `IAsyncEnumerable<T>`
- May have parameters (for parameterized tests)
- May be static (static tests are supported)

#### Supported Method Signatures
```csharp
[Test] public void SimpleTest() { }
[Test] public async Task AsyncTest() { }
[Test] public async ValueTask ValueTaskTest() { }
[Test] public void ParameterizedTest(int value) { }
[Test] public static void StaticTest() { }
```

## Attributes

### Core Attributes

#### `[TestClass]`
Marks a class as containing test methods.

```csharp
[AttributeUsage(AttributeTargets.Class)]
public class TestClassAttribute : Attribute
{
    public string? DisplayName { get; set; }
    public string? Category { get; set; }
    public bool Skip { get; set; }
    public string? SkipReason { get; set; }
}
```

**Properties:**
- `DisplayName`: Custom display name for the test class
- `Category`: Category for grouping tests
- `Skip`: Whether to skip all tests in this class
- `SkipReason`: Reason for skipping (required if Skip = true)

#### `[Test]`
Marks a method as a test case.

```csharp
[AttributeUsage(AttributeTargets.Method)]
public class TestAttribute : Attribute
{
    public string? DisplayName { get; set; }
    public string? Category { get; set; }
    public bool Skip { get; set; }
    public string? SkipReason { get; set; }
    public int Timeout { get; set; } = 0; // milliseconds, 0 = no timeout
    public int ExpectedExceptionType { get; set; }
}
```

**Properties:**
- `DisplayName`: Custom display name for the test
- `Category`: Category for grouping tests
- `Skip`: Whether to skip this test
- `SkipReason`: Reason for skipping
- `Timeout`: Maximum execution time in milliseconds
- `ExpectedExceptionType`: Type of exception expected to be thrown

#### `[TestData]`
Provides data for parameterized tests.

```csharp
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class TestDataAttribute : Attribute
{
    public object?[] Data { get; }
    public string? DisplayName { get; set; }
    public bool Skip { get; set; }
    public string? SkipReason { get; set; }

    public TestDataAttribute(params object?[] data)
    {
        Data = data;
    }
}
```

**Example:**
```csharp
[Test]
[TestData(1, 2, 3)]
[TestData(5, 7, 12)]
[TestData(-1, 1, 0, DisplayName = "Adding negative and positive")]
public void Add_VariousInputs_ReturnsExpectedSum(int a, int b, int expected)
{
    var result = _calculator.Add(a, b);
    Assert.That(result).IsEqualTo(expected);
}
```

#### Lifecycle Attributes

```csharp
[AttributeUsage(AttributeTargets.Method)]
public class SetupAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Method)]
public class CleanupAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Method)]
public class ClassSetupAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Method)]
public class ClassCleanupAttribute : Attribute { }
```

**Requirements:**
- Setup/Cleanup methods must be public and return void or Task
- ClassSetup/ClassCleanup methods must be static
- Multiple setup/cleanup methods are allowed (execution order is undefined)

### Advanced Attributes

#### `[Repeat]`
Executes a test multiple times.

```csharp
[AttributeUsage(AttributeTargets.Method)]
public class RepeatAttribute : Attribute
{
    public int Count { get; }

    public RepeatAttribute(int count)
    {
        if (count <= 0) throw new ArgumentException("Count must be positive");
        Count = count;
    }
}
```

#### `[Retry]`
Retries a test on failure.

```csharp
[AttributeUsage(AttributeTargets.Method)]
public class RetryAttribute : Attribute
{
    public int MaxAttempts { get; }
    public int DelayMs { get; set; } = 0;

    public RetryAttribute(int maxAttempts = 3)
    {
        if (maxAttempts <= 0) throw new ArgumentException("MaxAttempts must be positive");
        MaxAttempts = maxAttempts;
    }
}
```

#### `[Parallel]`
Controls parallel execution behavior.

```csharp
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class ParallelAttribute : Attribute
{
    public ParallelExecution Execution { get; set; } = ParallelExecution.Enabled;
    public string? Group { get; set; } // Tests in same group run sequentially
}

public enum ParallelExecution
{
    Enabled,    // Can run in parallel with other tests
    Disabled,   // Must run sequentially
    Required    // Must run in parallel (fails if not possible)
}
```

## Assertion API

### Core Assertion Interface

```csharp
public static class Assert
{
    public static AssertionBuilder<T> That<T>(T actual) => new(actual);
    public static void True(bool condition, string? message = null) { }
    public static void False(bool condition, string? message = null) { }
    public static void Null<T>(T? value, string? message = null) where T : class { }
    public static void NotNull<T>(T? value, string? message = null) where T : class { }
    public static void Throws<T>(Action action, string? message = null) where T : Exception { }
    public static Task<T> ThrowsAsync<T>(Func<Task> action, string? message = null) where T : Exception { }
}
```

### Fluent Assertion Builder

```csharp
public class AssertionBuilder<T>
{
    public AssertionBuilder<T> IsEqualTo(T expected, string? message = null);
    public AssertionBuilder<T> IsNotEqualTo(T expected, string? message = null);
    public AssertionBuilder<T> IsNull(string? message = null);
    public AssertionBuilder<T> IsNotNull(string? message = null);
    public AssertionBuilder<T> IsSameAs(T expected, string? message = null);
    public AssertionBuilder<T> IsNotSameAs(T expected, string? message = null);
}
```

### Type-Specific Assertions

#### String Assertions
```csharp
public static class StringAssertions
{
    public static AssertionBuilder<string> Contains(this AssertionBuilder<string> builder, string expected);
    public static AssertionBuilder<string> DoesNotContain(this AssertionBuilder<string> builder, string expected);
    public static AssertionBuilder<string> StartsWith(this AssertionBuilder<string> builder, string expected);
    public static AssertionBuilder<string> EndsWith(this AssertionBuilder<string> builder, string expected);
    public static AssertionBuilder<string> IsEmpty(this AssertionBuilder<string> builder);
    public static AssertionBuilder<string> IsNotEmpty(this AssertionBuilder<string> builder);
    public static AssertionBuilder<string> HasLength(this AssertionBuilder<string> builder, int expected);
    public static AssertionBuilder<string> Matches(this AssertionBuilder<string> builder, string pattern);
}
```

#### Numeric Assertions
```csharp
public static class NumericAssertions
{
    public static AssertionBuilder<T> IsGreaterThan<T>(this AssertionBuilder<T> builder, T expected) where T : IComparable<T>;
    public static AssertionBuilder<T> IsGreaterThanOrEqualTo<T>(this AssertionBuilder<T> builder, T expected) where T : IComparable<T>;
    public static AssertionBuilder<T> IsLessThan<T>(this AssertionBuilder<T> builder, T expected) where T : IComparable<T>;
    public static AssertionBuilder<T> IsLessThanOrEqualTo<T>(this AssertionBuilder<T> builder, T expected) where T : IComparable<T>;
    public static AssertionBuilder<T> IsBetween<T>(this AssertionBuilder<T> builder, T min, T max) where T : IComparable<T>;
    public static AssertionBuilder<double> IsCloseTo(this AssertionBuilder<double> builder, double expected, double tolerance = 1e-6);
}
```

#### Collection Assertions
```csharp
public static class CollectionAssertions
{
    public static AssertionBuilder<IEnumerable<T>> Contains<T>(this AssertionBuilder<IEnumerable<T>> builder, T expected);
    public static AssertionBuilder<IEnumerable<T>> DoesNotContain<T>(this AssertionBuilder<IEnumerable<T>> builder, T expected);
    public static AssertionBuilder<IEnumerable<T>> HasCount<T>(this AssertionBuilder<IEnumerable<T>> builder, int expected);
    public static AssertionBuilder<IEnumerable<T>> IsEmpty<T>(this AssertionBuilder<IEnumerable<T>> builder);
    public static AssertionBuilder<IEnumerable<T>> IsNotEmpty<T>(this AssertionBuilder<IEnumerable<T>> builder);
    public static AssertionBuilder<IEnumerable<T>> IsEquivalentTo<T>(this AssertionBuilder<IEnumerable<T>> builder, IEnumerable<T> expected);
    public static AssertionBuilder<IEnumerable<T>> IsOrdered<T>(this AssertionBuilder<IEnumerable<T>> builder);
}
```

## Test Data Sources

### Built-in Data Sources

#### `[TestDataSource]`
References a method or property that provides test data.

```csharp
[AttributeUsage(AttributeTargets.Method)]
public class TestDataSourceAttribute : Attribute
{
    public string MemberName { get; }
    public Type? MemberType { get; set; }

    public TestDataSourceAttribute(string memberName)
    {
        MemberName = memberName;
    }
}
```

**Example:**
```csharp
[Test]
[TestDataSource(nameof(GetCalculationData))]
public void Add_DataFromMethod_ReturnsExpectedResult(int a, int b, int expected)
{
    var result = _calculator.Add(a, b);
    Assert.That(result).IsEqualTo(expected);
}

public static IEnumerable<object[]> GetCalculationData()
{
    yield return new object[] { 1, 2, 3 };
    yield return new object[] { 5, 7, 12 };
    yield return new object[] { -1, 1, 0 };
}
```

#### `[CsvData]`
Loads test data from CSV files.

```csharp
[AttributeUsage(AttributeTargets.Method)]
public class CsvDataAttribute : Attribute
{
    public string FilePath { get; }
    public bool HasHeaders { get; set; } = true;
    public string Delimiter { get; set; } = ",";

    public CsvDataAttribute(string filePath)
    {
        FilePath = filePath;
    }
}
```

#### `[JsonData]`
Loads test data from JSON files.

```csharp
[AttributeUsage(AttributeTargets.Method)]
public class JsonDataAttribute : Attribute
{
    public string FilePath { get; }
    public string? PropertyPath { get; set; }

    public JsonDataAttribute(string filePath)
    {
        FilePath = filePath;
    }
}
```

## Test Execution

### Execution Order

1. **Class Construction**: Test class instance is created
2. **ClassSetup**: Static class setup methods are called (once per class)
3. **For each test method**:
   - **Setup**: Instance setup methods are called
   - **Test Execution**: The test method is invoked
   - **Cleanup**: Instance cleanup methods are called
4. **ClassCleanup**: Static class cleanup methods are called (once per class)
5. **Class Disposal**: Test class instance is disposed if it implements IDisposable

### Parallel Execution

Tests can run in parallel by default. Control parallel execution using:

```csharp
// Disable parallel execution for entire class
[TestClass]
[Parallel(Execution = ParallelExecution.Disabled)]
public class SerialTests { }

// Group tests that must run sequentially
[TestClass]
public class GroupedTests
{
    [Test]
    [Parallel(Group = "Database")]
    public void DatabaseTest1() { }

    [Test]
    [Parallel(Group = "Database")]
    public void DatabaseTest2() { }

    [Test] // Can run in parallel with any other test
    public void IndependentTest() { }
}
```

### Exception Handling

#### Expected Exceptions
```csharp
[Test]
[ExpectedException(typeof(ArgumentNullException))]
public void Method_NullInput_ThrowsArgumentNull()
{
    // Test that expects ArgumentNullException
}

// Alternative fluent syntax
[Test]
public void Method_NullInput_ThrowsArgumentNull()
{
    Assert.Throws<ArgumentNullException>(() => method.DoSomething(null));
}
```

#### Unhandled Exceptions
- Any unhandled exception causes the test to fail
- Exception details are captured in test results
- Stack traces are preserved for debugging

### Timeouts

```csharp
[Test(Timeout = 5000)] // 5 seconds
public async Task LongRunningTest()
{
    await SomeAsyncOperation();
}
```

## Test Results

### Result Types

```csharp
public enum TestStatus
{
    Passed,
    Failed,
    Skipped,
    Inconclusive
}

public class TestResult
{
    public string TestName { get; }
    public string ClassName { get; }
    public TestStatus Status { get; }
    public TimeSpan Duration { get; }
    public string? ErrorMessage { get; }
    public string? StackTrace { get; }
    public IDictionary<string, object?> Properties { get; }
}
```

### Output and Logging

```csharp
public interface ITestContext
{
    void WriteLine(string message);
    void WriteLine(string format, params object[] args);
    void AddProperty(string name, object? value);
    CancellationToken CancellationToken { get; }
}
```

**Usage in tests:**
```csharp
[Test]
public void TestWithOutput(ITestContext context)
{
    context.WriteLine("Starting test execution");
    context.AddProperty("ExecutedAt", DateTime.Now);
    // ... test logic
}
```

## Configuration

### Global Configuration

```csharp
[assembly: UXUnitConfiguration(
    ParallelExecution = true,
    MaxDegreeOfParallelism = 4,
    DefaultTimeout = 30000,
    StopOnFirstFailure = false
)]
```

### Per-Assembly Configuration

```csharp
// In AssemblyInfo.cs or any source file
[assembly: TestAssembly(
    DisplayName = "My Test Assembly",
    Category = "Integration",
    SetupClass = typeof(AssemblySetup),
    CleanupClass = typeof(AssemblyCleanup)
)]
```

## Custom Extensions

### Custom Assertions

```csharp
public static class CustomAssertions
{
    public static AssertionBuilder<string> IsValidEmail(this AssertionBuilder<string> builder)
    {
        return builder.Matches(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", "Expected a valid email address");
    }

    public static AssertionBuilder<T> SatisfiesCondition<T>(
        this AssertionBuilder<T> builder,
        Func<T, bool> predicate,
        string message)
    {
        // Custom assertion implementation
        return builder;
    }
}
```

### Custom Attributes

```csharp
[AttributeUsage(AttributeTargets.Method)]
public class DatabaseTestAttribute : Attribute, ITestMethodAttribute
{
    public void OnBeforeTest(ITestContext context)
    {
        // Setup database transaction
    }

    public void OnAfterTest(ITestContext context, TestResult result)
    {
        // Rollback database transaction
    }
}
```

## Migration from xUnit

### Attribute Mapping

| xUnit | UXUnit |
|-------|---------|
| `[Fact]` | `[Test]` |
| `[Theory]` + `[InlineData]` | `[Test]` + `[TestData]` |
| `[ClassData]` | `[TestDataSource]` |
| `IClassFixture<T>` | Constructor injection |
| `ICollectionFixture<T>` | `[ClassSetup]`/`[ClassCleanup]` |
| `[Skip]` | `Skip = true` property |

### Code Examples

**xUnit:**
```csharp
public class CalculatorTests
{
    [Fact]
    public void Add_TwoNumbers_ReturnsSum()
    {
        var result = new Calculator().Add(2, 3);
        Assert.Equal(5, result);
    }

    [Theory]
    [InlineData(1, 2, 3)]
    [InlineData(5, 7, 12)]
    public void Add_VariousInputs_ReturnsExpectedSum(int a, int b, int expected)
    {
        var result = new Calculator().Add(a, b);
        Assert.Equal(expected, result);
    }
}
```

**UXUnit:**
```csharp
[TestClass]
public class CalculatorTests
{
    [Test]
    public void Add_TwoNumbers_ReturnsSum()
    {
        var result = new Calculator().Add(2, 3);
        Assert.That(result).IsEqualTo(5);
    }

    [Test]
    [TestData(1, 2, 3)]
    [TestData(5, 7, 12)]
    public void Add_VariousInputs_ReturnsExpectedSum(int a, int b, int expected)
    {
        var result = new Calculator().Add(a, b);
        Assert.That(result).IsEqualTo(expected);
    }
}
```