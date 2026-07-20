using XunitAssert = Xunit.Assert;
using XunitFact = Xunit.FactAttribute;

namespace NXTest.Runtime.Tests;

public class TestFrameworkTests
{
    [XunitFact]
    public void GetPlatformArguments_BenchmarkModeDefaultsToDetailedOutput()
    {
        var arguments = TestFramework.GetPlatformArguments(["--bench"], runBenchmarks: true);

        XunitAssert.Equal(["--output", "Detailed"], arguments);
    }

    [XunitFact]
    public void GetPlatformArguments_BenchmarkModePreservesExplicitOutput()
    {
        var arguments = TestFramework.GetPlatformArguments(
            ["--bench", "--output", "Normal"],
            runBenchmarks: true
        );

        XunitAssert.Equal(["--output", "Normal"], arguments);
    }

    [XunitFact]
    public void GetPlatformArguments_TestModeDoesNotChangeOutput()
    {
        var arguments = TestFramework.GetPlatformArguments([], runBenchmarks: false);

        XunitAssert.Empty(arguments);
    }
}
