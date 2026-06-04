using System.Windows.Controls;
using Opti.App.ViewModels;

namespace Opti.App.Views;

public partial class DashboardPage : Page
{
    public DashboardPage()
    {
        InitializeComponent();
        DataContext = new DashboardViewModel();
    }
}
