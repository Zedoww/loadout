using System.Threading.Tasks;
using Wpf.Ui.Controls;

namespace Loadout.App.Services;

/// <summary>Asks the user to confirm an action. Abstracted so view models stay testable.</summary>
public interface IConfirmationService
{
    Task<bool> ConfirmAsync(string title, string message, string confirmText = "Confirm");
}

/// <summary>WPF-UI themed confirmation dialog.</summary>
public sealed class ConfirmationService : IConfirmationService
{
    public async Task<bool> ConfirmAsync(string title, string message, string confirmText = "Confirm")
    {
        var box = new MessageBox
        {
            Title = title,
            Content = message,
            PrimaryButtonText = confirmText,
            CloseButtonText = "Cancel",
        };

        return await box.ShowDialogAsync() == MessageBoxResult.Primary;
    }
}
