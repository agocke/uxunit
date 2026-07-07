
using System.Threading;

namespace NXTest;

/// <summary>
/// Optional base class for test classes, providing common functionality.
/// </summary>
public abstract class TestBase
{
    private CancellationTokenSource? Cts = null;

    public void SetCts(CancellationTokenSource cts)
    {
        Cts = cts;
    }

    protected CancellationToken CancellationToken => Cts?.Token ?? CancellationToken.None;
}