# NXTest

Meta-package for the NXTest source-generated testing framework. Installing it pulls in
everything you need: `NXTest.Core` (attributes), `NXTest.Generators` (compile-time test
discovery), and `NXTest.Runtime` (the test runner).

## Usage

Add the package (plus an assertion library such as `xunit.v3.assert`) to an executable
test project. The entry point is generated for you via Microsoft.Testing.Platform, so no
`Program.cs` is required:

```xml
<PropertyGroup>
  <OutputType>Exe</OutputType>
</PropertyGroup>
```

```csharp
using NXTest;

public class CalculatorTests
{
    [Fact]
    public void Add_ReturnsSum() => Assert.Equal(5, 2 + 3);

    [Theory]
    [InlineData(1, 2, 3)]
    [InlineData(-1, 1, 0)]
    public void Add_Cases(int a, int b, int expected) => Assert.Equal(expected, a + b);
}
```

Run with `dotnet test` or by executing the produced test app.
