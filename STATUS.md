# UXUnit Project Status

This document tracks the current status of UXUnit development against its design goals and requirements.

## Project Overview

UXUnit is a next-generation unit testing framework for .NET that leverages source generators to provide compile-time test discovery, validation, and code generation.

**Last Updated:** September 23, 2025

## Core Goals Status

### ✅ Performance Goals
- [x] **Compile-time code generation architecture**: Core source generator infrastructure in place
- [x] **Eliminate runtime reflection**: Direct method invocation approach designed
- [ ] **Optimized test execution paths**: Generator implementation needed
- [ ] **Built-in parallel execution support**: Runtime implementation needed

### 🔄 Developer Experience Goals
- [x] **Clean attribute-based API**: Core attributes defined (`[Test]`, `[TestClass]`, etc.)
- [x] **XUnit compatibility**: Basic compatibility attributes implemented (`[Fact]`, `[Theory]`, `[InlineData]`)
- [ ] **Rich tooling support**: Source generator needs to provide metadata
- [ ] **Clear error messages**: Diagnostic generation needed
- [ ] **IntelliSense integration**: Generated code needs to be IDE-friendly

### 🔄 Reliability Goals
- [x] **Compile-time validation architecture**: Attribute system supports validation
- [ ] **Test configuration error detection**: Source generator validation needed
- [ ] **Structured exception handling**: Runtime implementation needed
- [ ] **Resource leak detection**: Advanced feature for later implementation

### ✅ Simplicity Goals
- [x] **Minimal boilerplate**: Attribute-based approach reduces boilerplate
- [x] **Intuitive APIs**: Clean attribute design matches expectations
- [ ] **Minimal configuration**: Default conventions need implementation

### 🔄 Extensibility Goals
- [x] **Custom attribute support**: Interface-based extensibility designed
- [ ] **Custom assertions**: Assertion library needed
- [ ] **Custom test behaviors**: Advanced feature for later implementation

## Implementation Status

### Core Components

#### UXUnit.Core ✅ (Basic Implementation)
- [x] Core interfaces defined (`ITestClassRunner`, etc.)
- [x] Base attribute classes (`TestAttribute`, `TestClassAttribute`, etc.)
- [x] XUnit compatibility attributes (`FactAttribute`, `TheoryAttribute`, `InlineDataAttribute`)
- [x] Model classes for test metadata
- [ ] Advanced attributes (retry, repeat, parallel control)

#### UXUnit.Generators 🔄 (In Progress)
- [x] Basic source generator structure
- [ ] Test class discovery
- [ ] Test method analysis and validation
- [ ] Code generation for test runners
- [ ] Compile-time diagnostics

#### UXUnit.Runtime 🔄 (In Progress)
- [x] Basic test runner interface
- [ ] Test execution engine
- [ ] Parallel execution support
- [ ] Result collection and reporting
- [ ] Test lifecycle management

#### UXUnit.Assertions ❌ (Not Started)
- [ ] Fluent assertion API
- [ ] Detailed failure messages
- [ ] Custom assertion extensions
- [ ] XUnit assertion compatibility

### XUnit Compatibility Status

#### ✅ Implemented
- [x] `[Fact]` attribute - Maps to `[Test]`
- [x] `[Theory]` attribute - Maps to `[Test]`
- [x] `[InlineData]` attribute - Maps to `[TestData]`

#### 🔄 Planned
- [ ] `[MemberData]` attribute
- [ ] `[ClassData]` attribute
- [ ] `[Trait]` attribute
- [ ] Constructor/Dispose lifecycle support
- [ ] Collection fixtures

#### ❌ Not Planned (Breaking Changes Acceptable)
- Collection parallelization (different approach)
- Dynamic test discovery
- Some advanced XUnit features

### Testing & Validation

#### ✅ Test Infrastructure
- [x] Compatibility test projects (UXUnitCompat, XUnitCompat)
- [x] Shared test files for validation
- [x] Comparison script for output validation
- [x] Automated compatibility test suite (`UXUnit.CompatibilityTests`)

#### 🔄 Test Coverage
- [x] Basic compatibility tests exist
- [x] Automated validation of compatibility assets
- [x] XUnit baseline validation
- [ ] Source generator tests needed
- [ ] Runtime execution tests needed
- [ ] Performance benchmark tests needed

## Migration Path

### Phase 1: Basic Compatibility ✅ (Current)
- [x] Core XUnit attributes (`[Fact]`, `[Theory]`, `[InlineData]`)
- [x] Basic test structure compatibility

### Phase 2: Source Generation 🔄 (In Progress)
- [ ] Complete source generator implementation
- [ ] Test discovery and validation
- [ ] Generated runner code
- [ ] Compile-time diagnostics

### Phase 3: Runtime Implementation
- [ ] Test execution engine
- [ ] Result reporting
- [ ] Parallel execution
- [ ] Integration with test tooling

### Phase 4: Advanced Features
- [ ] Advanced XUnit compatibility (`[MemberData]`, etc.)
- [ ] Custom assertion library
- [ ] Performance optimizations
- [ ] Advanced diagnostics

## Known Issues & Limitations

### Current Limitations
1. **Source generator incomplete**: Core generation logic not implemented
2. **Runtime not functional**: Test execution engine not implemented
3. **No assertion library**: Currently depends on XUnit assertions
4. **Limited XUnit compatibility**: Only basic attributes implemented
5. **No tooling integration**: VS Test Explorer, dotnet test not supported

### Design Decisions
1. **Non-sealed attributes**: Made base attributes non-sealed to enable XUnit compatibility inheritance
2. **Clean slate approach**: Not maintaining full XUnit API compatibility where it conflicts with UXUnit design
3. **Compile-time focus**: Prioritizing compile-time safety over runtime flexibility

## Next Steps (Priority Order)

### High Priority
1. **Complete source generator**: Implement test discovery and runner generation
2. **Basic runtime**: Implement test execution and result collection
3. **Build integration**: Add MSBuild targets and dotnet CLI support

### Medium Priority
4. **Extended XUnit compatibility**: Add `[MemberData]` and `[Trait]` support
5. **Assertion library**: Create UXUnit-specific assertions (with XUnit compatibility)
6. **Performance optimization**: Benchmark and optimize generated code

### Low Priority
7. **Advanced features**: Retry logic, parallel execution fine-tuning
8. **Tooling integration**: VS Test Explorer, coverage tools
9. **Documentation**: Complete API docs and migration guide

## Success Metrics

- [ ] **Compatibility**: Existing XUnit tests run without modification (basic scenarios)
- [ ] **Performance**: 50%+ faster test execution vs XUnit (when complete)
- [ ] **Reliability**: Compile-time detection of 90%+ of common test errors
- [ ] **Adoption**: Clear migration path with automated tooling support