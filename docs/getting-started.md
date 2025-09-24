# Getting Started with UXUnit

## Installation

### Package References

Add the following package references to your test project:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="UXUnit.Core" Version="1.0.0" />
    <PackageReference Include="UXUnit.Generators" Version="1.0.0">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
    </PackageReference>
    <PackageReference Include="UXUnit.Assertions" Version="1.0.0" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
  </ItemGroup>

  <!-- Reference your code under test -->
  <ItemGroup>
    <ProjectReference Include="../YourProject/YourProject.csproj" />
  </ItemGroup>
</Project>
```

### Global Usings (Optional)

Add common usings to a `GlobalUsings.cs` file:

```csharp
global using UXUnit;
global using UXUnit.Assertions;
global using static UXUnit.Assert;
```

## Your First Test

Create a simple test class:

```csharp
[TestClass]
public class BasicTests
{
    [Test]
    public void SimpleMathTest()
    {
        var result = 2 + 2;
        Assert.That(result).IsEqualTo(4);
    }

    [Test]
    public async Task AsyncTest()
    {
        await Task.Delay(10);
        Assert.That(true).IsTrue();
    }
}
```

## Parameterized Tests

Use `[TestData]` attributes for parameterized tests:

```csharp
[TestClass]
public class CalculatorTests
{
    [Test]
    [TestData(1, 2, 3)]
    [TestData(5, 7, 12)]
    [TestData(-1, 1, 0)]
    [TestData(0, 0, 0)]
    public void Add_ReturnsExpectedResult(int a, int b, int expected)
    {
        var calculator = new Calculator();
        var result = calculator.Add(a, b);
        Assert.That(result).IsEqualTo(expected);
    }
}
```

## Setup and Cleanup

Use lifecycle methods for test initialization and cleanup:

```csharp
[TestClass]
public class DatabaseTests
{
    private DatabaseContext _context;
    private static string _connectionString;

    [ClassSetup]
    public static void InitializeTestSuite()
    {
        _connectionString = "Server=localhost;Database=TestDb;";
        // Initialize test database
    }

    [Setup]
    public void SetupTest()
    {
        _context = new DatabaseContext(_connectionString);
        _context.Database.BeginTransaction();
    }

    [Test]
    public void CanInsertUser()
    {
        var user = new User("John Doe", "john@example.com");
        _context.Users.Add(user);
        _context.SaveChanges();

        Assert.That(user.Id).IsGreaterThan(0);
    }

    [Cleanup]
    public void CleanupTest()
    {
        _context?.Database.RollbackTransaction();
        _context?.Dispose();
    }

    [ClassCleanup]
    public static void CleanupTestSuite()
    {
        // Clean up test database
    }
}
```

## Advanced Assertions

UXUnit provides a fluent assertion API:

```csharp
[TestClass]
public class AssertionExamples
{
    [Test]
    public void StringAssertions()
    {
        var text = "Hello, World!";
        
        Assert.That(text)
            .IsNotNull()
            .Contains("World")
            .StartsWith("Hello")
            .HasLength(13);
    }

    [Test]
    public void CollectionAssertions()
    {
        var numbers = new[] { 1, 2, 3, 4, 5 };
        
        Assert.That(numbers)
            .IsNotEmpty()
            .HasCount(5)
            .Contains(3)
            .IsOrdered();
    }

    [Test]
    public void NumericAssertions()
    {
        var value = 42.0;
        
        Assert.That(value)
            .IsGreaterThan(40)
            .IsLessThan(50)
            .IsBetween(40, 50)
            .IsCloseTo(42.1, tolerance: 0.2);
    }

    [Test]
    public void ExceptionAssertions()
    {
        var calculator = new Calculator();
        
        Assert.Throws<ArgumentException>(() => 
            calculator.Divide(10, 0));
    }
}
```

## Test Data from External Sources

### CSV Data

```csharp
[TestClass]
public class CsvDataTests
{
    [Test]
    [CsvData("testdata/calculations.csv")]
    public void Calculate_FromCsv_ReturnsExpected(int a, int b, string operation, double expected)
    {
        var calculator = new Calculator();
        double result = operation switch
        {
            "add" => calculator.Add(a, b),
            "subtract" => calculator.Subtract(a, b),
            "multiply" => calculator.Multiply(a, b),
            "divide" => calculator.Divide(a, b),
            _ => throw new ArgumentException($"Unknown operation: {operation}")
        };
        
        Assert.That(result).IsCloseTo(expected, 0.001);
    }
}
```

CSV file (`testdata/calculations.csv`):
```csv
a,b,operation,expected
2,3,add,5
5,2,subtract,3
4,3,multiply,12
10,2,divide,5
```

### Method Data Source

```csharp
[TestClass]
public class DataSourceTests
{
    [Test]
    [TestDataSource(nameof(GetUserTestData))]
    public void ValidateUser_WithVariousInputs_ReturnsExpectedResult(
        string name, string email, bool expectedValid, string expectedError)
    {
        var validator = new UserValidator();
        var result = validator.Validate(name, email);
        
        Assert.That(result.IsValid).IsEqualTo(expectedValid);
        if (!expectedValid)
        {
            Assert.That(result.ErrorMessage).Contains(expectedError);
        }
    }

    public static IEnumerable<object[]> GetUserTestData()
    {
        yield return new object[] { "John Doe", "john@example.com", true, "" };
        yield return new object[] { "", "john@example.com", false, "Name is required" };
        yield return new object[] { "John Doe", "invalid-email", false, "Invalid email" };
        yield return new object[] { "John Doe", "", false, "Email is required" };
    }
}
```

## Custom Attributes and Extensions

### Custom Assertion

```csharp
public static class CustomAssertions
{
    public static AssertionBuilder<string> IsValidEmail(this AssertionBuilder<string> builder)
    {
        var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        return builder.Satisfies(
            email => emailRegex.IsMatch(email ?? ""),
            "Expected a valid email address");
    }

    public static AssertionBuilder<T> Satisfies<T>(
        this AssertionBuilder<T> builder,
        Func<T, bool> predicate, 
        string message)
    {
        // Custom assertion implementation
        if (!predicate(builder.ActualValue))
        {
            throw new AssertionException(message);
        }
        return builder;
    }
}

// Usage
[Test]
public void ValidateEmailFormat()
{
    var email = "user@example.com";
    Assert.That(email).IsValidEmail();
}
```

### Custom Test Attribute

```csharp
[AttributeUsage(AttributeTargets.Method)]
public class DatabaseTestAttribute : Attribute, ITestMethodAttribute
{
    public void OnBeforeTest(ITestContext context)
    {
        // Start database transaction
        var connection = GetDatabaseConnection();
        connection.BeginTransaction();
        context.AddProperty("DatabaseTransaction", connection);
    }

    public void OnAfterTest(ITestContext context, TestResult result)
    {
        // Rollback database transaction
        if (context.GetProperty<IDbConnection>("DatabaseTransaction") is { } connection)
        {
            connection.Rollback();
            connection.Dispose();
        }
    }

    private static IDbConnection GetDatabaseConnection()
    {
        // Return database connection
        return new SqlConnection("connection-string");
    }
}

// Usage
[TestClass]
public class IntegrationTests
{
    [Test]
    [DatabaseTest]
    public void TestDatabaseOperation()
    {
        // Test will run within a transaction that gets rolled back
    }
}
```

## Parallel Execution

Control parallel execution at class and method levels:

```csharp
// Run tests in this class sequentially
[TestClass]
[Parallel(Execution = ParallelExecution.Disabled)]
public class SequentialTests
{
    [Test]
    public void TestOne() { }
    
    [Test] 
    public void TestTwo() { }
}

// Group tests that access shared resources
[TestClass]
public class ResourceTests
{
    [Test]
    [Parallel(Group = "FileSystem")]
    public void TestFileOperation1() { }
    
    [Test]
    [Parallel(Group = "FileSystem")]
    public void TestFileOperation2() { }
    
    [Test] // Can run in parallel with other ungrouped tests
    public void TestIndependentOperation() { }
}
```

## Configuration

Configure test execution behavior:

```csharp
// In AssemblyInfo.cs or any source file
[assembly: UXUnitConfiguration(
    ParallelExecution = true,
    MaxDegreeOfParallelism = 4,
    DefaultTimeout = 30000,
    StopOnFirstFailure = false
)]

[assembly: TestAssembly(
    DisplayName = "My Integration Tests",
    Category = "Integration"
)]
```

## Running Tests

### Command Line

```bash
# Run all tests
dotnet test

# Run tests with filter
dotnet test --filter "Category=Integration"
dotnet test --filter "ClassName~Database"

# Run with specific configuration
dotnet test --configuration Release --logger trx
```

### IDE Integration

UXUnit integrates with Visual Studio Test Explorer and other .NET test runners through the standard test adapter interface.

## Best Practices

### 1. Organize Tests by Feature
```csharp
[TestClass]
public class UserRegistrationTests
{
    // Group related tests together
}
```

### 2. Use Descriptive Test Names
```csharp
[Test]
public void RegisterUser_WithValidData_CreatesUserSuccessfully()
{
    // Clear, descriptive test name
}
```

### 3. Follow AAA Pattern
```csharp
[Test]
public void CalculateDiscount_ForPremiumCustomer_AppliesCorrectRate()
{
    // Arrange
    var customer = new Customer { IsPremium = true };
    var calculator = new DiscountCalculator();
    
    // Act
    var discount = calculator.Calculate(customer, 100);
    
    // Assert
    Assert.That(discount).IsEqualTo(10);
}
```

### 4. Use Setup Methods Wisely
```csharp
[TestClass]
public class ServiceTests
{
    private IService _service;
    private Mock<IDependency> _mockDependency;

    [Setup]
    public void Setup()
    {
        _mockDependency = new Mock<IDependency>();
        _service = new Service(_mockDependency.Object);
    }
}
```

### 5. Handle Async Tests Properly
```csharp
[Test]
public async Task ProcessAsync_WithValidInput_CompletesSuccessfully()
{
    var processor = new AsyncProcessor();
    
    await processor.ProcessAsync("valid-input");
    
    Assert.That(processor.IsCompleted).IsTrue();
}
```

## Migration from xUnit

### Attribute Mapping

Replace xUnit attributes with UXUnit equivalents:

```csharp
// Before (xUnit)
public class TestClass
{
    [Fact]
    public void Test1() { }
    
    [Theory]
    [InlineData(1, 2, 3)]
    public void Test2(int a, int b, int expected) { }
}

// After (UXUnit)
[TestClass]
public class TestClass
{
    [Test]
    public void Test1() { }
    
    [Test]
    [TestData(1, 2, 3)]
    public void Test2(int a, int b, int expected) { }
}
```

### Assertion Updates

```csharp
// Before (xUnit)
Assert.Equal(expected, actual);
Assert.True(condition);
Assert.Throws<Exception>(() => method());

// After (UXUnit)  
Assert.That(actual).IsEqualTo(expected);
Assert.That(condition).IsTrue();
Assert.Throws<Exception>(() => method());
```

This should get you up and running with UXUnit! Check out the other documentation files for more detailed information about the framework's architecture and advanced features.