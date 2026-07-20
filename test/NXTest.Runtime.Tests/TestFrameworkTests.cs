using XunitAssert = Xunit.Assert;
using XunitFact = Xunit.FactAttribute;

namespace NXTest.Runtime.Tests;

public class TestFrameworkTests
{
    [XunitFact]
    public void GetPlatformArguments_StripsBenchmarkFlag()
    {
        var arguments = TestFramework.GetPlatformArguments(["--bench"], runBenchmarks: true);

        XunitAssert.Empty(arguments);
    }

    [XunitFact]
    public void GetPlatformArguments_DoesNotInjectOutputVerbosity()
    {
        var arguments = TestFramework.GetPlatformArguments(
            ["--bench", "--timeout", "30s"],
            runBenchmarks: true
        );

        XunitAssert.Equal(["--timeout", "30s"], arguments);
    }

    [XunitFact]
    public void GetPlatformArguments_TestModePassesArgumentsThrough()
    {
        var arguments = TestFramework.GetPlatformArguments([], runBenchmarks: false);

        XunitAssert.Empty(arguments);
    }
}
