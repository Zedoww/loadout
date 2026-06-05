using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using Loadout.App.ViewModels;

namespace Loadout.App.Views;

public partial class ProcessesPage : Page
{
    public ProcessesPage()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<ProcessesViewModel>();
    }
}
