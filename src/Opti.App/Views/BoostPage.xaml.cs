using System.Windows.Controls;
using Opti.App.ViewModels;

namespace Opti.App.Views;

public partial class BoostPage : Page
{
    public BoostPage()
    {
        InitializeComponent();
        DataContext = new BoostViewModel();
    }
}
