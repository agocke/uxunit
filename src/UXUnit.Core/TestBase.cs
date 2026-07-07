
using System.Threading;

namespace UXUnit;

/// <summary>
/// Optional base class for test classes, providing common functionality.
/// </summary>
public abstract class TestBase
{
    internal readonly CancellationTokenSource Cts = new();

    protected CancellationToken CancellationToken => Cts.Token;
}