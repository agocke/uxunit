using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace UXUnit.Generators.Tests;

public static class TestHelpers
{
    public static async Task<CSharpCompilation> CreateCompilation(string src, MetadataReference[]? additionalRefs = null, string? assemblyName = null)
    {
        additionalRefs ??= Array.Empty<MetadataReference>();
        IEnumerable<MetadataReference> refs = await Config.Net10Ref.ResolveAsync(null, default);
        refs = refs.Concat(additionalRefs);
        refs = refs.Append(MetadataReference.CreateFromFile(typeof(UXUnit.TestStatus).Assembly.Location));
        return CSharpCompilation.Create(
            assemblyName ?? "TestAssembly",
            new[] { CSharpSyntaxTree.ParseText(src) },
            references: refs,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithNullableContextOptions(NullableContextOptions.Enable));
    }

    public static GeneratorDriver RunGenerator(Compilation compilation)
    {
        var generator = new TestGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: new ISourceGenerator[] { generator.AsSourceGenerator() });
        return driver.RunGenerators(compilation);
    }
}
