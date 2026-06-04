using System.Windows.Controls;
using Opti.App.ViewModels;

namespace Opti.App.Views;

public partial class CleanupPage : Page
{
    public CleanupPage()
    {
        InitializeComponent();
        DataContext = new CleanupViewModel();
    }
}
