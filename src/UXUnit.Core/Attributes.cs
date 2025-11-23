using System;

namespace UXUnit;

// XUnit Compatibility Attributes
// These attributes provide direct compatibility with XUnit syntax
// allowing existing XUnit tests to work with UXUnit without modification

/// <summary>
/// XUnit-compatible attribute for marking individual test methods.
/// Maps directly to UXUnit's TestAttribute.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class FactAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FactAttribute"/> class.
    /// </summary>
    public FactAttribute() { }
}

/// <summary>
/// XUnit-compatible attribute for parameterized tests.
/// Maps to UXUnit's TestAttribute with data sources.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class TheoryAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TheoryAttribute"/> class.
    /// </summary>
    public TheoryAttribute() { }
}

/// <summary>
/// XUnit-compatible inline data attribute.
/// Maps directly to UXUnit's TestDataAttribute.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class InlineDataAttribute : Attribute
{
    /// <summary>
    /// Gets the test data arguments.
    /// </summary>
    public object?[] Data { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="InlineDataAttribute"/> class.
    /// </summary>
    /// <param name="data">The test data arguments.</param>
    public InlineDataAttribute(params object?[] data)
    {
        Data = data;
    }
}
