using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services;
using RizaCanKilicIsTakibi.Services.Abstractions;
using RizaCanKilicIsTakibi.ViewModels;
using System.Windows;

namespace RizaCanKilicIsTakibi.Tests;

public class MainViewModelRollbackTests
{
    [Fact]
    public async Task ImportBackup_Rolls_Back_To_Previous_State_When_Persist_Fails()
    {
        var root = Path.Combine(Path.GetTempPath(), "RizaCanKilicIsTakibiTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var databasePath = Path.Combine(root, "rollback.db");

        try
        {
            var settings = new AppSettings
            {
                AutoBackupEnabled = false,
                AutoBackupMinutes = 15,
                SeedSampleDataOnEmpty = false
            };

            var originalTask = new TaskItem
            {
                Title = "Orijinal Görev",
                Description = "Mevcut veri",
                BoardType = TaskBoardType.Genel,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            var originalYibfEntry = new YibfAnaBilgiEntry
            {
                AdaParsel = "111/1",
                YapiSahibi = "Eski Sahip",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            var importedTask = new TaskItem
            {
                Title = "İçe Aktarılan Görev",
                Description = "Yeni veri",
                BoardType = TaskBoardType.Acil,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            var importedYibfEntry = new YibfAnaBilgiEntry
            {
                AdaParsel = "999/9",
                YapiSahibi = "Yeni Sahip",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            var taskRepository = new SqliteTaskRepository(databasePath);
            var actionRepository = new SqliteActionRepository(databasePath);
            var missingProjectRepository = new SqliteMissingProjectRepository(databasePath);
            var karotRepository = new SqliteKarotRepository(databasePath);
            var tadilatRepository = new SqliteTadilatRepository(databasePath);
            var realYibfRepository = new SqliteYibfRepository(databasePath);

            await taskRepository.SaveManyAsync([originalTask.Clone()]);
            await realYibfRepository.SaveManyAsync([CloneEntry(originalYibfEntry)], Array.Empty<YibfAnaBilgiEvent>(), Array.Empty<YibfIsTakibiEntry>(), Array.Empty<YibfCellState>());

            var notificationService = new NotificationService();
            var undoRedoService = new UndoRedoService();
            var backupData = new BackupRestoreData
            {
                Tasks = [importedTask.Clone()],
                YibfAnaBilgiEntries = [CloneEntry(importedYibfEntry)]
            };

            var mainViewModel = CreateMainViewModel(
                settings,
                notificationService,
                undoRedoService,
                taskRepository,
                actionRepository,
                missingProjectRepository,
                karotRepository,
                tadilatRepository,
                new FailOnceYibfRepository(realYibfRepository),
                new TestBackupService(backupData),
                new TestFileDialogService(Path.Combine(root, "import.json")));

            await mainViewModel.InitializeAsync();

            await mainViewModel.ImportBackupCommand.ExecuteAsync(null);

            var storedTasks = await taskRepository.GetAllAsync();
            var storedYibfEntries = await realYibfRepository.GetAnaBilgiEntriesAsync();

            Assert.Contains(storedTasks, item => item.Title == originalTask.Title);
            Assert.DoesNotContain(storedTasks, item => item.Title == importedTask.Title);
            Assert.Contains(storedYibfEntries, item => item.AdaParsel == originalYibfEntry.AdaParsel && item.YapiSahibi == originalYibfEntry.YapiSahibi);
            Assert.DoesNotContain(storedYibfEntries, item => item.AdaParsel == importedYibfEntry.AdaParsel);

            Assert.Contains(mainViewModel.GeneralBoard.Tasks, item => item.Title == originalTask.Title);
            Assert.DoesNotContain(mainViewModel.UrgentBoard.Tasks, item => item.Title == importedTask.Title);
            Assert.Contains(mainViewModel.YibfModule.AnaBilgiEntries, item => item.AdaParsel == originalYibfEntry.AdaParsel);
            Assert.DoesNotContain(mainViewModel.YibfModule.AnaBilgiEntries, item => item.AdaParsel == importedYibfEntry.AdaParsel);
        }
        finally
        {
            if (Directory.Exists(root))
            {
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
        }
    }

    [Fact]
    public async Task ResetLiveData_Rolls_Back_To_Previous_State_When_Persist_Fails()
    {
        var root = Path.Combine(Path.GetTempPath(), "RizaCanKilicIsTakibiTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var databasePath = Path.Combine(root, "reset-rollback.db");

        try
        {
            var settings = new AppSettings
            {
                AutoBackupEnabled = false,
                AutoBackupMinutes = 15,
                SeedSampleDataOnEmpty = false
            };

            var originalTask = CreateTask("Korunacak Görev", TaskBoardType.Genel);
            var originalYibfEntry = CreateYibfEntry("222/2", "Sabit Sahip");

            var taskRepository = new SqliteTaskRepository(databasePath);
            var actionRepository = new SqliteActionRepository(databasePath);
            var missingProjectRepository = new SqliteMissingProjectRepository(databasePath);
            var karotRepository = new SqliteKarotRepository(databasePath);
            var tadilatRepository = new SqliteTadilatRepository(databasePath);
            var realYibfRepository = new SqliteYibfRepository(databasePath);

            await taskRepository.SaveManyAsync([originalTask.Clone()]);
            await realYibfRepository.SaveManyAsync([CloneEntry(originalYibfEntry)], Array.Empty<YibfAnaBilgiEvent>(), Array.Empty<YibfIsTakibiEntry>(), Array.Empty<YibfCellState>());

            var notificationService = new NotificationService();
            var undoRedoService = new UndoRedoService();
            var mainViewModel = CreateMainViewModel(
                settings,
                notificationService,
                undoRedoService,
                taskRepository,
                actionRepository,
                missingProjectRepository,
                karotRepository,
                tadilatRepository,
                new FailOnceYibfRepository(realYibfRepository),
                new TestBackupService(new BackupRestoreData()),
                new TestFileDialogService());

            await mainViewModel.InitializeAsync();

            await mainViewModel.ResetLiveDataCommand.ExecuteAsync(null);

            var storedTasks = await taskRepository.GetAllAsync();
            var storedYibfEntries = await realYibfRepository.GetAnaBilgiEntriesAsync();

            Assert.Contains(storedTasks, item => item.Title == originalTask.Title);
            Assert.Contains(storedYibfEntries, item => item.AdaParsel == originalYibfEntry.AdaParsel);
            Assert.Contains(mainViewModel.GeneralBoard.Tasks, item => item.Title == originalTask.Title);
            Assert.Contains(mainViewModel.YibfModule.AnaBilgiEntries, item => item.AdaParsel == originalYibfEntry.AdaParsel);
        }
        finally
        {
            await DeleteDirectoryWithRetriesAsync(root);
        }
    }

    [Fact]
    public async Task ClearSelectedTab_Rolls_Back_To_Previous_State_When_Persist_Fails()
    {
        var root = Path.Combine(Path.GetTempPath(), "RizaCanKilicIsTakibiTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var databasePath = Path.Combine(root, "clear-tab-rollback.db");

        try
        {
            var settings = new AppSettings
            {
                AutoBackupEnabled = false,
                AutoBackupMinutes = 15,
                SeedSampleDataOnEmpty = false
            };

            var originalYibfEntry = CreateYibfEntry("333/3", "Sekme Sahibi");

            var taskRepository = new SqliteTaskRepository(databasePath);
            var actionRepository = new SqliteActionRepository(databasePath);
            var missingProjectRepository = new SqliteMissingProjectRepository(databasePath);
            var karotRepository = new SqliteKarotRepository(databasePath);
            var tadilatRepository = new SqliteTadilatRepository(databasePath);
            var realYibfRepository = new SqliteYibfRepository(databasePath);

            await realYibfRepository.SaveManyAsync([CloneEntry(originalYibfEntry)], Array.Empty<YibfAnaBilgiEvent>(), Array.Empty<YibfIsTakibiEntry>(), Array.Empty<YibfCellState>());

            var notificationService = new NotificationService();
            var undoRedoService = new UndoRedoService();
            var mainViewModel = CreateMainViewModel(
                settings,
                notificationService,
                undoRedoService,
                taskRepository,
                actionRepository,
                missingProjectRepository,
                karotRepository,
                tadilatRepository,
                new FailOnceYibfRepository(realYibfRepository),
                new TestBackupService(new BackupRestoreData()),
                new TestFileDialogService());

            await mainViewModel.InitializeAsync();
            mainViewModel.SelectedClearTab = mainViewModel.ClearableTabs.First(item => item.Tab == MainNavigationTab.YibfAnaBilgi);

            await mainViewModel.ClearSelectedTabCommand.ExecuteAsync(null);

            var storedYibfEntries = await realYibfRepository.GetAnaBilgiEntriesAsync();

            Assert.Contains(storedYibfEntries, item => item.AdaParsel == originalYibfEntry.AdaParsel);
            Assert.Contains(mainViewModel.YibfModule.AnaBilgiEntries, item => item.AdaParsel == originalYibfEntry.AdaParsel);
        }
        finally
        {
            await DeleteDirectoryWithRetriesAsync(root);
        }
    }

    [Fact]
    public async Task SaveAllTabsAsync_Returns_False_And_Keeps_Module_Dirty_When_Module_Persist_Fails()
    {
        var root = Path.Combine(Path.GetTempPath(), "RizaCanKilicIsTakibiTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var databasePath = Path.Combine(root, "save-all-fail.db");

        try
        {
            var settings = new AppSettings
            {
                AutoBackupEnabled = false,
                AutoBackupMinutes = 15,
                SeedSampleDataOnEmpty = false
            };

            var notificationService = new NotificationService();
            var undoRedoService = new UndoRedoService();
            var realYibfRepository = new SqliteYibfRepository(databasePath);
            var mainViewModel = CreateMainViewModel(
                settings,
                notificationService,
                undoRedoService,
                new SqliteTaskRepository(databasePath),
                new SqliteActionRepository(databasePath),
                new SqliteMissingProjectRepository(databasePath),
                new SqliteKarotRepository(databasePath),
                new SqliteTadilatRepository(databasePath),
                new FailOnceYibfRepository(realYibfRepository),
                new TestBackupService(new BackupRestoreData()),
                new TestFileDialogService());

            await mainViewModel.InitializeAsync();
            mainViewModel.YibfModule.LoadFromBackup(
                [CreateYibfEntry("555/5", "Kaydedilemeyen Sahip")],
                Array.Empty<YibfAnaBilgiEvent>(),
                Array.Empty<YibfIsTakibiEntry>(),
                Array.Empty<YibfCellState>());

            var result = await mainViewModel.SaveAllTabsAsync();
            var storedYibfEntries = await realYibfRepository.GetAnaBilgiEntriesAsync();

            Assert.False(result);
            Assert.True(mainViewModel.YibfModule.HasUnsavedChanges);
            Assert.Empty(storedYibfEntries);
        }
        finally
        {
            await DeleteDirectoryWithRetriesAsync(root);
        }
    }

    [Fact]
    public async Task SaveAllTabsAsync_Returns_False_And_Leaves_Module_Dirty_When_Module_Persist_Fails()
    {
        var root = Path.Combine(Path.GetTempPath(), "RizaCanKilicIsTakibiTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var databasePath = Path.Combine(root, "save-all-fail.db");

        try
        {
            var settings = new AppSettings
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
            var realYibfRepository = new SqliteYibfRepository(databasePath);

            var notificationService = new NotificationService();
            var undoRedoService = new UndoRedoService();
            var mainViewModel = CreateMainViewModel(
                settings,
                notificationService,
                undoRedoService,
                taskRepository,
                actionRepository,
                missingProjectRepository,
                karotRepository,
                tadilatRepository,
                new FailOnceYibfRepository(realYibfRepository),
                new TestBackupService(new BackupRestoreData()),
                new TestFileDialogService());

            await mainViewModel.InitializeAsync();
            mainViewModel.YibfModule.LoadFromBackup([CreateYibfEntry("777/7", "Kaydedilemeyen Sahip")], Array.Empty<YibfAnaBilgiEvent>(), Array.Empty<YibfIsTakibiEntry>(), Array.Empty<YibfCellState>());

            var saved = await mainViewModel.SaveAllTabsAsync();
            var storedYibfEntries = await realYibfRepository.GetAnaBilgiEntriesAsync();

            Assert.False(saved);
            Assert.True(mainViewModel.YibfModule.HasUnsavedChanges);
            Assert.Empty(storedYibfEntries);
        }
        finally
        {
            await DeleteDirectoryWithRetriesAsync(root);
        }
    }

    [Fact]
    public async Task SaveAllTabsSafelyAsync_Restores_Persisted_Data_When_Later_Module_Save_Fails()
    {
        var root = Path.Combine(Path.GetTempPath(), "RizaCanKilicIsTakibiTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var databasePath = Path.Combine(root, "safe-save-fail.db");

        try
        {
            var settings = new AppSettings
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
            var realYibfRepository = new SqliteYibfRepository(databasePath);

            await taskRepository.SaveManyAsync([CreateTask("Kalici Gorev", TaskBoardType.Genel)]);

            var notificationService = new NotificationService();
            var undoRedoService = new UndoRedoService();
            var mainViewModel = CreateMainViewModel(
                settings,
                notificationService,
                undoRedoService,
                taskRepository,
                actionRepository,
                missingProjectRepository,
                karotRepository,
                tadilatRepository,
                new FailOnceYibfRepository(realYibfRepository),
                new TestBackupService(new BackupRestoreData()),
                new TestFileDialogService());

            await mainViewModel.InitializeAsync();

            var task = Assert.Single(mainViewModel.GeneralBoard.Tasks);
            task.Title = "Gecici Degisim";
            mainViewModel.YibfModule.LoadFromBackup(
                [CreateYibfEntry("888/8", "Kaydedilemeyen Sahip")],
                Array.Empty<YibfAnaBilgiEvent>(),
                Array.Empty<YibfIsTakibiEntry>(),
                Array.Empty<YibfCellState>());

            var saved = await mainViewModel.SaveAllTabsSafelyAsync();
            var storedTasks = await taskRepository.GetAllAsync();

            Assert.False(saved);
            Assert.Contains(storedTasks, item => item.Title == "Kalici Gorev");
            Assert.DoesNotContain(storedTasks, item => item.Title == "Gecici Degisim");
            Assert.Equal("Gecici Degisim", Assert.Single(mainViewModel.GeneralBoard.Tasks).Title);
            Assert.True(mainViewModel.HasUnsavedChanges);
        }
        finally
        {
            await DeleteDirectoryWithRetriesAsync(root);
        }
    }

    [Fact]
    public async Task ManualBackupCommand_Initializes_Lazy_Modules_Before_Creating_Backup()
    {
        var root = Path.Combine(Path.GetTempPath(), "RizaCanKilicIsTakibiTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var databasePath = Path.Combine(root, "manual-backup.db");
        var backupPath = Path.Combine(root, "manual-backup.json");

        try
        {
            var settings = new AppSettings
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

            await yibfRepository.SaveManyAsync(
                [CreateYibfEntry("909/9", "Yedeklenecek Sahip")],
                Array.Empty<YibfAnaBilgiEvent>(),
                Array.Empty<YibfIsTakibiEntry>(),
                Array.Empty<YibfCellState>());

            var backupService = new TestBackupService(new BackupRestoreData());
            var notificationService = new NotificationService();
            var undoRedoService = new UndoRedoService();
            var mainViewModel = CreateMainViewModel(
                settings,
                notificationService,
                undoRedoService,
                taskRepository,
                actionRepository,
                missingProjectRepository,
                karotRepository,
                tadilatRepository,
                yibfRepository,
                backupService,
                new TestFileDialogService(savePath: backupPath));

            await mainViewModel.InitializeAsync();

            await mainViewModel.ManualBackupCommand.ExecuteAsync(null);

            Assert.Equal(1, backupService.LastYibfAnaBilgiEntryCount);
        }
        finally
        {
            await DeleteDirectoryWithRetriesAsync(root);
        }
    }

    private static YibfAnaBilgiEntry CloneEntry(YibfAnaBilgiEntry entry)
        => new()
        {
            Id = entry.Id,
            AdaParsel = entry.AdaParsel,
            YibfNo = entry.YibfNo,
            Idare = entry.Idare,
            YapiSahibi = entry.YapiSahibi,
            Muteahhit = entry.Muteahhit,
            DisplayOrder = entry.DisplayOrder,
            CreatedAt = entry.CreatedAt,
            UpdatedAt = entry.UpdatedAt
        };

    private static TaskItem CreateTask(string title, TaskBoardType boardType)
        => new()
        {
            Title = title,
            Description = "Test verisi",
            BoardType = boardType,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

    private static YibfAnaBilgiEntry CreateYibfEntry(string adaParsel, string yapiSahibi)
        => new()
        {
            AdaParsel = adaParsel,
            YapiSahibi = yapiSahibi,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

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

    private static MainViewModel CreateMainViewModel(
        AppSettings settings,
        INotificationService notificationService,
        IUndoRedoService undoRedoService,
        ITaskRepository taskRepository,
        IActionRepository actionRepository,
        IMissingProjectRepository missingProjectRepository,
        IKarotRepository karotRepository,
        ITadilatRepository tadilatRepository,
        IYibfRepository yibfRepository,
        IBackupService backupService,
        IFileDialogService fileDialogService)
    {
        var confirmationService = new TestConfirmationService();

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
            fileDialogService,
            notificationService,
            confirmationService,
            new TestTadilatCellNoteDialogService(),
            undoRedoService);

        var yibfModule = new YibfModuleViewModel(
            yibfRepository,
            new TestYibfImportService(),
            fileDialogService,
            notificationService,
            confirmationService,
            new TestTadilatCellNoteDialogService(),
            new TestYibfAnaBilgiEventDialogService(),
            new TestYibfAnaBilgiEntryDialogService(),
            undoRedoService);

        return new MainViewModel(
            taskRepository,
            backupService,
            new TestAppSettingsService(settings),
            new TestLastSaveMetadataService(),
            new TestImportExportService(),
            new TestGenelIsTakibiExcelImportService(),
            notificationService,
            confirmationService,
            new SearchService(),
            new ContextQueryService(),
            new ContextInsightBuilder(new SearchService()),
            undoRedoService,
            fileDialogService,
            settings,
            new DashboardViewModel(),
            new SearchOverlayViewModel(),
            new TaskDetailViewModel(),
            new ToastHostViewModel(notificationService),
            actionModule,
            missingProjectModule,
            karotModule,
            tadilatModule,
            yibfModule);
    }

    private sealed class FailOnceYibfRepository : IYibfRepository
    {
        private readonly IYibfRepository _inner;
        private bool _shouldFail = true;

        public FailOnceYibfRepository(IYibfRepository inner) => _inner = inner;

        public Task<IReadOnlyList<YibfAnaBilgiEntry>> GetAnaBilgiEntriesAsync(CancellationToken cancellationToken = default)
            => _inner.GetAnaBilgiEntriesAsync(cancellationToken);

        public Task<IReadOnlyList<YibfAnaBilgiEvent>> GetAnaBilgiEventsAsync(CancellationToken cancellationToken = default)
            => _inner.GetAnaBilgiEventsAsync(cancellationToken);

        public Task<IReadOnlyList<YibfIsTakibiEntry>> GetIsTakibiEntriesAsync(CancellationToken cancellationToken = default)
            => _inner.GetIsTakibiEntriesAsync(cancellationToken);

        public Task<IReadOnlyList<YibfCellState>> GetCellStatesAsync(CancellationToken cancellationToken = default)
            => _inner.GetCellStatesAsync(cancellationToken);

        public async Task SaveManyAsync(IEnumerable<YibfAnaBilgiEntry> anaBilgiEntries, IEnumerable<YibfAnaBilgiEvent> anaBilgiEvents, IEnumerable<YibfIsTakibiEntry> isTakibiEntries, IEnumerable<YibfCellState> cellStates, CancellationToken cancellationToken = default)
        {
            if (_shouldFail)
            {
                _shouldFail = false;
                throw new InvalidOperationException("Simulated YIBF save failure.");
            }

            await _inner.SaveManyAsync(anaBilgiEntries, anaBilgiEvents, isTakibiEntries, cellStates, cancellationToken);
        }

        public Task DeleteIsTakibiAsync(Guid id, CancellationToken cancellationToken = default)
            => _inner.DeleteIsTakibiAsync(id, cancellationToken);
    }

    private sealed class TestConfirmationService : IConfirmationService
    {
        public bool Confirm(ConfirmationRequest request) => true;
    }

    private sealed class TestBackupService : IBackupService
    {
        private readonly BackupRestoreData _backupData;

        public TestBackupService(BackupRestoreData backupData) => _backupData = backupData;

        public int LastYibfAnaBilgiEntryCount { get; private set; }

        public Task<int> ClearManagedBackupsAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
        
        public Task<int> CleanOldBackupsAsync(int keepCount = 30, CancellationToken cancellationToken = default) => Task.FromResult(0);

        public Task<BackupMetadata> CreateBackupAsync(IEnumerable<TaskItem> tasks, string? backupPath = null, IEnumerable<ActionEntry>? actionEntries = null, IEnumerable<MissingProjectEntry>? missingProjectEntries = null, IEnumerable<MissingProjectCellState>? missingProjectCellStates = null, IEnumerable<KarotEntry>? karotEntries = null, IEnumerable<KarotCellState>? karotCellStates = null, IEnumerable<TadilatEntry>? tadilatEntries = null, IEnumerable<YibfAnaBilgiEntry>? yibfAnaBilgiEntries = null, IEnumerable<YibfAnaBilgiEvent>? yibfAnaBilgiEvents = null, IEnumerable<YibfIsTakibiEntry>? yibfIsTakibiEntries = null, IEnumerable<YibfCellState>? yibfCellStates = null, IEnumerable<TadilatCellState>? tadilatCellStates = null, IEnumerable<QuickTaskTemplate>? quickTaskTemplates = null, IEnumerable<ProjectCatalogEntry>? projectCatalogEntries = null, IEnumerable<Personnel>? personnel = null, IEnumerable<PersonnelAssignment>? personnelAssignments = null, CancellationToken cancellationToken = default)
        {
            LastYibfAnaBilgiEntryCount = yibfAnaBilgiEntries?.Count() ?? 0;
            return Task.FromResult(new BackupMetadata { BackupFilePath = backupPath ?? string.Empty, CreatedAt = DateTime.Now, TaskCount = tasks.Count() });
        }

        public Task<BackupRestoreData> RestoreBackupAsync(string backupPath, CancellationToken cancellationToken = default)
            => Task.FromResult(_backupData);

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
        public DateTime? LoadLastSuccessfulSaveAt() => null;
        public Task SaveLastSuccessfulSaveAtAsync(DateTime timestamp, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class TestImportExportService : IImportExportService
    {
        public Task ExportExcelAsync(IEnumerable<TaskItem> tasks, string filePath, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task ExportWorkbookAsync(ExcelWorkbookExportModel workbook, string filePath, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task ExportWorkbookAsPdfAsync(ExcelWorkbookExportModel workbook, string filePath, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<TaskItem>> ImportExcelAsync(string filePath, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<TaskItem>>(Array.Empty<TaskItem>());
        public Task ExportPdfAsync(IEnumerable<TaskItem> tasks, string filePath, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task ExportReportPackAsync(ReportPackExportModel pack, string filePath, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task ExportPngAsync(UIElement visual, string filePath, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task ExportScrollablePngAsync(UIElement visual, string filePath, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class TestGenelIsTakibiExcelImportService : IGenelIsTakibiExcelImportService
    {
        public GenelIsTakibiExcelImportResult ImportFromFile(string filePath, string aksiyonaEkleneceklerDistrict = "GENEL")
            => new();
    }

    private sealed class TestFileDialogService : IFileDialogService
    {
        private readonly string? _openPath;
        private readonly string? _savePath;

        public TestFileDialogService(string? openPath = null, string? savePath = null)
        {
            _openPath = openPath;
            _savePath = savePath;
        }

        public string? ShowSaveDialog(string title, string filter, string defaultExtension) => _savePath;

        public string? ShowOpenDialog(string title, string filter, bool multiselect = false) => _openPath;
    }

    private sealed class TestAddActionEntryDialogService : IAddActionEntryDialogService
    {
        public Task<ActionEntry?> ShowDialogAsync(string district, ActionEntryCategory category, CancellationToken cancellationToken = default)
            => Task.FromResult<ActionEntry?>(null);
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
        public Task<YibfAnaBilgiEventDialogResult?> ShowDialogAsync(DateTime? eventDate, string description, string backgroundColor, string noteText, string approvalStatus = "", CancellationToken cancellationToken = default)
            => Task.FromResult<YibfAnaBilgiEventDialogResult?>(null);
    }

    private sealed class TestYibfAnaBilgiEntryDialogService : IYibfAnaBilgiEntryDialogService
    {
        public Task<YibfAnaBilgiEntryDialogResult?> ShowDialogAsync(YibfAnaBilgiEntryDialogResult? initialValues = null, bool isEditMode = false, CancellationToken cancellationToken = default)
            => Task.FromResult<YibfAnaBilgiEntryDialogResult?>(null);
    }
}
