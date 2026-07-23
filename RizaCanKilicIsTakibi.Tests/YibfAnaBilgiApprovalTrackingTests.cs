using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services;
using RizaCanKilicIsTakibi.Services.Abstractions;
using RizaCanKilicIsTakibi.ViewModels;

namespace RizaCanKilicIsTakibi.Tests;

public class YibfAnaBilgiApprovalTrackingTests
{
    [Fact]
    public void Dialog_Defaults_EventDate_To_Today_And_Maps_Category_Color()
    {
        var vm = new YibfAnaBilgiEventDialogViewModel(null, string.Empty, string.Empty, string.Empty);

        Assert.Equal(DateTime.Today, vm.EventDate?.Date);
        Assert.True(vm.IsColorEditable);

        vm.SelectedApprovalStatus = YibfAnaBilgiApprovalStatuses.Incelenecek;
        Assert.Equal(YibfAnaBilgiApprovalStatuses.ColorIncelenecek, vm.SelectedColor);
        Assert.False(vm.IsColorEditable);

        vm.SelectedApprovalStatus = YibfAnaBilgiApprovalStatuses.MuelliftenRevize;
        Assert.Equal(YibfAnaBilgiApprovalStatuses.ColorMuelliftenRevize, vm.SelectedColor);

        YibfAnaBilgiEventDialogResult? result = null;
        vm.RequestClose += (_, dialogResult) => result = dialogResult;
        vm.Description = "Denetçiye gönderildi";
        vm.SaveCommand.Execute(null);

        Assert.NotNull(result);
        Assert.Equal(YibfAnaBilgiApprovalStatuses.MuelliftenRevize, result!.ApprovalStatus);
        Assert.Equal(YibfAnaBilgiApprovalStatuses.ColorMuelliftenRevize, result.BackgroundColor);
        Assert.Equal(DateTime.Today, result.EventDate?.Date);
    }

    [Fact]
    public void PendingItem_Computes_Days_And_Overdue_Flag()
    {
        var entry = new YibfAnaBilgiEntry { AdaParsel = "1-1", YapiSahibi = "Test" };
        var pendingEvent = new YibfAnaBilgiEvent
        {
            EntryId = entry.Id,
            EventDate = DateTime.Today.AddDays(-10),
            Description = "Bekleyen",
            ApprovalStatus = YibfAnaBilgiApprovalStatuses.DenetcidenDonus,
            BackgroundColor = YibfAnaBilgiApprovalStatuses.ColorDenetcidenDonus
        };

        var item = new YibfPendingItemViewModel(entry, pendingEvent);

        Assert.Equal("Denetçiden dönüş bekleniyor", item.StatusLabel);
        Assert.Equal(10, item.DaysElapsed);
        Assert.Equal("10 gün", item.DaysElapsedText);
        Assert.True(item.IsOverdue);
        Assert.Equal(YibfAnaBilgiApprovalStatuses.DenetcidenDonus, item.FilterKey);
    }

    [Fact]
    public void ApprovalStatuses_Labels_And_Colors_Match_Plan()
    {
        Assert.Equal("İncelenecek", YibfAnaBilgiApprovalStatuses.GetLabel(YibfAnaBilgiApprovalStatuses.Incelenecek));
        Assert.Equal(YibfAnaBilgiApprovalStatuses.ColorIncelenecek, YibfAnaBilgiApprovalStatuses.GetColorForStatus(YibfAnaBilgiApprovalStatuses.Incelenecek));
        Assert.Equal(YibfAnaBilgiApprovalStatuses.ColorDenetcidenDonus, YibfAnaBilgiApprovalStatuses.GetColorForStatus(YibfAnaBilgiApprovalStatuses.DenetcidenDonus));
        Assert.Equal(YibfAnaBilgiApprovalStatuses.ColorMuelliftenRevize, YibfAnaBilgiApprovalStatuses.GetColorForStatus(YibfAnaBilgiApprovalStatuses.MuelliftenRevize));
        Assert.Equal(YibfAnaBilgiApprovalStatuses.ColorOnaylanan, YibfAnaBilgiApprovalStatuses.GetColorForStatus(YibfAnaBilgiApprovalStatuses.Onaylanan));
        Assert.True(YibfAnaBilgiApprovalStatuses.IsApproved(YibfAnaBilgiApprovalStatuses.Onaylanan));
        Assert.False(YibfAnaBilgiApprovalStatuses.IsExplicitPending(YibfAnaBilgiApprovalStatuses.Onaylanan));
        Assert.Equal(YibfAnaBilgiApprovalStatuses.FilterKategorisiz, YibfAnaBilgiApprovalStatuses.GetFilterKey(string.Empty));
    }

    [Fact]
    public void ProjeOnayTakibi_Includes_Categories_Excludes_Approved_And_Keeps_Legacy()
    {
        var root = Path.Combine(Path.GetTempPath(), "RizaCanKilicIsTakibiTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var entry = new YibfAnaBilgiEntry
            {
                AdaParsel = "10-1",
                YapiSahibi = "Sahip",
                DisplayOrder = 0,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            var events = new[]
            {
                new YibfAnaBilgiEvent
                {
                    EntryId = entry.Id,
                    EventDate = DateTime.Today.AddDays(-3),
                    Description = "İncelenecek olay",
                    ApprovalStatus = YibfAnaBilgiApprovalStatuses.Incelenecek,
                    BackgroundColor = YibfAnaBilgiApprovalStatuses.ColorIncelenecek,
                    DisplayOrder = 0
                },
                new YibfAnaBilgiEvent
                {
                    EntryId = entry.Id,
                    EventDate = DateTime.Today.AddDays(-20),
                    Description = "Revize olay",
                    ApprovalStatus = YibfAnaBilgiApprovalStatuses.MuelliftenRevize,
                    BackgroundColor = YibfAnaBilgiApprovalStatuses.ColorMuelliftenRevize,
                    DisplayOrder = 1
                },
                new YibfAnaBilgiEvent
                {
                    EntryId = entry.Id,
                    EventDate = DateTime.Today.AddDays(-1),
                    Description = "Onaylandı",
                    ApprovalStatus = YibfAnaBilgiApprovalStatuses.Onaylanan,
                    BackgroundColor = YibfAnaBilgiApprovalStatuses.ColorOnaylanan,
                    DisplayOrder = 2
                },
                new YibfAnaBilgiEvent
                {
                    EntryId = entry.Id,
                    EventDate = DateTime.Today.AddDays(-5),
                    Description = "Eski sarı",
                    ApprovalStatus = string.Empty,
                    BackgroundColor = "#FFFFFF00",
                    DisplayOrder = 3
                }
            };

            var module = new YibfModuleViewModel(
                new SqliteYibfRepository(Path.Combine(root, "yibf.db")),
                new StubYibfImportService(),
                new StubFileDialogService(),
                new NotificationService(),
                new StubConfirmationService(),
                new StubNoteDialogService(),
                new StubEventDialogService(),
                new StubEntryDialogService(),
                new UndoRedoService());

            module.LoadFromBackup([entry], events, Array.Empty<YibfIsTakibiEntry>(), Array.Empty<YibfCellState>(), markDirty: false);

            Assert.Equal(3, module.BekleyenIsler.Count);
            Assert.DoesNotContain(module.BekleyenIsler, item => item.Summary == "Onaylandı");
            Assert.Contains(module.BekleyenIsler, item => item.Summary == "Revize olay");
            Assert.Contains(module.BekleyenIsler, item => item.Summary == "Eski sarı" && item.FilterKey == YibfAnaBilgiApprovalStatuses.FilterKategorisiz);
            Assert.Equal("Revize olay", module.BekleyenIsler[0].Summary);

            module.PendingApprovalFilter = YibfAnaBilgiApprovalStatuses.MuelliftenRevize;
            Assert.Equal(1, module.FilteredBekleyenIslerCount);
            Assert.Equal(1, module.PendingFilterMuelliftenRevizeCount);
            Assert.Equal(1, module.PendingFilterKategorisizCount);
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
                // SQLite can briefly lock temp files on Windows; ignore cleanup races.
            }
        }
    }

    private sealed class StubYibfImportService : IYibfImportService
    {
        public Task<YibfImportData> ImportAsync(string filePath, CancellationToken cancellationToken = default)
            => Task.FromResult(new YibfImportData
            {
                AnaBilgiEntries = Array.Empty<YibfAnaBilgiEntry>(),
                AnaBilgiEvents = Array.Empty<YibfAnaBilgiEvent>(),
                IsTakibiEntries = Array.Empty<YibfIsTakibiEntry>(),
                CellStates = Array.Empty<YibfCellState>()
            });
    }

    private sealed class StubFileDialogService : IFileDialogService
    {
        public string? ShowSaveDialog(string title, string filter, string defaultExtension) => null;
        public string? ShowOpenDialog(string title, string filter, bool multiselect = false) => null;
    }

    private sealed class StubConfirmationService : IConfirmationService
    {
        public bool Confirm(ConfirmationRequest request) => true;
    }

    private sealed class StubNoteDialogService : ITadilatCellNoteDialogService
    {
        public Task<TadilatCellNoteDialogResult?> ShowDialogAsync(string currentNote, CancellationToken cancellationToken = default)
            => Task.FromResult<TadilatCellNoteDialogResult?>(null);
    }

    private sealed class StubEventDialogService : IYibfAnaBilgiEventDialogService
    {
        public Task<YibfAnaBilgiEventDialogResult?> ShowDialogAsync(DateTime? eventDate, string description, string backgroundColor, string noteText, string approvalStatus = "", CancellationToken cancellationToken = default)
            => Task.FromResult<YibfAnaBilgiEventDialogResult?>(null);
    }

    private sealed class StubEntryDialogService : IYibfAnaBilgiEntryDialogService
    {
        public Task<YibfAnaBilgiEntryDialogResult?> ShowDialogAsync(
            YibfAnaBilgiEntryDialogResult? initialValues = null,
            bool isEditMode = false,
            CancellationToken cancellationToken = default)
            => Task.FromResult<YibfAnaBilgiEntryDialogResult?>(null);
    }
}
