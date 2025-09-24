# Development Guidelines for UXUnit

## Project Structure

### Unified Artifacts Directory

This project uses a unified artifacts directory structure managed by `Directory.Build.props`:

```
artifacts/
├── bin/           # Compiled binaries for each project
├── obj/           # Intermediate build files for each project  
├── packages/      # Generated NuGet packages
└── test-results/  # Test execution results
```

**Benefits:**
- **Clean source tree**: No `bin/` or `obj/` folders scattered throughout the source
- **Easier cleanup**: Single `artifacts/` directory can be safely deleted
- **CI/CD friendly**: Simplified artifact collection and caching
- **IDE performance**: Faster indexing without intermediate files in source directories

### Build Configuration

The `Directory.Build.props` file establishes:
- Unified output paths for all projects
- Common compiler settings (nullable reference types, warnings as errors)
- Shared package metadata
- Source Link configuration for debugging

## Code Style Standards

### General Principles
- **Nullable Reference Types**: Always enabled (`<Nullable>enable</Nullable>`)
- **Warnings as Errors**: Treat warnings as compilation errors for code quality
- **Latest C# Features**: Use `<LangVersion>latest</LangVersion>` for modern syntax
- **Code Style Enforcement**: Enable `<EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>`

### Project-Specific Guidelines

#### UXUnit.Core
- Contains fundamental types, attributes, and interfaces
- Must be compatible with .NET 6.0+ for broad compatibility
- No external dependencies except system libraries

#### UXUnit.Generators  
- Source generator implementation
- Must reference `Microsoft.CodeAnalysis.CSharp`
- Include `<IncludeBuildOutput>false</IncludeBuildOutput>` in project files
- Mark as analyzer: `<AnalyzerLanguage>C#</AnalyzerLanguage>`

#### UXUnit.Runtime
- Test execution engine and runtime support
- Can have more dependencies than Core project
- Should remain lightweight for fast test startup

### Testing Guidelines

#### XUnit Compatibility
- **Always use `xunit.assert`** for assertions - never create custom assertion libraries
- Use conditional compilation (`#if UXUNIT` / `#if XUNIT`) for framework-specific code
- Test projects should demonstrate compatibility by compiling with both frameworks

#### Test Organization
```
test/
├── Assets/XUnitCompatibility/  # Compatibility demonstration tests
├── UXUnit.Core.Tests/          # Unit tests for Core library
├── UXUnit.Generators.Tests/    # Unit tests for source generators
└── UXUnit.Integration.Tests/   # End-to-end integration tests
```

## Build and Development

### Prerequisites
- .NET 8.0 SDK or later
- Any editor/IDE that supports .NET development

### Common Commands

```bash
# Clean all artifacts
rm -rf artifacts/

# Build entire solution
dotnet build

# Run all tests
dotnet test

# Create packages
dotnet pack

# Test XUnit compatibility
cd test/Assets/XUnitCompatibility
./build-xunit.sh    # Test with XUnit
./build-uxunit.sh   # Test with UXUnit
```

### IDE Configuration

**Visual Studio / VS Code:**
- The unified artifacts directory improves solution load time
- Intermediate files don't clutter the file explorer
- Search/indexing is faster without bin/obj folders

**Rider:**
- Configure exclusions for `artifacts/` directory in project settings
- Enable nullable reference type analysis
- Use built-in code style enforcement

## Package Management

### NuGet Package Creation
All packages inherit common metadata from `Directory.Build.props`:
- Consistent versioning across all packages
- Shared license and repository information  
- Automatic README inclusion
- Source Link support for debugging

### Package References
- Use explicit versions for external dependencies
- Keep dependency versions aligned across projects
- Prefer `PrivateAssets="All"` for build-time dependencies

## Continuous Integration

### Artifact Collection
```yaml
# Example CI configuration for artifacts
- name: Upload Artifacts
  uses: actions/upload-artifact@v3
  with:
    name: build-artifacts
    path: artifacts/
```

### Build Caching
```yaml
# Cache the artifacts directory structure  
- name: Cache Build Outputs
  uses: actions/cache@v3
  with:
    path: artifacts/
    key: ${{ runner.os }}-build-${{ hashFiles('**/*.csproj') }}
```

## Best Practices

### For Contributors
1. **Never commit artifacts**: The `artifacts/` directory should be in `.gitignore`
2. **Clean builds**: Use `rm -rf artifacts/` before important builds
3. **Test compatibility**: Always verify XUnit compatibility when changing test-related code
4. **Update documentation**: Keep this guide current with any structural changes

### For Code Reviews
1. **Check project files**: Verify new projects follow the Directory.Build.props conventions
2. **Verify test compatibility**: Ensure test changes work with both XUnit and UXUnit
3. **Package metadata**: Confirm appropriate package settings for new libraries

### Performance Considerations
- The unified artifacts approach reduces I/O overhead during builds
- Fewer directories to scan improves IDE and build performance
- Easier to implement incremental builds and caching strategies

## Troubleshooting

### Common Issues

**Build outputs in wrong location:**
- Check that `Directory.Build.props` is in the solution root
- Verify project files don't override `BaseOutputPath` or `BaseIntermediateOutputPath`

**Missing packages directory:**
- Ensure `<PackageOutputPath>` is correctly set
- Check that packaging projects have `<GeneratePackageOnBuild>true</GeneratePackageOnBuild>`

**IDE not recognizing structure:**
- Restart IDE after adding `Directory.Build.props`  
- Clear IDE caches if needed
- Verify `.gitignore` excludes `artifacts/` directory