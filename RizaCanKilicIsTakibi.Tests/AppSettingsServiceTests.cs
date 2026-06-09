using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services;

namespace RizaCanKilicIsTakibi.Tests;

public class AppSettingsServiceTests
{
    [Fact]
    public async Task SaveAsync_Writes_Without_Leaving_Temp_File()
    {
        var root = CreateTempRoot();
        var settingsPath = Path.Combine(root, "settings.json");

        try
        {
            var service = new AppSettingsService(settingsPath);

            await service.SaveAsync(new AppSettings
            {
                AutoBackupEnabled = true,
                AutoBackupMinutes = 30,
                SeedSampleDataOnEmpty = false
            });

            await service.SaveAsync(new AppSettings
            {
                AutoBackupEnabled = false,
                AutoBackupMinutes = 10,
                SeedSampleDataOnEmpty = true
            });

            Assert.True(File.Exists(settingsPath));
            Assert.Empty(Directory.EnumerateFiles(root, "*.tmp", SearchOption.TopDirectoryOnly));
            var loadResult = service.Load();
            Assert.Equal(AppSettingsLoadStatus.Success, loadResult.Status);
            Assert.Equal(10, loadResult.Settings.AutoBackupMinutes);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task Load_Returns_Corrupted_Result_And_Preserves_Broken_File()
    {
        var root = CreateTempRoot();
        var settingsPath = Path.Combine(root, "settings.json");

        try
        {
            await File.WriteAllTextAsync(settingsPath, "{ this is not valid json");
            var service = new AppSettingsService(settingsPath);

            var loadResult = service.Load();

            Assert.Equal(AppSettingsLoadStatus.Corrupted, loadResult.Status);
            Assert.True(loadResult.Settings.AutoBackupEnabled);
            Assert.Equal(15, loadResult.Settings.AutoBackupMinutes);
            Assert.False(File.Exists(settingsPath));
            Assert.False(string.IsNullOrWhiteSpace(loadResult.CorruptBackupPath));
            Assert.True(File.Exists(loadResult.CorruptBackupPath!));

            await service.SaveAsync(new AppSettings
            {
                AutoBackupEnabled = false,
                AutoBackupMinutes = 5,
                SeedSampleDataOnEmpty = true
            });

            Assert.True(File.Exists(settingsPath));
            Assert.Equal(5, service.Load().Settings.AutoBackupMinutes);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task Load_Returns_Missing_Result_When_File_Does_Not_Exist()
    {
        var root = CreateTempRoot();
        var settingsPath = Path.Combine(root, "settings.json");

        try
        {
            var service = new AppSettingsService(settingsPath);

            var loadResult = service.Load();

            Assert.Equal(AppSettingsLoadStatus.Missing, loadResult.Status);
            Assert.True(loadResult.Settings.AutoBackupEnabled);
            Assert.Equal(15, loadResult.Settings.AutoBackupMinutes);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    private static string CreateTempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "RizaCanKilicIsTakibiTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    private static void DeleteDirectory(string root)
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, true);
        }
    }
}
