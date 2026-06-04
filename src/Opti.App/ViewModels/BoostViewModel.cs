using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Opti.App.Services;

namespace Opti.App.ViewModels;

public partial class BoostViewModel : ObservableObject
{
    [ObservableProperty] private bool _isBoostActive;
    [ObservableProperty] private string _statusText = "";
    [ObservableProperty] private string _buttonText = "Activer le mode jeu";

    public ObservableCollection<string> Log { get; } = new();

    public BoostViewModel() => RefreshState();

    private void RefreshState()
    {
        IsBoostActive = AppServices.GameBoost.IsActive;
        StatusText = IsBoostActive
            ? "Mode jeu ACTIF — les optimisations sont appliquées."
            : "Mode jeu inactif. Clique pour passer ton PC en mode performance.";
        ButtonText = IsBoostActive ? "Désactiver et restaurer" : "Activer le mode jeu";
    }

    [RelayCommand]
    private void Toggle()
    {
        Log.Clear();

        var results = AppServices.GameBoost.IsActive
            ? AppServices.GameBoost.Restore()
            : AppServices.GameBoost.Apply();

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
