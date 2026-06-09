using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services.Abstractions;
using System.IO;
using System.Text.Json;

namespace RizaCanKilicIsTakibi.Services;

public sealed class AppSettingsService : IAppSettingsService
{
    private readonly string _settingsPath;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public AppSettingsService(string settingsPath)
    {
        _settingsPath = settingsPath;
        var directory = Path.GetDirectoryName(_settingsPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    internal string SettingsPath => _settingsPath;

    public AppSettingsLoadResult Load()
    {
        if (!File.Exists(_settingsPath))
        {
            return new AppSettingsLoadResult
            {
                Settings = new AppSettings(),
                Status = AppSettingsLoadStatus.Missing,
                OriginalPath = _settingsPath
            };
        }

        try
        {
            var json = File.ReadAllText(_settingsPath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions);
            if (settings is null)
            {
                return CreateCorruptedLoadResult("Ayar dosyası boş veya okunamaz durumda.");
            }

            return new AppSettingsLoadResult
            {
                Settings = settings,
                Status = AppSettingsLoadStatus.Success,
                OriginalPath = _settingsPath
            };
        }
        catch (Exception ex)
        {
            return CreateCorruptedLoadResult(ex.Message);
        }
    }

    public async Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(settings ?? new AppSettings(), _jsonOptions);
        var directory = Path.GetDirectoryName(_settingsPath) ?? Directory.GetCurrentDirectory();
        Directory.CreateDirectory(directory);

        var tempPath = Path.Combine(directory, $".{Path.GetFileName(_settingsPath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            await File.WriteAllTextAsync(tempPath, json, cancellationToken);

            if (File.Exists(_settingsPath))
            {
                File.Replace(tempPath, _settingsPath, destinationBackupFileName: null, ignoreMetadataErrors: true);
            }
            else
            {
                File.Move(tempPath, _settingsPath);
            }
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    private string? TryPreserveCorruptSettingsFile(out string? backupError)
    {
        backupError = null;

        try
        {
            var directory = Path.GetDirectoryName(_settingsPath) ?? Directory.GetCurrentDirectory();
            Directory.CreateDirectory(directory);

            var fileName = $"{Path.GetFileNameWithoutExtension(_settingsPath)}.{DateTime.Now:yyyyMMdd_HHmmss_fff}.corrupt{Path.GetExtension(_settingsPath)}";
            var corruptPath = Path.Combine(directory, fileName);

            File.Move(_settingsPath, corruptPath);
            return corruptPath;
        }
        catch (Exception moveEx)
        {
            try
            {
                var directory = Path.GetDirectoryName(_settingsPath) ?? Directory.GetCurrentDirectory();
                var fileName = $"{Path.GetFileNameWithoutExtension(_settingsPath)}.{DateTime.Now:yyyyMMdd_HHmmss_fff}.{Guid.NewGuid():N}.corrupt{Path.GetExtension(_settingsPath)}";
                var corruptPath = Path.Combine(directory, fileName);
                File.Copy(_settingsPath, corruptPath, overwrite: false);
                return corruptPath;
            }
            catch (Exception copyEx)
            {
                backupError = $"{moveEx.Message}; {copyEx.Message}";
                return null;
            }
        }
    }

    private AppSettingsLoadResult CreateCorruptedLoadResult(string errorMessage)
    {
        var corruptBackupPath = TryPreserveCorruptSettingsFile(out var backupError);
        var combinedError = string.IsNullOrWhiteSpace(backupError)
            ? errorMessage
            : $"{errorMessage} | Kurtarma kopyası oluşturulamadı: {backupError}";

        return new AppSettingsLoadResult
        {
            Settings = new AppSettings(),
            Status = AppSettingsLoadStatus.Corrupted,
            OriginalPath = _settingsPath,
            CorruptBackupPath = corruptBackupPath,
            ErrorMessage = combinedError
        };
    }
}
