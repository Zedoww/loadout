using System.Windows.Threading;
using Wpf.Ui.Controls;
using Opti.App.Views;

namespace Opti.App;

public partial class MainWindow : FluentWindow
{
    public MainWindow()
    {
        InitializeComponent();

        // On navigue APRÈS que l'événement Loaded soit entièrement propagé
        // (priorité Background), afin que la barre de titre WPF-UI ait fini de
        // s'initialiser. Sinon une exception pendant la navigation casserait la
        // propagation de Loaded et laisserait les boutons de fenêtre inutilisables.
        Loaded += (_, _) => Dispatcher.BeginInvoke(
            DispatcherPriority.Background,
            () => RootNavigation.Navigate(typeof(DashboardPage)));
    }
}
