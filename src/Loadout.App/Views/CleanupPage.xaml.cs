using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using Loadout.App.ViewModels;

namespace Loadout.App.Views;

public partial class CleanupPage : Page
{
    public CleanupPage()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<CleanupViewModel>();
    }
}
