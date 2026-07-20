# NXTest.Runtime

The execution engine and runner for the NXTest testing framework, built on
Microsoft.Testing.Platform.

Use it as the entry point of a test executable, passing the generated test registry:

```csharp
using NXTest.Generated;
using NXTest.Runtime;

return await TestFramework.RunAsync(args, TestRegistry.GetAllTests());
```

`TestExecutionOptions` controls parallelism (`ParallelMode`), stop-on-first-failure, and
related behavior.

Most users should install the **NXTest** meta-package, which includes this package along
with the core attributes and source generator.
