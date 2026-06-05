using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using Wpf.Ui.Appearance;

namespace Loadout.App.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    [ObservableProperty] private bool _isDarkTheme = true;

    public string Version { get; } =
        Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0";

    public string RepositoryUrl => "https://github.com/Zedoww/loadout";

    partial void OnIsDarkThemeChanged(bool value) =>
        ApplicationThemeManager.Apply(value ? ApplicationTheme.Dark : ApplicationTheme.Light);
}
