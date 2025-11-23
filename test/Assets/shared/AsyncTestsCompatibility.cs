using Xunit;

namespace UXUnit.Compatibility.Tests;

/// <summary>
/// Demonstrates async test compatibility between XUnit and UXUnit.
/// </summary>
public class AsyncTestsCompatibility
{
    [Fact]
    public async Task AsyncTest_ShouldComplete()
    {
        await Task.Delay(10);
        Assert.True(true);
    }

    [Fact]
    public async Task AsyncTest_ShouldThrowException()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await Task.Delay(5);
            throw new InvalidOperationException("Test exception");
        });
    }

    [Theory]
    [InlineData(100)]
    [InlineData(200)]
    [InlineData(50)]
    public async Task ParameterizedAsyncTest_WithDelay(int delayMs)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        await Task.Delay(delayMs);
        stopwatch.Stop();

        // Allow some tolerance for timing
        Assert.True(stopwatch.ElapsedMilliseconds >= delayMs - 50);
    }
}
