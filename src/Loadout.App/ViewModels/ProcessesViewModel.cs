using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Loadout.App.Converters;
using Loadout.Core.Optimization;

namespace Loadout.App.ViewModels;

/// <summary>A single process row, with its suspend/resume actions.</summary>
public partial class ProcessRowViewModel : ObservableObject
{
    private readonly ProcessService _process;

    public string Name { get; }
    public int Count { get; }
    public long Bytes { get; }

    public string MemoryText => BytesToReadableConverter.Format(Bytes);
    public string CountText => Count > 1 ? $"{Count} processes" : "1 process";

    [ObservableProperty] private string _status = "";

    public ProcessRowViewModel(ProcessGroup group, ProcessService process)
    {
        _process = process;
        Name = group.Name;
        Count = group.Count;
        Bytes = group.WorkingSetBytes;
    }

    [RelayCommand]
    private void Suspend() => Status = _process.Suspend(Name).Message;

    [RelayCommand]
    private void Resume() => Status = _process.Resume(Name).Message;
}

public partial class ProcessesViewModel : ObservableObject
{
    private readonly ProcessService _process;
    private readonly MemoryCleaner _memory;

    [ObservableProperty] private string _summary = "Top applications by memory usage.";
    [ObservableProperty] private bool _isBusy;

    public ObservableCollection<ProcessRowViewModel> Processes { get; } = new();

    public ProcessesViewModel(ProcessService process, MemoryCleaner memory)
    {
        _process = process;
        _memory = memory;
        Refresh();
    }

    [RelayCommand]
    private void Refresh()
    {
        Processes.Clear();
        foreach (var g in _process.ListTopByMemory())
            Processes.Add(new ProcessRowViewModel(g, _process));

        Summary = $"{Processes.Count} applications shown (system processes are hidden).";
    }

    [RelayCommand]
    private async Task FreeMemoryAsync()
    {
        IsBusy = true;
        try
        {
            var result = await Task.Run(() => _memory.Clean());
            Summary = result.Success
                ? $"Memory freed — {BytesToReadableConverter.Format(result.BytesFreed)} reclaimed."
                : result.Message;
            Refresh();
        }
        finally { IsBusy = false; }
    }
}
