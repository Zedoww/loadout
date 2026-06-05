using System.Windows.Threading;
using Wpf.Ui.Controls;
using Loadout.App.Views;

namespace Loadout.App;

public partial class MainWindow : FluentWindow
{
    public MainWindow()
    {
        InitializeComponent();

        // Navigate AFTER the Loaded event has fully propagated (Background
        // priority), so the WPF-UI title bar finishes initializing. Otherwise an
        // exception during navigation would break the Loaded broadcast and leave
        // the window caption buttons unusable.
        Loaded += (_, _) => Dispatcher.BeginInvoke(
            DispatcherPriority.Background,
            () => RootNavigation.Navigate(typeof(DashboardPage)));
    }
}
