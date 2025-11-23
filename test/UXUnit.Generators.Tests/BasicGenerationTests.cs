using System.Linq;
using System.Threading.Tasks;
using Microsoft;
using VerifyXunit;
using XunitFact = Xunit.FactAttribute;

namespace UXUnit.Generators.Tests;

public class BasicGenerationTests
{
    [XunitFact]
    public Task GeneratesMetadataForSimpleFact()
    {
        var source = """
using UXUnit;

public class SimpleTests
{
    [Fact]
    public void PassingTest()
    {
        // Test implementation
    }
}
""";
        return VerifyGenerator(source);
    }

    [XunitFact]
    public Task GeneratesMetadataForTheoryWithInlineData()
    {
        var source = """
using UXUnit;

public class MathTests
{
    [Theory]
    [InlineData(1, 2, 3)]
    [InlineData(5, 7, 12)]
    public void AddTest(int a, int b, int sum)
    {
        // Test implementation
    }
}
""";
        return VerifyGenerator(source);
    }

    [XunitFact]
    public Task GeneratesMetadataForMultipleTestMethods()
    {
        var source = """
using UXUnit;

public class MixedTests
{
    [Fact]
    public void Test1()
    {
    }

    [Fact]
    public void Test2()
    {
    }

    [Theory]
    [InlineData("hello")]
    [InlineData("world")]
    public void StringTest(string input)
    {
    }
}
""";
        return VerifyGenerator(source);
    }

    [XunitFact]
    public Task GeneratesMetadataForAsyncTest()
    {
        var source = """
using System.Threading.Tasks;
using UXUnit;

public class AsyncTests
{
    [Fact]
    public async Task AsyncPassingTest()
    {
        await Task.CompletedTask;
    }
}
""";
        return VerifyGenerator(source);
    }

    [XunitFact]
    public Task GeneratesSeparateFilesForDifferentClasses()
    {
        var source = """
using UXUnit;

public class TestClass1
{
    [Fact]
    public void Test1()
    {
    }
}

public class TestClass2
{
    [Fact]
    public void Test2()
    {
    }
}
""";
        return VerifyGenerator(source);
    }

    private static async Task VerifyGenerator(string source)
    {
        var compilation = await TestHelpers.CreateCompilation(source);

        // Check for compilation errors
        var diagnostics = compilation.GetDiagnostics();
        var errors = diagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error).ToList();
        if (errors.Any())
        {
            foreach (var error in errors)
            {
                System.Console.WriteLine($"Compilation Error: {error}");
            }
        }

        var driver = TestHelpers.RunGenerator(compilation);

        // Check generator diagnostics
        var result = driver.GetRunResult();
        foreach (var diag in result.Diagnostics)
        {
            System.Console.WriteLine($"Generator Diagnostic: {diag}");
        }

        System.Console.WriteLine($"Generated {result.GeneratedTrees.Length} files");
        foreach (var tree in result.GeneratedTrees)
        {
            System.Console.WriteLine($"  - {tree.FilePath}");
        }

        await Verifier.Verify(driver)
            .UseDirectory("Snapshots")
            .ScrubLinesContaining("AssemblyName");  // Scrub assembly GUID from snapshots
    }
}
