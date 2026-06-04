using System.Windows.Controls;
using Loadout.App.ViewModels;

namespace Loadout.App.Views;

public partial class CleanupPage : Page
{
    public CleanupPage()
    {
        InitializeComponent();
        DataContext = new CleanupViewModel();
    }
}
