using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services;
using RizaCanKilicIsTakibi.Services.Abstractions;
using RizaCanKilicIsTakibi.ViewModels;

namespace RizaCanKilicIsTakibi.Tests;

public class YibfAnaBilgiApprovalTrackingTests
{
    [Fact]
    public void Dialog_Applies_Category_Color_And_Saves_It()
    {
        var vm = new YibfAnaBilgiEventDialogViewModel(null, string.Empty, string.Empty, string.Empty);

        Assert.Equal(DateTime.Today, vm.EventDate?.Date);
        Assert.Equal(YibfAnaBilgiApprovalStatuses.ColorKategorisiz, vm.SelectedColor);

        vm.SelectedApprovalStatus = YibfAnaBilgiApprovalStatuses.Incelenecek;
        Assert.Equal(YibfAnaBilgiApprovalStatuses.ColorIncelenecek, vm.SelectedColor);

        vm.SelectedApprovalStatus = YibfAnaBilgiApprovalStatuses.Pasif;
        Assert.Equal(YibfAnaBilgiApprovalStatuses.ColorPasif, vm.SelectedColor);

        YibfAnaBilgiEventDialogResult? result = null;
        vm.RequestClose += (_, dialogResult) => result = dialogResult;
        vm.Description = "Pasif proje";
        vm.SaveCommand.Execute(null);

        Assert.NotNull(result);
        Assert.Equal(YibfAnaBilgiApprovalStatuses.Pasif, result!.ApprovalStatus);
        Assert.Equal(YibfAnaBilgiApprovalStatuses.ColorPasif, result.BackgroundColor);
        Assert.Equal(DateTime.Today, result.EventDate?.Date);
    }

    [Fact]
    public void Dialog_Uses_Category_Color_Even_When_Legacy_Color_Passed()
    {
        var vm = new YibfAnaBilgiEventDialogViewModel(
            DateTime.Today,
            "eski",
            "#FF4F81BD",
            string.Empty,
            YibfAnaBilgiApprovalStatuses.Beklenen);

        Assert.Equal(YibfAnaBilgiApprovalStatuses.ColorBeklenen, vm.SelectedColor);
        Assert.Equal(YibfAnaBilgiApprovalStatuses.Beklenen, vm.SelectedApprovalStatus);
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
        Assert.Equal("#FFFFA500", YibfAnaBilgiApprovalStatuses.ColorDenetcidenDonus);
        Assert.Equal("#FFFFFF00", YibfAnaBilgiApprovalStatuses.ColorMuelliftenRevize);
        Assert.Equal(YibfAnaBilgiApprovalStatuses.ColorDenetcidenDonus, YibfAnaBilgiApprovalStatuses.GetColorForStatus(YibfAnaBilgiApprovalStatuses.DenetcidenDonus));
        Assert.Equal(YibfAnaBilgiApprovalStatuses.ColorMuelliftenRevize, YibfAnaBilgiApprovalStatuses.GetColorForStatus(YibfAnaBilgiApprovalStatuses.MuelliftenRevize));
        Assert.Equal(YibfAnaBilgiApprovalStatuses.ColorOnaylanan, YibfAnaBilgiApprovalStatuses.GetColorForStatus(YibfAnaBilgiApprovalStatuses.Onaylanan));
        Assert.Equal("Beklenen", YibfAnaBilgiApprovalStatuses.GetLabel(YibfAnaBilgiApprovalStatuses.Beklenen));
        Assert.Equal("#FFE8E0A8", YibfAnaBilgiApprovalStatuses.ColorBeklenen);
        Assert.Equal("#FF9E9E9E", YibfAnaBilgiApprovalStatuses.ColorPasif);
        Assert.Equal("#FFD9D9D9", YibfAnaBilgiApprovalStatuses.ColorKategorisiz);
        Assert.Equal(YibfAnaBilgiApprovalStatuses.ColorBeklenen, YibfAnaBilgiApprovalStatuses.GetDefaultColorForStatus(YibfAnaBilgiApprovalStatuses.Beklenen));
        Assert.Equal(0, YibfAnaBilgiApprovalStatuses.GetUrgencyRank(YibfAnaBilgiApprovalStatuses.Incelenecek));
        Assert.Equal(3, YibfAnaBilgiApprovalStatuses.GetUrgencyRank(YibfAnaBilgiApprovalStatuses.Beklenen));
        Assert.True(YibfAnaBilgiApprovalStatuses.IsExplicitPending(YibfAnaBilgiApprovalStatuses.Beklenen));
        Assert.NotEqual(YibfAnaBilgiApprovalStatuses.ColorMuelliftenRevize, YibfAnaBilgiApprovalStatuses.ColorBeklenen);
        Assert.Equal("Pasif", YibfAnaBilgiApprovalStatuses.GetLabel(YibfAnaBilgiApprovalStatuses.Pasif));
        Assert.Equal(YibfAnaBilgiApprovalStatuses.ColorPasif, YibfAnaBilgiApprovalStatuses.GetDefaultColorForStatus(YibfAnaBilgiApprovalStatuses.Pasif));
        Assert.True(YibfAnaBilgiApprovalStatuses.IsApproved(YibfAnaBilgiApprovalStatuses.Onaylanan));
        Assert.True(YibfAnaBilgiApprovalStatuses.IsPassive(YibfAnaBilgiApprovalStatuses.Pasif));
        Assert.False(YibfAnaBilgiApprovalStatuses.IsExplicitPending(YibfAnaBilgiApprovalStatuses.Onaylanan));
        Assert.False(YibfAnaBilgiApprovalStatuses.IsExplicitPending(YibfAnaBilgiApprovalStatuses.Pasif));
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
                    EventDate = DateTime.Today.AddDays(-8),
                    Description = "Beklenen olay",
                    ApprovalStatus = YibfAnaBilgiApprovalStatuses.Beklenen,
                    BackgroundColor = YibfAnaBilgiApprovalStatuses.ColorBeklenen,
                    DisplayOrder = 2
                },
                new YibfAnaBilgiEvent
                {
                    EntryId = entry.Id,
                    EventDate = DateTime.Today.AddDays(-1),
                    Description = "Onaylandı",
                    ApprovalStatus = YibfAnaBilgiApprovalStatuses.Onaylanan,
                    BackgroundColor = YibfAnaBilgiApprovalStatuses.ColorOnaylanan,
                    DisplayOrder = 3
                },
                new YibfAnaBilgiEvent
                {
                    EntryId = entry.Id,
                    EventDate = DateTime.Today.AddDays(-2),
                    Description = "Pasif olay",
                    ApprovalStatus = YibfAnaBilgiApprovalStatuses.Pasif,
                    BackgroundColor = YibfAnaBilgiApprovalStatuses.ColorPasif,
                    DisplayOrder = 4
                },
                new YibfAnaBilgiEvent
                {
                    EntryId = entry.Id,
                    EventDate = DateTime.Today.AddDays(-5),
                    Description = "Eski sarı",
                    ApprovalStatus = string.Empty,
                    BackgroundColor = "#FFFFFF00",
                    DisplayOrder = 5
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

            Assert.Equal(4, module.BekleyenIsler.Count);
            Assert.DoesNotContain(module.BekleyenIsler, item => item.Summary == "Onaylandı");
            Assert.DoesNotContain(module.BekleyenIsler, item => item.Summary == "Pasif olay");
            Assert.Contains(module.BekleyenIsler, item => item.Summary == "Revize olay");
            Assert.Contains(module.BekleyenIsler, item => item.Summary == "Beklenen olay" && item.FilterKey == YibfAnaBilgiApprovalStatuses.Beklenen);
            Assert.Contains(module.BekleyenIsler, item => item.Summary == "Eski sarı" && item.FilterKey == YibfAnaBilgiApprovalStatuses.FilterKategorisiz);
            Assert.Equal("Revize olay", module.BekleyenIsler[0].Summary);

            module.PendingApprovalFilter = YibfAnaBilgiApprovalStatuses.MuelliftenRevize;
            Assert.Equal(1, module.FilteredBekleyenIslerCount);
            Assert.Equal(1, module.PendingFilterMuelliftenRevizeCount);
            Assert.Equal(1, module.PendingFilterBeklenenCount);
            Assert.Equal(1, module.PendingFilterKategorisizCount);

            module.PendingApprovalFilter = YibfAnaBilgiApprovalStatuses.Beklenen;
            Assert.Equal(1, module.FilteredBekleyenIslerCount);
            Assert.Equal("Beklenen olay", module.BekleyenIsler.Single(item => item.FilterKey == YibfAnaBilgiApprovalStatuses.Beklenen).Summary);
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

    [Fact]
    public void ProjeOnayTakibi_Groups_All_Pending_Events_Under_Same_Entry()
    {
        var root = Path.Combine(Path.GetTempPath(), "RizaCanKilicIsTakibiTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var entry = new YibfAnaBilgiEntry
            {
                AdaParsel = "55-5",
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
                    EventDate = DateTime.Today.AddDays(-2),
                    Description = "İncelenecek kart",
                    ApprovalStatus = YibfAnaBilgiApprovalStatuses.Incelenecek,
                    BackgroundColor = YibfAnaBilgiApprovalStatuses.ColorIncelenecek,
                    DisplayOrder = 0
                },
                new YibfAnaBilgiEvent
                {
                    EntryId = entry.Id,
                    EventDate = DateTime.Today.AddDays(-1),
                    Description = "Beklenen kart",
                    ApprovalStatus = YibfAnaBilgiApprovalStatuses.Beklenen,
                    BackgroundColor = YibfAnaBilgiApprovalStatuses.ColorBeklenen,
                    DisplayOrder = 1
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

            Assert.Equal(2, module.BekleyenIsler.Count);
            var group = Assert.Single(module.BekleyenGruplar);
            Assert.Equal(2, group.EventCount);
            Assert.Equal(2, group.VisibleEvents.Count);
            Assert.Equal("2 olay", group.EventCountText);
            Assert.Contains(group.AllEvents, item => item.Summary == "İncelenecek kart");
            Assert.Contains(group.AllEvents, item => item.Summary == "Beklenen kart");
            Assert.Equal(0, group.UrgencyRank);
            Assert.Equal(1, module.FilteredBekleyenGruplarCount);

            module.PendingApprovalFilter = YibfAnaBilgiApprovalStatuses.Beklenen;
            Assert.Equal(1, module.FilteredBekleyenGruplarCount);
            Assert.Equal("1 olay", group.EventCountText);
            Assert.Equal("Beklenen kart", Assert.Single(group.VisibleEvents).Summary);
            Assert.Equal(2, group.AllEvents.Count);

            module.PendingApprovalFilter = YibfAnaBilgiApprovalStatuses.Incelenecek;
            Assert.Equal(1, module.FilteredBekleyenGruplarCount);
            Assert.Equal("İncelenecek kart", Assert.Single(group.VisibleEvents).Summary);

            module.PendingApprovalFilter = YibfAnaBilgiApprovalStatuses.FilterAll;
            Assert.Equal(2, group.VisibleEvents.Count);
            Assert.Equal("2 olay", group.EventCountText);
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

    [Fact]
    public async Task EditPendingItem_Opens_Dialog_For_Selected_Pending_Event()
    {
        var root = Path.Combine(Path.GetTempPath(), "RizaCanKilicIsTakibiTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var entry = new YibfAnaBilgiEntry
            {
                AdaParsel = "22-2",
                YapiSahibi = "Sahip",
                DisplayOrder = 0,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
            var pendingEvent = new YibfAnaBilgiEvent
            {
                EntryId = entry.Id,
                EventDate = DateTime.Today.AddDays(-4),
                Description = "Eski açıklama",
                ApprovalStatus = YibfAnaBilgiApprovalStatuses.Incelenecek,
                BackgroundColor = YibfAnaBilgiApprovalStatuses.ColorIncelenecek,
                DisplayOrder = 0
            };

            var dialogService = new CapturingEventDialogService
            {
                Result = new YibfAnaBilgiEventDialogResult
                {
                    EventDate = DateTime.Today,
                    Description = "Güncellendi",
                    BackgroundColor = YibfAnaBilgiApprovalStatuses.ColorDenetcidenDonus,
                    ApprovalStatus = YibfAnaBilgiApprovalStatuses.DenetcidenDonus,
                    NoteText = "not"
                }
            };

            var module = new YibfModuleViewModel(
                new SqliteYibfRepository(Path.Combine(root, "yibf.db")),
                new StubYibfImportService(),
                new StubFileDialogService(),
                new NotificationService(),
                new StubConfirmationService(),
                new StubNoteDialogService(),
                dialogService,
                new StubEntryDialogService(),
                new UndoRedoService());

            module.LoadFromBackup([entry], [pendingEvent], Array.Empty<YibfIsTakibiEntry>(), Array.Empty<YibfCellState>(), markDirty: false);
            var pendingItem = Assert.Single(module.BekleyenIsler);

            await module.EditPendingItemCommand.ExecuteAsync(pendingItem);

            Assert.Equal(1, dialogService.CallCount);
            Assert.Equal("Eski açıklama", dialogService.LastDescription);
            Assert.Equal(YibfAnaBilgiApprovalStatuses.Incelenecek, dialogService.LastApprovalStatus);
            Assert.Equal(pendingEvent.Id, module.SelectedAnaBilgiEvent?.Id);
            Assert.Equal("Güncellendi", module.SelectedAnaBilgiEvent?.Description);
            Assert.Equal(YibfAnaBilgiApprovalStatuses.DenetcidenDonus, module.SelectedAnaBilgiEvent?.ApprovalStatus);
            Assert.Equal("Denetçiden dönüş bekleniyor", Assert.Single(module.BekleyenIsler).StatusLabel);
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

    private sealed class CapturingEventDialogService : IYibfAnaBilgiEventDialogService
    {
        public YibfAnaBilgiEventDialogResult? Result { get; set; }
        public int CallCount { get; private set; }
        public string? LastDescription { get; private set; }
        public string? LastApprovalStatus { get; private set; }

        public Task<YibfAnaBilgiEventDialogResult?> ShowDialogAsync(
            DateTime? eventDate,
            string description,
            string backgroundColor,
            string noteText,
            string approvalStatus = "",
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastDescription = description;
            LastApprovalStatus = approvalStatus;
            return Task.FromResult(Result);
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
