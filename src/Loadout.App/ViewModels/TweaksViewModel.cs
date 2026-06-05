using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Loadout.Core.Optimization;

namespace Loadout.App.ViewModels;

/// <summary>Une ligne représentant un tweak registre, avec son état et son bascule.</summary>
public partial class TweakRowViewModel : ObservableObject
{
    private readonly TweakService _service;
    private readonly TweakDefinition _def;

    public string Name => _def.Name;
    public string Description => _def.Description;
    public bool RequiresReboot => _def.RequiresReboot;

    [ObservableProperty] private bool _isApplied;
    [ObservableProperty] private string _buttonText = "Activer";
    [ObservableProperty] private string _status = "";

    public TweakRowViewModel(TweakDefinition def, TweakService service)
    {
        _def = def;
        _service = service;
        Refresh();
    }

    private void Refresh()
    {
        IsApplied = _service.IsApplied(_def);
        ButtonText = IsApplied ? "Désactiver" : "Activer";
    }

    [RelayCommand]
    private void Toggle()
    {
        var result = IsApplied ? _service.Revert(_def) : _service.Apply(_def);
        Status = result.Message;
        Refresh();
    }
}

public partial class TweaksViewModel : ObservableObject
{
    public ObservableCollection<TweakRowViewModel> Tweaks { get; } = new();

    public TweaksViewModel(TweakService service)
    {
        foreach (var def in service.Definitions)
            Tweaks.Add(new TweakRowViewModel(def, service));
    }
}
