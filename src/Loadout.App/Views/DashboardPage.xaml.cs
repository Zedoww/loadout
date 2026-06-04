using System.Windows.Controls;
using Loadout.App.ViewModels;

namespace Loadout.App.Views;

public partial class DashboardPage : Page
{
    public DashboardPage()
    {
        InitializeComponent();
        DataContext = new DashboardViewModel();
    }
}
