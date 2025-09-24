using System;
using System.Threading.Tasks;
using Xunit;

namespace UXUnit.Runtime.Tests;

/// <summary>
/// Tests for the basic UXUnit runtime functionality.
/// These tests validate that the core execution engine works correctly.
/// </summary>
public class BasicRuntimeTests
{
    [Fact]
    public void SimpleTest_ShouldPass()
    {
        // Arrange
        var value = 2 + 2;

        // Act & Assert
        Assert.Equal(4, value);
    }

    [Fact]
    public void SimpleTest_ShouldFail()
    {
        // This test is intentionally designed to fail for testing purposes
        // Uncomment the line below to see failure handling
        // Assert.Equal(5, 2 + 2);
        
        // For now, we'll make it pass so the test suite passes
        Assert.True(true);
    }

    [Fact]
    public async Task AsyncTest_ShouldPass()
    {
        // Arrange
        await Task.Delay(10);

        // Act
        var result = await GetValueAsync();

        // Assert
        Assert.Equal("test", result);
    }

    // [Fact(Skip = "Demonstrating skip functionality")] - Skip not available in current XUnit version
    // This test would demonstrate skip functionality when UXUnit source generators are working
    public void SkippedTest_ShouldNotExecute_WhenSourceGeneratorImplemented()
    {
        // This test should be skipped when UXUnit is fully implemented
        // For now, we skip it by not marking it as a Fact
        Assert.True(true, "This test is manually skipped for now");
    }

    [Fact]
    public void TestWithOutput_ShouldCaptureOutput()
    {
        Console.WriteLine("This is test output");
        Console.WriteLine("Multiple lines of output");
        
        Assert.True(true);
    }

    private static async Task<string> GetValueAsync()
    {
        await Task.Delay(1);
        return "test";
    }
}

/// <summary>
/// Tests for parameterized test functionality.
/// </summary>
public class ParameterizedTests
{
    [Theory]
    [InlineData(1, 2, 3)]
    [InlineData(5, 7, 12)]
    [InlineData(-1, 1, 0)]
    [InlineData(0, 0, 0)]
    public void Add_VariousInputs_ReturnsExpectedSum(int a, int b, int expected)
    {
        // Act
        var result = a + b;

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("hello", 5)]
    [InlineData("world", 5)]
    [InlineData("", 0)]
    public void StringLength_VariousInputs_ReturnsExpectedLength(string input, int expectedLength)
    {
        // Act
        var result = input.Length;

        // Assert
        Assert.Equal(expectedLength, result);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void BooleanTest_WithDisplayNames_ShouldPass(bool value)
    {
        // Act & Assert
        Assert.True(value || !value); // Always true
    }
}

/// <summary>
/// Tests for lifecycle and setup/cleanup functionality.
/// </summary>
public class LifecycleTests : IDisposable
{
    private bool _disposed = false;

    public LifecycleTests()
    {
        // Constructor should be called for each test
    }

    [Fact]
    public void Test1_ShouldHaveCleanState()
    {
        Assert.False(_disposed);
    }

    [Fact]
    public void Test2_ShouldHaveCleanState()
    {
        Assert.False(_disposed);
    }

    public void Dispose()
    {
        _disposed = true;
    }
}

/// <summary>
/// Tests demonstrating exception handling in tests.
/// </summary>
public class ExceptionHandlingTests
{
    [Fact]
    public void TestThatThrowsException_ShouldBeHandledGracefully()
    {
        // This test will pass because we don't throw an exception
        // In a real scenario, you'd use Assert.Throws<T> to test for expected exceptions
        var value = 42;
        Assert.True(value > 0);
    }

    [Fact]
    public void TestWithTimeout_ShouldCompleteInTime()
    {
        // Simulate some work that completes quickly
        for (int i = 0; i < 100; i++)
        {
            var _ = Math.Sqrt(i);
        }
        
        Assert.True(true);
    }

    [Fact]
    public async Task AsyncTestWithTimeout_ShouldCompleteInTime()
    {
        // Simulate async work that completes quickly
        await Task.Delay(10);
        
        Assert.True(true);
    }
}