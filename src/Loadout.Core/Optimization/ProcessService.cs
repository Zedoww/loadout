using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace Loadout.Core.Optimization;

/// <summary>Un groupe de processus partageant le même nom, avec leur empreinte mémoire.</summary>
public sealed record ProcessGroup(string Name, long WorkingSetBytes, int Count);

/// <summary>
/// Gère les processus en cours d'exécution. La suspension utilise
/// <c>NtSuspendProcess</c> / <c>NtResumeProcess</c> : c'est une mise en pause
/// **entièrement réversible** (contrairement à la fermeture forcée), idéale pour
/// libérer CPU/RAM pendant une session de jeu sans perdre l'état des applications.
/// </summary>
public sealed class ProcessService
{
    [DllImport("ntdll.dll")]
    private static extern uint NtSuspendProcess(IntPtr processHandle);

    [DllImport("ntdll.dll")]
    private static extern uint NtResumeProcess(IntPtr processHandle);

    private readonly ILogger<ProcessService> _logger;

    /// <summary>Processus système qu'il ne faut JAMAIS suspendre (stabilité de Windows).</summary>
    private static readonly HashSet<string> Critical = new(StringComparer.OrdinalIgnoreCase)
    {
        "System", "Idle", "Registry", "smss", "csrss", "wininit", "winlogon",
        "services", "lsass", "svchost", "dwm", "explorer", "fontdrvhost",
        "Memory Compression", "LsaIso", "spoolsv", "SecurityHealthService",
        "ctfmon", "ShellExperienceHost", "StartMenuExperienceHost", "SearchHost",
        "TextInputHost", "audiodg", "Loadout", "dotnet", "conhost", "WUDFHost",
    };

    public ProcessService(ILogger<ProcessService> logger) => _logger = logger;

    /// <summary>Indique si un processus peut être suspendu sans risque pour le système.</summary>
    public bool IsSuspendable(string processName) => !Critical.Contains(processName);

    /// <summary>
    /// Liste les applications les plus gourmandes en mémoire, regroupées par nom,
    /// en excluant les processus système critiques.
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
            catch { /* process terminé ou inaccessible */ }
            finally { p.Dispose(); }
        }

        return groups
            .Select(kv => new ProcessGroup(kv.Key, kv.Value.bytes, kv.Value.count))
            .OrderByDescending(g => g.WorkingSetBytes)
            .Take(count)
            .ToList();
    }

    /// <summary>Suspend tous les processus portant ce nom. Réversible via <see cref="Resume"/>.</summary>
    public OptimizationResult Suspend(string processName)
    {
        if (!IsSuspendable(processName))
            return OptimizationResult.Fail($"« {processName} » est un processus système protégé.");

        return Apply(processName, NtSuspendProcess, "suspendu");
    }

    /// <summary>Reprend tous les processus portant ce nom préalablement suspendus.</summary>
    public OptimizationResult Resume(string processName) =>
        Apply(processName, NtResumeProcess, "repris");

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
            catch (Win32Exception) { /* accès refusé : processus protégé */ }
            catch (InvalidOperationException) { /* déjà terminé */ }
            finally { p.Dispose(); }
        }

        if (affected == 0)
            return OptimizationResult.Fail($"Aucun processus « {processName} » {verb}.");

        _logger.LogInformation("{Count} processus « {Name} » {Verb}.", affected, processName, verb);
        return OptimizationResult.Ok($"{affected} processus « {processName} » {verb}.");
    }
}
