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
    public async Task Karot_GetEntriesSnapshot_And_Persist_Preserve_ProjectId()
    {
        var databasePath = BuildDatabasePath();
        var projectId = Guid.NewGuid();
        var repository = new SqliteKarotRepository(databasePath);
        var module = new KarotModuleViewModel(
            repository,
            new TestKarotStatusDialogService(),
            new NotificationService(),
            new CallbackConfirmationService(),
            new TestTadilatCellNoteDialogService(),
            new UndoRedoService());

        var entryId = Guid.NewGuid();
        module.LoadFromBackup(
        [
            new KarotEntry
            {
                Id = entryId,
                AdaParsel = "101/1",
                YapiSahibi = "Sahip",
                Status = KarotStatus.KarotAlinacak,
                ProjectId = projectId,
                DisplayOrder = 0,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            }
        ], markDirty: false);

        Assert.Equal(projectId, module.GetEntriesSnapshot().Single().ProjectId);

        module.MarkDirty();
        await module.PersistAsync();

        var saved = await repository.GetAllAsync();
        Assert.Equal(projectId, Assert.Single(saved).ProjectId);
    }

    [Fact]
    public async Task Karot_Negative_Status_Requests_Action_Draft_After_Confirmation()
    {
        var statusDialog = new TestKarotStatusDialogService { Result = KarotStatus.KarotAlindiOlumsuz };
        var module = new KarotModuleViewModel(
            new SqliteKarotRepository(BuildDatabasePath()),
            statusDialog,
            new NotificationService(),
            new CallbackConfirmationService(),
            new TestTadilatCellNoteDialogService(),
            new UndoRedoService());
        var entry = new KarotEntry { AdaParsel = "101/1", YapiSahibi = "Sahip" };
        KarotEntry? requested = null;
        module.NegativeStatusActionDraftHandler = item =>
        {
            requested = item;
            return Task.CompletedTask;
        };
        module.LoadFromBackup([entry]);

        await module.OpenKarotStatusDialogCommand.ExecuteAsync(module.Entries.Single());

        Assert.NotNull(requested);
        Assert.Equal(KarotStatus.KarotAlindiOlumsuz, requested!.Status);
        Assert.Equal(entry.Id, requested.Id);
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

    [Fact]
    public async Task Tadilat_Move_Entry_Up_Stays_Within_District_And_Persists_Order()
    {
        var databasePath = BuildDatabasePath();
        var undoRedo = new UndoRedoService();
        var module = new TadilatModuleViewModel(
            new SqliteTadilatRepository(databasePath),
            new TestTadilatImportService(),
            new TestFileDialogService(),
            new NotificationService(),
            new CallbackConfirmationService(),
            new TestTadilatCellNoteDialogService(),
            undoRedo);

        var otherDistrict = new TadilatEntry { Id = Guid.NewGuid(), District = "AYANCIK", JobName = "Başka ilçe", SubTab = TadilatSubTab.Aktif, DisplayOrder = 0, CreatedAt = DateTime.Now.AddMinutes(-3), UpdatedAt = DateTime.Now.AddMinutes(-3) };
        var first = new TadilatEntry { Id = Guid.NewGuid(), District = "SİNOP", JobName = "İlk", SubTab = TadilatSubTab.Aktif, DisplayOrder = 0, CreatedAt = DateTime.Now.AddMinutes(-2), UpdatedAt = DateTime.Now.AddMinutes(-2) };
        var second = new TadilatEntry { Id = Guid.NewGuid(), District = "SİNOP", JobName = "İkinci", SubTab = TadilatSubTab.Aktif, DisplayOrder = 1, CreatedAt = DateTime.Now.AddMinutes(-1), UpdatedAt = DateTime.Now.AddMinutes(-1) };
        module.LoadFromBackup([otherDistrict, first, second], Array.Empty<TadilatCellState>(), markDirty: false);
        module.SelectedEntry = module.AktifEntries.Single(item => item.Id == first.Id);

        await module.MoveEntryUpCommand.ExecuteAsync(module.SelectedEntry);
        Assert.Equal(["İlk", "İkinci"], module.AktifEntries.Where(item => item.District == "SİNOP").OrderBy(item => item.DisplayOrder).Select(item => item.JobName).ToArray());

        module.SelectedEntry = module.AktifEntries.Single(item => item.Id == second.Id);
        await module.MoveEntryUpCommand.ExecuteAsync(module.SelectedEntry);
        Assert.Equal(["İkinci", "İlk"], module.AktifEntries.Where(item => item.District == "SİNOP").OrderBy(item => item.DisplayOrder).Select(item => item.JobName).ToArray());

        undoRedo.Undo();
        Assert.Equal(["İlk", "İkinci"], module.AktifEntries.Where(item => item.District == "SİNOP").OrderBy(item => item.DisplayOrder).Select(item => item.JobName).ToArray());

        undoRedo.Redo();
        await module.PersistAsync();

        var stored = await new SqliteTadilatRepository(databasePath).GetAllAsync();
        Assert.Equal(["İkinci", "İlk"], stored.Where(item => item.District == "SİNOP").OrderBy(item => item.DisplayOrder).Select(item => item.JobName).ToArray());
        Assert.Equal("Başka ilçe", stored.Single(item => item.District == "AYANCIK").JobName);
    }

    [Fact]
    public void Tadilat_DisplayRows_Flattens_Districts_For_Row_Level_Virtualization()
    {
        var module = new TadilatModuleViewModel(
            new SqliteTadilatRepository(BuildDatabasePath()),
            new TestTadilatImportService(),
            new TestFileDialogService(),
            new NotificationService(),
            new CallbackConfirmationService(),
            new TestTadilatCellNoteDialogService(),
            new UndoRedoService());

        var ayancik = new TadilatEntry { Id = Guid.NewGuid(), District = "AYANCIK", JobName = "Ayancık işi", SubTab = TadilatSubTab.Aktif, DisplayOrder = 0, CreatedAt = DateTime.Now.AddMinutes(-3), UpdatedAt = DateTime.Now.AddMinutes(-3) };
        var firstSinop = new TadilatEntry { Id = Guid.NewGuid(), District = "SİNOP", JobName = "Sinop ilk", SubTab = TadilatSubTab.Aktif, DisplayOrder = 0, CreatedAt = DateTime.Now.AddMinutes(-2), UpdatedAt = DateTime.Now.AddMinutes(-2) };
        var secondSinop = new TadilatEntry { Id = Guid.NewGuid(), District = "SİNOP", JobName = "Sinop ikinci", SubTab = TadilatSubTab.Aktif, DisplayOrder = 1, CreatedAt = DateTime.Now.AddMinutes(-1), UpdatedAt = DateTime.Now.AddMinutes(-1) };

        module.LoadFromBackup([ayancik, secondSinop, firstSinop], Array.Empty<TadilatCellState>(), markDirty: false);

        var realRows = module.DisplayRows.Where(row => !row.IsPlaceholder).ToList();
        Assert.Equal(["Ayancık işi", "Sinop ilk", "Sinop ikinci"], realRows.Select(row => row.Entry!.JobName).ToArray());
        Assert.True(realRows.Single(row => row.Entry!.Id == ayancik.Id).IsFirstInDistrict);
        Assert.True(realRows.Single(row => row.Entry!.Id == firstSinop.Id).IsFirstInDistrict);
        Assert.False(realRows.Single(row => row.Entry!.Id == secondSinop.Id).IsFirstInDistrict);
        Assert.Contains(module.DisplayRows, row => row.IsPlaceholder && row.IsFirstInDistrict);
    }

    [Fact]
    public void Tadilat_Merkez_Entries_Are_Grouped_Under_Sinop_Without_Merkez_Placeholder()
    {
        var module = new TadilatModuleViewModel(
            new SqliteTadilatRepository(BuildDatabasePath()),
            new TestTadilatImportService(),
            new TestFileDialogService(),
            new NotificationService(),
            new CallbackConfirmationService(),
            new TestTadilatCellNoteDialogService(),
            new UndoRedoService());

        var merkez = new TadilatEntry
        {
            Id = Guid.NewGuid(),
            District = "MERKEZ",
            JobName = "Merkez işi",
            SubTab = TadilatSubTab.Aktif,
            DisplayOrder = 0,
            CreatedAt = DateTime.Now.AddMinutes(-2),
            UpdatedAt = DateTime.Now.AddMinutes(-2)
        };
        var sinop = new TadilatEntry
        {
            Id = Guid.NewGuid(),
            District = "SİNOP",
            JobName = "Sinop işi",
            SubTab = TadilatSubTab.Aktif,
            DisplayOrder = 1,
            CreatedAt = DateTime.Now.AddMinutes(-1),
            UpdatedAt = DateTime.Now.AddMinutes(-1)
        };

        module.LoadFromBackup([merkez, sinop], Array.Empty<TadilatCellState>(), markDirty: false);

        Assert.DoesNotContain("MERKEZ", module.Districts);
        Assert.DoesNotContain(module.DisplayRows, row => row.District == "MERKEZ" && row.IsPlaceholder);
        Assert.DoesNotContain(module.DistrictCounts, item => item.District == "MERKEZ");
        Assert.Equal(2, module.DisplayRows.Count(row => !row.IsPlaceholder && row.District == "SİNOP"));
        Assert.Equal(["Merkez işi", "Sinop işi"], module.DisplayRows
            .Where(row => !row.IsPlaceholder && row.District == "SİNOP")
            .Select(row => row.Entry!.JobName)
            .ToArray());
    }

    [Fact]
    public async Task Yibf_Move_AnaBilgi_Entry_Uses_Visible_Order_And_Persists()
    {
        var databasePath = BuildDatabasePath();
        var undoRedo = new UndoRedoService();
        var module = new YibfModuleViewModel(
            new SqliteYibfRepository(databasePath),
            new TestYibfImportService(),
            new TestFileDialogService(),
            new NotificationService(),
            new CallbackConfirmationService(),
            new TestTadilatCellNoteDialogService(),
            new CallbackYibfAnaBilgiEventDialogService(),
            new TestYibfAnaBilgiEntryDialogService(),
            undoRedo);

        var first = new YibfAnaBilgiEntry { Id = Guid.NewGuid(), AdaParsel = "1", YapiSahibi = "Alt", DisplayOrder = 0, CreatedAt = DateTime.Now.AddMinutes(-3), UpdatedAt = DateTime.Now.AddMinutes(-3) };
        var second = new YibfAnaBilgiEntry { Id = Guid.NewGuid(), AdaParsel = "2", YapiSahibi = "Orta", DisplayOrder = 1, CreatedAt = DateTime.Now.AddMinutes(-2), UpdatedAt = DateTime.Now.AddMinutes(-2) };
        var third = new YibfAnaBilgiEntry { Id = Guid.NewGuid(), AdaParsel = "3", YapiSahibi = "Üst", DisplayOrder = 2, CreatedAt = DateTime.Now.AddMinutes(-1), UpdatedAt = DateTime.Now.AddMinutes(-1) };
        module.LoadFromBackup([first, second, third], Array.Empty<YibfAnaBilgiEvent>(), Array.Empty<YibfIsTakibiEntry>(), Array.Empty<YibfCellState>(), markDirty: false);
        module.SelectedAnaBilgiEntry = module.AnaBilgiEntries.Single(item => item.Id == second.Id);

        await module.MoveAnaBilgiEntryUpCommand.ExecuteAsync(module.SelectedAnaBilgiEntry);
        Assert.Equal(["2", "3", "1"], module.TumIsler.Select(item => item.Entry.AdaParsel).ToArray());

        undoRedo.Undo();
        Assert.Equal(["3", "2", "1"], module.TumIsler.Select(item => item.Entry.AdaParsel).ToArray());

        undoRedo.Redo();
        await module.PersistAsync();

        var stored = await new SqliteYibfRepository(databasePath).GetAnaBilgiEntriesAsync();
        Assert.Equal(["2", "3", "1"], stored.OrderByDescending(item => item.DisplayOrder).Select(item => item.AdaParsel).ToArray());
    }

    [Fact]
    public async Task Yibf_Move_IsTakibi_Entry_Uses_Row_Order_And_Persists()
    {
        var databasePath = BuildDatabasePath();
        var undoRedo = new UndoRedoService();
        var module = new YibfModuleViewModel(
            new SqliteYibfRepository(databasePath),
            new TestYibfImportService(),
            new TestFileDialogService(),
            new NotificationService(),
            new CallbackConfirmationService(),
            new TestTadilatCellNoteDialogService(),
            new CallbackYibfAnaBilgiEventDialogService(),
            new TestYibfAnaBilgiEntryDialogService(),
            undoRedo);

        var first = new YibfIsTakibiEntry { Id = Guid.NewGuid(), JobName = "İlk", DisplayOrder = 0, CreatedAt = DateTime.Now.AddMinutes(-3), UpdatedAt = DateTime.Now.AddMinutes(-3) };
        var second = new YibfIsTakibiEntry { Id = Guid.NewGuid(), JobName = "İkinci", DisplayOrder = 1, CreatedAt = DateTime.Now.AddMinutes(-2), UpdatedAt = DateTime.Now.AddMinutes(-2) };
        var third = new YibfIsTakibiEntry { Id = Guid.NewGuid(), JobName = "Üçüncü", DisplayOrder = 2, CreatedAt = DateTime.Now.AddMinutes(-1), UpdatedAt = DateTime.Now.AddMinutes(-1) };
        module.LoadFromBackup(Array.Empty<YibfAnaBilgiEntry>(), Array.Empty<YibfAnaBilgiEvent>(), [first, second, third], Array.Empty<YibfCellState>(), markDirty: false);
        module.SelectedIsTakibiEntry = module.IsTakibiEntries.Single(item => item.Id == second.Id);

        await module.MoveIsTakibiEntryDownCommand.ExecuteAsync(module.SelectedIsTakibiEntry);
        Assert.Equal(["İlk", "Üçüncü", "İkinci"], module.IsTakibiRows.Select(item => item.Entry.JobName).ToArray());

        undoRedo.Undo();
        Assert.Equal(["İlk", "İkinci", "Üçüncü"], module.IsTakibiRows.Select(item => item.Entry.JobName).ToArray());

        undoRedo.Redo();
        await module.PersistAsync();

        var stored = await new SqliteYibfRepository(databasePath).GetIsTakibiEntriesAsync();
        Assert.Equal(["İlk", "Üçüncü", "İkinci"], stored.OrderBy(item => item.DisplayOrder).Select(item => item.JobName).ToArray());
    }

    [Fact]
    public void Yibf_IsTakibi_SearchText_Filters_Visible_Rows_And_Clears()
    {
        var module = new YibfModuleViewModel(
            new SqliteYibfRepository(BuildDatabasePath()),
            new TestYibfImportService(),
            new TestFileDialogService(),
            new NotificationService(),
            new CallbackConfirmationService(),
            new TestTadilatCellNoteDialogService(),
            new CallbackYibfAnaBilgiEventDialogService(),
            new TestYibfAnaBilgiEntryDialogService(),
            new UndoRedoService());

        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        var thirdId = Guid.NewGuid();
        module.LoadFromBackup(
            Array.Empty<YibfAnaBilgiEntry>(),
            Array.Empty<YibfAnaBilgiEvent>(),
            [
                new YibfIsTakibiEntry { Id = firstId, JobName = "Kadıköy Ana", DisplayOrder = 0, CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
                new YibfIsTakibiEntry { Id = secondId, JobName = "Üsküdar İstinat", DisplayOrder = 1, CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
                new YibfIsTakibiEntry { Id = thirdId, JobName = "Kadıköy Blok", DisplayOrder = 2, EvraklarTamMi = "eksik evrak", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now }
            ],
            [
                new YibfCellState { EntryId = secondId, ColumnKey = "JobName", NoteText = "özel not-xyz" }
            ],
            markDirty: false);

        Assert.Equal(3, module.IsTakibiRows.Count);
        Assert.Equal("Kayıt: 3", module.IsTakibiEntryCountDisplay);

        module.IsTakibiSearchText = "Kadıköy";
        Assert.Equal(2, module.IsTakibiRows.Count);
        Assert.Equal(["Kadıköy Ana", "Kadıköy Blok"], module.IsTakibiRows.Select(row => row.Entry.JobName).ToArray());
        Assert.Equal("Görünen: 2 / 3", module.IsTakibiEntryCountDisplay);
        Assert.False(module.HasNoVisibleIsTakibiResults);

        module.IsTakibiSearchText = "not-xyz";
        Assert.Equal(["Üsküdar İstinat"], module.IsTakibiRows.Select(row => row.Entry.JobName).ToArray());

        module.IsTakibiSearchText = "bulunamaz";
        Assert.Empty(module.IsTakibiRows);
        Assert.True(module.HasNoVisibleIsTakibiResults);

        module.ClearIsTakibiSearchCommand.Execute(null);
        Assert.Equal(3, module.IsTakibiRows.Count);
        Assert.False(module.HasActiveIsTakibiSearch);
        Assert.Equal(3, module.IsTakibiEntries.Count);
    }

    [Fact]
    public void Yibf_RequestIsTakibiScroll_Clears_Active_Search()
    {
        var module = new YibfModuleViewModel(
            new SqliteYibfRepository(BuildDatabasePath()),
            new TestYibfImportService(),
            new TestFileDialogService(),
            new NotificationService(),
            new CallbackConfirmationService(),
            new TestTadilatCellNoteDialogService(),
            new CallbackYibfAnaBilgiEventDialogService(),
            new TestYibfAnaBilgiEntryDialogService(),
            new UndoRedoService());

        var targetId = Guid.NewGuid();
        module.LoadFromBackup(
            Array.Empty<YibfAnaBilgiEntry>(),
            Array.Empty<YibfAnaBilgiEvent>(),
            [
                new YibfIsTakibiEntry { Id = Guid.NewGuid(), JobName = "SadeceAlfa", DisplayOrder = 0, CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
                new YibfIsTakibiEntry { Id = targetId, JobName = "Hedef Satır", DisplayOrder = 1, CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now }
            ],
            Array.Empty<YibfCellState>(),
            markDirty: false);

        module.IsTakibiSearchText = "SadeceAlfa";
        Assert.Single(module.IsTakibiRows);

        module.RequestIsTakibiScroll(targetId);
        Assert.False(module.HasActiveIsTakibiSearch);
        Assert.Equal(2, module.IsTakibiRows.Count);
        Assert.Equal(targetId, module.PendingIsTakibiScrollTargetId);
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

        public Task<YibfAnaBilgiEventDialogResult?> ShowDialogAsync(DateTime? eventDate, string description, string backgroundColor, string noteText, string approvalStatus = "", CancellationToken cancellationToken = default)
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

        public Task<YibfAnaBilgiEventDialogResult?> ShowDialogAsync(DateTime? eventDate, string description, string backgroundColor, string noteText, string approvalStatus = "", CancellationToken cancellationToken = default)
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
        public KarotStatus? Result { get; init; }

        public Task<KarotStatus?> ShowDialogAsync(KarotStatus currentStatus, CancellationToken cancellationToken = default)
            => Task.FromResult(Result);
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
        public string? ShowFolderDialog(string title) => null;
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
