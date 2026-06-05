using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Wpf.Ui.Appearance;
using Loadout.App.Services;

namespace Loadout.App;

/// <summary>
/// Application entry point and composition root.
/// </summary>
public partial class App : Application
{
    /// <summary>Signature accent (electric indigo) used across the UI.</summary>
    public static readonly Color BrandAccent = Color.FromRgb(0x7C, 0x6C, 0xFF);

    /// <summary>Global service provider (resolved from views).</summary>
    public static IServiceProvider Services { get; private set; } = default!;

    private static string LogDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Loadout", "logs");

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        ConfigureLogging();

        // Make WPF-UI controls (nav selection, primary buttons, toggles) adopt our brand accent.
        ApplicationAccentColorManager.Apply(BrandAccent);

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            Log.Fatal(args.ExceptionObject as Exception, "Unhandled exception (app domain)");

        var services = new ServiceCollection();
        services.AddLoadoutServices();
        Services = services.BuildServiceProvider();

        Log.Information("Loadout starting up.");
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
        Log.Error(e.Exception, "Unhandled exception on the UI thread");
        MessageBox.Show(e.Exception.Message, "Loadout — error",
            MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information("Loadout shutting down.");
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}
