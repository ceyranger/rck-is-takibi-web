using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services.Abstractions;
using System.IO;

namespace RizaCanKilicIsTakibi.Helpers;

internal static class AppSettingsRecoveryNotifier
{
    public static void NotifyIfNeeded(AppSettingsLoadResult? result, INotificationService? notificationService)
    {
        if (result?.Status != AppSettingsLoadStatus.Corrupted || notificationService is null)
        {
            return;
        }

        notificationService.ShowToast(BuildMessage(result), ToastType.Warning, TimeSpan.FromSeconds(8));
    }

    internal static string BuildMessage(AppSettingsLoadResult result)
    {
        var suffix = string.IsNullOrWhiteSpace(result.CorruptBackupPath)
            ? "Bozuk ayar dosyası korunamadı."
            : $"Kurtarılan kopya: {Path.GetFileName(result.CorruptBackupPath)}";

        if (string.IsNullOrWhiteSpace(result.ErrorMessage))
        {
            return $"Ayar dosyası bozuk olduğu için varsayılan ayarlarla açıldı. {suffix}";
        }

        return $"Ayar dosyası bozuk olduğu için varsayılan ayarlarla açıldı. {suffix} Ayrıntı: {result.ErrorMessage}";
    }
}
