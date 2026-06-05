using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Loadout.Core.Optimization;

/// <summary>
/// State captured before Surge is activated, so everything can be restored.
/// </summary>
public sealed record SurgeState
{
    public Guid? PreviousPowerPlan { get; init; }
    public IReadOnlyList<string> SuspendedProcesses { get; init; } = Array.Empty<string>();
    public DateTime AppliedAt { get; init; } = DateTime.Now;
}

/// <summary>
/// Orchestrates "Surge". Applies a set of reversible optimizations, then knows
/// how to return to the initial state thanks to a snapshot persisted on disk
/// (it survives an application restart).
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
    /// Background apps suspended by default during a Surge when they are running.
    /// Suspension is a reversible pause; they are resumed automatically on restore.
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

    /// <summary>Activates Surge and remembers the previous state.</summary>
    public IReadOnlyList<OptimizationResult> Apply()
    {
        var log = new List<OptimizationResult>();

        // 1. Capture state before any change (written BEFORE any action so a
        //    restore stays possible even after an unexpected shutdown).
        var toSuspend = DefaultBackgroundApps
            .Where(name => Process.GetProcessesByName(name).Length > 0)
            .ToList();

        var state = new SurgeState
        {
            PreviousPowerPlan = _power.GetActivePlan(),
            SuspendedProcesses = toSuspend,
        };
        File.WriteAllText(_statePath, JsonSerializer.Serialize(state, JsonOpts));
        _logger.LogInformation("Surge activated. Previous plan: {Plan}", state.PreviousPowerPlan);

        // 2. High-performance power plan.
        log.Add(_power.ActivateHighPerformance());

        // 3. Free memory.
        log.Add(_memory.Clean());

        // 4. Pause background apps.
        foreach (var name in toSuspend)
            log.Add(_process.Suspend(name));

        return log;
    }

    /// <summary>Restores the pre-Surge state.</summary>
    public IReadOnlyList<OptimizationResult> Restore()
    {
        var log = new List<OptimizationResult>();
        var state = CurrentState;

        if (state is null)
        {
            log.Add(OptimizationResult.Fail("No active Surge to restore."));
            return log;
        }

        // 1. Resume the suspended apps.
        foreach (var name in state.SuspendedProcesses)
            log.Add(_process.Resume(name));

        // 2. Restore the original power plan.
        if (state.PreviousPowerPlan is Guid plan)
            log.Add(_power.SetActivePlan(plan));
        else
            log.Add(_power.SetActivePlan(PowerPlanService.Balanced));

        try { File.Delete(_statePath); }
        catch { /* will be rewritten on the next Apply */ }

        _logger.LogInformation("Surge deactivated, state restored.");
        log.Add(OptimizationResult.Ok("Surge deactivated, original state restored."));
        return log;
    }
}
