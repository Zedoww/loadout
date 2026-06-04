using Loadout.Core.Backup;
using Loadout.Core.Monitoring;
using Loadout.Core.Optimization;

namespace Loadout.App.Services;

/// <summary>
/// Conteneur de services simple et partagé pour l'application.
/// (Suffisant pour cette taille de projet ; remplaçable par un DI complet plus tard.)
/// </summary>
public static class AppServices
{
    public static HardwareMonitor Monitor { get; } = new();
    public static PowerPlanService PowerPlan { get; } = new();
    public static MemoryCleaner Memory { get; } = new();
    public static TempCleaner Temp { get; } = new();
    public static RestorePointService RestorePoint { get; } = new();
    public static SurgeService Surge { get; } = new(PowerPlan, Memory);
}
