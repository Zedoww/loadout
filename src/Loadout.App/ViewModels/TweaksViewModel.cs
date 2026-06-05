using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Loadout.Core.Optimization;

namespace Loadout.App.ViewModels;

/// <summary>A single registry tweak row, with its current state and toggle.</summary>
public partial class TweakRowViewModel : ObservableObject
{
    private readonly TweakService _service;
    private readonly TweakDefinition _def;

    public string Name => _def.Name;
    public string Description => _def.Description;
    public bool RequiresReboot => _def.RequiresReboot;

    [ObservableProperty] private bool _isApplied;
    [ObservableProperty] private string _buttonText = "Enable";
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
        ButtonText = IsApplied ? "Disable" : "Enable";
    }

    [RelayCommand]
    private void Toggle()
    {
        var result = IsApplied ? _service.Revert(_def) : _service.Apply(_def);
        Status = result.Message;
        Refresh();
    }
}

/// <summary>A named group of tweaks (Performance, Privacy, Interface...).</summary>
public sealed class TweakCategoryViewModel
{
    public string Name { get; }
    public ObservableCollection<TweakRowViewModel> Tweaks { get; }

    public TweakCategoryViewModel(string name, IEnumerable<TweakRowViewModel> tweaks)
    {
        Name = name;
        Tweaks = new ObservableCollection<TweakRowViewModel>(tweaks);
    }
}

public partial class TweaksViewModel : ObservableObject
{
    public ObservableCollection<TweakCategoryViewModel> Categories { get; } = new();

    public TweaksViewModel(TweakService service)
    {
        foreach (var group in service.Definitions.GroupBy(d => d.Category))
            Categories.Add(new TweakCategoryViewModel(
                group.Key,
                group.Select(d => new TweakRowViewModel(d, service))));
    }
}
