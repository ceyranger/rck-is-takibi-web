using System.IO;
namespace RizaCanKilicIsTakibi.Services;

/// <summary>
/// Uygulama dosya yollarını merkezi olarak yöneten servis.
/// Tüm dosyalar uygulamanın bulunduğu klasörde tutulur.
/// </summary>
public sealed class PathService
{
    private readonly string _applicationDirectory;
    private readonly string _dataDirectory;
    private readonly string _backupDirectory;
    private readonly string _logsDirectory;

    public PathService()
    {
        _applicationDirectory = GetApplicationDirectory();
        _dataDirectory = Path.Combine(_applicationDirectory, "Data");
        _backupDirectory = Path.Combine(_applicationDirectory, "Backup");
        _logsDirectory = Path.Combine(_applicationDirectory, "Logs");

        EnsureDirectoriesExist();
    }

    /// <summary>
    /// Uygulamanın bulunduğu klasör yolu
    /// </summary>
    public string ApplicationDirectory => _applicationDirectory;

    /// <summary>
    /// Veritabanı dosyalarının bulunduğu klasör yolu
    /// </summary>
    public string DataDirectory => _dataDirectory;

    /// <summary>
    /// Yedek dosyalarının bulunduğu klasör yolu
    /// </summary>
    public string BackupDirectory => _backupDirectory;

    /// <summary>
    /// Log dosyalarının bulunduğu klasör yolu
    /// </summary>
    public string LogsDirectory => _logsDirectory;

    /// <summary>
    /// Ana veritabanı dosyasının tam yolu
    /// </summary>
    public string DatabasePath => Path.Combine(_dataDirectory, "tasks.db");

    /// <summary>
    /// Ayarlar dosyasının tam yolu
    /// </summary>
    public string SettingsPath => Path.Combine(_dataDirectory, "settings.json");

    /// <summary>
    /// Son başarılı global kayıt zamanının tutulduğu metadata dosyası
    /// </summary>
    public string LastSaveMetadataPath => Path.Combine(_dataDirectory, "last-save.json");

    /// <summary>
    /// Ani kapanış sonrası kurtarma anlık görüntüsü
    /// </summary>
    public string PendingRecoveryPath => Path.Combine(_dataDirectory, "pending-recovery.json");

    /// <summary>
    /// Kaydedilmemiş oturum işareti (ani kapanışta kalır)
    /// </summary>
    public string SessionDirtyFlagPath => Path.Combine(_dataDirectory, "session-dirty.flag");

    /// <summary>
    /// Hata log dosyasının tam yolu
    /// </summary>
    public string ErrorLogPath => Path.Combine(_logsDirectory, "app-errors.log");

    private static string GetApplicationDirectory()
    {
        var baseDirectory = AppContext.BaseDirectory;
        
        if (!string.IsNullOrWhiteSpace(baseDirectory))
        {
            return baseDirectory;
        }

        // Son fallback: mevcut çalışma dizini
        return Directory.GetCurrentDirectory();
    }

    private void EnsureDirectoriesExist()
    {
        Directory.CreateDirectory(_dataDirectory);
        Directory.CreateDirectory(_backupDirectory);
        Directory.CreateDirectory(_logsDirectory);
    }
}
