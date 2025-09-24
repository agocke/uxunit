# UXUnit - Source-Generated Unit Testing Framework

A modern, source-generated replacement for xUnit that provides compile-time test discovery, enhanced performance, and improved developer experience.

## Overview

UXUnit leverages C# source generators to create a high-performance testing framework that eliminates runtime reflection and provides compile-time test validation.

## Key Features

- **Source-Generated Test Discovery**: Tests are discovered at compile time, eliminating runtime reflection overhead
- **Compile-Time Validation**: Test method signatures and attributes are validated during compilation
- **Zero Runtime Dependencies**: No heavy framework dependencies at runtime
- **Enhanced Performance**: Significantly faster test execution through pre-compiled test runners
- **Rich Assertion Library**: Uses `xunit.assert` for compatibility and comprehensive assertion capabilities
- **Parameterized Tests**: Full support for data-driven tests with source generators
- **Parallel Execution**: Built-in support for parallel test execution with fine-grained control
- **XUnit Compatibility**: Designed for easy migration from XUnit with shared assertion library

## Documentation

- [Design Document](./docs/design.md) - High-level architecture and design decisions
- [Specification](./docs/specification.md) - Detailed API and behavior specifications
- [Data Model](./docs/data-model.md) - Internal data structures and models
- [Getting Started](./docs/getting-started.md) - Quick start guide

## Project Structure

```
├── src/
│   ├── UXUnit.Core/           # Core framework types and interfaces
│   ├── UXUnit.Generators/     # Source generators
│   └── UXUnit.Runtime/        # Test runner and execution engine
├── test/
│   ├── Assets/XUnitCompatibility/ # XUnit compatibility demonstration
│   ├── UXUnit.Core.Tests/
│   ├── UXUnit.Generators.Tests/
│   └── UXUnit.Integration.Tests/
└── docs/                      # Documentation
```

## Quick Example

```csharp
using UXUnit;
using Xunit; // Use XUnit assertions

[TestClass]
public class CalculatorTests
{
    [Test]
    public void Add_TwoNumbers_ReturnsSum()
    {
        var calculator = new Calculator();
        var result = calculator.Add(2, 3);
        Assert.Equal(5, result); // XUnit assertion
    }

    [Test]
    [TestData(1, 2, 3)]
    [TestData(5, 7, 12)]
    [TestData(-1, 1, 0)]
    public void Add_VariousInputs_ReturnsExpectedSum(int a, int b, int expected)
    {
        var calculator = new Calculator();
        var result = calculator.Add(a, b);
        Assert.Equal(expected, result); // XUnit assertion
    }
}
```

## License

MIT License - see [LICENSE](LICENSE) for details.