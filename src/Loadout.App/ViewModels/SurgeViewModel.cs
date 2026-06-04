using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Loadout.App.Services;

namespace Loadout.App.ViewModels;

public partial class SurgeViewModel : ObservableObject
{
    [ObservableProperty] private bool _isSurgeActive;
    [ObservableProperty] private string _statusText = "";
    [ObservableProperty] private string _buttonText = "Activer Surge";

    public ObservableCollection<string> Log { get; } = new();

    public SurgeViewModel() => RefreshState();

    private void RefreshState()
    {
        IsSurgeActive = AppServices.Surge.IsActive;
        StatusText = IsSurgeActive
            ? "Surge ACTIF — les optimisations sont appliquées."
            : "Surge inactif. Clique pour propulser ton PC en mode performance.";
        ButtonText = IsSurgeActive ? "Désactiver et restaurer" : "Activer Surge";
    }

    [RelayCommand]
    private void Toggle()
    {
        Log.Clear();

        var results = AppServices.Surge.IsActive
            ? AppServices.Surge.Restore()
            : AppServices.Surge.Apply();

        foreach (var r in results)
        {
            string line = (r.Success ? "✓ " : "✗ ") + r.Message;
            if (r.BytesFreed > 0)
                line += $" ({Converters.BytesToReadableConverter.Format(r.BytesFreed)} libérés)";
            Log.Add(line);
        }

        RefreshState();
    }
}
