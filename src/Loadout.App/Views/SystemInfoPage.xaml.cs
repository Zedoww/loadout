using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using Loadout.App.ViewModels;

namespace Loadout.App.Views;

public partial class SystemInfoPage : Page
{
    public SystemInfoPage()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<SystemInfoViewModel>();
    }
}
