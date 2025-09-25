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
    [Fact]
    public void SimpleMathTest()
    {
        var result = 2 + 2;
        Assert.Equal(4, result);
    }

    [Fact]
    public async Task AsyncTest()
    {
        await Task.Delay(10);
        Assert.True(true);
    }
}
```

## Parameterized Tests

Use `[Theory]` and `[InlineData]` attributes for parameterized tests:

```csharp
[TestClass]
public class CalculatorTests
{
    [Theory]
    [InlineData(1, 2, 3)]
    [InlineData(5, 7, 12)]
    [InlineData(-1, 1, 0)]
    [InlineData(0, 0, 0)]
    public void Add_ReturnsExpectedResult(int a, int b, int expected)
    {
        var calculator = new Calculator();
        var result = calculator.Add(a, b);
        Assert.Equal(expected, result);
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

    [Fact]
    public void CanInsertUser()
    {
        var user = new User("John Doe", "john@example.com");
        _context.Users.Add(user);
        _context.SaveChanges();

        Assert.True(user.Id > 0);
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
    [Fact]
    public void StringAssertions()
    {
        var text = "Hello, World!";
        
        Assert.NotNull(text);
        Assert.Contains("World", text);
        Assert.StartsWith("Hello", text);
        Assert.Equal(13, text.Length);
    }

    [Fact]
    public void CollectionAssertions()
    {
        var numbers = new[] { 1, 2, 3, 4, 5 };
        
        Assert.NotEmpty(numbers);
        Assert.Equal(5, numbers.Length);
        Assert.Contains(3, numbers);
        Assert.Equal(numbers.OrderBy(x => x), numbers);
    }

    [Fact]
    public void NumericAssertions()
    {
        var value = 42.0;
        
        Assert.True(value > 40);
        Assert.True(value < 50);
        Assert.InRange(value, 40, 50);
        Assert.Equal(42.1, value, precision: 1);
    }

    [Fact]
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
    [Theory]
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
        
        Assert.Equal(expected, result, precision: 3);
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
    [Theory]
    [TestDataSource(nameof(GetUserTestData))]
    public void ValidateUser_WithVariousInputs_ReturnsExpectedResult(
        string name, string email, bool expectedValid, string expectedError)
    {
        var validator = new UserValidator();
        var result = validator.Validate(name, email);
        
        Assert.Equal(expectedValid, result.IsValid);
        if (!expectedValid)
        {
            Assert.Contains(expectedError, result.ErrorMessage);
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
[Fact]
public void ValidateEmailFormat()
{
    var email = "user@example.com";
    Assert.True(email.IsValidEmail());
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
    [Fact]
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
    [Fact]
    public void TestOne() { }
    
    [Fact] 
    public void TestTwo() { }
}

// Group tests that access shared resources
[TestClass]
public class ResourceTests
{
    [Fact]
    [Parallel(Group = "FileSystem")]
    public void TestFileOperation1() { }
    
    [Fact]
    [Parallel(Group = "FileSystem")]
    public void TestFileOperation2() { }
    
    [Fact] // Can run in parallel with other ungrouped tests
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
[Fact]
public void RegisterUser_WithValidData_CreatesUserSuccessfully()
{
    // Clear, descriptive test name
}
```

### 3. Follow AAA Pattern
```csharp
[Fact]
public void CalculateDiscount_ForPremiumCustomer_AppliesCorrectRate()
{
    // Arrange
    var customer = new Customer { IsPremium = true };
    var calculator = new DiscountCalculator();
    
    // Act
    var discount = calculator.Calculate(customer, 100);
    
    // Assert
    Assert.Equal(10, discount);
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
[Fact]
public async Task ProcessAsync_WithValidInput_CompletesSuccessfully()
{
    var processor = new AsyncProcessor();
    
    await processor.ProcessAsync("valid-input");
    
    Assert.True(processor.IsCompleted);
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
public class TestClass
{
    [Fact]
    public void Test1() { }
    
    [Theory]
    [InlineData(1, 2, 3)]
    public void Test2(int a, int b, int expected) { }
}
```

### Assertion Updates

```csharp
// Before (xUnit)
Assert.Equal(expected, actual);
Assert.True(condition);
Assert.Throws<Exception>(() => method());

// After (UXUnit using xUnit assertions)  
Assert.Equal(expected, actual);
Assert.True(condition);
Assert.Throws<Exception>(() => method());
```

This should get you up and running with UXUnit! Check out the other documentation files for more detailed information about the framework's architecture and advanced features.