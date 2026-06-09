using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services.Abstractions;
using System.Windows;

namespace RizaCanKilicIsTakibi.Services;

public sealed class ConfirmationService : IConfirmationService
{
    public bool Confirm(ConfirmationRequest request)
    {
        var result = MessageBox.Show(
            request.Message,
            request.Title,
            MessageBoxButton.YesNo,
            request.IsDestructive ? MessageBoxImage.Warning : MessageBoxImage.Question);

        return result == MessageBoxResult.Yes;
    }
}
