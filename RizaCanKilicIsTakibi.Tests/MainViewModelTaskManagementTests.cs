using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services;
using RizaCanKilicIsTakibi.Services.Abstractions;
using RizaCanKilicIsTakibi.ViewModels;
using System.Windows;

namespace RizaCanKilicIsTakibi.Tests;

public class MainViewModelTaskManagementTests
{
    [Fact]
    public async Task SaveStatus_Uses_Last_Persisted_Time_On_Startup()
    {
        var root = CreateTempRoot();
        var databasePath = Path.Combine(root, "save-status-initial.db");
        var settingsPath = Path.Combine(root, "settings.json");
        var metadataPath = Path.Combine(root, "last-save.json");

        try
        {
            var settings = new AppSettings
            {
                AutoBackupEnabled = false,
                AutoBackupMinutes = 15,
                SeedSampleDataOnEmpty = false
            };
            var settingsService = new AppSettingsService(settingsPath);
            var metadataService = new LastSaveMetadataService(metadataPath);
            var baseline = new DateTime(2026, 3, 28, 18, 30, 0);

            _ = new SqliteTaskRepository(databasePath);
            await metadataService.SaveLastSuccessfulSaveAtAsync(baseline);
            await settingsService.SaveAsync(settings);
            File.SetLastWriteTime(databasePath, baseline.AddDays(1));
            File.SetLastWriteTime(settingsPath, baseline.AddDays(1).AddMinutes(3));

            var mainViewModel = await CreateMainViewModelAsync(databasePath, settingsService, metadataService, settings);

            Assert.Equal("Kaydedildi", mainViewModel.SaveStatusText);
            Assert.Equal(baseline, mainViewModel.LastSuccessfulSaveAt);
            Assert.Equal($"Son kayıt: {baseline:dd.MM.yyyy HH:mm}", mainViewModel.SaveStatusTimestampText);
        }
        finally
        {
            await DeleteDirectoryWithRetriesAsync(root);
        }
    }

    [Fact]
    public async Task SaveStatus_Uses_Session_Default_When_No_Persisted_Save_Exists()
    {
        var root = CreateTempRoot();
        var databasePath = Path.Combine(root, "save-status-empty.db");

        try
        {
            var mainViewModel = await CreateMainViewModelAsync(databasePath);

            Assert.Equal("Kaydedildi", mainViewModel.SaveStatusText);
            Assert.Equal("Bu oturumda kayıt yapılmadı", mainViewModel.SaveStatusTimestampText);
            Assert.Null(mainViewModel.LastSuccessfulSaveAt);
        }
        finally
        {
            await DeleteDirectoryWithRetriesAsync(root);
        }
    }

    [Fact]
    public async Task SaveStatus_Falls_Back_To_Persisted_Time_When_Metadata_Is_Missing()
    {
        var root = CreateTempRoot();
        var databasePath = Path.Combine(root, "save-status-fallback.db");
        var settingsPath = Path.Combine(root, "settings.json");

        try
        {
            var settings = new AppSettings
            {
                AutoBackupEnabled = false,
                AutoBackupMinutes = 15,
                SeedSampleDataOnEmpty = false
            };
            var settingsService = new AppSettingsService(settingsPath);
            var baseline = new DateTime(2026, 3, 27, 11, 15, 0);

            _ = new SqliteTaskRepository(databasePath);
            await settingsService.SaveAsync(settings);
            File.SetLastWriteTime(databasePath, baseline);
            File.SetLastWriteTime(settingsPath, baseline.AddMinutes(2));

            var mainViewModel = await CreateMainViewModelAsync(databasePath, settingsService, settings: settings);

            Assert.Equal(baseline.AddMinutes(2), mainViewModel.LastSuccessfulSaveAt);
            Assert.Equal($"Son kayıt: {baseline.AddMinutes(2):dd.MM.yyyy HH:mm}", mainViewModel.SaveStatusTimestampText);
        }
        finally
        {
            await DeleteDirectoryWithRetriesAsync(root);
        }
    }

    [Fact]
    public async Task SaveStatus_Shows_Unsaved_Warning_And_Updates_After_General_Save()
    {
        var root = CreateTempRoot();
        var databasePath = Path.Combine(root, "save-status-general.db");
        var metadataPath = Path.Combine(root, "last-save.json");

        try
        {
            var metadataService = new LastSaveMetadataService(metadataPath);
            var mainViewModel = await CreateMainViewModelAsync(databasePath, lastSaveMetadataService: metadataService);

            mainViewModel.AddGeneralTaskCommand.Execute(null);

            Assert.True(mainViewModel.HasAnyUnsavedChanges);
            Assert.Equal("Kaydedilmedi", mainViewModel.SaveStatusText);
            Assert.Equal("Bu oturumda kayıt yapılmadı", mainViewModel.SaveStatusTimestampText);

            await mainViewModel.SaveActiveTabCommand.ExecuteAsync(null);

            Assert.False(mainViewModel.HasAnyUnsavedChanges);
            Assert.Equal("Kaydedildi", mainViewModel.SaveStatusText);
            Assert.NotNull(mainViewModel.LastSuccessfulSaveAt);
            Assert.StartsWith("Son kayıt:", mainViewModel.SaveStatusTimestampText);
            Assert.Equal(mainViewModel.LastSuccessfulSaveAt, metadataService.LoadLastSuccessfulSaveAt());
        }
        finally
        {
            await DeleteDirectoryWithRetriesAsync(root);
        }
    }

    [Fact]
    public async Task SaveStatus_Updates_After_Settings_Save()
    {
        var root = CreateTempRoot();
        var databasePath = Path.Combine(root, "save-status-settings.db");

        try
        {
            var mainViewModel = await CreateMainViewModelAsync(databasePath);

            mainViewModel.AutoBackupMinutes = 30;
            mainViewModel.SelectMainTabCommand.Execute(MainNavigationTab.Ayarlar);

            Assert.True(mainViewModel.HasAnyUnsavedChanges);
            Assert.Equal("Kaydedilmedi", mainViewModel.SaveStatusText);

            await mainViewModel.SaveActiveTabCommand.ExecuteAsync(null);

            Assert.False(mainViewModel.HasAnyUnsavedChanges);
            Assert.NotNull(mainViewModel.LastSuccessfulSaveAt);
            Assert.StartsWith("Son kayıt:", mainViewModel.SaveStatusTimestampText);
        }
        finally
        {
            await DeleteDirectoryWithRetriesAsync(root);
        }
    }

    [Fact]
    public async Task SaveStatus_Does_Not_Advance_When_Settings_Save_Fails()
    {
        var root = CreateTempRoot();
        var databasePath = Path.Combine(root, "save-status-settings-fail.db");

        try
        {
            var failingSettingsService = new FailSaveAppSettingsService(new AppSettings
            {
                AutoBackupEnabled = false,
                AutoBackupMinutes = 15,
                SeedSampleDataOnEmpty = false
            });
            var mainViewModel = await CreateMainViewModelAsync(databasePath, failingSettingsService, settings: failingSettingsService.Settings);

            mainViewModel.AutoBackupMinutes = 45;
            mainViewModel.SelectMainTabCommand.Execute(MainNavigationTab.Ayarlar);

            var beforeSave = mainViewModel.LastSuccessfulSaveAt;
            await mainViewModel.SaveActiveTabCommand.ExecuteAsync(null);

            Assert.True(mainViewModel.HasAnyUnsavedChanges);
            Assert.Equal("Kaydedilmedi", mainViewModel.SaveStatusText);
            Assert.Equal(beforeSave, mainViewModel.LastSuccessfulSaveAt);
        }
        finally
        {
            await DeleteDirectoryWithRetriesAsync(root);
        }
    }

    [Fact]
    public async Task AddGeneralTaskCommand_Inserts_New_Task_At_Top_And_UndoRedo_Preserves_Position()
    {
        var root = CreateTempRoot();
        var databasePath = Path.Combine(root, "main.db");

        try
        {
            var taskRepository = new SqliteTaskRepository(databasePath);
            await taskRepository.SaveManyAsync(
            [
                CreateTask("Eski 1", TaskBoardType.Genel, 0),
                CreateTask("Eski 2", TaskBoardType.Genel, 1)
            ]);

            var mainViewModel = await CreateMainViewModelAsync(databasePath);

            mainViewModel.AddGeneralTaskCommand.Execute(null);

            Assert.Equal("Yeni iş", mainViewModel.GeneralBoard.Tasks[0].Title);
            Assert.Equal(new[] { 0, 1, 2 }, mainViewModel.GeneralBoard.Tasks.Select(task => task.SortOrder));

            mainViewModel.UndoCommand.Execute(null);
            Assert.DoesNotContain(mainViewModel.GeneralBoard.Tasks, task => task.Title == "Yeni iş");

            mainViewModel.RedoCommand.Execute(null);
            Assert.Equal("Yeni iş", mainViewModel.GeneralBoard.Tasks[0].Title);
        }
        finally
        {
            await DeleteDirectoryWithRetriesAsync(root);
        }
    }

    [Fact]
    public async Task QuickUrgentTaskDialog_Adds_Selected_Templates_To_Top_With_UndoRedo()
    {
        var root = CreateTempRoot();
        var databasePath = Path.Combine(root, "quick-urgent.db");

        try
        {
            var taskRepository = new SqliteTaskRepository(databasePath);
            await taskRepository.SaveManyAsync(
            [
                CreateTask("Mevcut acil", TaskBoardType.Acil, 0)
            ]);

            var dialogService = new TestQuickTaskTemplateDialogService(["Birinci hızlı iş", "İkinci hızlı iş"]);
            var mainViewModel = await CreateMainViewModelAsync(
                databasePath,
                quickTaskTemplateRepository: new SqliteQuickTaskTemplateRepository(databasePath),
                quickTaskTemplateDialogService: dialogService);

            await mainViewModel.OpenQuickUrgentTaskDialogCommand.ExecuteAsync(null);

            Assert.Equal(
                ["Birinci hızlı iş", "İkinci hızlı iş", "Mevcut acil"],
                mainViewModel.UrgentBoard.Tasks.Select(task => task.Title).ToArray());
            Assert.Equal([0, 1, 2], mainViewModel.UrgentBoard.Tasks.Select(task => task.SortOrder).ToArray());
            Assert.True(mainViewModel.HasAnyUnsavedChanges);

            mainViewModel.UndoCommand.Execute(null);
            Assert.Equal(["Mevcut acil"], mainViewModel.UrgentBoard.Tasks.Select(task => task.Title).ToArray());

            mainViewModel.RedoCommand.Execute(null);
            Assert.Equal(
                ["Birinci hızlı iş", "İkinci hızlı iş", "Mevcut acil"],
                mainViewModel.UrgentBoard.Tasks.Select(task => task.Title).ToArray());
        }
        finally
        {
            await DeleteDirectoryWithRetriesAsync(root);
        }
    }

    [Fact]
    public async Task QuickTaskTemplateDialog_SelectAll_Returns_All_Template_Titles()
    {
        var root = CreateTempRoot();
        var databasePath = Path.Combine(root, "quick-dialog.db");

        try
        {
            var repository = new SqliteQuickTaskTemplateRepository(databasePath);
            repository.ReplaceAll(
            [
                new QuickTaskTemplate { GroupName = "Aybaşı İşlemleri", Title = "Acil kontrol", SortOrder = 0, CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
                new QuickTaskTemplate { GroupName = "Aybaşı İşlemleri", Title = "Evrak iste", SortOrder = 1, CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now }
            ]);
            var viewModel = new QuickTaskTemplateDialogViewModel(repository, repository.GetAll());

            viewModel.SelectedGroup = viewModel.Groups.Single(group => group.Name == "Aybaşı İşlemleri");
            viewModel.AddSelectedGroupTasksCommand.Execute(null);

            Assert.Equal(["Acil kontrol", "Evrak iste"], viewModel.SelectedTitles);
        }
        finally
        {
            await DeleteDirectoryWithRetriesAsync(root);
        }
    }

    [Fact]
    public async Task PasteTask_Still_Appends_To_End_Of_List()
    {
        var root = CreateTempRoot();
        var databasePath = Path.Combine(root, "paste.db");

        try
        {
            var taskRepository = new SqliteTaskRepository(databasePath);
            await taskRepository.SaveManyAsync(
            [
                CreateTask("İlk", TaskBoardType.Genel, 0),
                CreateTask("Son", TaskBoardType.Genel, 1)
            ]);

            var mainViewModel = await CreateMainViewModelAsync(databasePath);
            mainViewModel.GeneralBoard.SelectedTask = mainViewModel.GeneralBoard.Tasks[0];
            mainViewModel.CopyTaskCommand.Execute(null);
            mainViewModel.PasteTaskToBoardCommand.Execute(TaskBoardType.Genel);

            Assert.Equal("İlk", mainViewModel.GeneralBoard.Tasks[^1].Title);
            Assert.Equal(new[] { 0, 1, 2 }, mainViewModel.GeneralBoard.Tasks.Select(task => task.SortOrder));
        }
        finally
        {
            await DeleteDirectoryWithRetriesAsync(root);
        }
    }

    [Fact]
    public async Task SaveActiveTabCommand_Persists_Karot_Changes_After_Add_And_Edit()
    {
        var root = CreateTempRoot();
        var databasePath = Path.Combine(root, "karot-save.db");

        try
        {
            var karotRepository = new SqliteKarotRepository(databasePath);
            var mainViewModel = await CreateMainViewModelAsync(databasePath);

            mainViewModel.SelectMainTabCommand.Execute(MainNavigationTab.KarotTakibi);
            await mainViewModel.KarotModule.AddKarotEntryCommand.ExecuteAsync(null);
            mainViewModel.KarotModule.SelectedEntry!.AdaParsel = "101/1";
            mainViewModel.KarotModule.SelectedEntry.YapiSahibi = "Test Sahibi";

            Assert.True(mainViewModel.KarotModule.HasUnsavedChanges);

            await mainViewModel.SaveActiveTabCommand.ExecuteAsync(null);

            var stored = await karotRepository.GetAllAsync();
            Assert.False(mainViewModel.KarotModule.HasUnsavedChanges);
            Assert.Contains(stored, item => item.AdaParsel == "101/1" && item.YapiSahibi == "Test Sahibi");
        }
        finally
        {
            await DeleteDirectoryWithRetriesAsync(root);
        }
    }

    [Fact]
    public async Task KarotModule_PersistAsync_Clears_Dirty_State_After_New_Entry_Edit()
    {
        var root = CreateTempRoot();
        var databasePath = Path.Combine(root, "karot-module.db");

        try
        {
            var module = new KarotModuleViewModel(
                new SqliteKarotRepository(databasePath),
                new TestKarotStatusDialogService(),
                new NotificationService(),
                new ConfirmationServiceStub(),
                new TestTadilatCellNoteDialogService(),
                new UndoRedoService());

            await module.InitializeAsync();
            await module.AddKarotEntryCommand.ExecuteAsync(null);
            module.SelectedEntry!.AdaParsel = "200/5";
            module.SelectedEntry.Aciklama = "Yeni karot kaydı";

            Assert.True(module.HasUnsavedChanges);

            await module.PersistAsync(showErrorToast: true);

            Assert.False(module.HasUnsavedChanges);
        }
        finally
        {
            await DeleteDirectoryWithRetriesAsync(root);
        }
    }

    [Fact]
    public async Task SaveActiveTabCommand_Persists_Edits_On_Fifth_Karot_Row()
    {
        var root = CreateTempRoot();
        var databasePath = Path.Combine(root, "karot-fifth-row.db");

        try
        {
            var karotRepository = new SqliteKarotRepository(databasePath);
            var seedEntries = Enumerable.Range(1, 6)
                .Select(index => new KarotEntry
                {
                    AdaParsel = $"100/{index}",
                    YapiSahibi = $"Sahip {index}",
                    DisplayOrder = index - 1,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                })
                .ToList();

            await karotRepository.SaveManyAsync(seedEntries, Array.Empty<KarotCellState>());
            var mainViewModel = await CreateMainViewModelAsync(databasePath);

            mainViewModel.SelectMainTabCommand.Execute(MainNavigationTab.KarotTakibi);
            var fifthRow = mainViewModel.KarotModule.VisibleRows[4];
            mainViewModel.KarotModule.SelectedEntry = fifthRow.Entry;
            fifthRow.AdaParselCell.Text = "555/5";
            fifthRow.YapiSahibiCell.Text = "Beşinci Satır";

            Assert.True(mainViewModel.KarotModule.HasUnsavedChanges);

            await mainViewModel.SaveActiveTabCommand.ExecuteAsync(null);

            var stored = await karotRepository.GetAllAsync();
            Assert.False(mainViewModel.KarotModule.HasUnsavedChanges);
            Assert.Contains(stored, item => item.DisplayOrder == 4 && item.AdaParsel == "555/5" && item.YapiSahibi == "Beşinci Satır");
        }
        finally
        {
            await DeleteDirectoryWithRetriesAsync(root);
        }
    }

    [Fact]
    public async Task MissingProjectModule_Persists_Cell_Edit_After_LoadFromBackup_Reset()
    {
        var root = CreateTempRoot();
        var databasePath = Path.Combine(root, "missing-project-reset.db");

        try
        {
            var repository = new SqliteMissingProjectRepository(databasePath);
            await repository.SaveManyAsync(
            [
                new MissingProjectEntry
                {
                    Id = Guid.NewGuid(),
                    AdaParsel = "100/1",
                    YapiSahibi = "Sahip",
                    RecordMedium = MissingProjectMedium.Fiziki,
                    RecordMediumText = "Fiziksel",
                    MissingProjectText = "Eksik belge",
                    Description = "Açıklama",
                    DisplayOrder = 0,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                }
            ],
            Array.Empty<MissingProjectCellState>());

            var settings = new AppSettings { SeedSampleDataOnEmpty = false };
            var module = new MissingProjectModuleViewModel(
                repository,
                new NotificationService(),
                new ConfirmationServiceStub(),
                new TestTadilatCellNoteDialogService(),
                new UndoRedoService(),
                settings);

            await module.InitializeAsync();
            module.LoadFromBackup(module.GetEntriesSnapshot(), module.GetCellStatesSnapshot());
            await module.PersistAsync(showErrorToast: true);

            var cell = module.Rows[0].AdaParselCell;
            cell.DraftText = "700/1";
            module.CommitCellEditCommand.Execute(cell);

            Assert.True(module.HasUnsavedChanges);

            await module.PersistAsync(showErrorToast: true);

            var stored = await repository.GetAllAsync();
            Assert.False(module.HasUnsavedChanges);
            Assert.Contains(stored, item => item.AdaParsel == "700/1");
        }
        finally
        {
            await DeleteDirectoryWithRetriesAsync(root);
        }
    }

    [Fact]
    public async Task TadilatModule_Persists_Cell_Edit_After_LoadFromBackup_Reset()
    {
        var root = CreateTempRoot();
        var databasePath = Path.Combine(root, "tadilat-reset.db");

        try
        {
            var repository = new SqliteTadilatRepository(databasePath);
            await repository.SaveManyAsync(
            [
                new TadilatEntry
                {
                    Id = Guid.NewGuid(),
                    SubTab = TadilatSubTab.Aktif,
                    District = "MERKEZ",
                    JobName = "Eski İş",
                    DisplayOrder = 0,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                }
            ],
            Array.Empty<TadilatCellState>());

            var module = new TadilatModuleViewModel(
                repository,
                new TestTadilatImportService(),
                new TestFileDialogService(),
                new NotificationService(),
                new ConfirmationServiceStub(),
                new TestTadilatCellNoteDialogService(),
                new UndoRedoService());

            await module.InitializeAsync();
            module.LoadFromBackup(module.GetEntriesSnapshot(), module.GetCellStatesSnapshot());
            await module.PersistAsync(showErrorToast: true);

            var row = module.DistrictGroups.SelectMany(group => group.Rows).First(row => !row.IsPlaceholder);
            row.JobNameCell.DraftText = "Yeni İş";
            module.CommitCellEditCommand.Execute(row.JobNameCell);

            Assert.True(module.HasUnsavedChanges);

            await module.PersistAsync(showErrorToast: true);

            var stored = await repository.GetAllAsync();
            Assert.False(module.HasUnsavedChanges);
            Assert.Contains(stored, item => item.JobName == "Yeni İş");
        }
        finally
        {
            await DeleteDirectoryWithRetriesAsync(root);
        }
    }

    [Fact]
    public async Task YibfModule_Persists_IsTakibi_Edit_After_LoadFromBackup_Reset()
    {
        var root = CreateTempRoot();
        var databasePath = Path.Combine(root, "yibf-reset.db");

        try
        {
            var repository = new SqliteYibfRepository(databasePath);
            await repository.SaveManyAsync(
                Array.Empty<YibfAnaBilgiEntry>(),
                Array.Empty<YibfAnaBilgiEvent>(),
            [
                new YibfIsTakibiEntry
                {
                    Id = Guid.NewGuid(),
                    JobName = "Eski İş",
                    DisplayOrder = 0,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                }
            ],
                Array.Empty<YibfCellState>());

            var module = new YibfModuleViewModel(
                repository,
                new TestYibfImportService(),
                new TestFileDialogService(),
                new NotificationService(),
                new ConfirmationServiceStub(),
                new TestTadilatCellNoteDialogService(),
                new TestYibfAnaBilgiEventDialogService(),
                new TestYibfAnaBilgiEntryDialogService(),
                new UndoRedoService());

            await module.InitializeAsync();
            module.LoadFromBackup(
                module.GetAnaBilgiEntriesSnapshot(),
                module.GetAnaBilgiEventsSnapshot(),
                module.GetIsTakibiEntriesSnapshot(),
                module.GetCellStatesSnapshot());
            await module.PersistAsync(showErrorToast: true);

            var cell = module.IsTakibiRows[0].JobNameCell;
            cell.DraftText = "Yeni YİBF İş";
            module.CommitCellEditCommand.Execute(cell);

            Assert.True(module.HasUnsavedChanges);

            await module.PersistAsync(showErrorToast: true);

            var stored = await repository.GetIsTakibiEntriesAsync();
            Assert.False(module.HasUnsavedChanges);
            Assert.Contains(stored, item => item.JobName == "Yeni YİBF İş");
        }
        finally
        {
            await DeleteDirectoryWithRetriesAsync(root);
        }
    }

    [Fact]
    public async Task ActionModule_Persists_Edit_After_LoadFromBackup_Reset()
    {
        var root = CreateTempRoot();
        var databasePath = Path.Combine(root, "action-reset.db");

        try
        {
            var repository = new SqliteActionRepository(databasePath);
            await repository.SaveManyAsync(
            [
                new ActionEntry
                {
                    Id = Guid.NewGuid(),
                    Category = ActionEntryCategory.Aksiyon,
                    District = "MERKEZ",
                    OwnerParcelText = "Eski Malik",
                    WorkText = "Eski İş",
                    DisplayOrder = 0,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                }
            ]);

            var settings = new AppSettings { SeedSampleDataOnEmpty = false };
            var module = new ActionModuleViewModel(
                repository,
                new TestAddActionEntryDialogService(),
                new NotificationService(),
                new ConfirmationServiceStub(),
                new UndoRedoService(),
                settings);

            await module.InitializeAsync();
            module.LoadFromBackup(module.GetAllEntriesSnapshot());
            await module.PersistAsync(showErrorToast: true);

            var row = module.DistrictGroups.SelectMany(group => group.Rows).First(row => !row.IsPlaceholder);
            row.OwnerParcelDraft = "Yeni Malik";
            row.IsEditingOwnerParcel = true;
            await module.CommitOwnerParcelEditCommand.ExecuteAsync(row);

            Assert.True(module.HasUnsavedChanges);

            await module.PersistAsync(showErrorToast: true);

            var stored = await repository.GetByCategoryAsync(ActionEntryCategory.Aksiyon);
            Assert.False(module.HasUnsavedChanges);
            Assert.Contains(stored, item => item.OwnerParcelText == "Yeni Malik");
        }
        finally
        {
            await DeleteDirectoryWithRetriesAsync(root);
        }
    }

    [Fact]
    public async Task ActionModule_InsertEntryAbove_Adds_Record_In_Selected_District_Order_And_Supports_UndoRedo()
    {
        var root = CreateTempRoot();
        var databasePath = Path.Combine(root, "action-insert-above.db");

        try
        {
            var repository = new SqliteActionRepository(databasePath);
            await repository.SaveManyAsync(
            [
                new ActionEntry
                {
                    Id = Guid.NewGuid(),
                    Category = ActionEntryCategory.Aksiyon,
                    District = "MERKEZ",
                    OwnerParcelText = "İlk",
                    WorkText = "İlk İş",
                    DisplayOrder = 0,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                },
                new ActionEntry
                {
                    Id = Guid.NewGuid(),
                    Category = ActionEntryCategory.Aksiyon,
                    District = "MERKEZ",
                    OwnerParcelText = "İkinci",
                    WorkText = "İkinci İş",
                    DisplayOrder = 1,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                }
            ]);

            var dialog = new TestAddActionEntryDialogService
            {
                NextEntry = new ActionEntry
                {
                    OwnerParcelText = "Araya Giren",
                    WorkText = "Yeni İş"
                }
            };

            var undoRedoService = new UndoRedoService();
            var module = new ActionModuleViewModel(
                repository,
                dialog,
                new NotificationService(),
                new ConfirmationServiceStub(),
                undoRedoService,
                new AppSettings { SeedSampleDataOnEmpty = false });

            await module.InitializeAsync();
            var secondEntry = module.AksiyonEntries.OrderBy(item => item.DisplayOrder).Last();

            await module.InsertActionEntryAboveCommand.ExecuteAsync(secondEntry);

            var ordered = module.AksiyonEntries.Where(item => item.District == "MERKEZ").OrderBy(item => item.DisplayOrder).ToList();
            Assert.Equal(["İlk", "Araya Giren", "İkinci"], ordered.Select(item => item.OwnerParcelText));
            Assert.True(module.HasUnsavedChanges);

            undoRedoService.Undo();
            ordered = module.AksiyonEntries.Where(item => item.District == "MERKEZ").OrderBy(item => item.DisplayOrder).ToList();
            Assert.Equal(["İlk", "İkinci"], ordered.Select(item => item.OwnerParcelText));

            undoRedoService.Redo();
            ordered = module.AksiyonEntries.Where(item => item.District == "MERKEZ").OrderBy(item => item.DisplayOrder).ToList();
            Assert.Equal(["İlk", "Araya Giren", "İkinci"], ordered.Select(item => item.OwnerParcelText));

            await module.PersistAsync(showErrorToast: true);

            var stored = await repository.GetByCategoryAsync(ActionEntryCategory.Aksiyon);
            var storedOrdered = stored.Where(item => item.District == "MERKEZ").OrderBy(item => item.DisplayOrder).ToList();
            Assert.Equal(["İlk", "Araya Giren", "İkinci"], storedOrdered.Select(item => item.OwnerParcelText));
        }
        finally
        {
            await DeleteDirectoryWithRetriesAsync(root);
        }
    }

    [Fact]
    public async Task ActionModule_InsertEntryBelow_Adds_Record_In_AksiyonaEklenecekler_And_Persists()
    {
        var root = CreateTempRoot();
        var databasePath = Path.Combine(root, "action-insert-below.db");

        try
        {
            var repository = new SqliteActionRepository(databasePath);
            await repository.SaveManyAsync(
            [
                new ActionEntry
                {
                    Id = Guid.NewGuid(),
                    Category = ActionEntryCategory.AksiyonaEklenecekler,
                    District = "BOYABAT",
                    OwnerParcelText = "Bir",
                    WorkText = "Bir İş",
                    DisplayOrder = 0,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                },
                new ActionEntry
                {
                    Id = Guid.NewGuid(),
                    Category = ActionEntryCategory.AksiyonaEklenecekler,
                    District = "BOYABAT",
                    OwnerParcelText = "İki",
                    WorkText = "İki İş",
                    DisplayOrder = 1,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                }
            ]);

            var dialog = new TestAddActionEntryDialogService
            {
                NextEntry = new ActionEntry
                {
                    OwnerParcelText = "Ardına",
                    WorkText = "Alta Eklendi"
                }
            };

            var module = new ActionModuleViewModel(
                repository,
                dialog,
                new NotificationService(),
                new ConfirmationServiceStub(),
                new UndoRedoService(),
                new AppSettings { SeedSampleDataOnEmpty = false });

            await module.InitializeAsync();
            module.SelectedSubTab = ActionSubTab.AksiyonaEklenecekler;
            var firstEntry = module.AksiyonaEkleneceklerEntries.OrderBy(item => item.DisplayOrder).First();

            await module.InsertActionEntryBelowCommand.ExecuteAsync(firstEntry);
            await module.PersistAsync(showErrorToast: true);

            var stored = await repository.GetByCategoryAsync(ActionEntryCategory.AksiyonaEklenecekler);
            var ordered = stored.Where(item => item.District == "BOYABAT").OrderBy(item => item.DisplayOrder).ToList();
            Assert.Equal(["Bir", "Ardına", "İki"], ordered.Select(item => item.OwnerParcelText));
        }
        finally
        {
            await DeleteDirectoryWithRetriesAsync(root);
        }
    }

    [Fact]
    public async Task SaveActiveTabCommand_Persists_MissingProject_Edit_After_UndoRedo()
    {
        var root = CreateTempRoot();
        var databasePath = Path.Combine(root, "missing-project-undo-redo.db");

        try
        {
            var repository = new SqliteMissingProjectRepository(databasePath);
            await repository.SaveManyAsync(
            [
                new MissingProjectEntry
                {
                    AdaParsel = "101/1",
                    YapiSahibi = "İlk Sahip",
                    RecordMedium = MissingProjectMedium.Dijital,
                    RecordMediumText = "Dijital",
                    MissingProjectText = "Statik",
                    Description = "İlk açıklama",
                    DisplayOrder = 0,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                }
            ],
            Array.Empty<MissingProjectCellState>());

            var mainViewModel = await CreateMainViewModelAsync(databasePath);
            mainViewModel.SelectMainTabCommand.Execute(MainNavigationTab.EksikProje);

            var firstRow = mainViewModel.MissingProjectModule.Rows[0];
            firstRow.AdaParselCell.DraftText = "202/2";
            firstRow.AdaParselCell.IsEditing = true;
            mainViewModel.MissingProjectModule.CommitCellEditCommand.Execute(firstRow.AdaParselCell);

            mainViewModel.UndoCommand.Execute(null);
            mainViewModel.RedoCommand.Execute(null);

            var reloadedRow = mainViewModel.MissingProjectModule.Rows[0];
            reloadedRow.AdaParselCell.DraftText = "303/3";
            reloadedRow.AdaParselCell.IsEditing = true;
            mainViewModel.MissingProjectModule.CommitCellEditCommand.Execute(reloadedRow.AdaParselCell);

            Assert.True(mainViewModel.MissingProjectModule.HasUnsavedChanges);

            await mainViewModel.SaveActiveTabCommand.ExecuteAsync(null);

            var stored = await repository.GetAllAsync();
            Assert.False(mainViewModel.MissingProjectModule.HasUnsavedChanges);
            Assert.Contains(stored, item => item.AdaParsel == "303/3");
        }
        finally
        {
            await DeleteDirectoryWithRetriesAsync(root);
        }
    }

    [Fact]
    public async Task SaveActiveTabCommand_Persists_Tadilat_Edit_After_UndoRedo()
    {
        var root = CreateTempRoot();
        var databasePath = Path.Combine(root, "tadilat-undo-redo.db");

        try
        {
            var repository = new SqliteTadilatRepository(databasePath);
            await repository.SaveManyAsync(
            [
                new TadilatEntry
                {
                    SubTab = TadilatSubTab.Aktif,
                    District = "MERKEZ",
                    JobName = "İlk Tadilat",
                    ProjectType = "Ruhsat",
                    DisplayOrder = 0,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                }
            ],
            Array.Empty<TadilatCellState>());

            var mainViewModel = await CreateMainViewModelAsync(databasePath);
            mainViewModel.SelectMainTabCommand.Execute(MainNavigationTab.TadilatTakibi);

            var firstRow = mainViewModel.TadilatModule.DistrictGroups
                .SelectMany(group => group.Rows)
                .First(row => !row.IsPlaceholder);
            firstRow.JobNameCell.DraftText = "Birinci Güncelleme";
            firstRow.JobNameCell.IsEditing = true;
            mainViewModel.TadilatModule.CommitCellEditCommand.Execute(firstRow.JobNameCell);

            mainViewModel.UndoCommand.Execute(null);
            mainViewModel.RedoCommand.Execute(null);

            var reloadedRow = mainViewModel.TadilatModule.DistrictGroups
                .SelectMany(group => group.Rows)
                .First(row => !row.IsPlaceholder);
            reloadedRow.JobNameCell.DraftText = "İkinci Güncelleme";
            reloadedRow.JobNameCell.IsEditing = true;
            mainViewModel.TadilatModule.CommitCellEditCommand.Execute(reloadedRow.JobNameCell);

            Assert.True(mainViewModel.TadilatModule.HasUnsavedChanges);

            await mainViewModel.SaveActiveTabCommand.ExecuteAsync(null);

            var stored = await repository.GetAllAsync();
            Assert.False(mainViewModel.TadilatModule.HasUnsavedChanges);
            Assert.Contains(stored, item => item.JobName == "İkinci Güncelleme");
        }
        finally
        {
            await DeleteDirectoryWithRetriesAsync(root);
        }
    }

    [Fact]
    public async Task SaveActiveTabCommand_Persists_Yibf_IsTakibi_Edit_After_UndoRedo()
    {
        var root = CreateTempRoot();
        var databasePath = Path.Combine(root, "yibf-is-takibi-undo-redo.db");

        try
        {
            var repository = new SqliteYibfRepository(databasePath);
            await repository.SaveManyAsync(
                Array.Empty<YibfAnaBilgiEntry>(),
                Array.Empty<YibfAnaBilgiEvent>(),
                [
                    new YibfIsTakibiEntry
                    {
                        JobName = "İlk YİBF İşi",
                        DisplayOrder = 0,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    }
                ],
                Array.Empty<YibfCellState>());

            var mainViewModel = await CreateMainViewModelAsync(databasePath);
            mainViewModel.SelectMainTabCommand.Execute(MainNavigationTab.YibfIsTakibi);

            var firstRow = mainViewModel.YibfModule.IsTakibiRows[0];
            firstRow.JobNameCell.DraftText = "Birinci YİBF Güncelleme";
            firstRow.JobNameCell.IsEditing = true;
            mainViewModel.YibfModule.CommitCellEditCommand.Execute(firstRow.JobNameCell);

            mainViewModel.UndoCommand.Execute(null);
            mainViewModel.RedoCommand.Execute(null);

            var reloadedRow = mainViewModel.YibfModule.IsTakibiRows[0];
            reloadedRow.JobNameCell.DraftText = "İkinci YİBF Güncelleme";
            reloadedRow.JobNameCell.IsEditing = true;
            mainViewModel.YibfModule.CommitCellEditCommand.Execute(reloadedRow.JobNameCell);

            Assert.True(mainViewModel.YibfModule.HasUnsavedChanges);

            await mainViewModel.SaveActiveTabCommand.ExecuteAsync(null);

            var stored = await repository.GetIsTakibiEntriesAsync();
            Assert.False(mainViewModel.YibfModule.HasUnsavedChanges);
            Assert.Contains(stored, item => item.JobName == "İkinci YİBF Güncelleme");
        }
        finally
        {
            await DeleteDirectoryWithRetriesAsync(root);
        }
    }

    [Fact]
    public async Task YibfModule_Persists_AnaBilgi_Edit()
    {
        var root = CreateTempRoot();
        var databasePath = Path.Combine(root, "yibf-ana-bilgi-edit.db");

        try
        {
            var repository = new SqliteYibfRepository(databasePath);
            await repository.SaveManyAsync(
                [
                    new YibfAnaBilgiEntry
                    {
                        AdaParsel = "101/1",
                        YibfNo = "Y-1",
                        Idare = "Sinop",
                        YapiSahibi = "İlk Sahip",
                        Muteahhit = "İlk Müteahhit",
                        DisplayOrder = 0,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    }
                ],
                Array.Empty<YibfAnaBilgiEvent>(),
                Array.Empty<YibfIsTakibiEntry>(),
                Array.Empty<YibfCellState>());

            var dialog = new TestYibfAnaBilgiEntryDialogService
            {
                NextResult = new YibfAnaBilgiEntryDialogResult
                {
                    AdaParsel = "101/1",
                    YibfNo = "Y-1",
                    Idare = "Sinop",
                    YapiSahibi = "Yeni Sahip",
                    Muteahhit = "İlk Müteahhit"
                }
            };

            var module = new YibfModuleViewModel(
                repository,
                new TestYibfImportService(),
                new TestFileDialogService(),
                new NotificationService(),
                new ConfirmationServiceStub(),
                new TestTadilatCellNoteDialogService(),
                new TestYibfAnaBilgiEventDialogService(),
                dialog,
                new UndoRedoService());

            await module.InitializeAsync();
            module.SelectedAnaBilgiEntry = module.AnaBilgiEntries[0];
            await module.EditAnaBilgiEntryCommand.ExecuteAsync(null);

            Assert.True(module.HasUnsavedChanges);

            await module.PersistAsync(showErrorToast: true);

            var stored = await repository.GetAnaBilgiEntriesAsync();
            Assert.False(module.HasUnsavedChanges);
            Assert.Contains(stored, item => item.YapiSahibi == "Yeni Sahip");
        }
        finally
        {
            await DeleteDirectoryWithRetriesAsync(root);
        }
    }

    [Fact]
    public async Task SaveActiveTabCommand_Persists_Action_Edit_After_UndoRedo()
    {
        var root = CreateTempRoot();
        var databasePath = Path.Combine(root, "action-undo-redo.db");

        try
        {
            var repository = new SqliteActionRepository(databasePath);
            await repository.SaveManyAsync(
            [
                new ActionEntry
                {
                    Category = ActionEntryCategory.Aksiyon,
                    District = "MERKEZ",
                    OwnerParcelText = "İlk Malik",
                    WorkText = "İlk İş",
                    DisplayOrder = 0,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                }
            ]);

            var mainViewModel = await CreateMainViewModelAsync(databasePath);
            mainViewModel.SelectMainTabCommand.Execute(MainNavigationTab.Aksiyon);

            var firstRow = mainViewModel.ActionModule.DistrictGroups
                .SelectMany(group => group.Rows)
                .First(row => !row.IsPlaceholder && row.Entry is not null && row.Entry.District == "MERKEZ");
            firstRow.OwnerParcelDraft = "Birinci Aksiyon Güncelleme";
            firstRow.IsEditingOwnerParcel = true;
            await mainViewModel.ActionModule.CommitOwnerParcelEditCommand.ExecuteAsync(firstRow);

            mainViewModel.UndoCommand.Execute(null);
            mainViewModel.RedoCommand.Execute(null);

            var reloadedRow = mainViewModel.ActionModule.DistrictGroups
                .SelectMany(group => group.Rows)
                .First(row => !row.IsPlaceholder && row.Entry is not null && row.Entry.District == "MERKEZ");
            reloadedRow.OwnerParcelDraft = "İkinci Aksiyon Güncelleme";
            reloadedRow.IsEditingOwnerParcel = true;
            await mainViewModel.ActionModule.CommitOwnerParcelEditCommand.ExecuteAsync(reloadedRow);

            Assert.True(mainViewModel.ActionModule.HasUnsavedChanges);

            await mainViewModel.SaveActiveTabCommand.ExecuteAsync(null);

            var stored = await repository.GetByCategoryAsync(ActionEntryCategory.Aksiyon);
            Assert.False(mainViewModel.ActionModule.HasUnsavedChanges);
            Assert.Contains(stored, item => item.OwnerParcelText == "İkinci Aksiyon Güncelleme");
        }
        finally
        {
            await DeleteDirectoryWithRetriesAsync(root);
        }
    }

    [Fact]
    public async Task SaveActiveTabCommand_Persists_General_Task_Title_Edit()
    {
        var root = CreateTempRoot();
        var databasePath = Path.Combine(root, "general-task-title.db");

        try
        {
            var taskRepository = new SqliteTaskRepository(databasePath);
            await taskRepository.SaveManyAsync(
            [
                CreateTask("Eski Başlık", TaskBoardType.Genel, 0)
            ]);

            var mainViewModel = await CreateMainViewModelAsync(databasePath);
            mainViewModel.SelectMainTabCommand.Execute(MainNavigationTab.GenelIsTakibi);

            var task = mainViewModel.GeneralBoard.Tasks[0];
            task.Title = "Yeni Başlık";

            Assert.True(mainViewModel.HasUnsavedChanges);

            await mainViewModel.SaveActiveTabCommand.ExecuteAsync(null);

            var stored = await taskRepository.GetAllAsync();
            Assert.False(mainViewModel.HasUnsavedChanges);
            Assert.Contains(stored, item => item.Title == "Yeni Başlık");
        }
        finally
        {
            await DeleteDirectoryWithRetriesAsync(root);
        }
    }

    [Fact]
    public async Task SaveActiveTabCommand_Persists_Loaded_General_Task_Title_Edit()
    {
        var root = CreateTempRoot();
        var databasePath = Path.Combine(root, "general-save.db");

        try
        {
            var taskRepository = new SqliteTaskRepository(databasePath);
            await taskRepository.SaveManyAsync(
            [
                CreateTask("Eski Genel", TaskBoardType.Genel, 0)
            ]);

            var mainViewModel = await CreateMainViewModelAsync(databasePath);

            mainViewModel.SelectMainTabCommand.Execute(MainNavigationTab.GenelIsTakibi);
            var task = Assert.Single(mainViewModel.GeneralBoard.Tasks);
            task.Title = "Guncel Genel";

            Assert.True(mainViewModel.HasUnsavedChanges);

            await mainViewModel.SaveActiveTabCommand.ExecuteAsync(null);

            var stored = await taskRepository.GetAllAsync();
            Assert.False(mainViewModel.HasUnsavedChanges);
            Assert.Contains(stored, item => item.BoardType == TaskBoardType.Genel && item.Title == "Guncel Genel");
        }
        finally
        {
            await DeleteDirectoryWithRetriesAsync(root);
        }
    }

    [Fact]
    public async Task SaveActiveTabCommand_Persists_Loaded_MissingProject_Edit()
    {
        var root = CreateTempRoot();
        var databasePath = Path.Combine(root, "missing-project-save.db");

        try
        {
            var repository = new SqliteMissingProjectRepository(databasePath);
            var entry = new MissingProjectEntry
            {
                AdaParsel = "101-1",
                YapiSahibi = "Eski Sahip",
                RecordMedium = MissingProjectMedium.Fiziki,
                RecordMediumText = "Fiziksel",
                MissingProjectText = "Eski Proje",
                Description = string.Empty,
                DisplayOrder = 0,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            await repository.SaveManyAsync([entry], Array.Empty<MissingProjectCellState>());

            var mainViewModel = await CreateMainViewModelAsync(databasePath);
            mainViewModel.SelectMainTabCommand.Execute(MainNavigationTab.EksikProje);

            var row = Assert.Single(mainViewModel.MissingProjectModule.Rows);
            row.MissingProjectCell.DraftText = "Yeni Eksik Proje";
            mainViewModel.MissingProjectModule.CommitCellEditCommand.Execute(row.MissingProjectCell);

            Assert.True(mainViewModel.MissingProjectModule.HasUnsavedChanges);

            await mainViewModel.SaveActiveTabCommand.ExecuteAsync(null);

            var stored = await repository.GetAllAsync();
            Assert.False(mainViewModel.MissingProjectModule.HasUnsavedChanges);
            Assert.Contains(stored, item => item.Id == entry.Id && item.MissingProjectText == "Yeni Eksik Proje");
        }
        finally
        {
            await DeleteDirectoryWithRetriesAsync(root);
        }
    }

    [Fact]
    public async Task SaveActiveTabCommand_Persists_Loaded_Tadilat_Edit()
    {
        var root = CreateTempRoot();
        var databasePath = Path.Combine(root, "tadilat-save.db");

        try
        {
            var repository = new SqliteTadilatRepository(databasePath);
            var entry = new TadilatEntry
            {
                District = "MERKEZ",
                JobName = "Eski Tadilat",
                ProjectType = "Proje",
                DisplayOrder = 0,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                SubTab = TadilatSubTab.Aktif
            };

            await repository.SaveManyAsync([entry], Array.Empty<TadilatCellState>());

            var mainViewModel = await CreateMainViewModelAsync(databasePath);
            mainViewModel.SelectMainTabCommand.Execute(MainNavigationTab.TadilatTakibi);

            var row = mainViewModel.TadilatModule.DistrictGroups
                .SelectMany(group => group.Rows)
                .First(row => !row.IsPlaceholder);
            row.JobNameCell.DraftText = "Yeni Tadilat";
            mainViewModel.TadilatModule.CommitCellEditCommand.Execute(row.JobNameCell);

            Assert.True(mainViewModel.TadilatModule.HasUnsavedChanges);

            await mainViewModel.SaveActiveTabCommand.ExecuteAsync(null);

            var stored = await repository.GetAllAsync();
            Assert.False(mainViewModel.TadilatModule.HasUnsavedChanges);
            Assert.Contains(stored, item => item.Id == entry.Id && item.JobName == "Yeni Tadilat");
        }
        finally
        {
            await DeleteDirectoryWithRetriesAsync(root);
        }
    }

    [Fact]
    public async Task SaveActiveTabCommand_Persists_Loaded_Yibf_IsTakibi_Edit()
    {
        var root = CreateTempRoot();
        var databasePath = Path.Combine(root, "yibf-save.db");

        try
        {
            var repository = new SqliteYibfRepository(databasePath);
            var anaBilgiEntry = new YibfAnaBilgiEntry
            {
                AdaParsel = "501-1",
                YibfNo = "Y-001",
                Idare = "Sinop",
                YapiSahibi = "Eski Sahip",
                Muteahhit = "Muteahhit",
                DisplayOrder = 0,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
            var isTakibiEntry = new YibfIsTakibiEntry
            {
                JobName = "Eski YIBF",
                DisplayOrder = 0,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            await repository.SaveManyAsync([anaBilgiEntry], Array.Empty<YibfAnaBilgiEvent>(), [isTakibiEntry], Array.Empty<YibfCellState>());

            var mainViewModel = await CreateMainViewModelAsync(databasePath);
            mainViewModel.SelectMainTabCommand.Execute(MainNavigationTab.YibfIsTakibi);

            var row = Assert.Single(mainViewModel.YibfModule.IsTakibiRows);
            row.JobNameCell.DraftText = "Yeni YIBF";
            mainViewModel.YibfModule.CommitCellEditCommand.Execute(row.JobNameCell);

            Assert.True(mainViewModel.YibfModule.HasUnsavedChanges);

            await mainViewModel.SaveActiveTabCommand.ExecuteAsync(null);

            var storedEntries = await repository.GetIsTakibiEntriesAsync();
            Assert.False(mainViewModel.YibfModule.HasUnsavedChanges);
            Assert.Contains(storedEntries, item => item.Id == isTakibiEntry.Id && item.JobName == "Yeni YIBF");
        }
        finally
        {
            await DeleteDirectoryWithRetriesAsync(root);
        }
    }

    [Fact]
    public async Task SaveActiveTabCommand_Persists_Loaded_Action_Edit()
    {
        var root = CreateTempRoot();
        var databasePath = Path.Combine(root, "action-save.db");

        try
        {
            var repository = new SqliteActionRepository(databasePath);
            var entry = new ActionEntry
            {
                Category = ActionEntryCategory.Aksiyon,
                District = "MERKEZ",
                OwnerParcelText = "Eski Ada",
                WorkText = "Eski Is",
                DisplayOrder = 0,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            await repository.SaveManyAsync([entry]);

            var mainViewModel = await CreateMainViewModelAsync(databasePath);
            mainViewModel.SelectMainTabCommand.Execute(MainNavigationTab.Aksiyon);

            var row = mainViewModel.ActionModule.DistrictGroups
                .SelectMany(group => group.Rows)
                .First(row => !row.IsPlaceholder && row.Entry?.District == "MERKEZ");
            row.IsEditingOwnerParcel = true;
            row.OwnerParcelDraft = "Yeni Ada";
            await mainViewModel.ActionModule.CommitOwnerParcelEditCommand.ExecuteAsync(row);

            Assert.True(mainViewModel.ActionModule.HasUnsavedChanges);

            await mainViewModel.SaveActiveTabCommand.ExecuteAsync(null);

            var stored = await repository.GetByCategoryAsync(ActionEntryCategory.Aksiyon);
            Assert.False(mainViewModel.ActionModule.HasUnsavedChanges);
            Assert.Contains(stored, item => item.Id == entry.Id && item.OwnerParcelText == "Yeni Ada");
        }
        finally
        {
            await DeleteDirectoryWithRetriesAsync(root);
        }
    }

    [Fact]
    public async Task SaveActiveTabCommand_Commits_Pending_Action_Edit_Before_Persist()
    {
        var root = CreateTempRoot();
        var databasePath = Path.Combine(root, "action-pending-save.db");

        try
        {
            var repository = new SqliteActionRepository(databasePath);
            var entry = new ActionEntry
            {
                Category = ActionEntryCategory.Aksiyon,
                District = "MERKEZ",
                OwnerParcelText = "Eski Ada",
                WorkText = "Eski Is",
                DisplayOrder = 0,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            await repository.SaveManyAsync([entry]);

            var mainViewModel = await CreateMainViewModelAsync(databasePath);
            mainViewModel.SelectMainTabCommand.Execute(MainNavigationTab.Aksiyon);

            var row = mainViewModel.ActionModule.DistrictGroups
                .SelectMany(group => group.Rows)
                .First(item => !item.IsPlaceholder && item.Entry?.Id == entry.Id);

            row.IsEditingOwnerParcel = true;
            row.OwnerParcelDraft = "Bekleyen Ada";

            await mainViewModel.SaveActiveTabCommand.ExecuteAsync(null);

            var stored = await repository.GetByCategoryAsync(ActionEntryCategory.Aksiyon);
            Assert.Contains(stored, item => item.Id == entry.Id && item.OwnerParcelText == "Bekleyen Ada");
        }
        finally
        {
            await DeleteDirectoryWithRetriesAsync(root);
        }
    }

    [Fact]
    public async Task SaveActiveTabCommand_Commits_Pending_MissingProject_Edit_Before_Persist()
    {
        var root = CreateTempRoot();
        var databasePath = Path.Combine(root, "missing-project-pending-save.db");

        try
        {
            var repository = new SqliteMissingProjectRepository(databasePath);
            var entry = new MissingProjectEntry
            {
                AdaParsel = "101-1",
                YapiSahibi = "Sahip",
                RecordMedium = MissingProjectMedium.Fiziki,
                RecordMediumText = "Fiziksel",
                MissingProjectText = "Eski Proje",
                Description = string.Empty,
                DisplayOrder = 0,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            await repository.SaveManyAsync([entry], Array.Empty<MissingProjectCellState>());

            var mainViewModel = await CreateMainViewModelAsync(databasePath);
            mainViewModel.SelectMainTabCommand.Execute(MainNavigationTab.EksikProje);

            var row = Assert.Single(mainViewModel.MissingProjectModule.Rows);
            row.MissingProjectCell.IsEditing = true;
            row.MissingProjectCell.DraftText = "Bekleyen Eksik Proje";

            await mainViewModel.SaveActiveTabCommand.ExecuteAsync(null);

            var stored = await repository.GetAllAsync();
            Assert.Contains(stored, item => item.Id == entry.Id && item.MissingProjectText == "Bekleyen Eksik Proje");
        }
        finally
        {
            await DeleteDirectoryWithRetriesAsync(root);
        }
    }

    [Fact]
    public async Task SaveActiveTabCommand_Commits_Pending_Tadilat_Edit_Before_Persist()
    {
        var root = CreateTempRoot();
        var databasePath = Path.Combine(root, "tadilat-pending-save.db");

        try
        {
            var repository = new SqliteTadilatRepository(databasePath);
            var entry = new TadilatEntry
            {
                District = "MERKEZ",
                JobName = "Eski Tadilat",
                ProjectType = "Proje",
                DisplayOrder = 0,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                SubTab = TadilatSubTab.Aktif
            };

            await repository.SaveManyAsync([entry], Array.Empty<TadilatCellState>());

            var mainViewModel = await CreateMainViewModelAsync(databasePath);
            mainViewModel.SelectMainTabCommand.Execute(MainNavigationTab.TadilatTakibi);

            var row = mainViewModel.TadilatModule.DistrictGroups
                .SelectMany(group => group.Rows)
                .First(item => !item.IsPlaceholder && item.Entry?.Id == entry.Id);
            row.JobNameCell.IsEditing = true;
            row.JobNameCell.DraftText = "Bekleyen Tadilat";

            await mainViewModel.SaveActiveTabCommand.ExecuteAsync(null);

            var stored = await repository.GetAllAsync();
            Assert.Contains(stored, item => item.Id == entry.Id && item.JobName == "Bekleyen Tadilat");
        }
        finally
        {
            await DeleteDirectoryWithRetriesAsync(root);
        }
    }

    [Fact]
    public async Task SaveActiveTabCommand_Commits_Pending_Yibf_Edit_Before_Persist()
    {
        var root = CreateTempRoot();
        var databasePath = Path.Combine(root, "yibf-pending-save.db");

        try
        {
            var repository = new SqliteYibfRepository(databasePath);
            var anaBilgiEntry = new YibfAnaBilgiEntry
            {
                AdaParsel = "501-1",
                YibfNo = "Y-001",
                Idare = "Sinop",
                YapiSahibi = "Sahip",
                Muteahhit = "Muteahhit",
                DisplayOrder = 0,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
            var isTakibiEntry = new YibfIsTakibiEntry
            {
                JobName = "Eski YIBF",
                DisplayOrder = 0,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            await repository.SaveManyAsync([anaBilgiEntry], Array.Empty<YibfAnaBilgiEvent>(), [isTakibiEntry], Array.Empty<YibfCellState>());

            var mainViewModel = await CreateMainViewModelAsync(databasePath);
            mainViewModel.SelectMainTabCommand.Execute(MainNavigationTab.YibfIsTakibi);

            var row = Assert.Single(mainViewModel.YibfModule.IsTakibiRows);
            row.JobNameCell.IsEditing = true;
            row.JobNameCell.DraftText = "Bekleyen YIBF";

            await mainViewModel.SaveActiveTabCommand.ExecuteAsync(null);

            var storedEntries = await repository.GetIsTakibiEntriesAsync();
            Assert.Contains(storedEntries, item => item.Id == isTakibiEntry.Id && item.JobName == "Bekleyen YIBF");
        }
        finally
        {
            await DeleteDirectoryWithRetriesAsync(root);
        }
    }

    private static async Task<MainViewModel> CreateMainViewModelAsync(
        string databasePath,
        IAppSettingsService? appSettingsService = null,
        ILastSaveMetadataService? lastSaveMetadataService = null,
        AppSettings? settings = null,
        IQuickTaskTemplateRepository? quickTaskTemplateRepository = null,
        IQuickTaskTemplateDialogService? quickTaskTemplateDialogService = null)
    {
        settings ??= new AppSettings
        {
            AutoBackupEnabled = false,
            AutoBackupMinutes = 15,
            SeedSampleDataOnEmpty = false
        };

        var taskRepository = new SqliteTaskRepository(databasePath);
        var actionRepository = new SqliteActionRepository(databasePath);
        var missingProjectRepository = new SqliteMissingProjectRepository(databasePath);
        var karotRepository = new SqliteKarotRepository(databasePath);
        var tadilatRepository = new SqliteTadilatRepository(databasePath);
        var yibfRepository = new SqliteYibfRepository(databasePath);
        var notificationService = new NotificationService();
        var undoRedoService = new UndoRedoService();
        var confirmationService = new ConfirmationServiceStub();

        var actionModule = new ActionModuleViewModel(
            actionRepository,
            new TestAddActionEntryDialogService(),
            notificationService,
            confirmationService,
            undoRedoService,
            settings);

        var missingProjectModule = new MissingProjectModuleViewModel(
            missingProjectRepository,
            notificationService,
            confirmationService,
            new TestTadilatCellNoteDialogService(),
            undoRedoService,
            settings);

        var karotModule = new KarotModuleViewModel(
            karotRepository,
            new TestKarotStatusDialogService(),
            notificationService,
            confirmationService,
            new TestTadilatCellNoteDialogService(),
            undoRedoService);

        var tadilatModule = new TadilatModuleViewModel(
            tadilatRepository,
            new TestTadilatImportService(),
            new TestFileDialogService(),
            notificationService,
            confirmationService,
            new TestTadilatCellNoteDialogService(),
            undoRedoService);

        var yibfModule = new YibfModuleViewModel(
            yibfRepository,
            new TestYibfImportService(),
            new TestFileDialogService(),
            notificationService,
            confirmationService,
            new TestTadilatCellNoteDialogService(),
            new TestYibfAnaBilgiEventDialogService(),
            new TestYibfAnaBilgiEntryDialogService(),
            undoRedoService);

        var viewModel = new MainViewModel(
            taskRepository,
            new TestBackupService(),
            appSettingsService ?? new TestAppSettingsService(settings),
            lastSaveMetadataService ?? new TestLastSaveMetadataService(),
            new TestImportExportService(),
            notificationService,
            confirmationService,
            new SearchService(),
            new ContextQueryService(),
            new ContextInsightBuilder(new SearchService()),
            undoRedoService,
            new TestFileDialogService(),
            settings,
            new DashboardViewModel(),
            new SearchOverlayViewModel(),
            new TaskDetailViewModel(),
            new ToastHostViewModel(notificationService),
            actionModule,
            missingProjectModule,
            karotModule,
            tadilatModule,
            yibfModule,
            quickTaskTemplateRepository,
            quickTaskTemplateDialogService);

        await viewModel.InitializeAsync();
        return viewModel;
    }

    private static TaskItem CreateTask(string title, TaskBoardType boardType, int sortOrder)
        => new()
        {
            Title = title,
            Description = string.Empty,
            BoardType = boardType,
            SortOrder = sortOrder,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

    private static string CreateTempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "RizaCanKilicIsTakibiTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    private static async Task DeleteDirectoryWithRetriesAsync(string root)
    {
        if (!Directory.Exists(root))
        {
            return;
        }

        for (var attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                Directory.Delete(root, true);
                break;
            }
            catch (IOException) when (attempt < 2)
            {
                await Task.Delay(100);
            }
            catch (UnauthorizedAccessException) when (attempt < 2)
            {
                await Task.Delay(100);
            }
            catch (IOException)
            {
                break;
            }
            catch (UnauthorizedAccessException)
            {
                break;
            }
        }
    }

    private sealed class ConfirmationServiceStub : IConfirmationService
    {
        public bool Confirm(ConfirmationRequest request) => true;
    }

    private sealed class TestBackupService : IBackupService
    {
        public Task<int> ClearManagedBackupsAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
        public Task<int> CleanOldBackupsAsync(int keepCount = 30, CancellationToken cancellationToken = default) => Task.FromResult(0);
        public Task<BackupMetadata> CreateBackupAsync(IEnumerable<TaskItem> tasks, string? backupPath = null, IEnumerable<ActionEntry>? actionEntries = null, IEnumerable<MissingProjectEntry>? missingProjectEntries = null, IEnumerable<MissingProjectCellState>? missingProjectCellStates = null, IEnumerable<KarotEntry>? karotEntries = null, IEnumerable<KarotCellState>? karotCellStates = null, IEnumerable<TadilatEntry>? tadilatEntries = null, IEnumerable<YibfAnaBilgiEntry>? yibfAnaBilgiEntries = null, IEnumerable<YibfAnaBilgiEvent>? yibfAnaBilgiEvents = null, IEnumerable<YibfIsTakibiEntry>? yibfIsTakibiEntries = null, IEnumerable<YibfCellState>? yibfCellStates = null, IEnumerable<TadilatCellState>? tadilatCellStates = null, IEnumerable<QuickTaskTemplate>? quickTaskTemplates = null, CancellationToken cancellationToken = default)
            => Task.FromResult(new BackupMetadata { BackupFilePath = backupPath ?? string.Empty, CreatedAt = DateTime.Now, TaskCount = tasks.Count() });
        public Task<BackupRestoreData> RestoreBackupAsync(string backupPath, CancellationToken cancellationToken = default) => Task.FromResult(new BackupRestoreData());
        public void ScheduleAutoBackup(TimeSpan interval, Func<Task> callback) { }
        public void StopAutoBackup() { }
    }

    private sealed class TestAppSettingsService : IAppSettingsService
    {
        private readonly AppSettings _settings;
        public TestAppSettingsService(AppSettings settings) => _settings = settings;
        public AppSettingsLoadResult Load() => new()
        {
            Settings = _settings,
            Status = AppSettingsLoadStatus.Success
        };
        public Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class TestLastSaveMetadataService : ILastSaveMetadataService
    {
        public DateTime? LastSuccessfulSaveAt { get; private set; }

        public DateTime? LoadLastSuccessfulSaveAt() => LastSuccessfulSaveAt;

        public Task SaveLastSuccessfulSaveAtAsync(DateTime timestamp, CancellationToken cancellationToken = default)
        {
            LastSuccessfulSaveAt = timestamp;
            return Task.CompletedTask;
        }
    }

    private sealed class FailSaveAppSettingsService : IAppSettingsService
    {
        public FailSaveAppSettingsService(AppSettings settings) => Settings = settings;

        public AppSettings Settings { get; }

        public AppSettingsLoadResult Load() => new()
        {
            Settings = Settings,
            Status = AppSettingsLoadStatus.Success
        };

        public Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
            => throw new IOException("Ayar kaydetme testi için hata");
    }

    private sealed class TestImportExportService : IImportExportService
    {
        public Task ExportExcelAsync(IEnumerable<TaskItem> tasks, string filePath, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task ExportWorkbookAsync(ExcelWorkbookExportModel workbook, string filePath, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<TaskItem>> ImportExcelAsync(string filePath, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<TaskItem>>(Array.Empty<TaskItem>());
        public Task ExportPdfAsync(IEnumerable<TaskItem> tasks, string filePath, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task ExportPngAsync(UIElement visual, string filePath, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task ExportScrollablePngAsync(UIElement visual, string filePath, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class TestFileDialogService : IFileDialogService
    {
        public string? ShowOpenDialog(string title, string filter, bool multiselect = false) => null;
        public string? ShowSaveDialog(string title, string filter, string defaultExtension) => null;
    }

    private sealed class TestAddActionEntryDialogService : IAddActionEntryDialogService
    {
        public ActionEntry? NextEntry { get; set; }

        public Task<ActionEntry?> ShowDialogAsync(string district, ActionEntryCategory category, CancellationToken cancellationToken = default)
        {
            var nextEntry = NextEntry;
            NextEntry = null;
            return Task.FromResult<ActionEntry?>(nextEntry);
        }
    }

    private sealed class TestQuickTaskTemplateDialogService : IQuickTaskTemplateDialogService
    {
        private readonly IReadOnlyList<string>? _titles;

        public TestQuickTaskTemplateDialogService(IReadOnlyList<string>? titles)
        {
            _titles = titles;
        }

        public Task<IReadOnlyList<string>?> ShowDialogAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(_titles);
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

    private sealed class TestYibfAnaBilgiEventDialogService : IYibfAnaBilgiEventDialogService
    {
        public Task<YibfAnaBilgiEventDialogResult?> ShowDialogAsync(DateTime? eventDate, string description, string backgroundColor, string noteText, CancellationToken cancellationToken = default)
            => Task.FromResult<YibfAnaBilgiEventDialogResult?>(null);
    }

    private sealed class TestYibfAnaBilgiEntryDialogService : IYibfAnaBilgiEntryDialogService
    {
        public YibfAnaBilgiEntryDialogResult? NextResult { get; set; }

        public Task<YibfAnaBilgiEntryDialogResult?> ShowDialogAsync(YibfAnaBilgiEntryDialogResult? initialValues = null, bool isEditMode = false, CancellationToken cancellationToken = default)
        {
            var result = NextResult;
            NextResult = null;
            return Task.FromResult(result);
        }
    }
}
