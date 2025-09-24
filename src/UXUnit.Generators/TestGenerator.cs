using Microsoft.CodeAnalysis;

namespace UXUnit.Generators;

/// <summary>
/// Source generator that creates test runners for UXUnit tests.
/// This is a minimal stub implementation for compatibility testing.
/// </summary>
[Generator]
public class TestGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
        // Initialization logic will be implemented later
    }

    public void Execute(GeneratorExecutionContext context)
    {
        // For now, this is a stub to allow compilation
        // Full implementation will generate test discovery and execution code
    }
}