using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.Testing;

namespace UXUnit.Generators.Tests;

internal static class Config
{
    public static ReferenceAssemblies Net10Ref =>
        new ReferenceAssemblies(
            "net10.0",
            new PackageIdentity("Microsoft.NETCore.App.Ref", "10.0.0-rc.2.25502.107"),
            Path.Combine("ref", "net10.0"))
        .WithNuGetConfigFilePath(Path.Combine(
            GetDirectoryPath(),
            "..",
            "..",
            "NuGet.config"));

    private static string GetDirectoryPath([CallerFilePath]string path = "") => Path.GetDirectoryName(path)!;
}