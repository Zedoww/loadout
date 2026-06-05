using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Loadout.App.ViewModels;
using Loadout.Core.Backup;
using Loadout.Core.Monitoring;
using Loadout.Core.Optimization;

namespace Loadout.App.Services;

/// <summary>
/// Composition root: registers every service, ViewModel and the main window in
/// the dependency-injection container.
/// </summary>
public static class ServiceRegistration
{
    public static IServiceCollection AddLoadoutServices(this IServiceCollection services)
    {
        // Logging (Serilog plugged behind the Microsoft.Extensions.Logging abstraction).
        services.AddLogging(builder => builder.AddSerilog(dispose: true));

        // System services (Loadout.Core) — no heavy shared state, so singletons.
        services.AddSingleton<HardwareMonitor>();
        services.AddSingleton<PowerPlanService>();
        services.AddSingleton<MemoryCleaner>();
        services.AddSingleton<TempCleaner>();
        services.AddSingleton<RestorePointService>();
        services.AddSingleton<ProcessService>();
        services.AddSingleton<TweakService>();
        services.AddSingleton<SurgeService>();

        // View models.
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<SurgeViewModel>();
        services.AddTransient<ProcessesViewModel>();
        services.AddTransient<TweaksViewModel>();
        services.AddTransient<CleanupViewModel>();
        services.AddTransient<SettingsViewModel>();

        // Main window.
        services.AddSingleton<MainWindow>();

        return services;
    }
}
