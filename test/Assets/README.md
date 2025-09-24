# UXUnit/XUnit Compatibility Tests

This directory contains compatibility test projects that validate UXUnit works identically to XUnit when executing the same test code.

## Project Structure

- **`UXUnitCompat/`** - Test project using UXUnit framework
- **`XUnitCompat/`** - Test project using XUnit framework  
- **`shared/`** - Shared test code that works with both frameworks

## Shared Test Code

The shared test files use conditional compilation (`#if UXUNIT`) to switch between framework-specific attributes while keeping the same test logic:

```csharp
#if UXUNIT
[UXUnit.Test]
#else
[Fact]
#endif
public void MyTest()
{
    // Same test logic for both frameworks
    Assert.True(true);
}
```

This approach ensures:
- ✅ Identical test behavior across frameworks
- ✅ Same assertions and test logic
- ✅ Framework-specific attributes and setup/teardown

## Validation Script

Use `./compare-compat-outputs.sh` to validate compatibility:

```bash
# Show what UXUnit should produce (XUnit baseline)
./compare-compat-outputs.sh baseline

# Compare outputs (once UXUnit generator is implemented)
./compare-compat-outputs.sh compare

# Show help
./compare-compat-outputs.sh help
```

## Expected Behavior

When the UXUnit generator is fully implemented:

1. **UXUnitCompat** should build and run successfully
2. **Both projects** should produce identical test results
3. **All tests** should pass with the same names and outcomes
4. **Test counts** should match exactly

## Current Status

- ✅ **XUnitCompat** - Works correctly, provides baseline
- ⏳ **UXUnitCompat** - Waiting for generator implementation
- ✅ **Comparison Script** - Ready to validate once generator is complete

## Test Coverage

The compatibility tests include:

- **Basic Tests**: Simple assertions, collections, exceptions
- **Parameterized Tests**: Data-driven test cases  
- **Async Tests**: Asynchronous test methods
- **Setup/Teardown**: Framework-specific initialization/cleanup

## Integration

This validation should be part of the UXUnit development workflow:

1. Implement UXUnit generator features
2. Run `./compare-compat-outputs.sh compare`
3. Fix any compatibility issues until outputs match exactly
4. Repeat for new features

This ensures UXUnit maintains full compatibility with XUnit behavior.