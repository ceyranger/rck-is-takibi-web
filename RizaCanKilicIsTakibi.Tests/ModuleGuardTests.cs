using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services;
using RizaCanKilicIsTakibi.Services.Abstractions;
using RizaCanKilicIsTakibi.ViewModels;

namespace RizaCanKilicIsTakibi.Tests;

public class ModuleGuardTests
{
    [Fact]
    public async Task Karot_Delete_Command_Does_Not_Crash_When_Selection_Clears_During_Confirmation()
    {
        var confirmationService = new CallbackConfirmationService();
        var module = new KarotModuleViewModel(
            new SqliteKarotRepository(BuildDatabasePath()),
            new TestKarotStatusDialogService(),
            new NotificationService(),
            confirmationService,
            new TestTadilatCellNoteDialogService(),
            new UndoRedoService());
        confirmationService.Callback = () => module.SelectedEntry = null;

        var entry = new KarotEntry { AdaParsel = "101/1", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now };
        module.LoadFromBackup([entry]);
        module.SelectedEntry = module.Entries.Single();

        await module.DeleteKarotEntryCommand.ExecuteAsync(null);

        Assert.Empty(module.Entries);
    }

    [Fact]
    public async Task Tadilat_Delete_Command_Does_Not_Crash_When_Selection_Clears_During_Confirmation()
    {
        var confirmationService = new CallbackConfirmationService();
        var module = new TadilatModuleViewModel(
            new SqliteTadilatRepository(BuildDatabasePath()),
            new TestTadilatImportService(),
            new TestFileDialogService(),
            new NotificationService(),
            confirmationService,
            new TestTadilatCellNoteDialogService(),
            new UndoRedoService());
        confirmationService.Callback = () => module.SelectedEntry = null;

        var entry = new TadilatEntry
        {
            District = "MERKEZ",
            SubTab = TadilatSubTab.Aktif,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        module.LoadFromBackup([entry], Array.Empty<TadilatCellState>());
        module.SelectedEntry = module.AktifEntries.Single();

        await module.DeleteEntryCommand.ExecuteAsync(null);

        Assert.Empty(module.AktifEntries);
    }

    [Fact]
    public async Task Yibf_Add_Event_Command_Does_Not_Crash_When_Selection_Disappears_During_Dialog()
    {
        var notificationService = new NotificationService();
        var dialogService = new CallbackYibfAnaBilgiEventDialogService();
        var module = new YibfModuleViewModel(
            new SqliteYibfRepository(BuildDatabasePath()),
            new TestYibfImportService(),
            new TestFileDialogService(),
            notificationService,
            new CallbackConfirmationService(),
            new TestTadilatCellNoteDialogService(),
            dialogService,
            new TestYibfAnaBilgiEntryDialogService(),
            new UndoRedoService());
        dialogService.Callback = () =>
        {
            module.SelectedAnaBilgiEntry = null;
            module.AnaBilgiEntries.Clear();
        };

        var entry = new YibfAnaBilgiEntry
        {
            AdaParsel = "202/2",
            YapiSahibi = "Test",
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        module.AnaBilgiEntries.Add(entry);
        module.SelectedAnaBilgiEntry = entry;

        await module.AddAnaBilgiEventCommand.ExecuteAsync(null);

        Assert.Empty(module.AnaBilgiEvents);
    }

    [Fact]
    public async Task Yibf_Edit_Entry_Command_Does_Not_Crash_When_Selection_Disappears_During_Dialog()
    {
        var notificationService = new NotificationService();
        var dialogService = new CallbackYibfAnaBilgiEntryDialogService();
        var module = new YibfModuleViewModel(
            new SqliteYibfRepository(BuildDatabasePath()),
            new TestYibfImportService(),
            new TestFileDialogService(),
            notificationService,
            new CallbackConfirmationService(),
            new TestTadilatCellNoteDialogService(),
            new CallbackYibfAnaBilgiEventDialogService(),
            dialogService,
            new UndoRedoService());
        dialogService.Callback = () =>
        {
            module.SelectedAnaBilgiEntry = null;
            module.AnaBilgiEntries.Clear();
        };

        var entry = new YibfAnaBilgiEntry
        {
            AdaParsel = "303/3",
            YapiSahibi = "Test",
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        module.AnaBilgiEntries.Add(entry);
        module.SelectedAnaBilgiEntry = entry;

        await module.EditAnaBilgiEntryCommand.ExecuteAsync(null);

        Assert.Empty(module.AnaBilgiEntries);
    }

    [Fact]
    public async Task Yibf_Edit_Event_Command_Does_Not_Crash_When_Selection_Disappears_During_Dialog()
    {
        var notificationService = new NotificationService();
        var dialogService = new CallbackYibfAnaBilgiEventDialogService();
        var module = new YibfModuleViewModel(
            new SqliteYibfRepository(BuildDatabasePath()),
            new TestYibfImportService(),
            new TestFileDialogService(),
            notificationService,
            new CallbackConfirmationService(),
            new TestTadilatCellNoteDialogService(),
            dialogService,
            new TestYibfAnaBilgiEntryDialogService(),
            new UndoRedoService());
        dialogService.Callback = () =>
        {
            module.SelectedAnaBilgiEvent = null;
            module.AnaBilgiEvents.Clear();
        };

        var entry = new YibfAnaBilgiEntry
        {
            Id = Guid.NewGuid(),
            AdaParsel = "404/4",
            YapiSahibi = "Test",
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        var evt = new YibfAnaBilgiEvent
        {
            EntryId = entry.Id,
            EventDate = DateTime.Today,
            Description = "Takip"
        };

        module.LoadFromBackup([entry], [evt], Array.Empty<YibfIsTakibiEntry>(), Array.Empty<YibfCellState>());
        module.SelectedAnaBilgiEvent = module.AnaBilgiEvents.Single();

        await module.EditAnaBilgiEventCommand.ExecuteAsync(null);

        Assert.Empty(module.AnaBilgiEvents);
    }

    [Fact]
    public async Task Yibf_Adds_Multiple_AnaBilgi_Events_And_Persists_All()
    {
        var databasePath = BuildDatabasePath();
        var eventDialogService = new QueueYibfAnaBilgiEventDialogService();
        var module = new YibfModuleViewModel(
            new SqliteYibfRepository(databasePath),
            new TestYibfImportService(),
            new TestFileDialogService(),
            new NotificationService(),
            new CallbackConfirmationService(),
            new TestTadilatCellNoteDialogService(),
            eventDialogService,
            new TestYibfAnaBilgiEntryDialogService(),
            new UndoRedoService());

        var entry = new YibfAnaBilgiEntry
        {
            Id = Guid.NewGuid(),
            AdaParsel = "505/5",
            YapiSahibi = "Test",
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        module.LoadFromBackup([entry], Array.Empty<YibfAnaBilgiEvent>(), Array.Empty<YibfIsTakibiEntry>(), Array.Empty<YibfCellState>(), markDirty: false);
        module.SelectedAnaBilgiEntry = module.AnaBilgiEntries.Single();
        eventDialogService.Results.Enqueue(new YibfAnaBilgiEventDialogResult { EventDate = DateTime.Today, Description = "İlk olay" });
        eventDialogService.Results.Enqueue(new YibfAnaBilgiEventDialogResult { EventDate = DateTime.Today.AddDays(1), Description = "İkinci olay" });

        await module.AddAnaBilgiEventCommand.ExecuteAsync(null);
        await module.AddAnaBilgiEventCommand.ExecuteAsync(null);
        await module.PersistAsync();

        var stored = await new SqliteYibfRepository(databasePath).GetAnaBilgiEventsAsync();
        Assert.Equal(["İlk olay", "İkinci olay"], stored.OrderBy(item => item.DisplayOrder).Select(item => item.Description).ToArray());
    }

    [Fact]
    public async Task Yibf_Delete_AnaBilgi_Events_Consecutively_Removes_All_From_Persisted_Data()
    {
        var databasePath = BuildDatabasePath();
        var module = new YibfModuleViewModel(
            new SqliteYibfRepository(databasePath),
            new TestYibfImportService(),
            new TestFileDialogService(),
            new NotificationService(),
            new CallbackConfirmationService(),
            new TestTadilatCellNoteDialogService(),
            new CallbackYibfAnaBilgiEventDialogService(),
            new TestYibfAnaBilgiEntryDialogService(),
            new UndoRedoService());

        var entry = new YibfAnaBilgiEntry
        {
            Id = Guid.NewGuid(),
            AdaParsel = "606/6",
            YapiSahibi = "Test",
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };
        var firstEvent = new YibfAnaBilgiEvent
        {
            Id = Guid.NewGuid(),
            EntryId = entry.Id,
            EventDate = DateTime.Today,
            Description = "İlk olay",
            DisplayOrder = 0
        };
        var secondEvent = new YibfAnaBilgiEvent
        {
            Id = Guid.NewGuid(),
            EntryId = entry.Id,
            EventDate = DateTime.Today.AddDays(1),
            Description = "İkinci olay",
            DisplayOrder = 1
        };

        module.LoadFromBackup([entry], [firstEvent, secondEvent], Array.Empty<YibfIsTakibiEntry>(), Array.Empty<YibfCellState>(), markDirty: false);
        module.SelectedAnaBilgiEntry = module.AnaBilgiEntries.Single();
        module.SelectedAnaBilgiEvent = module.AnaBilgiEvents.Single(item => item.Id == secondEvent.Id);

        await module.DeleteAnaBilgiEventCommand.ExecuteAsync(null);
        await module.DeleteAnaBilgiEventCommand.ExecuteAsync(null);
        await module.PersistAsync();

        Assert.Empty(module.AnaBilgiEvents);
        Assert.Empty(module.VisibleEvents);
        var stored = await new SqliteYibfRepository(databasePath).GetAnaBilgiEventsAsync();
        Assert.Empty(stored);
    }

    private static string BuildDatabasePath()
        => Path.Combine(Path.GetTempPath(), "RizaCanKilicIsTakibiTests", $"{Guid.NewGuid():N}.db");

    private sealed class CallbackConfirmationService : IConfirmationService
    {
        public Action? Callback { get; set; }

        public bool Confirm(ConfirmationRequest request)
        {
            Callback?.Invoke();
            return true;
        }
    }

    private sealed class CallbackYibfAnaBilgiEventDialogService : IYibfAnaBilgiEventDialogService
    {
        public Action? Callback { get; set; }

        public Task<YibfAnaBilgiEventDialogResult?> ShowDialogAsync(DateTime? eventDate, string description, string backgroundColor, string noteText, CancellationToken cancellationToken = default)
        {
            Callback?.Invoke();
            return Task.FromResult<YibfAnaBilgiEventDialogResult?>(new YibfAnaBilgiEventDialogResult
            {
                EventDate = DateTime.Today,
                Description = "Takip",
                BackgroundColor = "#FFFFFF00",
                NoteText = string.Empty
            });
        }
    }

    private sealed class QueueYibfAnaBilgiEventDialogService : IYibfAnaBilgiEventDialogService
    {
        public Queue<YibfAnaBilgiEventDialogResult> Results { get; } = new();

        public Task<YibfAnaBilgiEventDialogResult?> ShowDialogAsync(DateTime? eventDate, string description, string backgroundColor, string noteText, CancellationToken cancellationToken = default)
            => Task.FromResult<YibfAnaBilgiEventDialogResult?>(Results.Dequeue());
    }

    private sealed class CallbackYibfAnaBilgiEntryDialogService : IYibfAnaBilgiEntryDialogService
    {
        public Action? Callback { get; set; }

        public Task<YibfAnaBilgiEntryDialogResult?> ShowDialogAsync(YibfAnaBilgiEntryDialogResult? initialValues = null, bool isEditMode = false, CancellationToken cancellationToken = default)
        {
            Callback?.Invoke();
            return Task.FromResult<YibfAnaBilgiEntryDialogResult?>(new YibfAnaBilgiEntryDialogResult
            {
                AdaParsel = "Güncel Ada Parsel",
                YibfNo = string.Empty,
                Idare = string.Empty,
                YapiSahibi = "Güncel Sahip",
                Muteahhit = string.Empty
            });
        }
    }

    private sealed class TestKarotStatusDialogService : IKarotStatusDialogService
    {
        public Task<KarotStatus?> ShowDialogAsync(KarotStatus currentStatus, CancellationToken cancellationToken = default)
            => Task.FromResult<KarotStatus?>(null);
    }

    private sealed class TestTadilatCellNoteDialogService : ITadilatCellNoteDialogService
    {
        public Task<TadilatCellNoteDialogResult?> ShowDialogAsync(string currentNote, CancellationToken cancellationToken = default)
            => Task.FromResult<TadilatCellNoteDialogResult?>(null);
    }

    private sealed class TestTadilatImportService : ITadilatImportService
    {
        public Task<TadilatImportData> ImportAsync(string filePath, CancellationToken cancellationToken = default)
            => Task.FromResult(new TadilatImportData
            {
                Entries = Array.Empty<TadilatEntry>(),
                CellStates = Array.Empty<TadilatCellState>()
            });
    }

    private sealed class TestFileDialogService : IFileDialogService
    {
        public string? ShowSaveDialog(string title, string filter, string defaultExtension) => null;

        public string? ShowOpenDialog(string title, string filter, bool multiselect = false) => null;
    }

    private sealed class TestYibfImportService : IYibfImportService
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

    private sealed class TestYibfAnaBilgiEntryDialogService : IYibfAnaBilgiEntryDialogService
    {
        public Task<YibfAnaBilgiEntryDialogResult?> ShowDialogAsync(YibfAnaBilgiEntryDialogResult? initialValues = null, bool isEditMode = false, CancellationToken cancellationToken = default)
            => Task.FromResult<YibfAnaBilgiEntryDialogResult?>(null);
    }
}
