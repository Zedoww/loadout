using System.IO;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Loadout.App.Services;

namespace Loadout.App;

/// <summary>
/// Point d'entrée et composition root de l'application.
/// </summary>
public partial class App : Application
{
    /// <summary>Fournisseur de services global (résolution depuis les vues).</summary>
    public static IServiceProvider Services { get; private set; } = default!;

    private static string LogDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Loadout", "logs");

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        ConfigureLogging();

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            Log.Fatal(args.ExceptionObject as Exception, "Exception non gérée (domaine d'application)");

        var services = new ServiceCollection();
        services.AddLoadoutServices();
        Services = services.BuildServiceProvider();

        Log.Information("Loadout démarre.");
        Services.GetRequiredService<MainWindow>().Show();
    }

    private static void ConfigureLogging()
    {
        Directory.CreateDirectory(LogDirectory);
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(
                Path.Combine(LogDirectory, "loadout-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Log.Error(e.Exception, "Exception non gérée sur le thread d'interface");
        MessageBox.Show(e.Exception.Message, "Loadout — erreur",
            MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information("Loadout s'arrête.");
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}
