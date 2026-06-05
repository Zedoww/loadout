using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Loadout.App.Converters;
using Loadout.Core.Optimization;

namespace Loadout.App.ViewModels;

public partial class SurgeViewModel : ObservableObject
{
    private readonly SurgeService _surge;

    [ObservableProperty] private bool _isSurgeActive;
    [ObservableProperty] private string _statusText = "";
    [ObservableProperty] private string _buttonText = "Activer Surge";

    public ObservableCollection<string> Log { get; } = new();

    public SurgeViewModel(SurgeService surge)
    {
        _surge = surge;
        RefreshState();
    }

    private void RefreshState()
    {
        IsSurgeActive = _surge.IsActive;
        StatusText = IsSurgeActive
            ? "Surge ACTIF — les optimisations sont appliquées."
            : "Surge inactif. Clique pour propulser ton PC en mode performance.";
        ButtonText = IsSurgeActive ? "Désactiver et restaurer" : "Activer Surge";
    }

    [RelayCommand]
    private void Toggle()
    {
        Log.Clear();

        var results = _surge.IsActive
            ? _surge.Restore()
            : _surge.Apply();

        foreach (var r in results)
        {
            string line = (r.Success ? "✓ " : "✗ ") + r.Message;
            if (r.BytesFreed > 0)
                line += $" ({BytesToReadableConverter.Format(r.BytesFreed)} libérés)";
            Log.Add(line);
        }

        RefreshState();
    }
}
