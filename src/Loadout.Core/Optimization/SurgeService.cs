using System.Text.Json;

namespace Loadout.Core.Optimization;

/// <summary>
/// État capturé avant l'activation du Surge, pour pouvoir tout restaurer.
/// </summary>
public sealed record SurgeState
{
    public Guid? PreviousPowerPlan { get; init; }
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
    private readonly string _statePath;

    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    public SurgeService(PowerPlanService power, MemoryCleaner memory)
    {
        _power = power;
        _memory = memory;

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

        // 1. Capture de l'état avant modification.
        var state = new SurgeState { PreviousPowerPlan = _power.GetActivePlan() };
        File.WriteAllText(_statePath, JsonSerializer.Serialize(state, JsonOpts));

        // 2. Plan d'alimentation hautes performances.
        log.Add(_power.ActivateHighPerformance());

        // 3. Libération de la mémoire.
        log.Add(_memory.Clean());

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

        if (state.PreviousPowerPlan is Guid plan)
            log.Add(_power.SetActivePlan(plan));
        else
            log.Add(_power.SetActivePlan(PowerPlanService.Balanced));

        try { File.Delete(_statePath); }
        catch { /* sera réécrit au prochain Apply */ }

        log.Add(OptimizationResult.Ok("Surge désactivé, état initial restauré."));
        return log;
    }
}
