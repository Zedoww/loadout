using System.Text.Json;

namespace Opti.Core.Optimization;

/// <summary>
/// État capturé avant l'activation du mode jeu, pour pouvoir tout restaurer.
/// </summary>
public sealed record BoostState
{
    public Guid? PreviousPowerPlan { get; init; }
    public DateTime AppliedAt { get; init; } = DateTime.Now;
}

/// <summary>
/// Orchestrateur du « mode jeu ». Applique un ensemble d'optimisations
/// réversibles puis sait revenir à l'état initial grâce à un instantané
/// persisté sur disque (survit à un redémarrage de l'application).
/// </summary>
public sealed class GameBoostService
{
    private readonly PowerPlanService _power;
    private readonly MemoryCleaner _memory;
    private readonly string _statePath;

    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    public GameBoostService(PowerPlanService power, MemoryCleaner memory)
    {
        _power = power;
        _memory = memory;

        string dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Opti");
        Directory.CreateDirectory(dir);
        _statePath = Path.Combine(dir, "boost-state.json");
    }

    public bool IsActive => File.Exists(_statePath);

    public BoostState? CurrentState
    {
        get
        {
            if (!IsActive) return null;
            try { return JsonSerializer.Deserialize<BoostState>(File.ReadAllText(_statePath)); }
            catch { return null; }
        }
    }

    /// <summary>Active le mode jeu et mémorise l'état précédent.</summary>
    public IReadOnlyList<OptimizationResult> Apply()
    {
        var log = new List<OptimizationResult>();

        // 1. Capture de l'état avant modification.
        var state = new BoostState { PreviousPowerPlan = _power.GetActivePlan() };
        File.WriteAllText(_statePath, JsonSerializer.Serialize(state, JsonOpts));

        // 2. Plan d'alimentation hautes performances.
        log.Add(_power.ActivateHighPerformance());

        // 3. Libération de la mémoire.
        log.Add(_memory.Clean());

        return log;
    }

    /// <summary>Restaure l'état d'avant le mode jeu.</summary>
    public IReadOnlyList<OptimizationResult> Restore()
    {
        var log = new List<OptimizationResult>();
        var state = CurrentState;

        if (state is null)
        {
            log.Add(OptimizationResult.Fail("Aucun mode jeu actif à restaurer."));
            return log;
        }

        if (state.PreviousPowerPlan is Guid plan)
            log.Add(_power.SetActivePlan(plan));
        else
            log.Add(_power.SetActivePlan(PowerPlanService.Balanced));

        try { File.Delete(_statePath); }
        catch { /* sera réécrit au prochain Apply */ }

        log.Add(OptimizationResult.Ok("Mode jeu désactivé, état initial restauré."));
        return log;
    }
}
