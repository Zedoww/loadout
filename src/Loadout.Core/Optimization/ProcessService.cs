using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace Loadout.Core.Optimization;

/// <summary>A group of processes sharing the same name, with their memory footprint.</summary>
public sealed record ProcessGroup(string Name, long WorkingSetBytes, int Count);

/// <summary>
/// Manages running processes. Suspension uses <c>NtSuspendProcess</c> /
/// <c>NtResumeProcess</c>: a **fully reversible** pause (unlike force-closing),
/// ideal for freeing CPU/RAM during a gaming session without losing app state.
/// </summary>
public sealed class ProcessService
{
    [DllImport("ntdll.dll")]
    private static extern uint NtSuspendProcess(IntPtr processHandle);

    [DllImport("ntdll.dll")]
    private static extern uint NtResumeProcess(IntPtr processHandle);

    private readonly ILogger<ProcessService> _logger;

    /// <summary>System processes that must NEVER be suspended (Windows stability).</summary>
    private static readonly HashSet<string> Critical = new(StringComparer.OrdinalIgnoreCase)
    {
        "System", "Idle", "Registry", "smss", "csrss", "wininit", "winlogon",
        "services", "lsass", "svchost", "dwm", "explorer", "fontdrvhost",
        "Memory Compression", "LsaIso", "spoolsv", "SecurityHealthService",
        "ctfmon", "ShellExperienceHost", "StartMenuExperienceHost", "SearchHost",
        "TextInputHost", "audiodg", "Loadout", "dotnet", "conhost", "WUDFHost",
    };

    public ProcessService(ILogger<ProcessService> logger) => _logger = logger;

    /// <summary>Tells whether a process can be safely suspended.</summary>
    public bool IsSuspendable(string processName) => !Critical.Contains(processName);

    /// <summary>
    /// Lists the most memory-hungry applications, grouped by name, excluding
    /// critical system processes.
    /// </summary>
    public IReadOnlyList<ProcessGroup> ListTopByMemory(int count = 25)
    {
        var groups = new Dictionary<string, (long bytes, int count)>(StringComparer.OrdinalIgnoreCase);

        foreach (var p in Process.GetProcesses())
        {
            try
            {
                string name = p.ProcessName;
                if (!IsSuspendable(name)) continue;

                long ws = p.WorkingSet64;
                if (groups.TryGetValue(name, out var agg))
                    groups[name] = (agg.bytes + ws, agg.count + 1);
                else
                    groups[name] = (ws, 1);
            }
            catch { /* process exited or inaccessible */ }
            finally { p.Dispose(); }
        }

        return groups
            .Select(kv => new ProcessGroup(kv.Key, kv.Value.bytes, kv.Value.count))
            .OrderByDescending(g => g.WorkingSetBytes)
            .Take(count)
            .ToList();
    }

    /// <summary>Suspends every process with this name. Reversible via <see cref="Resume"/>.</summary>
    public OptimizationResult Suspend(string processName)
    {
        if (!IsSuspendable(processName))
            return OptimizationResult.Fail($"'{processName}' is a protected system process.");

        return Apply(processName, NtSuspendProcess, "suspended");
    }

    /// <summary>Resumes every previously suspended process with this name.</summary>
    public OptimizationResult Resume(string processName) =>
        Apply(processName, NtResumeProcess, "resumed");

    private OptimizationResult Apply(string processName, Func<IntPtr, uint> action, string verb)
    {
        int affected = 0;
        foreach (var p in Process.GetProcessesByName(processName))
        {
            try
            {
                action(p.Handle);
                affected++;
            }
            catch (Win32Exception) { /* access denied: protected process */ }
            catch (InvalidOperationException) { /* already exited */ }
            finally { p.Dispose(); }
        }

        if (affected == 0)
            return OptimizationResult.Fail($"No '{processName}' process {verb}.");

        _logger.LogInformation("{Count} '{Name}' process(es) {Verb}.", affected, processName, verb);
        return OptimizationResult.Ok($"{affected} '{processName}' process(es) {verb}.");
    }
}
