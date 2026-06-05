using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Loadout.App.Converters;
using Loadout.Core.Backup;
using Loadout.Core.Optimization;

namespace Loadout.App.ViewModels;

public partial class CleanupViewModel : ObservableObject
{
    private readonly TempCleaner _temp;
    private readonly RestorePointService _restorePoint;

    [ObservableProperty] private string _scanSummary = "Lance une analyse pour estimer l'espace récupérable.";
    [ObservableProperty] private bool _isBusy;

    public ObservableCollection<string> Log { get; } = new();

    public CleanupViewModel(TempCleaner temp, RestorePointService restorePoint)
    {
        _temp = temp;
        _restorePoint = restorePoint;
    }

    [RelayCommand]
    private async Task ScanAsync()
    {
        IsBusy = true;
        try
        {
            long bytes = await Task.Run(() => _temp.Scan());
            ScanSummary = $"Espace temporaire récupérable : {BytesToReadableConverter.Format(bytes)}";
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task CleanAsync()
    {
        IsBusy = true;
        Log.Clear();
        try
        {
            var result = await Task.Run(() => _temp.Clean());
            string line = (result.Success ? "✓ " : "✗ ") + result.Message;
            if (result.BytesFreed > 0)
                line += $" — {BytesToReadableConverter.Format(result.BytesFreed)} libérés";
            Log.Add(line);
            ScanSummary = $"Nettoyage terminé : {BytesToReadableConverter.Format(result.BytesFreed)} libérés.";
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task CreateRestorePointAsync()
    {
        IsBusy = true;
        try
        {
            var result = await Task.Run(() =>
                _restorePoint.Create($"Loadout — {DateTime.Now:g}"));
            Log.Add((result.Success ? "✓ " : "✗ ") + result.Message);
        }
        finally { IsBusy = false; }
    }
}
