using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Loadout.Core.Optimization;

/// <summary>
/// État capturé avant l'activation du Surge, pour pouvoir tout restaurer.
/// </summary>
public sealed record SurgeState
{
    public Guid? PreviousPowerPlan { get; init; }
    public IReadOnlyList<string> SuspendedProcesses { get; init; } = Array.Empty<string>();
    public DateTime AppliedAt { get; init; } = DateTime.Now;
}

/// <summary>
/// Orchestrateur du « Surge ». Applique un ensemble d'optimisations
/// réversibles puis sait revenir à l'état initial grâce à un instantané
/// persisté sur disque (survit à un redémarrage de l'application).
/// </summary>
public sealed class SurgeService
{
    private readonly PowerPlanService _power;
    private readonly MemoryCleaner _memory;
    private readonly ProcessService _process;
    private readonly ILogger<SurgeService> _logger;
    private readonly string _statePath;

    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    /// <summary>
    /// Applications d'arrière-plan suspendues par défaut pendant un Surge si elles
    /// tournent. Suspension = pause réversible, reprise automatique à la restauration.
    /// </summary>
    private static readonly string[] DefaultBackgroundApps =
    {
        "chrome", "msedge", "firefox", "opera", "brave", "spotify", "OneDrive",
    };

    public SurgeService(PowerPlanService power, MemoryCleaner memory,
        ProcessService process, ILogger<SurgeService> logger)
    {
        _power = power;
        _memory = memory;
        _process = process;
        _logger = logger;

        string dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Loadout");
        Directory.CreateDirectory(dir);
        _statePath = Path.Combine(dir, "surge-state.json");
    }

    public bool IsActive => File.Exists(_statePath);

    public SurgeState? CurrentState
    {
        get
        {
            if (!IsActive) return null;
            try { return JsonSerializer.Deserialize<SurgeState>(File.ReadAllText(_statePath)); }
            catch { return null; }
        }
    }

    /// <summary>Active le Surge et mémorise l'état précédent.</summary>
    public IReadOnlyList<OptimizationResult> Apply()
    {
        var log = new List<OptimizationResult>();

        // 1. Capture de l'état avant modification (écrit AVANT toute action pour
        //    qu'une restauration reste possible même en cas d'arrêt impromptu).
        var toSuspend = DefaultBackgroundApps
            .Where(name => Process.GetProcessesByName(name).Length > 0)
            .ToList();

        var state = new SurgeState
        {
            PreviousPowerPlan = _power.GetActivePlan(),
            SuspendedProcesses = toSuspend,
        };
        File.WriteAllText(_statePath, JsonSerializer.Serialize(state, JsonOpts));
        _logger.LogInformation("Surge activé. Plan précédent : {Plan}", state.PreviousPowerPlan);

        // 2. Plan d'alimentation hautes performances.
        log.Add(_power.ActivateHighPerformance());

        // 3. Libération de la mémoire.
        log.Add(_memory.Clean());

        // 4. Mise en pause des applications d'arrière-plan.
        foreach (var name in toSuspend)
            log.Add(_process.Suspend(name));

        return log;
    }

    /// <summary>Restaure l'état d'avant le Surge.</summary>
    public IReadOnlyList<OptimizationResult> Restore()
    {
        var log = new List<OptimizationResult>();
        var state = CurrentState;

        if (state is null)
        {
            log.Add(OptimizationResult.Fail("Aucun Surge actif à restaurer."));
            return log;
        }

        // 1. Reprise des applications suspendues.
        foreach (var name in state.SuspendedProcesses)
            log.Add(_process.Resume(name));

        // 2. Restauration du plan d'alimentation d'origine.
        if (state.PreviousPowerPlan is Guid plan)
            log.Add(_power.SetActivePlan(plan));
        else
            log.Add(_power.SetActivePlan(PowerPlanService.Balanced));

        try { File.Delete(_statePath); }
        catch { /* sera réécrit au prochain Apply */ }

        _logger.LogInformation("Surge désactivé, état restauré.");
        log.Add(OptimizationResult.Ok("Surge désactivé, état initial restauré."));
        return log;
    }
}
