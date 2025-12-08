
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Testing.Platform.Extensions.TestFramework;

namespace UXUnit.Runtime;

public sealed class TestFramework : ITestFramework
{
    private const string s_displayName = "uxunit Microsoft.Testing.Platform framework";

    public string Uid => "64e8dd3a-ae2c-448f-9481-587f0252bfb8";

    public string Version => "1.0.0";

    public string DisplayName => s_displayName;

    public string Description => s_displayName;

    private TestFramework() { }

    public static async Task<int> RunAsync(
        string[] args,
        TestClassMetadata[] testClasses,
        TestExecutionOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        return await TestRunner.RunAsync(testClasses, options, cancellationToken);
    }

    public Task<CloseTestSessionResult> CloseTestSessionAsync(CloseTestSessionContext context)
    {
        throw new System.NotImplementedException();
    }

    public Task<CreateTestSessionResult> CreateTestSessionAsync(CreateTestSessionContext context)
    {
        throw new System.NotImplementedException();
    }

    public Task ExecuteRequestAsync(ExecuteRequestContext context)
    {
        throw new System.NotImplementedException();
    }

    public Task<bool> IsEnabledAsync()
    {
        throw new System.NotImplementedException();
    }
}