using System.Diagnostics;

namespace Loadout.Core.Common;

/// <summary>Small helper to launch an executable and capture its output.</summary>
public static class ProcessRunner
{
    public sealed record Result(int ExitCode, string StdOut, string StdErr)
    {
        public bool Success => ExitCode == 0;
    }

    public static Result Run(string fileName, string arguments, int timeoutMs = 30_000)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = new Process { StartInfo = psi };
        process.Start();

        string stdout = process.StandardOutput.ReadToEnd();
        string stderr = process.StandardError.ReadToEnd();

        if (!process.WaitForExit(timeoutMs))
        {
            try { process.Kill(true); } catch { /* best effort */ }
            return new Result(-1, stdout, "Timed out");
        }

        return new Result(process.ExitCode, stdout, stderr);
    }
}
