using System.Windows.Controls;
using Loadout.App.ViewModels;

namespace Loadout.App.Views;

public partial class SurgePage : Page
{
    public SurgePage()
    {
        InitializeComponent();
        DataContext = new SurgeViewModel();
    }
}
