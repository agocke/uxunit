using System;
using System.IO;
using Microsoft.CodeAnalysis.Testing;

namespace NXTest.Generators.Tests;

internal static class Config
{
    public static ReferenceAssemblies Net10Ref =>
        new ReferenceAssemblies(
            "net10.0",
            new PackageIdentity("Microsoft.NETCore.App.Ref", "10.0.0"),
            Path.Combine("ref", "net10.0"))
        .WithNuGetConfigFilePath(Path.Combine(
            AppContext.BaseDirectory,
            "NuGet.config"));
}