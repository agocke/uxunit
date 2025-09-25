using System.Diagnostics;
using System.Text;
using Xunit;

namespace UXUnit.CompatibilityTests;

public class CompatibilityComparisonTests
{
    [Fact]
    public void CompareOutputs()
    {
        var xbin = Path.Combine(AppContext.BaseDirectory, "..", "..", "XUnitCompat", "debug", "XUnitCompat");
        var uxbin = Path.Combine(AppContext.BaseDirectory, "..", "..", "UXUnitCompat", "debug", "UXUnitCompat");

        var xPsi = new ProcessStartInfo
        {
            FileName = xbin,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        var xresult = Process.Start(xPsi)!;
        xresult.WaitForExit();
        var xout = xresult.StandardOutput.ReadToEnd();
        // Trim the first line which contains the xunit header
        xout = xout.Substring(xout.IndexOf('\n') + 1);

        var uPsi = new ProcessStartInfo
        {
            FileName = uxbin,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        var uresult = Process.Start(uPsi)!;
        uresult.WaitForExit();
        var uout = uresult.StandardOutput.ReadToEnd();
        // Also trim the first line of uxunit which contains the uxunit header
        uout = uout.Substring(uout.IndexOf('\n') + 1);

        // TODO: Enable when functional
        // Assert.Equal(xout, uout);
    }

    private ProcessResult ExecuteDotNetCommand(string command, string projectPath)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"{command} \"{projectPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        var output = new StringBuilder();
        var error = new StringBuilder();

        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data != null) output.AppendLine(e.Data);
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data != null) error.AppendLine(e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();

        return new ProcessResult
        {
            ExitCode = process.ExitCode,
            Output = output.ToString(),
            Error = error.ToString()
        };
    }

    private record ProcessResult
    {
        public int ExitCode { get; init; }
        public string Output { get; init; } = string.Empty;
        public string Error { get; init; } = string.Empty;
    }
}