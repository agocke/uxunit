using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace NXTest.Generators.Tests;

public class BasicGenerationTests
{
    [Fact]
    public Task GeneratesMetadataForSimpleFact()
    {
        var source = """
using NXTest;

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

    [Fact]
    public Task GeneratesMetadataForTheoryWithInlineData()
    {
        var source = """
using NXTest;

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

    [Fact]
    public async Task GeneratesCompilableMetadataForMultiArgumentInlineDataWithNull()
    {
        var source = """
using NXTest;

public class NullableTheoryTests
{
    [Theory]
    [InlineData(null, "bob@example.com", 2)]
    public void SendsEmail(string? displayName, string email, int retryCount)
    {
    }
}
""";
        var compilation = await TestHelpers.CreateCompilation(source);
        var driver = TestHelpers.RunGenerator(compilation);
        var result = driver.GetRunResult();
        var generatedSource = result.GeneratedTrees
            .Single(tree => tree.FilePath.EndsWith("NullableTheoryTests_Metadata.g.cs"))
            .ToString();
        var generatedCompilation = compilation.AddSyntaxTrees(result.GeneratedTrees);
        var compilationErrors = generatedCompilation.GetDiagnostics()
            .Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
            .ToList();

        if (!generatedSource.Contains(
                "Arguments = ((string?, string, int))(null, \"bob@example.com\", 2),"))
        {
            throw new System.InvalidOperationException("Generated metadata did not cast to a typed tuple.");
        }

        if (compilationErrors.Count != 0)
        {
            throw new System.InvalidOperationException("Generated metadata did not compile.");
        }
    }

    [Fact]
    public Task GeneratesMetadataForMultipleTestMethods()
    {
        var source = """
using NXTest;

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

    [Fact]
    public Task GeneratesMetadataForAsyncTest()
    {
        var source = """
using System.Threading.Tasks;
using NXTest;

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

    [Fact]
    public Task GeneratesMetadataForStaticFactMethod()
    {
        var source = """
using NXTest;

public class StaticMethodTests
{
    [Fact]
    public static void StaticPassingTest()
    {
        // Test implementation
    }
}
""";
        return VerifyGenerator(source);
    }

    [Fact]
    public Task GeneratesMetadataForStaticTestClass()
    {
        var source = """
using NXTest;

public static class StaticTestClass
{
    [Fact]
    public static void StaticFact()
    {
        // Test implementation
    }
}
""";
        return VerifyGenerator(source);
    }

    [Fact]
    public Task GeneratesMetadataForStaticTheoryMethod()
    {
        var source = """
using NXTest;

public static class StaticTheoryClass
{
    [Theory]
    [InlineData(1, 2, 3)]
    [InlineData(5, 7, 12)]
    public static void StaticAddTest(int a, int b, int sum)
    {
        // Test implementation
    }
}
""";
        return VerifyGenerator(source);
    }

    [Fact]
    public Task GeneratesSeparateFilesForDifferentClasses()
    {
        var source = """
using NXTest;

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

    private static async Task VerifyGenerator(string source, [CallerMemberName] string? testName = null)
    {
        var compilation = await TestHelpers.CreateCompilation(source, assemblyName: testName);

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

        // Compile the generated code to check for compilation errors
        var generatedCompilation = compilation.AddSyntaxTrees(result.GeneratedTrees);
        var compilationDiagnostics = generatedCompilation.GetDiagnostics();
        var compilationErrors = compilationDiagnostics
            .Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
            .ToList();

        if (compilationErrors.Any())
        {
            foreach (var error in compilationErrors)
            {
                System.Console.WriteLine($"Generated Code Compilation Error: {error}");
            }
            throw new System.Exception($"Generated code has {compilationErrors.Count} compilation error(s)");
        }

        await GeneratorSnapshotVerifier.Verify(driver, $"{nameof(BasicGenerationTests)}.{testName}");
    }
}
