# NXTest.Generators

The C# source generator for the NXTest testing framework.

At compile time it discovers methods marked with `[Fact]` and `[Theory]`, then generates
strongly-typed test metadata and a `NXTest.Generated.TestRegistry` used to run the tests —
no runtime reflection required.

Reference it as an analyzer:

```xml
<PackageReference Include="NXTest.Generators" PrivateAssets="all"
                  IncludeAssets="runtime; build; native; contentfiles; analyzers" />
```

Most users should install the **NXTest** meta-package, which wires this up automatically.
