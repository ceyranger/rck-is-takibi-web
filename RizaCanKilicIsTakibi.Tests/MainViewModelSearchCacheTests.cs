using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services;
using RizaCanKilicIsTakibi.Services.Abstractions;
using RizaCanKilicIsTakibi.ViewModels;
using System.Windows;

namespace RizaCanKilicIsTakibi.Tests;

public class MainViewModelSearchCacheTests
{
    [Fact]
    public async Task SearchCache_Invalidates_When_Task_Title_Changes()
    {
        var root = Path.Combine(Path.GetTempPath(), "RizaCanKilicIsTakibiTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var databasePath = Path.Combine(root, "search-cache.db");

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
            var mainViewModel = CreateMainViewModel(databasePath, settings, notificationService, undoRedoService);

            await mainViewModel.InitializeAsync();

            var task = new TaskItem
            {
                Title = "Eski Başlık",
                Description = "Arama testi",
                BoardType = TaskBoardType.Genel,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            mainViewModel.GeneralBoard.AddTask(task);
            mainViewModel.OpenSearchCommand.Execute(null);
            mainViewModel.SearchOverlay.Query = "Eski Başlık";
            Assert.Contains(mainViewModel.SearchOverlay.Results, item => item.Title == "Eski Başlık");

            task.Title = "Yeni Başlık";
            task.UpdatedAt = DateTime.Now;
            mainViewModel.SearchOverlay.Query = "Yeni Başlık";

            Assert.Contains(mainViewModel.SearchOverlay.Results, item => item.Title == "Yeni Başlık");
            Assert.DoesNotContain(mainViewModel.SearchOverlay.Results, item => item.Title == "Eski Başlık");
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
    public async Task ImportBackup_Rolls_Back_When_Yibf_Save_Fails_Once()
    {
        var root = Path.Combine(Path.GetTempPath(), "RizaCanKilicIsTakibiTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var databasePath = Path.Combine(root, "import-rollback.db");

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
                Title = "Orijinal Genel İş",
                Description = "Kaynak veri",
                BoardType = TaskBoardType.Genel,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            var originalYibfEntry = new YibfAnaBilgiEntry
            {
                AdaParsel = "101/1",
                YapiSahibi = "Orijinal Sahip",
                YibfNo = "Y-001",
                Idare = "Sinop",
                Muteahhit = "Orijinal Müteahhit",
                DisplayOrder = 0,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            var taskRepository = new SqliteTaskRepository(databasePath);
            await taskRepository.SaveManyAsync([originalTask.Clone()]);

            var realYibfRepository = new SqliteYibfRepository(databasePath);
            await realYibfRepository.SaveManyAsync(
                [originalYibfEntry],
                Array.Empty<YibfAnaBilgiEvent>(),
                Array.Empty<YibfIsTakibiEntry>(),
                Array.Empty<YibfCellState>());

            var importedTask = new TaskItem
            {
                Title = "İçe Aktarılan İş",
                Description = "Yeni veri",
                BoardType = TaskBoardType.Genel,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            var importedYibfEntry = new YibfAnaBilgiEntry
            {
                AdaParsel = "202/2",
                YapiSahibi = "İçe Aktarılan Sahip",
                YibfNo = "Y-999",
                Idare = "Boyabat",
                Muteahhit = "Yeni Müteahhit",
                DisplayOrder = 0,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            var notificationService = new NotificationService();
            var undoRedoService = new UndoRedoService();
            var yibfRepository = new FailOnceYibfRepository(realYibfRepository);
            var mainViewModel = CreateMainViewModel(
                databasePath,
                settings,
                notificationService,
                undoRedoService,
                backupService: new RestoringBackupService(new BackupRestoreData
                {
                    Tasks = [importedTask],
                    YibfAnaBilgiEntries = [importedYibfEntry]
                }),
                fileDialogService: new FixedFileDialogService("import.json"),
                yibfRepository: yibfRepository);

            await mainViewModel.InitializeAsync();
            await mainViewModel.ImportBackupCommand.ExecuteAsync(null);

            var persistedTasks = (await taskRepository.GetAllAsync()).ToList();
            Assert.Single(persistedTasks);
            Assert.Equal(originalTask.Title, persistedTasks[0].Title);

            var persistedYibfEntries = (await realYibfRepository.GetAnaBilgiEntriesAsync()).ToList();
            Assert.Single(persistedYibfEntries);
            Assert.Equal(originalYibfEntry.AdaParsel, persistedYibfEntries[0].AdaParsel);
            Assert.Equal(originalYibfEntry.YapiSahibi, persistedYibfEntries[0].YapiSahibi);

            Assert.Single(mainViewModel.GeneralBoard.Tasks);
            Assert.Equal(originalTask.Title, mainViewModel.GeneralBoard.Tasks[0].Title);

            var viewModelYibfEntries = mainViewModel.YibfModule.GetAnaBilgiEntriesSnapshot();
            Assert.Single(viewModelYibfEntries);
            Assert.Equal(originalYibfEntry.AdaParsel, viewModelYibfEntries[0].AdaParsel);
            Assert.False(mainViewModel.HasAnyUnsavedChanges);
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

    private static MainViewModel CreateMainViewModel(
        string databasePath,
        AppSettings settings,
        INotificationService notificationService,
        IUndoRedoService undoRedoService,
        IBackupService? backupService = null,
        IFileDialogService? fileDialogService = null,
        IYibfRepository? yibfRepository = null)
    {
        var taskRepository = new SqliteTaskRepository(databasePath);
        var actionRepository = new SqliteActionRepository(databasePath);
        var missingProjectRepository = new SqliteMissingProjectRepository(databasePath);
        var karotRepository = new SqliteKarotRepository(databasePath);
        var tadilatRepository = new SqliteTadilatRepository(databasePath);
        var yibfRepo = yibfRepository ?? new SqliteYibfRepository(databasePath);
        var confirmationService = new TestConfirmationService();
        var dialogs = fileDialogService ?? new TestFileDialogService();

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
            dialogs,
            notificationService,
            confirmationService,
            new TestTadilatCellNoteDialogService(),
            undoRedoService);

        var yibfModule = new YibfModuleViewModel(
            yibfRepo,
            new TestYibfImportService(),
            dialogs,
            notificationService,
            confirmationService,
            new TestTadilatCellNoteDialogService(),
            new TestYibfAnaBilgiEventDialogService(),
            new TestYibfAnaBilgiEntryDialogService(),
            undoRedoService);

        return new MainViewModel(
            taskRepository,
            backupService ?? new TestBackupService(),
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
            dialogs,
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

    private sealed class TestConfirmationService : IConfirmationService
    {
        public bool Confirm(ConfirmationRequest request) => true;
    }

    private sealed class TestBackupService : IBackupService
    {
        public Task<int> ClearManagedBackupsAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
        public Task<int> CleanOldBackupsAsync(int keepCount = 30, CancellationToken cancellationToken = default) => Task.FromResult(0);
        public Task<BackupMetadata> CreateBackupAsync(IEnumerable<TaskItem> tasks, string? backupPath = null, IEnumerable<ActionEntry>? actionEntries = null, IEnumerable<MissingProjectEntry>? missingProjectEntries = null, IEnumerable<MissingProjectCellState>? missingProjectCellStates = null, IEnumerable<KarotEntry>? karotEntries = null, IEnumerable<KarotCellState>? karotCellStates = null, IEnumerable<TadilatEntry>? tadilatEntries = null, IEnumerable<YibfAnaBilgiEntry>? yibfAnaBilgiEntries = null, IEnumerable<YibfAnaBilgiEvent>? yibfAnaBilgiEvents = null, IEnumerable<YibfIsTakibiEntry>? yibfIsTakibiEntries = null, IEnumerable<YibfCellState>? yibfCellStates = null, IEnumerable<TadilatCellState>? tadilatCellStates = null, IEnumerable<QuickTaskTemplate>? quickTaskTemplates = null, IEnumerable<ProjectCatalogEntry>? projectCatalogEntries = null, CancellationToken cancellationToken = default)
            => Task.FromResult(new BackupMetadata { BackupFilePath = backupPath ?? string.Empty, CreatedAt = DateTime.Now, TaskCount = tasks.Count() });
        public Task<BackupRestoreData> RestoreBackupAsync(string backupPath, CancellationToken cancellationToken = default) => Task.FromResult(new BackupRestoreData());
        public void ScheduleAutoBackup(TimeSpan interval, Func<Task> callback) { }
        public void StopAutoBackup() { }
    }

    private sealed class RestoringBackupService(BackupRestoreData restoredData) : IBackupService
    {
        public Task<int> ClearManagedBackupsAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
        public Task<int> CleanOldBackupsAsync(int keepCount = 30, CancellationToken cancellationToken = default) => Task.FromResult(0);
        public Task<BackupMetadata> CreateBackupAsync(IEnumerable<TaskItem> tasks, string? backupPath = null, IEnumerable<ActionEntry>? actionEntries = null, IEnumerable<MissingProjectEntry>? missingProjectEntries = null, IEnumerable<MissingProjectCellState>? missingProjectCellStates = null, IEnumerable<KarotEntry>? karotEntries = null, IEnumerable<KarotCellState>? karotCellStates = null, IEnumerable<TadilatEntry>? tadilatEntries = null, IEnumerable<YibfAnaBilgiEntry>? yibfAnaBilgiEntries = null, IEnumerable<YibfAnaBilgiEvent>? yibfAnaBilgiEvents = null, IEnumerable<YibfIsTakibiEntry>? yibfIsTakibiEntries = null, IEnumerable<YibfCellState>? yibfCellStates = null, IEnumerable<TadilatCellState>? tadilatCellStates = null, IEnumerable<QuickTaskTemplate>? quickTaskTemplates = null, IEnumerable<ProjectCatalogEntry>? projectCatalogEntries = null, CancellationToken cancellationToken = default)
            => Task.FromResult(new BackupMetadata { BackupFilePath = backupPath ?? string.Empty, CreatedAt = DateTime.Now, TaskCount = tasks.Count() });
        public Task<BackupRestoreData> RestoreBackupAsync(string backupPath, CancellationToken cancellationToken = default)
            => Task.FromResult(restoredData);
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
        public string? ShowSaveDialog(string title, string filter, string defaultExtension) => null;
        public string? ShowOpenDialog(string title, string filter, bool multiselect = false) => null;
    }

    private sealed class FixedFileDialogService(string openPath) : IFileDialogService
    {
        public string? ShowSaveDialog(string title, string filter, string defaultExtension) => null;
        public string? ShowOpenDialog(string title, string filter, bool multiselect = false) => openPath;
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

    private sealed class FailOnceYibfRepository(IYibfRepository innerRepository) : IYibfRepository
    {
        private bool _hasFailed;

        public Task<IReadOnlyList<YibfAnaBilgiEntry>> GetAnaBilgiEntriesAsync(CancellationToken cancellationToken = default)
            => innerRepository.GetAnaBilgiEntriesAsync(cancellationToken);

        public Task<IReadOnlyList<YibfAnaBilgiEvent>> GetAnaBilgiEventsAsync(CancellationToken cancellationToken = default)
            => innerRepository.GetAnaBilgiEventsAsync(cancellationToken);

        public Task<IReadOnlyList<YibfIsTakibiEntry>> GetIsTakibiEntriesAsync(CancellationToken cancellationToken = default)
            => innerRepository.GetIsTakibiEntriesAsync(cancellationToken);

        public Task<IReadOnlyList<YibfCellState>> GetCellStatesAsync(CancellationToken cancellationToken = default)
            => innerRepository.GetCellStatesAsync(cancellationToken);

        public Task DeleteIsTakibiAsync(Guid id, CancellationToken cancellationToken = default)
            => innerRepository.DeleteIsTakibiAsync(id, cancellationToken);

        public Task SaveManyAsync(
            IEnumerable<YibfAnaBilgiEntry> anaBilgiEntries,
            IEnumerable<YibfAnaBilgiEvent> anaBilgiEvents,
            IEnumerable<YibfIsTakibiEntry> isTakibiEntries,
            IEnumerable<YibfCellState> cellStates,
            CancellationToken cancellationToken = default)
        {
            if (!_hasFailed)
            {
                _hasFailed = true;
                throw new InvalidOperationException("Simulated YİBF save failure.");
            }

            return innerRepository.SaveManyAsync(anaBilgiEntries, anaBilgiEvents, isTakibiEntries, cellStates, cancellationToken);
        }
    }
}
