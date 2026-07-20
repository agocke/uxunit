# NXTest - Source-Generated Unit Testing Framework

[![CI](https://github.com/agocke/nxtest/actions/workflows/ci.yml/badge.svg)](https://github.com/agocke/nxtest/actions/workflows/ci.yml)

A modern, source-generated replacement for xUnit that provides compile-time test discovery and simple functionality.

## Overview

NXTest leverages C# source generators to create a small, simple testing framework that eliminates runtime reflection and provides compile-time test validation. It aims to be an easy, drop-in replacement for the core of xUnit rather than a large, feature-rich framework.

## Key Features

- **Source-Generated Test Discovery**: Tests are discovered at compile time, eliminating runtime reflection overhead
- **Compile-Time Validation**: Test method signatures and attributes are validated during compilation
- **Rich Assertion Library**: Uses `xunit.assert` for compatibility and comprehensive assertion capabilities
- **Parameterized Tests**: Full support for data-driven tests with source generators
- **Basic Benchmarks**: Warmup and measured iterations with timing statistics

## Documentation

- [Design Document](./docs/design.md) - High-level architecture and design decisions
- [Specification](./docs/specification.md) - Detailed API and behavior specifications
- [Data Model](./docs/data-model.md) - Internal data structures and models
- [Getting Started](./docs/getting-started.md) - Quick start guide
- [Benchmarking](./docs/benchmarking.md) - Benchmark usage, semantics, and limitations

## Project Structure

```
├── src/
│   ├── NXTest/                # Meta-package that pulls in all NXTest packages
│   ├── NXTest.Core/           # Core framework types and attributes
│   ├── NXTest.Generators/     # Source generators
│   └── NXTest.Runtime/        # Test runner and execution engine
├── test/
│   ├── Assets/                # Compatibility assets (NXTestCompat, XUnitCompat, shared)
│   ├── NXTest.CompatibilityTests/
│   ├── NXTest.Generators.Tests/
│   └── NXTest.Runtime.Tests/
└── docs/                      # Documentation
```

## Quick Example

```csharp
using Xunit;

public class CalculatorTests
{
    [Fact]
    public void Add_TwoNumbers_ReturnsSum()
    {
        var calculator = new Calculator();
        var result = calculator.Add(2, 3);
        Assert.Equal(5, result);
    }

    [Theory]
    [InlineData(1, 2, 3)]
    [InlineData(5, 7, 12)]
    [InlineData(-1, 1, 0)]
    public void Add_VariousInputs_ReturnsExpectedSum(int a, int b, int expected)
    {
        var calculator = new Calculator();
        var result = calculator.Add(a, b);
        Assert.Equal(expected, result);
    }
}
```

Benchmarks use `[Bench]` and report calibrated per-operation statistics:

```csharp
[Bench]
public void ParsePayload()
{
    Benchmark.Consume(JsonSerializer.Deserialize<Message>(payload));
}
```

Benchmarks are excluded from normal test runs. Run the benchmark project directly
in Release mode so successful timing details are shown:

```bash
dotnet run --project perf/bench/bench.csproj -c Release -- \
  --bench
```

Benchmarks can use theory-style `[InlineData]`; every data row is calibrated and
reported independently.

## License

MIT License - see [LICENSE](LICENSE) for details.