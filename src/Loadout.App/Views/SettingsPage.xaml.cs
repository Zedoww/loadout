using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using Loadout.App.ViewModels;

namespace Loadout.App.Views;

public partial class SettingsPage : Page
{
    public SettingsPage()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<SettingsViewModel>();
    }
}
