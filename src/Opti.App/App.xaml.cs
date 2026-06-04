using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace Opti.App;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Opti", "startup.log");

    public App()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
        Log("=== App ctor ===");

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        Log("OnStartup");
        base.OnStartup(e);
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Log("DISPATCHER EXCEPTION: " + e.Exception);
        MessageBox.Show(e.Exception.ToString(), "Opti — erreur", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }

    private void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        => Log("DOMAIN EXCEPTION: " + e.ExceptionObject);

    internal static void Log(string message)
    {
        try { File.AppendAllText(LogPath, $"{DateTime.Now:HH:mm:ss.fff}  {message}{Environment.NewLine}"); }
        catch { /* ignore */ }
    }
}
