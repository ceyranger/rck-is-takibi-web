using RizaCanKilicIsTakibi.Models;

namespace RizaCanKilicIsTakibi.Services.Abstractions;

public interface INotificationService
{
    event EventHandler<ToastMessage>? ToastRequested;
    void ShowToast(string message, ToastType type = ToastType.Info, TimeSpan? duration = null);
}
