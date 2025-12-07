#if UXUNIT
using UXUnit;
#endif
using Xunit;

namespace UXUnit.Compatibility.Tests;

/// <summary>
/// Demonstrates compatibility between XUnit and UXUnit test frameworks.
/// This class can be compiled and executed with either framework.
/// </summary>
public class BasicTestsCompatibility : IDisposable
{
    [Fact]
    public void SimpleTest_ShouldPass()
    {
        // Using XUnit assertions in both frameworks
        Assert.True(true);
        Assert.Equal(42, 42);
    }

    [Fact]
    public void StringTest_ShouldValidateEquality()
    {
        var expected = "Hello, World!";
        var actual = "Hello, World!";

        Assert.NotNull(actual);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CollectionTest_ShouldValidateContents()
    {
        var numbers = new[] { 1, 2, 3, 4, 5 };

        Assert.NotEmpty(numbers);
        Assert.Equal(5, numbers.Length);
        Assert.Contains(3, numbers);
    }

    [Fact]
    public void ExceptionTest_ShouldThrowArgumentException()
    {
        Action throwAction = () => throw new ArgumentException("Test exception");
        var ex = Assert.Throws<ArgumentException>(throwAction);
        Assert.NotNull(ex);
    }

    [Fact]
    public void FailingTest_ShouldFail()
    {
        // This test is intentionally designed to fail
        Assert.Equal(1, 2);
    }

    // Parameterized tests - these require different approaches
    [Theory]
    [InlineData(1, 2, 3)]
    [InlineData(5, 10, 15)]
    [InlineData(-1, -2, -3)]
    public void ParameterizedTest_Addition(int a, int b, int expected)
    {
        var result = a + b;
        Assert.Equal(expected, result);
    }

    // Setup/Teardown methods have different names
    public BasicTestsCompatibility()
    {
        // Setup logic here
        // In XUnit, constructor serves as setup
        // In UXUnit, we use [Setup] attribute
    }

    public void Dispose()
    {
        // Cleanup logic for XUnit
    }
}
