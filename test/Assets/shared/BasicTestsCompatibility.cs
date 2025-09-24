using Xunit; // Always use XUnit assertions

namespace UXUnit.Compatibility.Tests;

/// <summary>
/// Demonstrates compatibility between XUnit and UXUnit test frameworks.
/// This class can be compiled and executed with either framework.
/// </summary>
#if UXUNIT
[UXUnit.TestClass]
#endif
public class BasicTestsCompatibility
#if XUNIT
    : IDisposable
#endif
{
    #if UXUNIT
    [UXUnit.Test]
    #else
    [Fact]
    #endif
    public void SimpleTest_ShouldPass()
    {
        // Using XUnit assertions in both frameworks
        Assert.True(true);
        Assert.Equal(42, 42);
    }

    #if UXUNIT
    [UXUnit.Test]
    #else
    [Fact]
    #endif
    public void StringTest_ShouldValidateEquality()
    {
        var expected = "Hello, World!";
        var actual = "Hello, World!";
        
        Assert.NotNull(actual);
        Assert.Equal(expected, actual);
    }

    #if UXUNIT
    [UXUnit.Test]
    #else
    [Fact]
    #endif
    public void CollectionTest_ShouldValidateContents()
    {
        var numbers = new[] { 1, 2, 3, 4, 5 };
        
        Assert.NotEmpty(numbers);
        Assert.Equal(5, numbers.Length);
        Assert.Contains(3, numbers);
    }

    #if UXUNIT
    [UXUnit.Test]
    #else
    [Fact]
    #endif
    public void ExceptionTest_ShouldThrowArgumentException()
    {
        Action throwAction = () => throw new ArgumentException("Test exception");
        var ex = Assert.Throws<ArgumentException>(throwAction);
        Assert.NotNull(ex);
    }

    // Parameterized tests - these require different approaches
    #if UXUNIT
    [UXUnit.Test]
    [UXUnit.TestData(1, 2, 3)]
    [UXUnit.TestData(5, 10, 15)]
    [UXUnit.TestData(-1, -2, -3)]
    public void ParameterizedTest_Addition(int a, int b, int expected)
    #else
    [Theory]
    [InlineData(1, 2, 3)]
    [InlineData(5, 10, 15)]
    [InlineData(-1, -2, -3)]
    public void ParameterizedTest_Addition(int a, int b, int expected)
    #endif
    {
        var result = a + b;
        Assert.Equal(expected, result);
    }

    // Setup/Teardown methods have different names
    #if UXUNIT
    [UXUnit.Setup]
    public void SetupMethod()
    #else
    public BasicTestsCompatibility()
    #endif
    {
        // Setup logic here
        // In XUnit, constructor serves as setup
        // In UXUnit, we use [Setup] attribute
    }

    #if UXUNIT
    [UXUnit.Cleanup]
    public void CleanupMethod()
    {
        // Cleanup logic for UXUnit
    }
    #else
    public void Dispose()
    {
        // Cleanup logic for XUnit
    }
    #endif
}