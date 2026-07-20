# NXTest.Core

Core types for the NXTest source-generated testing framework.

Provides the test attributes — `[Fact]`, `[Theory]`, `[InlineData]` — and the metadata
types (`TestClassMetadata`, `TestMethodMetadata`, `TestCaseInfo`) that the NXTest source
generator emits and the runtime consumes.

```csharp
using NXTest;

public class MyTests
{
    [Fact]
    public void Works() { /* ... */ }
}
```

Most users should install the **NXTest** meta-package, which includes this package along
with the source generator and runtime.
