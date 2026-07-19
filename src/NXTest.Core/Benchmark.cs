using System.Runtime.CompilerServices;

namespace NXTest;

/// <summary>
/// Utilities for writing benchmarks whose results cannot be removed as dead code.
/// </summary>
public static class Benchmark
{
    /// <summary>
    /// Makes a computed value observable to the runtime without boxing it.
    /// </summary>
    /// <typeparam name="T">The type of value to consume.</typeparam>
    /// <param name="value">The value produced by the benchmarked operation.</param>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Consume<T>(T value) { }
}
