using System.Timers;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using Loadout.Core.Monitoring;
using Timer = System.Timers.Timer;

namespace Loadout.App.ViewModels;

public partial class DashboardViewModel : ObservableObject, IDisposable
{
    private readonly HardwareMonitor _monitor;
    private readonly Timer _timer;
    private volatile bool _reading;

    [ObservableProperty] private string _cpuName = "Processeur";
    [ObservableProperty] private float? _cpuLoad;
    [ObservableProperty] private float? _cpuTemperature;

    [ObservableProperty] private string _gpuName = "Carte graphique";
    [ObservableProperty] private float? _gpuLoad;
    [ObservableProperty] private float? _gpuTemperature;

    [ObservableProperty] private float? _memoryLoad;
    [ObservableProperty] private string _memorySummary = "—";

    public DashboardViewModel(HardwareMonitor monitor)
    {
        _monitor = monitor;
        _timer = new Timer(1000) { AutoReset = true };
        _timer.Elapsed += OnTick;
        _timer.Start();
        // La première lecture est lancée par le timer sur un thread d'arrière-plan :
        // l'ouverture de LibreHardwareMonitor prend quelques secondes et ne doit
        // jamais bloquer le thread d'interface.
    }

    private void OnTick(object? sender, ElapsedEventArgs? e)
    {
        if (_reading) return;          // évite l'empilement si une lecture est lente
        _reading = true;
        try
        {
            SystemMetrics m = _monitor.Read();
            Application.Current?.Dispatcher.Invoke(() => Apply(m));
        }
        catch { /* on retentera au prochain tick */ }
        finally { _reading = false; }
    }

    private void Apply(SystemMetrics m)
    {
        if (!string.IsNullOrWhiteSpace(m.CpuName)) CpuName = m.CpuName!;
        CpuLoad = m.CpuLoad;
        CpuTemperature = m.CpuTemperature;

        if (!string.IsNullOrWhiteSpace(m.GpuName)) GpuName = m.GpuName!;
        GpuLoad = m.GpuLoad;
        GpuTemperature = m.GpuTemperature;

        MemoryLoad = m.MemoryLoad;
        MemorySummary = m is { MemoryUsedGb: not null, MemoryTotalGb: not null }
            ? $"{m.MemoryUsedGb:0.0} / {m.MemoryTotalGb:0.0} Go"
            : "—";
    }

    public void Dispose()
    {
        _timer.Stop();
        _timer.Elapsed -= OnTick;
        _timer.Dispose();
    }
}
