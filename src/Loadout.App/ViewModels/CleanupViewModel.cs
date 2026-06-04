using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Loadout.App.Converters;
using Loadout.App.Services;

namespace Loadout.App.ViewModels;

public partial class CleanupViewModel : ObservableObject
{
    [ObservableProperty] private string _scanSummary = "Lance une analyse pour estimer l'espace récupérable.";
    [ObservableProperty] private bool _isBusy;

    public ObservableCollection<string> Log { get; } = new();

    [RelayCommand]
    private async Task ScanAsync()
    {
        IsBusy = true;
        try
        {
            long bytes = await Task.Run(() => AppServices.Temp.Scan());
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
            var result = await Task.Run(() => AppServices.Temp.Clean());
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
                AppServices.RestorePoint.Create($"Loadout — {DateTime.Now:g}"));
            Log.Add((result.Success ? "✓ " : "✗ ") + result.Message);
        }
        finally { IsBusy = false; }
    }
}
