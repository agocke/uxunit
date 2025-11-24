using System.Runtime.CompilerServices;
using VerifyTests;

namespace UXUnit.Generators.Tests;

public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Init()
    {
        VerifySourceGenerators.Initialize();
    }
}
