using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Loadout.App.ViewModels;
using Loadout.Core.Backup;
using Loadout.Core.Monitoring;
using Loadout.Core.Optimization;

namespace Loadout.App.Services;

/// <summary>
/// Composition root : enregistre tous les services, ViewModels et la fenêtre
/// principale dans le conteneur d'injection de dépendances.
/// </summary>
public static class ServiceRegistration
{
    public static IServiceCollection AddLoadoutServices(this IServiceCollection services)
    {
        // Journalisation (Serilog branché derrière l'abstraction Microsoft.Extensions.Logging).
        services.AddLogging(builder => builder.AddSerilog(dispose: true));

        // Services système (Loadout.Core) — sans état partagé lourd, donc singletons.
        services.AddSingleton<HardwareMonitor>();
        services.AddSingleton<PowerPlanService>();
        services.AddSingleton<MemoryCleaner>();
        services.AddSingleton<TempCleaner>();
        services.AddSingleton<RestorePointService>();
        services.AddSingleton<ProcessService>();
        services.AddSingleton<TweakService>();
        services.AddSingleton<SurgeService>();

        // ViewModels.
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<SurgeViewModel>();
        services.AddTransient<ProcessesViewModel>();
        services.AddTransient<TweaksViewModel>();
        services.AddTransient<CleanupViewModel>();
        services.AddTransient<SettingsViewModel>();

        // Fenêtre principale.
        services.AddSingleton<MainWindow>();

        return services;
    }
}
