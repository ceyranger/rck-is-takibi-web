using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services;
using RizaCanKilicIsTakibi.Services.Abstractions;
using System.IO;
using System.Text;
using System.Windows;

namespace RizaCanKilicIsTakibi.Helpers;

public static class AppExceptionHandler
{
    private static PathService? _pathService;

    public static void Initialize(PathService pathService)
    {
        _pathService = pathService;
    }

    public static void Handle(Exception exception, INotificationService? notificationService = null)
    {
        try
        {
            WriteExceptionLog(exception);
            notificationService?.ShowToast($"Beklenmeyen hata: {exception.Message}", ToastType.Error);
            var shouldShowMessageBox =
                notificationService is null
                || Application.Current is null
                || Application.Current.Windows.Count == 0
                || Application.Current.MainWindow?.IsVisible != true;

            if (shouldShowMessageBox)
            {
                MessageBox.Show(
                    $"Beklenmeyen hata: {exception.Message}\n\nDetaylar log dosyasına yazıldı.",
                    "Uygulama Hatası",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        catch
        {
            // Intentionally ignored to avoid recursive exception handling loops.
        }
    }

    private static void WriteExceptionLog(Exception exception)
    {
        try
        {
            var logPath = _pathService?.ErrorLogPath;
            if (string.IsNullOrWhiteSpace(logPath))
            {
                return;
            }

            var builder = new StringBuilder();
            builder.AppendLine(new string('-', 90));
            builder.AppendLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            builder.AppendLine(exception.ToString());
            File.AppendAllText(logPath, builder.ToString());
        }
        catch
        {
            // Swallow to avoid recursive exception handling loops.
        }
    }
}
