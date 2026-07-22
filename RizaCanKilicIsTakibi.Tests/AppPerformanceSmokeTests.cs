using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services;
using RizaCanKilicIsTakibi.Services.Abstractions;
using RizaCanKilicIsTakibi.ViewModels;
using System.Diagnostics;
using System.Windows;
using Xunit.Abstractions;

namespace RizaCanKilicIsTakibi.Tests;

public class AppPerformanceSmokeTests
{
    private readonly ITestOutputHelper _output;

    public AppPerformanceSmokeTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task MainViewModel_Initialize_TabSwitch_And_Search_Remain_Responsive_On_Temp_5000_Plus_Data()
    {
        await ExecuteWithTempDirectoryAsync("appperf", async databasePath =>
        {
            await SeedApplicationDataAsync(databasePath);

            var settings = new AppSettings
            {
                AutoBackupEnabled = false,
                AutoBackupMinutes = 15,
                SeedSampleDataOnEmpty = false
            };

            var notificationService = new NotificationService();
            var confirmationService = new TestConfirmationService();
            var undoRedoService = new UndoRedoService();

            var taskRepository = new SqliteTaskRepository(databasePath);
            var actionRepository = new SqliteActionRepository(databasePath);
            var missingProjectRepository = new SqliteMissingProjectRepository(databasePath);
            var karotRepository = new SqliteKarotRepository(databasePath);
            var tadilatRepository = new SqliteTadilatRepository(databasePath);
            var yibfRepository = new SqliteYibfRepository(databasePath);

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

            var searchOverlay = new SearchOverlayViewModel();
            var mainViewModel = new MainViewModel(
                taskRepository,
                new TestBackupService(),
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
                new TestFileDialogService(),
                settings,
                new DashboardViewModel(),
                searchOverlay,
                new TaskDetailViewModel(),
                new ToastHostViewModel(notificationService),
                actionModule,
                missingProjectModule,
                karotModule,
                tadilatModule,
                yibfModule);

            var startup = Stopwatch.StartNew();
            await mainViewModel.InitializeAsync();
            startup.Stop();

            var tabSwitch = Stopwatch.StartNew();
            var tabs = new[]
            {
                MainNavigationTab.GenelIsTakibi,
                MainNavigationTab.Aksiyon,
                MainNavigationTab.EksikProje,
                MainNavigationTab.KarotTakibi,
                MainNavigationTab.TadilatTakibi,
                MainNavigationTab.YibfAnaBilgi,
                MainNavigationTab.YibfIsTakibi,
                MainNavigationTab.YibfBekleyenIsler
            };

            for (var round = 0; round < 8; round++)
            {
                foreach (var tab in tabs)
                {
                    mainViewModel.SelectMainTabCommand.Execute(tab);
                }
            }

            tabSwitch.Stop();

            var search = Stopwatch.StartNew();
            mainViewModel.OpenSearchCommand.Execute(null);
            searchOverlay.Query = "Sahip 12";
            search.Stop();

            await WaitUntilAsync(() => actionModule.DistrictGroups.Count > 0);
            await WaitUntilAsync(() => missingProjectModule.Rows.Count > 0);
            await WaitUntilAsync(() => karotModule.VisibleRows.Count > 0);
            await WaitUntilAsync(() => tadilatModule.DistrictGroups.Count > 0);
            await WaitUntilAsync(() => yibfModule.IsTakibiRows.Count > 0, timeoutMs: 20000);
            await WaitUntilAsync(() => searchOverlay.Results.Count > 0);

            Assert.True(searchOverlay.Results.Count > 0);
            Assert.True(actionModule.DistrictGroups.Count > 0);
            Assert.True(missingProjectModule.Rows.Count > 0);
            Assert.True(karotModule.VisibleRows.Count > 0);
            Assert.True(tadilatModule.DistrictGroups.Count > 0);
            Assert.True(yibfModule.IsTakibiRows.Count > 0);

            _output.WriteLine($"App startup initialize: {startup.ElapsedMilliseconds} ms");
            _output.WriteLine($"Tab switch x{tabs.Length * 8}: {tabSwitch.ElapsedMilliseconds} ms");
            _output.WriteLine($"Classic search query: {search.ElapsedMilliseconds} ms | results: {searchOverlay.Results.Count}");
        });
    }

    private static async Task SeedApplicationDataAsync(string databasePath)
    {
        var taskRepository = new SqliteTaskRepository(databasePath);
        var actionRepository = new SqliteActionRepository(databasePath);
        var missingProjectRepository = new SqliteMissingProjectRepository(databasePath);
        var karotRepository = new SqliteKarotRepository(databasePath);
        var tadilatRepository = new SqliteTadilatRepository(databasePath);
        var yibfRepository = new SqliteYibfRepository(databasePath);

        var tasks = Enumerable.Range(0, 1200)
            .Select(index =>
            {
                var item = new TaskItem
                {
                    Title = $"Genel Görev {index}",
                    Description = $"Ada parsel {700 + index}-{index % 20} için işlem",
                    BoardType = index % 4 == 0 ? TaskBoardType.Acil : TaskBoardType.Genel,
                    SortOrder = index,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };
                item.Notes.Add(new TaskNote
                {
                    Text = $"Sahip {index} notu",
                    CreatedAt = DateTime.Now
                });
                return item;
            })
            .ToList();
        await taskRepository.SaveManyAsync(tasks);

        for (var index = 0; index < 600; index++)
        {
            await actionRepository.AddAsync(new ActionEntry
            {
                Category = index % 2 == 0 ? ActionEntryCategory.Aksiyon : ActionEntryCategory.AksiyonaEklenecekler,
                District = $"ILCE-{index % 10}",
                OwnerParcelText = $"{500 + index}-{index % 15} Sahip {index}",
                WorkText = $"İş metni {index}",
                DisplayOrder = index,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            });
        }

        var missingEntries = Enumerable.Range(0, 900)
            .Select(index => new MissingProjectEntry
            {
                AdaParsel = $"{800 + index}-{index % 20}",
                YapiSahibi = $"Sahip {index}",
                RecordMedium = (MissingProjectMedium)(index % 3),
                RecordMediumText = (index % 3) switch
                {
                    0 => "Dijital",
                    1 => "Fiziksel",
                    _ => "Fiziksel + Dijital"
                },
                MissingProjectText = $"Eksik Proje {index % 12}",
                Description = $"Açıklama {index}",
                DisplayOrder = index
            })
            .ToList();

        var missingStates = missingEntries
            .Where((_, index) => index % 4 == 0)
            .Select(entry => new MissingProjectCellState
            {
                EntryId = entry.Id,
                ColumnKey = MissingProjectColumnKeys.MissingProjectText,
                BackgroundColor = "#FFFFFF00",
                NoteText = $"Not {entry.AdaParsel}"
            })
            .ToList();
        await missingProjectRepository.SaveManyAsync(missingEntries, missingStates);

        var karotEntries = Enumerable.Range(0, 900)
            .Select(index => new KarotEntry
            {
                SampleReceivedDate = DateTime.Today.AddDays(-(index % 30)),
                YibfNo = $"K-{index:D5}",
                AdaParsel = $"{900 + index}-{index % 18}",
                YapiSahibi = $"Sahip {index}",
                Muteahhit = $"Müteahhit {index % 250}",
                KatBilgisi = $"{index % 8}. Kat",
                BetonSinifi = "C30",
                TwentyEightDayResult = index % 5 == 0 ? "Olumlu" : string.Empty,
                BetonFirmasi = $"Firma {index % 25}",
                Laboratuvar = $"Lab {index % 10}",
                Aciklama = $"Karot açıklama {index}",
                Status = (KarotStatus)(index % 4),
                DisplayOrder = index
            })
            .ToList();

        var karotStates = karotEntries
            .Where((_, index) => index % 3 == 0)
            .Select(entry => new KarotCellState
            {
                EntryId = entry.Id,
                ColumnKey = KarotColumnKeys.Aciklama,
                NoteText = $"Not {entry.AdaParsel}"
            })
            .ToList();
        await karotRepository.SaveManyAsync(karotEntries, karotStates);

        var tadilatEntries = Enumerable.Range(0, 800)
            .Select(index => new TadilatEntry
            {
                District = $"ILCE-{index % 12}",
                JobName = $"Tadilat İşi {index}",
                ProjectType = $"Tür {index % 4}",
                DigitalReceived = index % 2 == 0 ? "Var" : string.Empty,
                InspectorApproved = index % 3 == 0 ? "Tamam" : string.Empty,
                OutputAndReportArrived = index % 4 == 0 ? "Hazır" : string.Empty,
                OfficialLetterSubmitted = index % 5 == 0 ? "Verildi" : string.Empty,
                ArchivedFromMunicipality = index % 6 == 0 ? "Arşiv" : string.Empty,
                Description1 = $"Açıklama1 {index}",
                Description2 = $"Açıklama2 {index}",
                DisplayOrder = index,
                SubTab = index % 5 == 0 ? TadilatSubTab.Biten : TadilatSubTab.Aktif
            })
            .ToList();

        var tadilatStates = tadilatEntries
            .Where((_, index) => index % 3 == 0)
            .Select(entry => new TadilatCellState
            {
                EntryId = entry.Id,
                ColumnKey = TadilatColumnKeys.JobName,
                BackgroundColor = "#FFFFFF00",
                NoteText = $"Not {entry.JobName}"
            })
            .ToList();
        await tadilatRepository.SaveManyAsync(tadilatEntries, tadilatStates);

        var yibfAna = Enumerable.Range(0, 400)
            .Select(index => new YibfAnaBilgiEntry
            {
                AdaParsel = $"{1000 + index}-{index % 22}",
                YibfNo = $"Y-{index:D6}",
                Idare = $"İdare {index % 9}",
                YapiSahibi = $"Sahip {index}",
                Muteahhit = $"Müteahhit {index % 200}",
                DisplayOrder = index
            })
            .ToList();

        var yibfEvents = yibfAna
            .Select((entry, index) => new YibfAnaBilgiEvent
            {
                EntryId = entry.Id,
                EventDate = DateTime.Today.AddDays(-(index % 50)),
                Description = $"Olay {index}",
                BackgroundColor = index % 2 == 0 ? "#FFFF0000" : "#FFFFFF00",
                NoteText = $"Not {index}",
                DisplayOrder = index
            })
            .ToList();

        var yibfRows = Enumerable.Range(0, 800)
            .Select(index => new YibfIsTakibiEntry
            {
                JobName = $"YİBF İş {index}",
                MuellifBilgileriGeldiMi = index % 2 == 0 ? "Evet" : string.Empty,
                EvraklarTamMi = index % 3 == 0 ? "Evet" : string.Empty,
                RuhsatBasvurusuYapildiMi = index % 4 == 0 ? "Evet" : string.Empty,
                DisplayOrder = index
            })
            .ToList();

        var yibfStates = yibfRows
            .Where((_, index) => index % 4 == 0)
            .Select(entry => new YibfCellState
            {
                EntryId = entry.Id,
                ColumnKey = YibfIsTakibiColumnKeys.JobName,
                BackgroundColor = "#FFFFFF00",
                NoteText = $"Not {entry.JobName}"
            })
            .ToList();
        await yibfRepository.SaveManyAsync(yibfAna, yibfEvents, yibfRows, yibfStates);
    }

    private static async Task ExecuteWithTempDirectoryAsync(string prefix, Func<string, Task> action)
    {
        var root = Path.Combine(Path.GetTempPath(), "RizaCanKilicIsTakibiAppPerf", $"{prefix}_{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        var databasePath = Path.Combine(root, "appperf.db");

        try
        {
            await action(databasePath);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                for (var attempt = 0; attempt < 5; attempt++)
                {
                    try
                    {
                        Directory.Delete(root, true);
                        break;
                    }
                    catch (IOException) when (attempt < 4)
                    {
                        await Task.Delay(100);
                    }
                    catch (UnauthorizedAccessException) when (attempt < 4)
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

    private static async Task WaitUntilAsync(Func<bool> condition, int timeoutMs = 5000, int pollMs = 50)
    {
        var deadline = Environment.TickCount64 + timeoutMs;
        while (!condition() && Environment.TickCount64 < deadline)
        {
            await Task.Delay(pollMs);
        }
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
        public string? ShowOpenDialog(string title, string filter, bool multiselect = false) => null;
        public string? ShowSaveDialog(string title, string filter, string defaultExtension) => null;
    }

    private sealed class TestAddActionEntryDialogService : IAddActionEntryDialogService
    {
        public Task<ActionEntry?> ShowDialogAsync(string district, ActionEntryCategory category, CancellationToken cancellationToken = default) => Task.FromResult<ActionEntry?>(null);
    }

    private sealed class TestKarotStatusDialogService : IKarotStatusDialogService
    {
        public Task<KarotStatus?> ShowDialogAsync(KarotStatus currentStatus, CancellationToken cancellationToken = default) => Task.FromResult<KarotStatus?>(currentStatus);
    }

    private sealed class TestTadilatCellNoteDialogService : ITadilatCellNoteDialogService
    {
        public Task<TadilatCellNoteDialogResult?> ShowDialogAsync(string currentNote, CancellationToken cancellationToken = default) => Task.FromResult<TadilatCellNoteDialogResult?>(null);
    }

    private sealed class TestTadilatImportService : ITadilatImportService
    {
        public Task<TadilatImportData> ImportAsync(string filePath, CancellationToken cancellationToken = default) => Task.FromResult(new TadilatImportData());
    }

    private sealed class TestYibfImportService : IYibfImportService
    {
        public Task<YibfImportData> ImportAsync(string filePath, CancellationToken cancellationToken = default) => Task.FromResult(new YibfImportData());
    }

    private sealed class TestYibfAnaBilgiEventDialogService : IYibfAnaBilgiEventDialogService
    {
        public Task<YibfAnaBilgiEventDialogResult?> ShowDialogAsync(DateTime? eventDate, string description, string backgroundColor, string noteText, CancellationToken cancellationToken = default)
            => Task.FromResult<YibfAnaBilgiEventDialogResult?>(null);
    }

    private sealed class TestYibfAnaBilgiEntryDialogService : IYibfAnaBilgiEntryDialogService
    {
        public Task<YibfAnaBilgiEntryDialogResult?> ShowDialogAsync(YibfAnaBilgiEntryDialogResult? initialValues = null, bool isEditMode = false, CancellationToken cancellationToken = default)
            => Task.FromResult<YibfAnaBilgiEntryDialogResult?>(null);
    }
}
