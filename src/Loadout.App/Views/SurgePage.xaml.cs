using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using Loadout.App.ViewModels;

namespace Loadout.App.Views;

public partial class SurgePage : Page
{
    public SurgePage()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<SurgeViewModel>();
    }
}
