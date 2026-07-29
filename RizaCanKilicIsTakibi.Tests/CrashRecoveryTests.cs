using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services;

namespace RizaCanKilicIsTakibi.Tests;

public class CrashRecoveryTests
{
    [Fact]
    public void SummaryBuilder_Reports_Count_Deltas_And_Samples()
    {
        var current = new BackupRestoreData
        {
            Tasks = [new TaskItem { Title = "Eski", BoardType = TaskBoardType.Genel }],
            YibfAnaBilgiEvents =
            [
                new YibfAnaBilgiEvent { Description = "Mevcut olay", ApprovalStatus = string.Empty }
            ]
        };
        var recovery = new BackupRestoreData
        {
            Tasks =
            [
                new TaskItem { Title = "Eski", BoardType = TaskBoardType.Genel },
                new TaskItem { Title = "Yeni iş", BoardType = TaskBoardType.Acil }
            ],
            YibfAnaBilgiEvents =
            [
                new YibfAnaBilgiEvent { Description = "Mevcut olay", ApprovalStatus = string.Empty },
                new YibfAnaBilgiEvent { Description = "Yeni olay", ApprovalStatus = YibfAnaBilgiApprovalStatuses.Incelenecek }
            ]
        };

        var lines = CrashRecoverySummaryBuilder.Build(recovery, current);

        Assert.Contains(lines, line => line.Contains("Genel / Acil işler", StringComparison.Ordinal) && line.Contains("+1", StringComparison.Ordinal));
        Assert.Contains(lines, line => line.Contains("Proje Takibi olay", StringComparison.Ordinal) && line.Contains("Yeni olay", StringComparison.Ordinal));
    }

    [Fact]
    public async Task SessionRecoveryService_Writes_And_Clears_Pending_Files()
    {
        var root = Path.Combine(Path.GetTempPath(), "RizaCanKilicIsTakibiTests", Guid.NewGuid().ToString("N"));
        var dataDir = Path.Combine(root, "Data");
        var backupDir = Path.Combine(root, "Backup");
        Directory.CreateDirectory(dataDir);
        Directory.CreateDirectory(backupDir);

        try
        {
            var pendingPath = Path.Combine(dataDir, "pending-recovery.json");
            var flagPath = Path.Combine(dataDir, "session-dirty.flag");
            var backupService = new BackupService(backupDir);
            var recoveryService = new SessionRecoveryService(pendingPath, flagPath, backupService);

            Assert.False(recoveryService.IsPendingRecoveryAvailable());

            await recoveryService.WriteRecoverySnapshotAsync(
                [new TaskItem { Title = "Kurtarılacak", BoardType = TaskBoardType.Acil }]);

            Assert.True(File.Exists(flagPath));
            Assert.True(File.Exists(pendingPath));
            Assert.True(recoveryService.IsPendingRecoveryAvailable());

            // Flag-only should not be required once snapshot exists.
            File.Delete(flagPath);
            Assert.True(recoveryService.IsPendingRecoveryAvailable());

            var restored = await recoveryService.LoadPendingRecoveryAsync();
            Assert.NotNull(restored);
            Assert.Single(restored!.Tasks);
            Assert.Equal("Kurtarılacak", restored.Tasks[0].Title);

            // Restore flag for clear test path
            File.WriteAllText(flagPath, DateTime.Now.ToString("O"));
            recoveryService.ClearPendingRecovery();
            Assert.False(recoveryService.IsPendingRecoveryAvailable());
            Assert.False(File.Exists(pendingPath));
            Assert.False(File.Exists(flagPath));
        }
        finally
        {
            try
            {
                if (Directory.Exists(root))
                {
                    Directory.Delete(root, true);
                }
            }
            catch (IOException)
            {
            }
        }
    }
}
