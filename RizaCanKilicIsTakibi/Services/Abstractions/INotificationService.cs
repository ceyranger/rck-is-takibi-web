using RizaCanKilicIsTakibi.Models;

namespace RizaCanKilicIsTakibi.Services.Abstractions;

public interface INotificationService
{
    event EventHandler<ToastMessage>? ToastRequested;
    event EventHandler<ToastMessage>? ToastActionInvoked;
    void ShowToast(string message, ToastType type = ToastType.Info, TimeSpan? duration = null);
    void ShowToast(
        string message,
        ToastType type,
        TimeSpan duration,
        string actionLabel,
        Action action);
}
