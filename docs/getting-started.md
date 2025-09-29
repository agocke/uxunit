# Getting Started

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
global using static UXUnit.Assert;
```

## Your First Test

Create a simple test class:

```csharp
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

Use standard xUnit patterns for test initialization and cleanup:

```csharp
public class DatabaseTests : IDisposable
{
    private readonly DatabaseContext _context;

    public DatabaseTests()
    {
        var connectionString = "Server=localhost;Database=TestDb;";
        _context = new DatabaseContext(connectionString);
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

    public void Dispose()
    {
        _context?.Database.RollbackTransaction();
        _context?.Dispose();
    }
}
```

## Advanced Assertions

UXUnit provides a fluent assertion API:

```csharp
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

Tests integrate with Visual Studio Test Explorer and other .NET test runners through the standard test adapter interface.

## Best Practices

### 1. Organize Tests by Feature
```csharp
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

### 4. Use Constructor/Dispose Pattern for Setup
```csharp
public class ServiceTests : IDisposable
{
    private readonly IService _service;
    private readonly Mock<IDependency> _mockDependency;

    public ServiceTests()
    {
        _mockDependency = new Mock<IDependency>();
        _service = new Service(_mockDependency.Object);
    }

    [Fact]
    public void SomeTest()
    {
        // Test implementation
    }

    public void Dispose()
    {
        // Cleanup if needed
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

## Migration from xUnit

### No Migration Required

Your existing xUnit tests work directly without any changes:

```csharp
// xUnit tests work as-is
public class TestClass
{
    [Fact]
    public void Test1() { }
    
    [Theory]
    [InlineData(1, 2, 3)]
    public void Test2(int a, int b, int expected) { }
}
```

### Assertions

Use standard xUnit assertions:

```csharp
Assert.Equal(expected, actual);
Assert.True(condition);
Assert.Throws<Exception>(() => method());
```

This should get you up and running! Your existing xUnit tests will work without any modifications.