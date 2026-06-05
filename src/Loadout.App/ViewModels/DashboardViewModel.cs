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

    [ObservableProperty] private string _cpuName = "Processor";
    [ObservableProperty] private float? _cpuLoad;
    [ObservableProperty] private float? _cpuTemperature;

    [ObservableProperty] private string _gpuName = "Graphics card";
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
        // The first read is triggered by the timer on a background thread:
        // opening LibreHardwareMonitor takes a few seconds and must never block
        // the UI thread.
    }

    private void OnTick(object? sender, ElapsedEventArgs? e)
    {
        if (_reading) return;          // avoid stacking up if a read is slow
        _reading = true;
        try
        {
            SystemMetrics m = _monitor.Read();
            Application.Current?.Dispatcher.Invoke(() => Apply(m));
        }
        catch { /* will retry on the next tick */ }
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
            ? $"{m.MemoryUsedGb:0.0} / {m.MemoryTotalGb:0.0} GB"
            : "—";
    }

    public void Dispose()
    {
        _timer.Stop();
        _timer.Elapsed -= OnTick;
        _timer.Dispose();
    }
}
