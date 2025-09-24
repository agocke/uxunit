using Xunit;

namespace UXUnit.Compatibility.Tests;

/// <summary>
/// Demonstrates async test compatibility between XUnit and UXUnit.
/// </summary>
#if UXUNIT
[UXUnit.TestClass]
#endif
public class AsyncTestsCompatibility
{
    #if UXUNIT
    [UXUnit.Test]
    #else
    [Fact]
    #endif
    public async Task AsyncTest_ShouldComplete()
    {
        await Task.Delay(10);
        Assert.True(true);
    }

    #if UXUNIT
    [UXUnit.Test]
    #else
    [Fact]
    #endif
    public async Task AsyncTest_ShouldThrowException()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await Task.Delay(5);
            throw new InvalidOperationException("Test exception");
        });
    }

    #if UXUNIT
    [UXUnit.Test]
    [UXUnit.TestData(100)]
    [UXUnit.TestData(200)]
    [UXUnit.TestData(50)]
    public async Task ParameterizedAsyncTest_WithDelay(int delayMs)
    #else
    [Theory]
    [InlineData(100)]
    [InlineData(200)]
    [InlineData(50)]
    public async Task ParameterizedAsyncTest_WithDelay(int delayMs)
    #endif
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        await Task.Delay(delayMs);
        stopwatch.Stop();
        
        // Allow some tolerance for timing
        Assert.True(stopwatch.ElapsedMilliseconds >= delayMs - 50);
    }
}