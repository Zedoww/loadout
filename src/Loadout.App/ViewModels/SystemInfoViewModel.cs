using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Loadout.Core.Monitoring;

namespace Loadout.App.ViewModels;

public partial class SystemInfoViewModel : ObservableObject
{
    private readonly SystemInfoService _service;

    // ── Loading state ──────────────────────────────────────────────────
    [ObservableProperty]
    private bool _isLoading;

    // ── OS ─────────────────────────────────────────────────────────────
    [ObservableProperty] private string _osName = "—";
    [ObservableProperty] private string _osBuild = "—";
    [ObservableProperty] private string _architecture = "—";
    [ObservableProperty] private string _installDate = "—";
    [ObservableProperty] private string _lastBoot = "—";
    [ObservableProperty] private string _computerName = "—";
    [ObservableProperty] private string _userName = "—";

    // ── CPU ────────────────────────────────────────────────────────────
    [ObservableProperty] private string _cpuName = "—";
    [ObservableProperty] private string _cpuCores = "—";
    [ObservableProperty] private string _cpuThreads = "—";
    [ObservableProperty] private string _cpuBaseClock = "—";
    [ObservableProperty] private string _cpuCache = "—";

    // ── GPU ────────────────────────────────────────────────────────────
    [ObservableProperty] private string _gpuName = "—";
    [ObservableProperty] private string _gpuVram = "—";
    [ObservableProperty] private string _gpuDriver = "—";
    [ObservableProperty] private string _gpuResolution = "—";

    // ── Memory ─────────────────────────────────────────────────────────
    [ObservableProperty] private string _ramTotal = "—";
    [ObservableProperty] private string _ramSticks = "—";

    // ── Storage ────────────────────────────────────────────────────────
    [ObservableProperty] private string _storageSummary = "—";

    // ── Motherboard / BIOS ─────────────────────────────────────────────
    [ObservableProperty] private string _motherboardName = "—";
    [ObservableProperty] private string _biosVersion = "—";

    // ── Network ────────────────────────────────────────────────────────
    [ObservableProperty] private string _networkSummary = "—";

    // ── Audio ──────────────────────────────────────────────────────────
    [ObservableProperty] private string _audioDevices = "—";

    public SystemInfoViewModel(SystemInfoService service)
    {
        _service = service;
        // Fire-and-forget: collect on a background thread so the UI never blocks.
        Task.Run(CollectAndApply);
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsLoading = true;
        try
        {
            await Task.Run(CollectAndApply);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void CollectAndApply()
    {
        IsLoading = true;
        try
        {
            var info = _service.Collect();
            Application.Current?.Dispatcher.Invoke(() => Apply(info));
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void Apply(SystemInfo info)
    {
        // ── OS ─────────────────────────────────────────────────────
        if (info.Os is { } os)
        {
            OsName = os.Version ?? "—";
            OsBuild = os.Locale ?? "—";
            Architecture = os.Architecture ?? "—";
            InstallDate = os.InstallDate is { } id ? id.ToString("d") : "—";
            LastBoot = os.LastBootTime is { } lb ? lb.ToString("g") : "—";
            ComputerName = os.ComputerName ?? "—";
            UserName = os.UserName ?? "—";
        }

        // ── CPU ────────────────────────────────────────────────────
        if (info.Cpu is { } cpu)
        {
            CpuName = cpu.Name ?? "—";
            CpuCores = cpu.Cores is { } c ? $"{c} Cores" : "—";
            CpuThreads = cpu.Threads is { } t ? $"{t} Threads" : "—";
            CpuBaseClock = cpu.BaseClockGhz is { } bc ? $"{bc:0.0#} GHz" : "—";
            CpuCache = FormatCache(cpu.L2Cache, cpu.L3Cache);
        }

        // ── GPU ────────────────────────────────────────────────────
        if (info.Gpu is { } gpu)
        {
            GpuName = gpu.Name ?? "—";
            GpuVram = gpu.Vram ?? "—";
            GpuDriver = gpu.DriverVersion ?? "—";
            GpuResolution = gpu.Resolution ?? "—";
        }

        // ── Memory ────────────────────────────────────────────────
        if (info.Ram is { } ram)
        {
            RamTotal = ram.TotalPhysical ?? "—";
            RamSticks = FormatSticks(ram.Sticks);
        }

        // ── Storage ───────────────────────────────────────────────
        if (info.Storage is { Drives: { } drives })
        {
            StorageSummary = drives.Count > 0
                ? string.Join("\n", drives.Select(d =>
                    $"{d.Model ?? "Drive"} ({d.MediaType ?? "Unknown"}) — {d.Size ?? "—"} (Status: {d.Status ?? "Unknown"})"))
                : "—";
        }

        // ── Motherboard ───────────────────────────────────────────
        if (info.Motherboard is { } mb)
        {
            MotherboardName = string.IsNullOrWhiteSpace(mb.Product)
                ? (mb.Manufacturer ?? "—")
                : $"{mb.Manufacturer} {mb.Product}".Trim();
            BiosVersion = mb.BiosVersion ?? "—";
        }

        // ── Network ──────────────────────────────────────────────
        if (info.Network is { } network)
        {
            NetworkSummary = network.Count > 0
                ? string.Join("\n", network.Select(n =>
                    $"{n.ConnectionName} ({n.Name}) — {n.Type} / {n.Speed} — IP: {n.IPv4 ?? n.IPv6 ?? "No IP"}"))
                : "—";
        }

        // ── Audio ────────────────────────────────────────────────
        if (info.Audio is { } audio)
        {
            AudioDevices = audio.Count > 0
                ? string.Join("\n", audio.Select(d => $"{d.Name} ({d.Status ?? "Active"})"))
                : "—";
        }
    }

    // ── Formatting helpers ─────────────────────────────────────────────

    private static string FormatCache(string? l2, string? l3)
    {
        var parts = new System.Collections.Generic.List<string>(2);
        if (!string.IsNullOrWhiteSpace(l2)) parts.Add($"L2 {l2}");
        if (!string.IsNullOrWhiteSpace(l3)) parts.Add($"L3 {l3}");
        return parts.Count > 0 ? string.Join(" / ", parts) : "—";
    }

    private static string FormatSticks(System.Collections.Generic.IReadOnlyList<RamStickInfo>? sticks)
    {
        if (sticks is null || sticks.Count == 0)
            return "—";

        // Group identical sticks
        var groups = sticks
            .GroupBy(s => new { s.Capacity, s.MemoryType, s.SpeedMhz })
            .Select(g =>
            {
                string qty = g.Count() > 1 ? $"{g.Count()}× " : "";
                string cap = g.Key.Capacity ?? "Unknown";
                string type = string.IsNullOrWhiteSpace(g.Key.MemoryType) ? "" : $" {g.Key.MemoryType}";
                string speed = g.Key.SpeedMhz > 0 ? $" {g.Key.SpeedMhz} MHz" : "";
                return $"{qty}{cap}{type}{speed}".Trim();
            });

        return string.Join(", ", groups);
    }
}
