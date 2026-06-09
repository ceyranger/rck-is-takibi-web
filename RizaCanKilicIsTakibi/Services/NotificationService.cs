using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services.Abstractions;

namespace RizaCanKilicIsTakibi.Services;

public sealed class NotificationService : INotificationService
{
    public event EventHandler<ToastMessage>? ToastRequested;

    public void ShowToast(string message, ToastType type = ToastType.Info, TimeSpan? duration = null)
    {
        var defaultDuration = type switch
        {
            ToastType.Success => TimeSpan.FromSeconds(4),
            ToastType.Warning => TimeSpan.FromSeconds(6),
            ToastType.Error => TimeSpan.FromSeconds(8),
            _ => TimeSpan.FromSeconds(3)
        };

        ToastRequested?.Invoke(this, new ToastMessage
        {
            Message = message,
            Type = type,
            Duration = duration ?? defaultDuration
        });
    }
}
