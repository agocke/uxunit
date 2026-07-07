# NXTest/XUnit Compatibility Tests

This directory contains compatibility test projects that validate NXTest works identically to XUnit when executing the same test code.

## Project Structure

- **`NXTestCompat/`** - Test project using NXTest framework
- **`XUnitCompat/`** - Test project using XUnit framework  
- **`shared/`** - Shared test code that works with both frameworks

## Shared Test Code

The shared test files use conditional compilation (`#if NXTEST`) to switch between framework-specific attributes while keeping the same test logic:

```csharp
#if NXTEST
[NXTest.Test]
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
# Show what NXTest should produce (XUnit baseline)
./compare-compat-outputs.sh baseline

# Compare outputs (once NXTest generator is implemented)
./compare-compat-outputs.sh compare

# Show help
./compare-compat-outputs.sh help
```

## Expected Behavior

When the NXTest generator is fully implemented:

1. **NXTestCompat** should build and run successfully
2. **Both projects** should produce identical test results
3. **All tests** should pass with the same names and outcomes
4. **Test counts** should match exactly

## Current Status

- ✅ **XUnitCompat** - Works correctly, provides baseline
- ⏳ **NXTestCompat** - Waiting for generator implementation
- ✅ **Comparison Script** - Ready to validate once generator is complete

## Test Coverage

The compatibility tests include:

- **Basic Tests**: Simple assertions, collections, exceptions
- **Parameterized Tests**: Data-driven test cases  
- **Async Tests**: Asynchronous test methods
- **Setup/Teardown**: Framework-specific initialization/cleanup

## Integration

This validation should be part of the NXTest development workflow:

1. Implement NXTest generator features
2. Run `./compare-compat-outputs.sh compare`
3. Fix any compatibility issues until outputs match exactly
4. Repeat for new features

This ensures NXTest maintains full compatibility with XUnit behavior.