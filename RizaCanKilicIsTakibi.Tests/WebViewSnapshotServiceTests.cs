using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services;
using RizaCanKilicIsTakibi.Services.Abstractions;
using RizaCanKilicIsTakibi.ViewModels;
using System.Text.Json;

namespace RizaCanKilicIsTakibi.Tests;

public sealed class WebViewSnapshotServiceTests
{
    [Fact]
    public async Task TryExportLatestAsync_Writes_Atomically_And_Includes_Derived()
    {
        var root = Path.Combine(Path.GetTempPath(), "RizaCanKilicIsTakibiTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var backupService = new BackupService(Path.Combine(root, "backups"));
            var service = new WebViewSnapshotService(backupService);

            var entry = new YibfAnaBilgiEntry
            {
                AdaParsel = "10-1",
                YapiSahibi = "Test Sahip",
                DisplayOrder = 0,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
            var pendingEvent = new YibfAnaBilgiEvent
            {
                EntryId = entry.Id,
                EventDate = DateTime.Today.AddDays(-3),
                Description = "Bekleyen olay",
                ApprovalStatus = YibfAnaBilgiApprovalStatuses.Incelenecek,
                BackgroundColor = YibfAnaBilgiApprovalStatuses.ColorIncelenecek,
                DisplayOrder = 0
            };

            var pendingItem = new YibfPendingItemViewModel(entry, pendingEvent);
            var pendingGroup = new YibfPendingGroupViewModel(entry, [pendingItem]);

            var tumEksikler = new TumEksiklerViewModel();
            tumEksikler.RefreshFrom(
                [entry],
                [pendingEvent],
                [],
                [],
                [],
                [],
                [],
                []);

            var derived = WebViewSnapshotDerivedBuilder.Build(
                WebViewSnapshotDerivedBuilder.GetAllTumEksiklerGroups(tumEksikler),
                [pendingGroup],
                [],
                []);

            var result = await service.TryExportLatestAsync(
                new WebViewSnapshotExportRequest
                {
                    Tasks =
                    [
                        new TaskItem
                        {
                            Title = "Acil görev",
                            BoardType = TaskBoardType.Acil,
                            SortOrder = 0
                        }
                    ],
                    YibfAnaBilgiEntries = [entry],
                    YibfAnaBilgiEvents = [pendingEvent],
                    Derived = derived
                },
                root);

            Assert.NotNull(result);
            Assert.True(File.Exists(result!.FilePath));
            Assert.Equal(Path.Combine(root, IWebViewSnapshotService.LatestFileName), result.FilePath);
            Assert.DoesNotContain(".tmp", Directory.GetFiles(root).Select(Path.GetFileName), StringComparer.OrdinalIgnoreCase);

            await using var stream = File.OpenRead(result.FilePath);
            var envelope = await JsonSerializer.DeserializeAsync<WebViewSnapshotEnvelope>(stream, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            Assert.NotNull(envelope);
            Assert.Equal(WebViewSnapshotEnvelope.ExpectedKind, envelope!.Kind);
            Assert.False(string.IsNullOrWhiteSpace(envelope.Checksum));
            Assert.Equal(WebViewSnapshotService.ComputeChecksum(envelope), envelope.Checksum);
            Assert.True(envelope.Data.TryGetProperty("tasks", out var tasksElement));
            Assert.Equal(1, tasksElement.GetArrayLength());
            Assert.NotEmpty(envelope.Derived.ProjeOnayItems);
            Assert.NotEmpty(envelope.Derived.TumEksikler);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task TryExportLatestAsync_Returns_Null_For_Empty_Directory()
    {
        var service = new WebViewSnapshotService(new BackupService(Path.GetTempPath()));
        var result = await service.TryExportLatestAsync(
            new WebViewSnapshotExportRequest
            {
                Tasks = [],
                Derived = new WebViewSnapshotDerived()
            },
            "   ");
        Assert.Null(result);
    }
}
