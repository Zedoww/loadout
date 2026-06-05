using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using Loadout.App.ViewModels;

namespace Loadout.App.Views;

public partial class TweaksPage : Page
{
    public TweaksPage()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<TweaksViewModel>();
    }
}
