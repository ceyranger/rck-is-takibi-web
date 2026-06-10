using CommunityToolkit.Mvvm.Input;
using Microsoft.Data.Sqlite;
using RizaCanKilicIsTakibi.Commands;
using RizaCanKilicIsTakibi.Helpers;
using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services;
using RizaCanKilicIsTakibi.Services.Abstractions;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace RizaCanKilicIsTakibi.ViewModels;

public sealed partial class MainViewModel : ViewModelBase
{
    private readonly ITaskRepository _taskRepository;
    private readonly IBackupService _backupService;
    private readonly IAppSettingsService _appSettingsService;
    private readonly ILastSaveMetadataService _lastSaveMetadataService;
    private readonly IImportExportService _importExportService;
    private readonly INotificationService _notificationService;
    private readonly IConfirmationService _confirmationService;
    private readonly ISearchService _searchService;
    private readonly IContextQueryService _contextQueryService;
    private readonly IContextInsightBuilder _contextInsightBuilder;
    private readonly IUndoRedoService _undoRedoService;
    private readonly IFileDialogService _fileDialogService;
    private readonly IQuickTaskTemplateRepository? _quickTaskTemplateRepository;
    private readonly IQuickTaskTemplateDialogService? _quickTaskTemplateDialogService;
    private readonly AppSettings _settings;
    private readonly SemaphoreSlim _operationGate = new(1, 1);
    private readonly AsyncLocal<int> _operationGateDepth = new();

    private TaskItem? _clipboardTask;
    private bool _isInitialized;
    private bool _isBusy;
    private bool _hasUnsavedChanges;
    private bool _suppressTaskDirtyTracking;
    private bool _isSavingGeneralTasks;
    private bool _isActionViewActivated;
    private bool _isTadilatViewActivated;
    private bool _isYibfViewActivated;
    private bool _isYibfAnaBilgiViewActivated;
    private bool _isYibfIsTakibiViewActivated;
    private bool _isYibfPendingViewActivated;
    private bool _isTumEksiklerViewActivated;
    private bool _isSettingsViewActivated;
    private bool _hasUnsavedSettings;
    private DateTime? _lastSuccessfulSaveAt;
    private TaskBoardViewModel _activeBoard;
    private Task? _actionModuleInitializationTask;
    private Task? _missingProjectModuleInitializationTask;
    private Task? _karotModuleInitializationTask;
    private Task? _tadilatModuleInitializationTask;
    private Task? _yibfModuleInitializationTask;
    private Task? _allModulesInitializationTask;
    private Task? _backgroundWarmupTask;
    private Task? _searchWarmupTask;
    private IReadOnlyList<SearchResultItem>? _searchCorpusCache;
    private MainNavigationTab _selectedMainTab = MainNavigationTab.GenelIsTakibi;
    private ClearTabOption? _selectedClearTab;
    private static readonly CultureInfo TurkishCulture = CultureInfo.GetCultureInfo("tr-TR");
    private const string StrongRedColor = "#FFFF0000";
    private const string StrongYellowColor = "#FFFFFF00";
    private const string LegacyPaleRedColor = "#FFF4C4C4";
    private const string LegacyPaleYellowColor = "#FFF7EDB3";
    private const string AcilLabel = "ACİL";
    private const string DikkatLabel = "DİKKAT";
    private const string CategoryGenel = "GENEL";
    private const string CategoryEksikProje = "EKSİK PROJE";
    private const string CategoryKarot = "KAROT";
    private const string CategoryTadilat = "TADİLAT";
    private const string CategoryYibfIsTakibi = "YİBF İŞ TAKİBİ";
    public sealed record ClearTabOption(MainNavigationTab Tab, string DisplayName);
    private sealed record ApplicationStateSnapshot(
        BackupRestoreData Data,
        AppSettings Settings,
        bool HasUnsavedTaskChanges,
        bool HasUnsavedSettings,
        bool HasUnsavedActionChanges,
        bool HasUnsavedMissingProjectChanges,
        bool HasUnsavedKarotChanges,
        bool HasUnsavedTadilatChanges,
        bool HasUnsavedYibfChanges);
    private sealed record PersistedFilesSnapshot(
        string RootDirectory,
        string DatabasePath,
        string DatabaseSnapshotPath,
        string WalPath,
        string WalSnapshotPath,
        string ShmPath,
        string ShmSnapshotPath,
        string? SettingsPath,
        string? SettingsSnapshotPath) : IDisposable
    {
        public void Dispose()
        {
            try
            {
                if (Directory.Exists(RootDirectory))
                {
                    Directory.Delete(RootDirectory, recursive: true);
                }
            }
            catch
            {
                // Geçici snapshot temizliği başarısız olsa da ana akışı bozma.
            }
        }
    }
    private enum PersistWithRollbackResult
    {
        Succeeded,
        RolledBack,
        RollbackFailed
    }

    public MainViewModel(
        ITaskRepository taskRepository,
        IBackupService backupService,
        IAppSettingsService appSettingsService,
        ILastSaveMetadataService lastSaveMetadataService,
        IImportExportService importExportService,
        INotificationService notificationService,
        IConfirmationService confirmationService,
        ISearchService searchService,
        IContextQueryService contextQueryService,
        IContextInsightBuilder contextInsightBuilder,
        IUndoRedoService undoRedoService,
        IFileDialogService fileDialogService,
        AppSettings settings,
        DashboardViewModel dashboard,
        SearchOverlayViewModel searchOverlay,
        TaskDetailViewModel detailPanel,
        ToastHostViewModel toastHost,
        ActionModuleViewModel actionModule,
        MissingProjectModuleViewModel missingProjectModule,
        KarotModuleViewModel karotModule,
        TadilatModuleViewModel tadilatModule,
        YibfModuleViewModel yibfModule,
        IQuickTaskTemplateRepository? quickTaskTemplateRepository = null,
        IQuickTaskTemplateDialogService? quickTaskTemplateDialogService = null)
    {
        _taskRepository = taskRepository;
        _backupService = backupService;
        _appSettingsService = appSettingsService;
        _lastSaveMetadataService = lastSaveMetadataService;
        _importExportService = importExportService;
        _notificationService = notificationService;
        _confirmationService = confirmationService;
        _searchService = searchService;
        _contextQueryService = contextQueryService;
        _contextInsightBuilder = contextInsightBuilder;
        _undoRedoService = undoRedoService;
        _fileDialogService = fileDialogService;
        _quickTaskTemplateRepository = quickTaskTemplateRepository;
        _quickTaskTemplateDialogService = quickTaskTemplateDialogService;
        _settings = settings;

        Dashboard = dashboard;
        SearchOverlay = searchOverlay;
        DetailPanel = detailPanel;
        ToastHost = toastHost;
        ActionModule = actionModule;
        MissingProjectModule = missingProjectModule;
        KarotModule = karotModule;
        TadilatModule = tadilatModule;
        YibfModule = yibfModule;
        TumEksikler = new TumEksiklerViewModel();
        ClearableTabs = BuildClearableTabs();
        _selectedClearTab = ClearableTabs.FirstOrDefault();

        UrgentBoard = new TaskBoardViewModel("Acil İşler", TaskBoardType.Acil);
        GeneralBoard = new TaskBoardViewModel("Genel İşler", TaskBoardType.Genel);
        _activeBoard = GeneralBoard;

        SelectMainTabCommand = new RelayCommand<MainNavigationTab>(SelectMainTab);

        AddGeneralTaskCommand = new RelayCommand(() => AddTask(TaskBoardType.Genel));
        AddUrgentTaskCommand = new RelayCommand(() => AddTask(TaskBoardType.Acil));
        OpenQuickUrgentTaskDialogCommand = new AsyncRelayCommand(OpenQuickUrgentTaskDialogAsync, CanOpenQuickUrgentTaskDialog);
        DeleteGeneralTaskCommand = new RelayCommand(() => DeleteTask(GeneralBoard.SelectedTask), () => GeneralBoard.SelectedTask is not null);
        DeleteUrgentTaskCommand = new RelayCommand(() => DeleteTask(UrgentBoard.SelectedTask), () => UrgentBoard.SelectedTask is not null);
        DeleteSelectedTaskCommand = new RelayCommand(() => DeleteSelectedTask(), () => SelectedTask is not null);
        DeleteTaskCommand = new RelayCommand<TaskItem?>(DeleteTask, task => task is not null);
        CopyTaskFromContextCommand = new RelayCommand<TaskItem?>(CopyTaskFromContext, task => task is not null);
        DeleteActiveSelectionCommand = new AsyncRelayCommand(DeleteActiveSelectionAsync);
        MoveTaskUpCommand = new RelayCommand(() => MoveTask(-1), CanMoveUp);
        MoveTaskDownCommand = new RelayCommand(() => MoveTask(1), CanMoveDown);
        CommitGeneralEditCommand = new RelayCommand(CommitGeneralEdit);
        SaveActiveTabCommand = new AsyncRelayCommand(SaveActiveTabAsync);
        CopyTaskCommand = new RelayCommand(CopySelectedTask, () => SelectedTask is not null);
        PasteTaskCommand = new RelayCommand(PasteTask, () => _clipboardTask is not null);
        PasteTaskToBoardCommand = new RelayCommand<TaskBoardType>(PasteTaskToBoard, _ => _clipboardTask is not null);

        OpenSearchCommand = new RelayCommand(OpenSearch);
        CloseSearchCommand = new RelayCommand(() => SearchOverlay.Close());
        RunContextQueryCommand = new RelayCommand(RunContextQuery);
        EscapeCommand = new RelayCommand(HandleEscape);

        ManualBackupCommand = new AsyncRelayCommand(ManualBackupAsync);
        ImportBackupCommand = new AsyncRelayCommand(ImportBackupAsync);
        ExportExcelCommand = new AsyncRelayCommand(ExportExcelAsync);
        ImportExcelCommand = new AsyncRelayCommand(ImportExcelAsync);
        ExportPdfCommand = new AsyncRelayCommand(ExportPdfAsync);
        ExportUrgentPngCommand = new AsyncRelayCommand<UIElement?>(ExportUrgentPngAsync);
        ExportGeneralPngCommand = new AsyncRelayCommand<UIElement?>(ExportGeneralPngAsync);
        ExportActionListPngCommand = new AsyncRelayCommand<UIElement?>(ExportActionListPngAsync);
        ExportMissingProjectPngCommand = new AsyncRelayCommand<UIElement?>(ExportMissingProjectPngAsync);
        ExportKarotPngCommand = new AsyncRelayCommand<UIElement?>(ExportKarotPngAsync);
        ExportYibfIsTakibiPngCommand = new AsyncRelayCommand<UIElement?>(ExportYibfIsTakibiPngAsync);
        RefreshTumEksiklerCommand = new AsyncRelayCommand(RefreshTumEksiklerAsync);
        CleanupBuildArtifactsCommand = new AsyncRelayCommand(CleanupBuildArtifactsAsync);
        SaveSettingsCommand = new AsyncRelayCommand(SaveSettingsWithConfirmationAsync, () => HasUnsavedSettings);
        ResetLiveDataCommand = new AsyncRelayCommand(() => ResetApplicationDataAsync(includeBackups: false));
        ResetAllDataAndBackupsCommand = new AsyncRelayCommand(() => ResetApplicationDataAsync(includeBackups: true));
        ClearSelectedTabCommand = new AsyncRelayCommand(ClearSelectedTabAsync, CanClearSelectedTab);

        UndoCommand = new RelayCommand(Undo, () => _undoRedoService.CanUndo);
        RedoCommand = new RelayCommand(Redo, () => _undoRedoService.CanRedo);

        MoveTaskToBoardCommand = new RelayCommand<DragDropTaskMoveRequest>(MoveTaskToBoard);
        SelectSearchResultCommand = new RelayCommand<SearchResultItem>(SelectSearchResult);
        SelectEksikItemCommand = new RelayCommand<EksikItemViewModel?>(SelectEksikItem);
        FocusBoardCommand = new RelayCommand<TaskBoardType>(FocusBoard);

        UrgentBoard.SelectedTaskChanged += OnBoardSelectedTaskChanged;
        GeneralBoard.SelectedTaskChanged += OnBoardSelectedTaskChanged;
        UrgentBoard.TasksChanged += OnBoardTasksChanged;
        GeneralBoard.TasksChanged += OnBoardTasksChanged;

        SearchOverlay.QueryChanged += OnSearchQueryChanged;
        SearchOverlay.ScopeChanged += OnSearchScopeChanged;
        SearchOverlay.ModeChanged += OnSearchModeChanged;
        DetailPanel.TaskChanged += (_, _) =>
        {
            MarkTaskDirty();
            InvalidateSearchCorpus();
        };

        _undoRedoService.StateChanged += (_, _) =>
        {
            UndoCommand.NotifyCanExecuteChanged();
            RedoCommand.NotifyCanExecuteChanged();
        };

        ObserveSearchSourceCollection(ActionModule.AksiyonEntries);
        ObserveSearchSourceCollection(ActionModule.AksiyonaEkleneceklerEntries);
        ObserveSearchSourceCollection(MissingProjectModule.Entries);
        ObserveSearchSourceCollection(KarotModule.Entries);
        ObserveSearchSourceCollection(TadilatModule.AktifEntries);
        ObserveSearchSourceCollection(TadilatModule.BitenEntries);
        ObserveSearchSourceCollection(YibfModule.AnaBilgiEntries);
        ObserveSearchSourceCollection(YibfModule.AnaBilgiEvents);
        ObserveSearchSourceCollection(YibfModule.IsTakibiEntries);
        ObserveModuleDirtyState(ActionModule);
        ObserveModuleDirtyState(MissingProjectModule);
        ObserveModuleDirtyState(KarotModule);
        ObserveModuleDirtyState(TadilatModule);
        ObserveModuleDirtyState(YibfModule);
    }

    public DashboardViewModel Dashboard { get; }
    public SearchOverlayViewModel SearchOverlay { get; }
    public TaskDetailViewModel DetailPanel { get; }
    public ToastHostViewModel ToastHost { get; }
    public ActionModuleViewModel ActionModule { get; }
    public MissingProjectModuleViewModel MissingProjectModule { get; }
    public KarotModuleViewModel KarotModule { get; }
    public TadilatModuleViewModel TadilatModule { get; }
    public YibfModuleViewModel YibfModule { get; }
    public TumEksiklerViewModel TumEksikler { get; }
    public TaskBoardViewModel UrgentBoard { get; }
    public TaskBoardViewModel GeneralBoard { get; }
    public ObservableRangeCollection<AcilIsOzetItemViewModel> AcilIsOzetItems { get; } = [];
    public ObservableCollection<ClearTabOption> ClearableTabs { get; }

    public MainNavigationTab SelectedMainTab
    {
        get => _selectedMainTab;
        set => SetProperty(ref _selectedMainTab, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (SetProperty(ref _isBusy, value))
            {
                ClearSelectedTabCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public ClearTabOption? SelectedClearTab
    {
        get => _selectedClearTab;
        set
        {
            if (SetProperty(ref _selectedClearTab, value))
            {
                ClearSelectedTabCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public bool HasUnsavedChanges
    {
        get => _hasUnsavedChanges;
        private set
        {
            if (SetProperty(ref _hasUnsavedChanges, value))
            {
                NotifySaveStatusChanged();
            }
        }
    }

    public bool HasAnyUnsavedChanges
        => HasUnsavedChanges || HasUnsavedSettings || ActionModule.HasUnsavedChanges || MissingProjectModule.HasUnsavedChanges || KarotModule.HasUnsavedChanges || TadilatModule.HasUnsavedChanges || YibfModule.HasUnsavedChanges;

    public DateTime? LastSuccessfulSaveAt
    {
        get => _lastSuccessfulSaveAt;
        private set
        {
            if (SetProperty(ref _lastSuccessfulSaveAt, value))
            {
                OnPropertyChanged(nameof(SaveStatusTimestampText));
            }
        }
    }

    public string SaveStatusText => HasAnyUnsavedChanges ? "Kaydedilmedi" : "Kaydedildi";

    public string SaveStatusTimestampText
        => LastSuccessfulSaveAt.HasValue
            ? $"Son kayıt: {LastSuccessfulSaveAt.Value:dd.MM.yyyy HH:mm}"
            : "Bu oturumda kayıt yapılmadı";

    public bool IsActionViewActivated
    {
        get => _isActionViewActivated;
        private set => SetProperty(ref _isActionViewActivated, value);
    }

    public bool IsTadilatViewActivated
    {
        get => _isTadilatViewActivated;
        private set => SetProperty(ref _isTadilatViewActivated, value);
    }

    public bool IsYibfAnaBilgiViewActivated
    {
        get => _isYibfAnaBilgiViewActivated;
        private set => SetProperty(ref _isYibfAnaBilgiViewActivated, value);
    }

    public bool IsYibfViewActivated
    {
        get => _isYibfViewActivated;
        private set => SetProperty(ref _isYibfViewActivated, value);
    }

    public bool IsYibfIsTakibiViewActivated
    {
        get => _isYibfIsTakibiViewActivated;
        private set => SetProperty(ref _isYibfIsTakibiViewActivated, value);
    }

    public bool IsYibfPendingViewActivated
    {
        get => _isYibfPendingViewActivated;
        private set => SetProperty(ref _isYibfPendingViewActivated, value);
    }

    public bool IsTumEksiklerViewActivated
    {
        get => _isTumEksiklerViewActivated;
        private set => SetProperty(ref _isTumEksiklerViewActivated, value);
    }

    public bool IsSettingsViewActivated
    {
        get => _isSettingsViewActivated;
        private set => SetProperty(ref _isSettingsViewActivated, value);
    }

    public bool HasUnsavedSettings
    {
        get => _hasUnsavedSettings;
        private set
        {
            if (SetProperty(ref _hasUnsavedSettings, value))
            {
                SaveSettingsCommand.NotifyCanExecuteChanged();
                NotifySaveStatusChanged();
            }
        }
    }

    public IReadOnlyList<int> AvailableAutoBackupIntervals { get; } = [5, 10, 15, 30, 60, 120];

    public bool AutoBackupEnabled
    {
        get => _settings.AutoBackupEnabled;
        set
        {
            if (_settings.AutoBackupEnabled == value)
            {
                return;
            }

            _settings.AutoBackupEnabled = value;
            OnPropertyChanged();
            HasUnsavedSettings = true;
        }
    }

    public int AutoBackupMinutes
    {
        get => _settings.AutoBackupMinutes;
        set
        {
            var normalized = Math.Max(1, value);
            if (_settings.AutoBackupMinutes == normalized)
            {
                return;
            }

            _settings.AutoBackupMinutes = normalized;
            OnPropertyChanged();
            HasUnsavedSettings = true;
        }
    }

    public bool SeedSampleDataOnEmpty
    {
        get => _settings.SeedSampleDataOnEmpty;
        set
        {
            if (_settings.SeedSampleDataOnEmpty == value)
            {
                return;
            }

            _settings.SeedSampleDataOnEmpty = value;
            OnPropertyChanged();
            HasUnsavedSettings = true;
        }
    }

    public TaskItem? SelectedTask => _activeBoard.SelectedTask;

    public RelayCommand<MainNavigationTab> SelectMainTabCommand { get; }

    public RelayCommand AddGeneralTaskCommand { get; }
    public RelayCommand AddUrgentTaskCommand { get; }
    public AsyncRelayCommand OpenQuickUrgentTaskDialogCommand { get; }
    public RelayCommand DeleteGeneralTaskCommand { get; }
    public RelayCommand DeleteUrgentTaskCommand { get; }
    public RelayCommand DeleteSelectedTaskCommand { get; }
    public RelayCommand<TaskItem?> DeleteTaskCommand { get; }
    public RelayCommand<TaskItem?> CopyTaskFromContextCommand { get; }
    public AsyncRelayCommand DeleteActiveSelectionCommand { get; }
    public RelayCommand MoveTaskUpCommand { get; }
    public RelayCommand MoveTaskDownCommand { get; }
    public RelayCommand CommitGeneralEditCommand { get; }
    public AsyncRelayCommand SaveActiveTabCommand { get; }
    public RelayCommand CopyTaskCommand { get; }
    public RelayCommand PasteTaskCommand { get; }
    public RelayCommand<TaskBoardType> PasteTaskToBoardCommand { get; }

    public RelayCommand OpenSearchCommand { get; }
    public RelayCommand CloseSearchCommand { get; }
    public RelayCommand RunContextQueryCommand { get; }
    public RelayCommand EscapeCommand { get; }

    public AsyncRelayCommand ManualBackupCommand { get; }
    public AsyncRelayCommand ImportBackupCommand { get; }
    public AsyncRelayCommand ExportExcelCommand { get; }
    public AsyncRelayCommand ImportExcelCommand { get; }
    public AsyncRelayCommand ExportPdfCommand { get; }
    public AsyncRelayCommand<UIElement?> ExportUrgentPngCommand { get; }
    public AsyncRelayCommand<UIElement?> ExportGeneralPngCommand { get; }
    public AsyncRelayCommand<UIElement?> ExportActionListPngCommand { get; }
    public AsyncRelayCommand<UIElement?> ExportMissingProjectPngCommand { get; }
    public AsyncRelayCommand<UIElement?> ExportKarotPngCommand { get; }
    public AsyncRelayCommand<UIElement?> ExportYibfIsTakibiPngCommand { get; }
    public AsyncRelayCommand RefreshTumEksiklerCommand { get; }
    public AsyncRelayCommand CleanupBuildArtifactsCommand { get; }
    public AsyncRelayCommand SaveSettingsCommand { get; }
    public AsyncRelayCommand ResetLiveDataCommand { get; }
    public AsyncRelayCommand ResetAllDataAndBackupsCommand { get; }
    public AsyncRelayCommand ClearSelectedTabCommand { get; }

    public RelayCommand UndoCommand { get; }
    public RelayCommand RedoCommand { get; }

    public RelayCommand<DragDropTaskMoveRequest> MoveTaskToBoardCommand { get; }
    public RelayCommand<SearchResultItem> SelectSearchResultCommand { get; }
    public RelayCommand<EksikItemViewModel?> SelectEksikItemCommand { get; }
    public RelayCommand<TaskBoardType> FocusBoardCommand { get; }

    public async Task InitializeAsync()
    {
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;
        IsBusy = true;
        _suppressTaskDirtyTracking = true;

        try
        {
            var allTasks = (await _taskRepository.GetAllAsync()).ToList();
            var seeded = false;
            if (allTasks.Count == 0 && _settings.SeedSampleDataOnEmpty)
            {
                allTasks = BuildSeedData();
                seeded = true;
            }

            var migratedDescriptions = MigrateTaskDescriptionsToNotes(allTasks);
            if (seeded || migratedDescriptions)
            {
                await _taskRepository.SaveManyAsync(allTasks);
            }

            UrgentBoard.ReplaceAll(allTasks.Where(task => task.BoardType == TaskBoardType.Acil));
            GeneralBoard.ReplaceAll(allTasks.Where(task => task.BoardType == TaskBoardType.Genel));

            RefreshDashboard();
            HasUnsavedChanges = false;
            InitializeLastSuccessfulSaveAtFromPersistedFiles(hasPersistedTaskData: allTasks.Count > 0 || seeded || migratedDescriptions);
            _notificationService.ShowToast("Görevler yüklendi.", ToastType.Success);

            ReconfigureAutoBackup();
            if (HasUiDispatcherContext())
            {
                StartBackgroundModuleWarmup();
            }
        }
        catch (Exception ex)
        {
            _isInitialized = false;
            _notificationService.ShowToast($"Yükleme hatası: {ex.Message}", ToastType.Error, TimeSpan.FromSeconds(5));
        }
        finally
        {
            _suppressTaskDirtyTracking = false;
            IsBusy = false;
        }
    }

    public Task<bool> SaveAllTabsAsync()
        => RunExclusiveOperationAsync(async () =>
        {
            PendingEditCommitHelper.FlushFocusedEditor();
            await EnsureAllModulesInitializedAsync();
            await CommitPendingEditsAcrossAllTabsAsync();
            var generalSaved = await PersistGeneralTasksAsync(showSuccessToast: false);
            await ActionModule.PersistAsync(showErrorToast: true);
            await MissingProjectModule.PersistAsync(showErrorToast: true);
            await KarotModule.PersistAsync(showErrorToast: true);
            await TadilatModule.PersistAsync(showErrorToast: true);
            await YibfModule.PersistAsync(showErrorToast: true);
            if (HasUnsavedSettings)
            {
                await SaveSettingsAsync(showSuccessToast: false);
            }

            var allSaved = generalSaved && !HasUnsavedSettings && !ActionModule.HasUnsavedChanges && !MissingProjectModule.HasUnsavedChanges && !KarotModule.HasUnsavedChanges && !TadilatModule.HasUnsavedChanges && !YibfModule.HasUnsavedChanges;
            if (allSaved)
            {
                await MarkGlobalSaveSucceededAsync();
            }

            return allSaved;
        });

    public Task<bool> SaveAllTabsSafelyAsync()
        => RunExclusiveOperationAsync(async () =>
        {
            var currentState = await CaptureApplicationStateSnapshotAsync();
            using var persistedFilesSnapshot = CapturePersistedFilesSnapshot();
            var saved = await SaveAllTabsAsync();
            if (saved)
            {
                return true;
            }

            if (persistedFilesSnapshot is not null && RestorePersistedFilesSnapshot(persistedFilesSnapshot))
            {
                RestoreApplicationStateSnapshot(currentState);
            }

            return false;
        });

    private Task<bool> PersistGeneralTasksSafelyAsync(bool showSuccessToast)
        => RunExclusiveOperationAsync(async () =>
        {
            var currentState = await CaptureApplicationStateSnapshotAsync();
            using var persistedFilesSnapshot = CapturePersistedFilesSnapshot();
            var saved = await PersistGeneralTasksAsync(showSuccessToast);
            if (saved)
            {
                return true;
            }

            if (persistedFilesSnapshot is not null && RestorePersistedFilesSnapshot(persistedFilesSnapshot))
            {
                RestoreApplicationStateSnapshot(currentState);
            }

            return false;
        });

    private void SelectMainTab(MainNavigationTab tab)
    {
        MarkTabViewActivated(tab);
        SelectedMainTab = tab;
        if (!HasUiDispatcherContext())
        {
            ActivateTabAsync(tab).GetAwaiter().GetResult();
            return;
        }

        RunSafeBackgroundTask(ActivateTabAsync(tab), $"'{tab}' sekmesi yüklenemedi.");
    }

    private static bool HasUiDispatcherContext()
        => Application.Current is not null && SynchronizationContext.Current is DispatcherSynchronizationContext;

    private void ObserveSearchSourceCollection<T>(ObservableCollection<T> collection)
        where T : INotifyPropertyChanged
    {
        collection.CollectionChanged += OnSearchSourceCollectionChanged;
        foreach (var item in collection)
        {
            item.PropertyChanged += OnSearchSourceItemPropertyChanged;
        }
    }

    private void OnSearchSourceCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (INotifyPropertyChanged item in e.OldItems)
            {
                item.PropertyChanged -= OnSearchSourceItemPropertyChanged;
            }
        }

        if (e.NewItems is not null)
        {
            foreach (INotifyPropertyChanged item in e.NewItems)
            {
                item.PropertyChanged -= OnSearchSourceItemPropertyChanged;
                item.PropertyChanged += OnSearchSourceItemPropertyChanged;
            }
        }

        if (e.Action == NotifyCollectionChangedAction.Reset && sender is System.Collections.IEnumerable enumerable)
        {
            foreach (var item in enumerable.OfType<INotifyPropertyChanged>())
            {
                item.PropertyChanged -= OnSearchSourceItemPropertyChanged;
                item.PropertyChanged += OnSearchSourceItemPropertyChanged;
            }
        }

        InvalidateSearchCorpus();
    }

    private void OnSearchSourceItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        InvalidateSearchCorpus();
    }

    private void InvalidateSearchCorpus()
    {
        _searchCorpusCache = null;
    }

    private IReadOnlyList<SearchResultItem> GetSearchCorpus()
        => _searchCorpusCache ??= BuildSearchCorpusCore();

    private void MarkTabViewActivated(MainNavigationTab tab)
    {
        switch (tab)
        {
            case MainNavigationTab.Aksiyon:
                IsActionViewActivated = true;
                break;
            case MainNavigationTab.TadilatTakibi:
                IsTadilatViewActivated = true;
                break;
            case MainNavigationTab.YibfAnaBilgi:
                IsYibfViewActivated = true;
                IsYibfAnaBilgiViewActivated = true;
                break;
            case MainNavigationTab.YibfIsTakibi:
                IsYibfViewActivated = true;
                IsYibfIsTakibiViewActivated = true;
                break;
            case MainNavigationTab.YibfBekleyenIsler:
                IsYibfPendingViewActivated = true;
                break;
            case MainNavigationTab.TumEksikler:
                IsTumEksiklerViewActivated = true;
                break;
            case MainNavigationTab.Ayarlar:
                IsSettingsViewActivated = true;
                break;
        }
    }

    private async Task AutoBackupAsync()
        => await RunExclusiveOperationAsync(async () =>
        {
            try
            {
                await EnsureAllModulesInitializedAsync();
                await _backupService.CreateBackupAsync(
                    AllTasks(),
                    actionEntries: ActionModule.GetAllEntriesSnapshot(),
                    missingProjectEntries: MissingProjectModule.GetEntriesSnapshot(),
                    missingProjectCellStates: MissingProjectModule.GetCellStatesSnapshot(),
                    karotEntries: KarotModule.GetEntriesSnapshot(),
                    karotCellStates: KarotModule.GetCellStatesSnapshot(),
                    tadilatEntries: TadilatModule.GetEntriesSnapshot(),
                    yibfAnaBilgiEntries: YibfModule.GetAnaBilgiEntriesSnapshot(),
                    yibfAnaBilgiEvents: YibfModule.GetAnaBilgiEventsSnapshot(),
                    yibfIsTakibiEntries: YibfModule.GetIsTakibiEntriesSnapshot(),
                    yibfCellStates: YibfModule.GetCellStatesSnapshot(),
                    tadilatCellStates: TadilatModule.GetCellStatesSnapshot(),
                    quickTaskTemplates: _quickTaskTemplateRepository?.GetAll());
                
                await _backupService.CleanOldBackupsAsync(30);
                
                _notificationService.ShowToast("Otomatik yedek oluşturuldu.", ToastType.Info);
            }
            catch (Exception ex)
            {
                _notificationService.ShowToast($"Otomatik yedek hatası: {ex.Message}", ToastType.Warning);
            }
        });

    private IEnumerable<TaskItem> AllTasks() => UrgentBoard.Tasks.Concat(GeneralBoard.Tasks);

    private BackupRestoreData CaptureBackupRestoreData()
        => new()
        {
            Tasks = AllTasks().Select(task => task.Clone()).ToList(),
            QuickTaskTemplates = _quickTaskTemplateRepository?.GetAll().Select(template => template.Clone()).ToList() ?? [],
            ActionEntries = ActionModule.GetAllEntriesSnapshot(),
            MissingProjectEntries = MissingProjectModule.GetEntriesSnapshot(),
            MissingProjectCellStates = MissingProjectModule.GetCellStatesSnapshot(),
            KarotEntries = KarotModule.GetEntriesSnapshot(),
            KarotCellStates = KarotModule.GetCellStatesSnapshot(),
            TadilatEntries = TadilatModule.GetEntriesSnapshot(),
            TadilatCellStates = TadilatModule.GetCellStatesSnapshot(),
            YibfAnaBilgiEntries = YibfModule.GetAnaBilgiEntriesSnapshot(),
            YibfAnaBilgiEvents = YibfModule.GetAnaBilgiEventsSnapshot(),
            YibfIsTakibiEntries = YibfModule.GetIsTakibiEntriesSnapshot(),
            YibfCellStates = YibfModule.GetCellStatesSnapshot()
        };

    private ApplicationStateSnapshot CaptureApplicationStateSnapshot()
        => new(
            CaptureBackupRestoreData(),
            CloneSettings(),
            HasUnsavedChanges,
            HasUnsavedSettings,
            ActionModule.HasUnsavedChanges,
            MissingProjectModule.HasUnsavedChanges,
            KarotModule.HasUnsavedChanges,
            TadilatModule.HasUnsavedChanges,
            YibfModule.HasUnsavedChanges);

    private async Task<ApplicationStateSnapshot> CaptureApplicationStateSnapshotAsync()
    {
        await EnsureAllModulesInitializedAsync();
        return CaptureApplicationStateSnapshot();
    }

    private AppSettings CloneSettings()
        => new()
        {
            AutoBackupEnabled = _settings.AutoBackupEnabled,
            AutoBackupMinutes = _settings.AutoBackupMinutes,
            SeedSampleDataOnEmpty = _settings.SeedSampleDataOnEmpty
        };

    private async Task<T> RunExclusiveOperationAsync<T>(Func<Task<T>> operation)
    {
        if (_operationGateDepth.Value > 0)
        {
            return await operation();
        }

        await _operationGate.WaitAsync();
        _operationGateDepth.Value++;
        try
        {
            return await operation();
        }
        finally
        {
            _operationGateDepth.Value--;
            _operationGate.Release();
        }
    }

    private Task RunExclusiveOperationAsync(Func<Task> operation)
        => RunExclusiveOperationAsync(async () =>
        {
            await operation();
            return true;
        });

    private void ApplyBackupRestoreData(BackupRestoreData restored, bool markModulesDirty = true)
    {
        _suppressTaskDirtyTracking = true;
        try
        {
            var taskSnapshot = restored.Tasks.Select(task => task.Clone()).ToList();
            _ = MigrateTaskDescriptionsToNotes(taskSnapshot);
            UrgentBoard.ReplaceAll(taskSnapshot.Where(task => task.BoardType == TaskBoardType.Acil));
            GeneralBoard.ReplaceAll(taskSnapshot.Where(task => task.BoardType == TaskBoardType.Genel));
            UrgentBoard.SelectedTask = null;
            GeneralBoard.SelectedTask = null;
            DetailPanel.Close();
            SearchOverlay.Close();
        }
        finally
        {
            _suppressTaskDirtyTracking = false;
        }

        ActionModule.LoadFromBackup(restored.ActionEntries, markModulesDirty);
        MissingProjectModule.LoadFromBackup(restored.MissingProjectEntries, restored.MissingProjectCellStates, markModulesDirty);
        KarotModule.LoadFromBackup(restored.KarotEntries, restored.KarotCellStates, markModulesDirty);
        TadilatModule.LoadFromBackup(restored.TadilatEntries, restored.TadilatCellStates, markModulesDirty);
        YibfModule.LoadFromBackup(restored.YibfAnaBilgiEntries, restored.YibfAnaBilgiEvents, restored.YibfIsTakibiEntries, restored.YibfCellStates, markModulesDirty);
        _quickTaskTemplateRepository?.ReplaceAll(restored.QuickTaskTemplates.Select(template => template.Clone()));
        MarkAllModulesStateLoadedFromSnapshot();
        InvalidateSearchCorpus();
        RefreshAcilIsOzet();
        RefreshDashboard();
        NotifySelectionCommands();
    }

    private void RestoreApplicationStateSnapshot(ApplicationStateSnapshot snapshot)
    {
        _suppressTaskDirtyTracking = true;
        try
        {
            var taskSnapshot = snapshot.Data.Tasks.Select(task => task.Clone()).ToList();
            _ = MigrateTaskDescriptionsToNotes(taskSnapshot);
            UrgentBoard.ReplaceAll(taskSnapshot.Where(task => task.BoardType == TaskBoardType.Acil));
            GeneralBoard.ReplaceAll(taskSnapshot.Where(task => task.BoardType == TaskBoardType.Genel));
            UrgentBoard.SelectedTask = null;
            GeneralBoard.SelectedTask = null;
            DetailPanel.Close();
            SearchOverlay.Close();
        }
        finally
        {
            _suppressTaskDirtyTracking = false;
        }

        ActionModule.LoadFromBackup(snapshot.Data.ActionEntries, snapshot.HasUnsavedActionChanges);
        MissingProjectModule.LoadFromBackup(snapshot.Data.MissingProjectEntries, snapshot.Data.MissingProjectCellStates, snapshot.HasUnsavedMissingProjectChanges);
        KarotModule.LoadFromBackup(snapshot.Data.KarotEntries, snapshot.Data.KarotCellStates, snapshot.HasUnsavedKarotChanges);
        TadilatModule.LoadFromBackup(snapshot.Data.TadilatEntries, snapshot.Data.TadilatCellStates, snapshot.HasUnsavedTadilatChanges);
        YibfModule.LoadFromBackup(snapshot.Data.YibfAnaBilgiEntries, snapshot.Data.YibfAnaBilgiEvents, snapshot.Data.YibfIsTakibiEntries, snapshot.Data.YibfCellStates, snapshot.HasUnsavedYibfChanges);
        _quickTaskTemplateRepository?.ReplaceAll(snapshot.Data.QuickTaskTemplates.Select(template => template.Clone()));
        MarkAllModulesStateLoadedFromSnapshot();
        InvalidateSearchCorpus();
        RefreshAcilIsOzet();
        RefreshDashboard();
        NotifySelectionCommands();

        _settings.AutoBackupEnabled = snapshot.Settings.AutoBackupEnabled;
        _settings.AutoBackupMinutes = snapshot.Settings.AutoBackupMinutes;
        _settings.SeedSampleDataOnEmpty = snapshot.Settings.SeedSampleDataOnEmpty;
        OnPropertyChanged(nameof(AutoBackupEnabled));
        OnPropertyChanged(nameof(AutoBackupMinutes));
        OnPropertyChanged(nameof(SeedSampleDataOnEmpty));

        HasUnsavedChanges = snapshot.HasUnsavedTaskChanges;
        HasUnsavedSettings = snapshot.HasUnsavedSettings;
    }

    private async Task<PersistWithRollbackResult> PersistWithRollbackAsync(ApplicationStateSnapshot previousState, string operationLabel)
    {
        using var persistedFilesSnapshot = CapturePersistedFilesSnapshot();
        if (await SaveAllTabsAsync())
        {
            return PersistWithRollbackResult.Succeeded;
        }

        if (persistedFilesSnapshot is not null && RestorePersistedFilesSnapshot(persistedFilesSnapshot))
        {
            RestoreApplicationStateSnapshot(previousState);
            _notificationService.ShowToast($"{operationLabel} kaydedilemedi; önceki veriler geri yüklendi.", ToastType.Warning, TimeSpan.FromSeconds(5));
            return PersistWithRollbackResult.RolledBack;
        }

        _notificationService.ShowToast($"{operationLabel} sırasında hata oluştu ve önceki veriler kalıcı olarak geri yazılamadı.", ToastType.Error, TimeSpan.FromSeconds(5));
        return PersistWithRollbackResult.RollbackFailed;
    }

    private PersistedFilesSnapshot? CapturePersistedFilesSnapshot()
    {
        if (_taskRepository is not SqliteTaskRepository taskRepository)
        {
            return null;
        }

        var databasePath = taskRepository.DatabasePath;
        if (string.IsNullOrWhiteSpace(databasePath))
        {
            return null;
        }

        var snapshotRoot = Path.Combine(Path.GetTempPath(), "RizaCanKilicIsTakibiSnapshots", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(snapshotRoot);

        var databaseSnapshotPath = Path.Combine(snapshotRoot, Path.GetFileName(databasePath));
        var walPath = $"{databasePath}-wal";
        var walSnapshotPath = Path.Combine(snapshotRoot, $"{Path.GetFileName(databasePath)}-wal");
        var shmPath = $"{databasePath}-shm";
        var shmSnapshotPath = Path.Combine(snapshotRoot, $"{Path.GetFileName(databasePath)}-shm");
        var settingsPath = (_appSettingsService as AppSettingsService)?.SettingsPath;
        var settingsSnapshotPath = string.IsNullOrWhiteSpace(settingsPath) ? null : Path.Combine(snapshotRoot, Path.GetFileName(settingsPath));

        SqliteConnection.ClearAllPools();
        SqliteConnectionSettings.TruncateWal(SqliteConnectionSettings.BuildConnectionString(databasePath));
        SqliteConnection.ClearAllPools();

        File.Copy(databasePath, databaseSnapshotPath, overwrite: true);

        if (File.Exists(walPath))
        {
            File.Copy(walPath, walSnapshotPath, overwrite: true);
        }

        if (File.Exists(shmPath))
        {
            File.Copy(shmPath, shmSnapshotPath, overwrite: true);
        }

        if (!string.IsNullOrWhiteSpace(settingsPath) && File.Exists(settingsPath))
        {
            File.Copy(settingsPath, settingsSnapshotPath!, overwrite: true);
        }

        return new PersistedFilesSnapshot(
            snapshotRoot,
            databasePath,
            databaseSnapshotPath,
            walPath,
            walSnapshotPath,
            shmPath,
            shmSnapshotPath,
            settingsPath,
            settingsSnapshotPath);
    }

    private static bool RestorePersistedFilesSnapshot(PersistedFilesSnapshot snapshot)
    {
        try
        {
            SqliteConnection.ClearAllPools();
            File.Copy(snapshot.DatabaseSnapshotPath, snapshot.DatabasePath, overwrite: true);

            RestoreOptionalSnapshot(snapshot.WalSnapshotPath, snapshot.WalPath);
            RestoreOptionalSnapshot(snapshot.ShmSnapshotPath, snapshot.ShmPath);

            if (!string.IsNullOrWhiteSpace(snapshot.SettingsPath))
            {
                RestoreOptionalSnapshot(snapshot.SettingsSnapshotPath, snapshot.SettingsPath);
            }

            SqliteConnection.ClearAllPools();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void RestoreOptionalSnapshot(string? snapshotPath, string targetPath)
    {
        if (!string.IsNullOrWhiteSpace(snapshotPath) && File.Exists(snapshotPath))
        {
            File.Copy(snapshotPath, targetPath, overwrite: true);
            return;
        }

        if (File.Exists(targetPath))
        {
            File.Delete(targetPath);
        }
    }

    private void InitializeLastSuccessfulSaveAtFromPersistedFiles(bool hasPersistedTaskData)
    {
        var persistedMetadataTime = _lastSaveMetadataService.LoadLastSuccessfulSaveAt();
        if (persistedMetadataTime.HasValue)
        {
            LastSuccessfulSaveAt = persistedMetadataTime;
            return;
        }

        DateTime? latestPersistedWriteTime = null;

        if (hasPersistedTaskData && _taskRepository is SqliteTaskRepository taskRepository)
        {
            latestPersistedWriteTime = GetLatestExistingFileWriteTime(taskRepository.DatabasePath);
        }

        if (_appSettingsService is AppSettingsService appSettingsService)
        {
            var settingsWriteTime = GetLatestExistingFileWriteTime(appSettingsService.SettingsPath);
            if (settingsWriteTime.HasValue && (!latestPersistedWriteTime.HasValue || settingsWriteTime.Value > latestPersistedWriteTime.Value))
            {
                latestPersistedWriteTime = settingsWriteTime;
            }
        }

        LastSuccessfulSaveAt = latestPersistedWriteTime;
    }

    private static DateTime? GetLatestExistingFileWriteTime(params string?[] paths)
    {
        DateTime? latestWriteTime = null;

        foreach (var path in paths)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                continue;
            }

            var writeTime = File.GetLastWriteTime(path);
            if (!latestWriteTime.HasValue || writeTime > latestWriteTime.Value)
            {
                latestWriteTime = writeTime;
            }
        }

        return latestWriteTime;
    }

    private void MarkAllModulesStateLoadedFromSnapshot()
    {
        _actionModuleInitializationTask = Task.CompletedTask;
        _missingProjectModuleInitializationTask = Task.CompletedTask;
        _karotModuleInitializationTask = Task.CompletedTask;
        _tadilatModuleInitializationTask = Task.CompletedTask;
        _yibfModuleInitializationTask = Task.CompletedTask;
        _allModulesInitializationTask = Task.CompletedTask;
    }

    private void MarkModuleStateLoadedFromSnapshot(MainNavigationTab tab)
    {
        switch (tab)
        {
            case MainNavigationTab.Aksiyon:
                _actionModuleInitializationTask = Task.CompletedTask;
                break;
            case MainNavigationTab.EksikProje:
                _missingProjectModuleInitializationTask = Task.CompletedTask;
                break;
            case MainNavigationTab.KarotTakibi:
                _karotModuleInitializationTask = Task.CompletedTask;
                break;
            case MainNavigationTab.TadilatTakibi:
                _tadilatModuleInitializationTask = Task.CompletedTask;
                break;
            case MainNavigationTab.YibfAnaBilgi:
            case MainNavigationTab.YibfIsTakibi:
            case MainNavigationTab.YibfBekleyenIsler:
                _yibfModuleInitializationTask = Task.CompletedTask;
                break;
            default:
                break;
        }

        if (_actionModuleInitializationTask is not null
            && _missingProjectModuleInitializationTask is not null
            && _karotModuleInitializationTask is not null
            && _tadilatModuleInitializationTask is not null
            && _yibfModuleInitializationTask is not null)
        {
            _allModulesInitializationTask = Task.CompletedTask;
        }
    }

    private static bool MigrateTaskDescriptionsToNotes(IEnumerable<TaskItem> tasks)
    {
        var updated = false;

        foreach (var task in tasks)
        {
            if (string.IsNullOrWhiteSpace(task.Description))
            {
                continue;
            }

            var description = task.Description.Trim();
            if (string.IsNullOrWhiteSpace(description))
            {
                task.Description = string.Empty;
                updated = true;
                continue;
            }

            var hasSameNote = task.Notes.Any(note => string.Equals(note.Text?.Trim(), description, StringComparison.OrdinalIgnoreCase));
            if (!hasSameNote)
            {
                task.Notes.Add(new TaskNote
                {
                    Text = description,
                    CreatedAt = DateTime.Now
                });
            }

            task.Description = string.Empty;
            task.UpdatedAt = DateTime.Now;
            updated = true;
        }

        return updated;
    }

    private void RefreshAcilIsOzet()
    {
        var items = new List<AcilIsOzetItemViewModel>();
        AppendGenelAcilIsOzetItems(items);
        AppendEksikProjeAcilIsOzetItems(items);
        AppendKarotAcilIsOzetItems(items);
        AppendTadilatAcilIsOzetItems(items);
        AppendYibfIsTakibiAcilIsOzetItems(items);

        var orderedItems = items
            .OrderBy(item => item.PriorityRank)
            .ThenBy(item => GetAcilIsCategoryOrder(item.Category))
            .ThenByDescending(item => item.SourceUpdatedAt)
            .ThenBy(item => item.Summary, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        AcilIsOzetItems.ReplaceRange(orderedItems);
    }

    private async Task ActivateTabAsync(MainNavigationTab tab)
    {
        await EnsureModulesForTabAsync(tab);

        if (tab == MainNavigationTab.YibfBekleyenIsler)
        {
            RefreshAcilIsOzet();
        }
        else if (tab == MainNavigationTab.TumEksikler)
        {
            RefreshTumEksikler();
        }
    }

    private Task EnsureActionModuleInitializedAsync()
        => EnsureModuleInitializedAsync(ref _actionModuleInitializationTask, ActionModule.InitializeAsync);

    private Task EnsureMissingProjectModuleInitializedAsync()
        => EnsureModuleInitializedAsync(ref _missingProjectModuleInitializationTask, MissingProjectModule.InitializeAsync);

    private Task EnsureKarotModuleInitializedAsync()
        => EnsureModuleInitializedAsync(ref _karotModuleInitializationTask, KarotModule.InitializeAsync);

    private Task EnsureTadilatModuleInitializedAsync()
        => EnsureModuleInitializedAsync(ref _tadilatModuleInitializationTask, TadilatModule.InitializeAsync);

    private Task EnsureYibfModuleInitializedAsync()
        => EnsureModuleInitializedAsync(ref _yibfModuleInitializationTask, YibfModule.InitializeAsync);

    private async Task EnsureModulesForTabAsync(MainNavigationTab tab)
    {
        switch (tab)
        {
            case MainNavigationTab.Aksiyon:
                await EnsureActionModuleInitializedAsync();
                break;
            case MainNavigationTab.EksikProje:
                await EnsureMissingProjectModuleInitializedAsync();
                break;
            case MainNavigationTab.KarotTakibi:
                await EnsureKarotModuleInitializedAsync();
                break;
            case MainNavigationTab.TadilatTakibi:
                await EnsureTadilatModuleInitializedAsync();
                break;
            case MainNavigationTab.YibfAnaBilgi:
            case MainNavigationTab.YibfIsTakibi:
                await EnsureYibfModuleInitializedAsync();
                break;
            case MainNavigationTab.YibfBekleyenIsler:
            case MainNavigationTab.TumEksikler:
                await EnsureAllModulesInitializedAsync();
                break;
        }
    }

    private async Task RefreshTumEksiklerAsync()
    {
        await EnsureAllModulesInitializedAsync();
        RefreshTumEksikler();
    }

    private void RefreshTumEksikler()
    {
        TumEksikler.RefreshFrom(
            YibfModule.AnaBilgiEntries,
            YibfModule.AnaBilgiEvents,
            YibfModule.IsTakibiEntries,
            YibfModule.CellStates,
            TadilatModule.AktifEntries,
            TadilatModule.CellStates,
            MissingProjectModule.Entries,
            KarotModule.Entries);
    }

    private Task EnsureAllModulesInitializedAsync()
    {
        if (_allModulesInitializationTask is null || _allModulesInitializationTask.IsCanceled || _allModulesInitializationTask.IsFaulted)
        {
            _allModulesInitializationTask = EnsureAllModulesInitializedCoreAsync();
        }

        return _allModulesInitializationTask;
    }

    private async Task EnsureAllModulesInitializedCoreAsync()
    {
        await EnsureActionModuleInitializedAsync();
        await EnsureMissingProjectModuleInitializedAsync();
        await EnsureKarotModuleInitializedAsync();
        await EnsureTadilatModuleInitializedAsync();
        await EnsureYibfModuleInitializedAsync();
    }

    private static Task EnsureModuleInitializedAsync(ref Task? initializationTask, Func<Task> initializeAsync)
    {
        if (initializationTask is null || initializationTask.IsCanceled || initializationTask.IsFaulted)
        {
            initializationTask = initializeAsync();
        }

        return initializationTask;
    }

    private void StartBackgroundModuleWarmup()
    {
        if (_backgroundWarmupTask is null || _backgroundWarmupTask.IsCanceled || _backgroundWarmupTask.IsFaulted || _backgroundWarmupTask.IsCompleted)
        {
            _backgroundWarmupTask = WarmModulesInBackgroundAsync();
        }
    }

    private void RunSafeBackgroundTask(Task task, string errorMessage)
    {
        _ = ObserveBackgroundTaskAsync(task, errorMessage);
    }

    private async Task ObserveBackgroundTaskAsync(Task task, string errorMessage)
    {
        try
        {
            await task;
        }
        catch (Exception ex)
        {
            _notificationService.ShowToast($"{errorMessage} {ex.Message}", ToastType.Error, TimeSpan.FromSeconds(5));
        }
    }

    private async Task WarmModulesInBackgroundAsync()
    {
        try
        {
            await Task.Delay(150);
            await EnsureActionModuleInitializedAsync();
            await Task.Delay(50);
            await EnsureMissingProjectModuleInitializedAsync();
            await Task.Delay(50);
            await EnsureKarotModuleInitializedAsync();
            await Task.Delay(50);
            await EnsureTadilatModuleInitializedAsync();
            await Task.Delay(50);
            await EnsureYibfModuleInitializedAsync();
            RefreshAcilIsOzet();
        }
        catch
        {
            // Background warmup is opportunistic; on-demand loading remains the source of truth.
        }
    }

    private void AppendGenelAcilIsOzetItems(ICollection<AcilIsOzetItemViewModel> target)
    {
        foreach (var task in UrgentBoard.Tasks)
        {
            var title = FirstNonEmpty(task.Title, "(Başlıksız görev)");
            var description = task.Description?.Trim();
            var summary = string.IsNullOrWhiteSpace(description) ? title : $"{title} - {description}";
            target.Add(new AcilIsOzetItemViewModel
            {
                Category = CategoryGenel,
                PriorityLabel = AcilLabel,
                PriorityRank = 0,
                Summary = summary,
                SourceUpdatedAt = task.UpdatedAt == default ? task.CreatedAt : task.UpdatedAt
            });
        }
    }

    private void AppendEksikProjeAcilIsOzetItems(ICollection<AcilIsOzetItemViewModel> target)
    {
        foreach (var entry in MissingProjectModule.Entries)
        {
            var priorityRank = 1;
            var priorityLabel = DikkatLabel;
            var ownerParcel = BuildOwnerParcelSummary(entry.AdaParsel, entry.YapiSahibi);
            var missingProjectText = FirstNonEmpty(entry.MissingProjectText, entry.Description, "-");
            var summary = string.IsNullOrWhiteSpace(ownerParcel)
                ? missingProjectText
                : $"{ownerParcel} - {missingProjectText}";

            target.Add(new AcilIsOzetItemViewModel
            {
                Category = CategoryEksikProje,
                PriorityLabel = priorityLabel,
                PriorityRank = priorityRank,
                Summary = summary,
                SourceUpdatedAt = entry.UpdatedAt == default ? entry.CreatedAt : entry.UpdatedAt
            });
        }
    }

    private void AppendKarotAcilIsOzetItems(ICollection<AcilIsOzetItemViewModel> target)
    {
        foreach (var entry in KarotModule.Entries)
        {
            string priorityLabel;
            var priorityRank = 1;
            switch (entry.Status)
            {
                case KarotStatus.KarotAlinacak:
                case KarotStatus.KarotAlindiSonucBekleniyor:
                    priorityLabel = DikkatLabel;
                    break;
                case KarotStatus.KarotAlindiOlumsuz:
                    priorityLabel = AcilLabel;
                    priorityRank = 0;
                    break;
                default:
                    continue;
            }

            var ownerParcel = BuildOwnerParcelSummary(entry.AdaParsel, entry.YapiSahibi);
            var floorInfo = FirstNonEmpty(entry.KatBilgisi, "-");
            var summary = string.IsNullOrWhiteSpace(ownerParcel)
                ? floorInfo
                : $"{ownerParcel} - {floorInfo}";

            target.Add(new AcilIsOzetItemViewModel
            {
                Category = CategoryKarot,
                PriorityLabel = priorityLabel,
                PriorityRank = priorityRank,
                Summary = summary,
                SourceUpdatedAt = entry.UpdatedAt == default ? entry.CreatedAt : entry.UpdatedAt
            });
        }
    }

    private void AppendTadilatAcilIsOzetItems(ICollection<AcilIsOzetItemViewModel> target)
    {
        var activeEntriesById = TadilatModule.AktifEntries.ToDictionary(entry => entry.Id);
        var groupedStates = TadilatModule.CellStates
            .Where(state => IsPendingSummaryColor(state.BackgroundColor))
            .GroupBy(state => state.EntryId);

        foreach (var stateGroup in groupedStates)
        {
            if (!activeEntriesById.TryGetValue(stateGroup.Key, out var entry))
            {
                continue;
            }

            var reasons = stateGroup
                .Select(state => BuildTadilatSummaryReason(state.ColumnKey))
                .Where(reason => !string.IsNullOrWhiteSpace(reason))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (reasons.Count == 0)
            {
                continue;
            }

            var priorityRank = stateGroup.Any(state => IsRedSummaryColor(state.BackgroundColor)) ? 0 : 1;
            var priorityLabel = priorityRank == 0 ? AcilLabel : DikkatLabel;
            var prefix = FirstNonEmpty(entry.JobName, "(İsimsiz iş)");
            if (!string.IsNullOrWhiteSpace(entry.ProjectType))
            {
                prefix = $"{prefix} {entry.ProjectType.Trim()}";
            }

            var reasonText = string.Join(" VE ", reasons);
            var summary = string.IsNullOrWhiteSpace(prefix) ? reasonText : $"{prefix} {reasonText}";
            target.Add(new AcilIsOzetItemViewModel
            {
                Category = CategoryTadilat,
                PriorityLabel = priorityLabel,
                PriorityRank = priorityRank,
                Summary = summary,
                SourceUpdatedAt = entry.UpdatedAt == default ? entry.CreatedAt : entry.UpdatedAt
            });
        }
    }

    private void AppendYibfIsTakibiAcilIsOzetItems(ICollection<AcilIsOzetItemViewModel> target)
    {
        var entriesById = YibfModule.IsTakibiEntries.ToDictionary(entry => entry.Id);
        var groupedStates = YibfModule.CellStates
            .Where(state => IsPendingSummaryColor(state.BackgroundColor))
            .GroupBy(state => state.EntryId);

        foreach (var stateGroup in groupedStates)
        {
            if (!entriesById.TryGetValue(stateGroup.Key, out var entry))
            {
                continue;
            }

            var orderedStates = stateGroup
                .OrderBy(state => GetYibfIsTakibiColumnOrder(state.ColumnKey))
                .ThenBy(state => state.ColumnKey, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var reasons = new List<string>();
            var reasonLookup = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var state in orderedStates)
            {
                var reason = BuildYibfIsTakibiSummaryReason(state.ColumnKey);
                if (string.IsNullOrWhiteSpace(reason) || !reasonLookup.Add(reason))
                {
                    continue;
                }

                reasons.Add(reason);
            }

            if (reasons.Count == 0)
            {
                continue;
            }

            var priorityRank = stateGroup.Any(state => IsRedSummaryColor(state.BackgroundColor)) ? 0 : 1;
            var priorityLabel = priorityRank == 0 ? AcilLabel : DikkatLabel;
            var jobName = FirstNonEmpty(entry.JobName, "(İsimsiz iş)");
            var summary = $"{jobName} - {string.Join(" VE ", reasons)}";

            target.Add(new AcilIsOzetItemViewModel
            {
                Category = CategoryYibfIsTakibi,
                PriorityLabel = priorityLabel,
                PriorityRank = priorityRank,
                Summary = summary,
                SourceUpdatedAt = entry.UpdatedAt == default ? entry.CreatedAt : entry.UpdatedAt
            });
        }
    }

    private static int GetAcilIsCategoryOrder(string category)
        => category switch
        {
            CategoryGenel => 0,
            CategoryEksikProje => 1,
            CategoryKarot => 2,
            CategoryTadilat => 3,
            CategoryYibfIsTakibi => 4,
            _ => int.MaxValue
        };

    private static string BuildOwnerParcelSummary(string? adaParsel, string? yapiSahibi)
    {
        var parcel = adaParsel?.Trim();
        var owner = yapiSahibi?.Trim();
        if (!string.IsNullOrWhiteSpace(parcel) && !string.IsNullOrWhiteSpace(owner))
        {
            return $"{parcel} + {owner}";
        }

        return FirstNonEmpty(parcel, owner);
    }

    private static string BuildTadilatSummaryReason(string? columnKey)
    {
        return columnKey switch
        {
            TadilatColumnKeys.DigitalReceived => "DİJİTAL GELMEDİ",
            TadilatColumnKeys.InspectorApproved => "DENETÇİ ONAYLAMADI",
            TadilatColumnKeys.OutputAndReportArrived => "ÇIKTI/RAPOR GELMEDİ",
            TadilatColumnKeys.OfficialLetterSubmitted => "ÜST YAZI TESLİM EDİLMEDİ",
            TadilatColumnKeys.ArchivedFromMunicipality => "PROJELER ARŞİVE EKLENMEDİ",
            _ => BuildFallbackSummaryReason(columnKey)
        };
    }

    private static string BuildYibfIsTakibiSummaryReason(string? columnKey)
    {
        return columnKey switch
        {
            YibfIsTakibiColumnKeys.MuellifBilgileriGeldiMi => "MÜELLİF BİLGİLERİ GELMEDİ",
            YibfIsTakibiColumnKeys.DenetciAtamalariYapildiMi => "DENETÇİ ATAMALARI YAPILMADI",
            YibfIsTakibiColumnKeys.TumProjelerinDijitaliVarMi => "TÜM PROJELERİN DİJİTALİ YOK",
            YibfIsTakibiColumnKeys.EvraklarTamMi => "EVRAKLAR TAM DEĞİL",
            YibfIsTakibiColumnKeys.YibfSozlesmeHazirlandiMi => "YİBF SÖZLEŞME/TAAHHÜTNAME HAZIR DEĞİL",
            YibfIsTakibiColumnKeys.DekontAlindiMi => "DEKONT ALINMADI",
            YibfIsTakibiColumnKeys.RuhsatBasvurusuYapildiMi => "RUHSAT BAŞVURUSU YAPILMADI",
            YibfIsTakibiColumnKeys.RuhsatNushasiAlindiMi => "RUHSAT NÜSHASI ALINMADI",
            YibfIsTakibiColumnKeys.IsyeriTeslimTutangiHazirlandiMi => "İŞYERİ TESLİM TUTANAĞI HAZIRLANMADI",
            YibfIsTakibiColumnKeys.IsgYazisiHazirlandiMi => "İSG YAZISI HAZIRLANMADI",
            YibfIsTakibiColumnKeys.SaglikGuvenlikPlaniGeldiMi => "SAĞLIK GÜVENLİK PLANI GELMEDİ",
            YibfIsTakibiColumnKeys.TemelTopraklamaTutanagiHazirlandiMi => "TEMEL TOPRAKLAMA TUTANAĞI HAZIRLANMADI",
            YibfIsTakibiColumnKeys.JobName => "İŞİN İSMİ İŞARETLİ",
            _ => BuildFallbackSummaryReason(columnKey)
        };
    }

    private static int GetYibfIsTakibiColumnOrder(string? columnKey)
    {
        return columnKey switch
        {
            YibfIsTakibiColumnKeys.JobName => 0,
            YibfIsTakibiColumnKeys.MuellifBilgileriGeldiMi => 1,
            YibfIsTakibiColumnKeys.DenetciAtamalariYapildiMi => 2,
            YibfIsTakibiColumnKeys.TumProjelerinDijitaliVarMi => 3,
            YibfIsTakibiColumnKeys.EvraklarTamMi => 4,
            YibfIsTakibiColumnKeys.YibfSozlesmeHazirlandiMi => 5,
            YibfIsTakibiColumnKeys.DekontAlindiMi => 6,
            YibfIsTakibiColumnKeys.RuhsatBasvurusuYapildiMi => 7,
            YibfIsTakibiColumnKeys.RuhsatNushasiAlindiMi => 8,
            YibfIsTakibiColumnKeys.IsyeriTeslimTutangiHazirlandiMi => 9,
            YibfIsTakibiColumnKeys.IsgYazisiHazirlandiMi => 10,
            YibfIsTakibiColumnKeys.SaglikGuvenlikPlaniGeldiMi => 11,
            YibfIsTakibiColumnKeys.TemelTopraklamaTutanagiHazirlandiMi => 12,
            _ => int.MaxValue
        };
    }

    private static string BuildFallbackSummaryReason(string? columnKey)
    {
        var normalized = columnKey?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return "İŞARETLİ SÜTUN";
        }

        var readable = SplitPascalCase(normalized).ToUpper(TurkishCulture);
        return $"{readable} İŞARETLİ";
    }

    private static string SplitPascalCase(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(value.Length + 8);
        for (var index = 0; index < value.Length; index++)
        {
            var current = value[index];
            if (index > 0)
            {
                var previous = value[index - 1];
                var hasNext = index + 1 < value.Length;
                var next = hasNext ? value[index + 1] : '\0';
                if ((char.IsUpper(current) && (char.IsLower(previous) || (hasNext && char.IsLower(next))))
                    || (char.IsDigit(current) && char.IsLetter(previous))
                    || (char.IsLetter(current) && char.IsDigit(previous)))
                {
                    builder.Append(' ');
                }
            }

            builder.Append(current);
        }

        return builder.ToString();
    }

    private static bool IsPendingSummaryColor(string? color)
    {
        var normalized = NormalizeSummaryColor(color);
        return string.Equals(normalized, StrongRedColor, StringComparison.OrdinalIgnoreCase)
               || string.Equals(normalized, StrongYellowColor, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsRedSummaryColor(string? color)
        => string.Equals(NormalizeSummaryColor(color), StrongRedColor, StringComparison.OrdinalIgnoreCase);

    private static string NormalizeSummaryColor(string? color)
    {
        if (string.IsNullOrWhiteSpace(color))
        {
            return string.Empty;
        }

        if (string.Equals(color, LegacyPaleRedColor, StringComparison.OrdinalIgnoreCase))
        {
            return StrongRedColor;
        }

        if (string.Equals(color, LegacyPaleYellowColor, StringComparison.OrdinalIgnoreCase))
        {
            return StrongYellowColor;
        }

        return color;
    }

    private static List<TaskItem> BuildSeedData()
    {
        return
        [
            new TaskItem
            {
                Title = "Teklif dosyasını tamamla",
                Description = "Müşteri için revize fiyat teklifini gönder.",
                BoardType = TaskBoardType.Acil,
                CreatedAt = DateTime.Now.AddDays(-2),
                UpdatedAt = DateTime.Now,
                DueDate = DateTime.Today.AddDays(1),
                SortOrder = 0,
                Notes = { new TaskNote { Text = "Son revizyon bekleniyor." } }
            },
            new TaskItem
            {
                Title = "Aylık rapor",
                Description = "Mart operasyon raporu hazırlanacak.",
                BoardType = TaskBoardType.Genel,
                CreatedAt = DateTime.Now.AddDays(-4),
                UpdatedAt = DateTime.Now,
                DueDate = DateTime.Today.AddDays(4),
                SortOrder = 0
            },
            new TaskItem
            {
                Title = "Saha kontrolü",
                Description = "Ekip kontrol listesi doğrulanacak.",
                BoardType = TaskBoardType.Acil,
                CreatedAt = DateTime.Now.AddDays(-6),
                UpdatedAt = DateTime.Now,
                DueDate = DateTime.Today.AddDays(-1),
                SortOrder = 1
            },
            new TaskItem
            {
                Title = "Arşiv düzenlemesi",
                Description = "Geçmiş proje klasörleri standardize edilecek.",
                BoardType = TaskBoardType.Genel,
                CreatedAt = DateTime.Now.AddDays(-3),
                UpdatedAt = DateTime.Now,
                SortOrder = 1
            }
        ];
    }

    // Task management methods moved to MainViewModel.TaskManagement.cs

    private void OpenSearch()
    {
        SearchOverlay.SelectedScope = MapSearchScope(SelectedMainTab);
        SearchOverlay.Open();
        OnSearchQueryChanged(this, SearchOverlay.Query);
        _searchWarmupTask ??= WarmupSearchAsync();
    }

    private async Task WarmupSearchAsync()
    {
        await EnsureAllModulesInitializedAsync();

        if (!SearchOverlay.IsOpen)
        {
            _searchWarmupTask = null;
            return;
        }

        if (SearchOverlay.IsClassicMode)
        {
            OnSearchQueryChanged(this, SearchOverlay.Query);
        }

        _searchWarmupTask = null;
    }

    private void ReconfigureAutoBackup()
    {
        _backupService.StopAutoBackup();
        if (AutoBackupEnabled)
        {
            _backupService.ScheduleAutoBackup(TimeSpan.FromMinutes(AutoBackupMinutes), AutoBackupAsync);
        }
    }

    private void ObserveModuleDirtyState(INotifyPropertyChanged module)
    {
        module.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ActionModule.HasUnsavedChanges))
            {
                NotifySaveStatusChanged();
            }
        };
    }

    private void NotifySaveStatusChanged()
    {
        OnPropertyChanged(nameof(HasAnyUnsavedChanges));
        OnPropertyChanged(nameof(SaveStatusText));
        OnPropertyChanged(nameof(SaveStatusTimestampText));
    }

    private async Task MarkGlobalSaveSucceededAsync(CancellationToken cancellationToken = default)
    {
        var timestamp = DateTime.Now;
        LastSuccessfulSaveAt = timestamp;

        try
        {
            await _lastSaveMetadataService.SaveLastSuccessfulSaveAtAsync(timestamp, cancellationToken);
        }
        catch (Exception ex)
        {
            _notificationService.ShowToast($"Son kayıt zamanı metadata dosyasına yazılamadı: {ex.Message}", ToastType.Warning, TimeSpan.FromSeconds(4));
        }

        NotifySaveStatusChanged();
    }

    private void HandleEscape()
    {
        if (SearchOverlay.IsOpen)
        {
            SearchOverlay.Close();
            UrgentBoard.FilterText = string.Empty;
            GeneralBoard.FilterText = string.Empty;
            return;
        }

        if (DetailPanel.HasTask)
        {
            UrgentBoard.SelectedTask = null;
            GeneralBoard.SelectedTask = null;
            DetailPanel.Close();
        }
    }

    private async Task SaveActiveTabAsync()
        => await RunExclusiveOperationAsync(async () =>
        {
            PendingEditCommitHelper.FlushFocusedEditor();
            await EnsureModulesForTabAsync(SelectedMainTab);
            await CommitPendingEditsForTabAsync(SelectedMainTab);
            var saveLabel = GetSaveOperationLabel();
            if (saveLabel is null)
            {
                _notificationService.ShowToast("Kaydedilecek değişiklik yok.", ToastType.Info, TimeSpan.FromSeconds(2));
                return;
            }

            switch (SelectedMainTab)
            {
                case MainNavigationTab.GenelIsTakibi:
                    if (!HasUnsavedChanges)
                    {
                        _notificationService.ShowToast("Kaydedilecek değişiklik yok.", ToastType.Info, TimeSpan.FromSeconds(2));
                        return;
                    }

                    if (await PersistGeneralTasksSafelyAsync(showSuccessToast: true))
                    {
                        await MarkGlobalSaveSucceededAsync();
                    }
                    break;
                case MainNavigationTab.Aksiyon:
                    if (!ActionModule.HasUnsavedChanges)
                    {
                        _notificationService.ShowToast("Kaydedilecek değişiklik yok.", ToastType.Info, TimeSpan.FromSeconds(2));
                        return;
                    }

                    await ActionModule.PersistAsync(showErrorToast: true);
                    if (!ActionModule.HasUnsavedChanges)
                    {
                        await MarkGlobalSaveSucceededAsync();
                        _notificationService.ShowToast("Aksiyon kayıtları kaydedildi.", ToastType.Success, TimeSpan.FromSeconds(2));
                    }
                    break;
                case MainNavigationTab.EksikProje:
                    if (!MissingProjectModule.HasUnsavedChanges)
                    {
                        _notificationService.ShowToast("Kaydedilecek değişiklik yok.", ToastType.Info, TimeSpan.FromSeconds(2));
                        return;
                    }

                    await MissingProjectModule.PersistAsync(showErrorToast: true);
                    if (!MissingProjectModule.HasUnsavedChanges)
                    {
                        await MarkGlobalSaveSucceededAsync();
                        _notificationService.ShowToast("Eksik proje kayıtları kaydedildi.", ToastType.Success, TimeSpan.FromSeconds(2));
                    }
                    break;
                case MainNavigationTab.KarotTakibi:
                    if (!KarotModule.HasUnsavedChanges)
                    {
                        _notificationService.ShowToast("Kaydedilecek değişiklik yok.", ToastType.Info, TimeSpan.FromSeconds(2));
                        return;
                    }

                    await KarotModule.PersistAsync(showErrorToast: true);
                    if (!KarotModule.HasUnsavedChanges)
                    {
                        await MarkGlobalSaveSucceededAsync();
                        _notificationService.ShowToast("Karot kayıtları kaydedildi.", ToastType.Success, TimeSpan.FromSeconds(2));
                    }
                    break;
                case MainNavigationTab.TadilatTakibi:
                    if (!TadilatModule.HasUnsavedChanges)
                    {
                        _notificationService.ShowToast("Kaydedilecek değişiklik yok.", ToastType.Info, TimeSpan.FromSeconds(2));
                        return;
                    }

                    await TadilatModule.PersistAsync(showErrorToast: true);
                    if (!TadilatModule.HasUnsavedChanges)
                    {
                        await MarkGlobalSaveSucceededAsync();
                        _notificationService.ShowToast("Tadilat kayıtları kaydedildi.", ToastType.Success, TimeSpan.FromSeconds(2));
                    }
                    break;
                case MainNavigationTab.YibfAnaBilgi:
                case MainNavigationTab.YibfIsTakibi:
                case MainNavigationTab.YibfBekleyenIsler:
                    if (!YibfModule.HasUnsavedChanges)
                    {
                        _notificationService.ShowToast("Kaydedilecek değişiklik yok.", ToastType.Info, TimeSpan.FromSeconds(2));
                        return;
                    }

                    await YibfModule.PersistAsync(showErrorToast: true);
                    if (!YibfModule.HasUnsavedChanges)
                    {
                        await MarkGlobalSaveSucceededAsync();
                        _notificationService.ShowToast("YİBF kayıtları kaydedildi.", ToastType.Success, TimeSpan.FromSeconds(2));
                    }
                    break;
                case MainNavigationTab.TumEksikler:
                    _notificationService.ShowToast("TÜM EKSİKLER ekranı salt okunur; kaydedilecek değişiklik yok.", ToastType.Info, TimeSpan.FromSeconds(2));
                    break;
                case MainNavigationTab.Ayarlar:
                    await SaveSettingsAsync();
                    break;
            }
        });

    private async Task CommitPendingEditsAcrossAllTabsAsync()
    {
        await ActionModule.CommitPendingEditsAsync();
        MissingProjectModule.CommitPendingEdits();
        TadilatModule.CommitPendingEdits();
        YibfModule.CommitPendingEdits();
    }

    private async Task CommitPendingEditsForTabAsync(MainNavigationTab tab)
    {
        switch (tab)
        {
            case MainNavigationTab.Aksiyon:
                await ActionModule.CommitPendingEditsAsync();
                break;
            case MainNavigationTab.EksikProje:
                MissingProjectModule.CommitPendingEdits();
                break;
            case MainNavigationTab.TadilatTakibi:
                TadilatModule.CommitPendingEdits();
                break;
            case MainNavigationTab.YibfAnaBilgi:
            case MainNavigationTab.YibfIsTakibi:
            case MainNavigationTab.YibfBekleyenIsler:
                YibfModule.CommitPendingEdits();
                break;
            case MainNavigationTab.TumEksikler:
                await CommitPendingEditsAcrossAllTabsAsync();
                break;
        }
    }

    private async Task SaveSettingsAsync(bool showSuccessToast = true)
    {
        try
        {
            await _appSettingsService.SaveAsync(_settings);
            ReconfigureAutoBackup();
            HasUnsavedSettings = false;
            await MarkGlobalSaveSucceededAsync();
            if (showSuccessToast)
            {
                _notificationService.ShowToast("Ayarlar kaydedildi.", ToastType.Success, TimeSpan.FromSeconds(2));
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowToast($"Ayar kaydetme hatası: {ex.Message}", ToastType.Error, TimeSpan.FromSeconds(4));
        }
    }

    private async Task SaveSettingsWithConfirmationAsync()
    {
        if (!HasUnsavedSettings)
        {
            _notificationService.ShowToast("Kaydedilecek değişiklik yok.", ToastType.Info, TimeSpan.FromSeconds(2));
            return;
        }

        if (!_confirmationService.Confirm(new ConfirmationRequest
            {
                Kind = ConfirmationKind.Save,
                Title = "Kaydet",
                Message = "Ayarlar kaydedilecek.\n\nDevam edilsin mi?",
                IsDestructive = false
            }))
        {
            return;
        }

        await SaveSettingsAsync();
    }

    private string? GetSaveOperationLabel()
        => SelectedMainTab switch
        {
            MainNavigationTab.GenelIsTakibi when HasUnsavedChanges => "Genel iş takibi değişiklikleri",
            MainNavigationTab.Aksiyon when ActionModule.HasUnsavedChanges => "Aksiyon kayıtları",
            MainNavigationTab.EksikProje when MissingProjectModule.HasUnsavedChanges => "Eksik proje kayıtları",
            MainNavigationTab.KarotTakibi when KarotModule.HasUnsavedChanges => "Karot kayıtları",
            MainNavigationTab.TadilatTakibi when TadilatModule.HasUnsavedChanges => "Tadilat kayıtları",
            MainNavigationTab.YibfAnaBilgi or MainNavigationTab.YibfIsTakibi or MainNavigationTab.YibfBekleyenIsler when YibfModule.HasUnsavedChanges => "YİBF kayıtları",
            MainNavigationTab.Ayarlar when HasUnsavedSettings => "Ayarlar",
            _ => null
        };

    private async Task ResetApplicationDataAsync(bool includeBackups)
        => await RunExclusiveOperationAsync(async () =>
        {
            var scopeText = includeBackups
                ? "Canlı veriler ve uygulamanın yönettiği otomatik JSON yedekleri silinecek."
                : "Yalnız canlı veriler silinecek. JSON yedekler korunacak.";
            if (!_confirmationService.Confirm(new ConfirmationRequest
                {
                    Kind = ConfirmationKind.Reset,
                    Title = includeBackups ? "Verileri ve Yedekleri Sıfırla" : "Canlı Verileri Sıfırla",
                    Message = $"{scopeText}\n\nİşlem geri alınamaz. Devam edilsin mi?",
                    IsDestructive = true
                }))
            {
                return;
            }

            try
            {
                IsBusy = true;
                _backupService.StopAutoBackup();
                var previousState = await CaptureApplicationStateSnapshotAsync();

                ApplyBackupRestoreData(new BackupRestoreData());
                _settings.SeedSampleDataOnEmpty = false;
                OnPropertyChanged(nameof(SeedSampleDataOnEmpty));
                HasUnsavedSettings = true;

                var persistResult = await PersistWithRollbackAsync(previousState, includeBackups ? "Veri ve yedek sıfırlama" : "Veri sıfırlama");
                if (persistResult != PersistWithRollbackResult.Succeeded)
                {
                    return;
                }

                if (includeBackups)
                {
                    try
                    {
                        await _backupService.ClearManagedBackupsAsync();
                    }
                    catch (Exception ex)
                    {
                        _notificationService.ShowToast($"Canlı veriler temizlendi ancak otomatik yedekler silinemedi: {ex.Message}", ToastType.Warning, TimeSpan.FromSeconds(5));
                        _undoRedoService.Clear();
                        return;
                    }
                }

                _undoRedoService.Clear();
                _notificationService.ShowToast(
                    includeBackups ? "Canlı veriler ve otomatik yedekler temizlendi." : "Canlı veriler temizlendi.",
                    ToastType.Success,
                    TimeSpan.FromSeconds(4));
            }
            catch (Exception ex)
            {
                _notificationService.ShowToast($"Veri sıfırlama hatası: {ex.Message}", ToastType.Error, TimeSpan.FromSeconds(5));
            }
            finally
            {
                _suppressTaskDirtyTracking = false;
                IsBusy = false;
                ReconfigureAutoBackup();
            }
        });

    private static ObservableCollection<ClearTabOption> BuildClearableTabs()
        =>
        [
            new(MainNavigationTab.GenelIsTakibi, "GENEL İŞ TAKİBİ"),
            new(MainNavigationTab.Aksiyon, "AKSİYON"),
            new(MainNavigationTab.EksikProje, "EKSİK PROJE"),
            new(MainNavigationTab.KarotTakibi, "KAROT TAKİBİ"),
            new(MainNavigationTab.TadilatTakibi, "TADİLAT TAKİBİ"),
            new(MainNavigationTab.YibfAnaBilgi, "YİBF ANA BİLGİ"),
            new(MainNavigationTab.YibfIsTakibi, "YİBF İŞ TAKİBİ")
        ];

    private bool CanClearSelectedTab()
        => SelectedClearTab is not null && !IsBusy;

    private async Task ClearSelectedTabAsync()
        => await RunExclusiveOperationAsync(async () =>
        {
            var selected = SelectedClearTab;
            if (selected is null)
            {
                _notificationService.ShowToast("Önce silinecek bir sekme seçin.", ToastType.Warning, TimeSpan.FromSeconds(2));
                return;
            }

            await EnsureAllModulesInitializedAsync();

            var affectedCount = GetClearTabRecordCount(selected.Tab);
            if (affectedCount <= 0)
            {
                _notificationService.ShowToast($"{selected.DisplayName} sekmesinde silinecek kayıt yok.", ToastType.Info, TimeSpan.FromSeconds(2));
                return;
            }

            if (!_confirmationService.Confirm(new ConfirmationRequest
                {
                    Kind = ConfirmationKind.Delete,
                    Title = "Sekmeyi Sil",
                    Message = $"{selected.DisplayName} sekmesindeki {affectedCount} kayıt kalıcı olarak silinecek.\n\nİşlem geri alınamaz. Devam edilsin mi?",
                    IsDestructive = true
                }))
            {
                return;
            }

            try
            {
                IsBusy = true;
                var previousState = await CaptureApplicationStateSnapshotAsync();

                ApplyClearTabData(selected.Tab);
                var persistResult = await PersistWithRollbackAsync(previousState, $"{selected.DisplayName} temizleme");
                if (persistResult != PersistWithRollbackResult.Succeeded)
                {
                    return;
                }

                _undoRedoService.Clear();
                _notificationService.ShowToast($"{selected.DisplayName} verileri temizlendi.", ToastType.Success, TimeSpan.FromSeconds(3));
            }
            catch (Exception ex)
            {
                _notificationService.ShowToast($"Sekme temizleme hatası: {ex.Message}", ToastType.Error, TimeSpan.FromSeconds(5));
            }
            finally
            {
                IsBusy = false;
            }
        });

    private int GetClearTabRecordCount(MainNavigationTab tab)
        => tab switch
        {
            MainNavigationTab.GenelIsTakibi => UrgentBoard.Tasks.Count + GeneralBoard.Tasks.Count,
            MainNavigationTab.Aksiyon => ActionModule.GetAllEntriesSnapshot().Count,
            MainNavigationTab.EksikProje => MissingProjectModule.Entries.Count + MissingProjectModule.CellStates.Count,
            MainNavigationTab.KarotTakibi => KarotModule.Entries.Count + KarotModule.CellStates.Count,
            MainNavigationTab.TadilatTakibi => TadilatModule.AktifEntries.Count + TadilatModule.BitenEntries.Count + TadilatModule.CellStates.Count,
            MainNavigationTab.YibfAnaBilgi => YibfModule.AnaBilgiEntries.Count + YibfModule.AnaBilgiEvents.Count,
            MainNavigationTab.YibfIsTakibi => YibfModule.IsTakibiEntries.Count + YibfModule.CellStates.Count,
            _ => 0
        };

    private void ApplyClearTabData(MainNavigationTab tab)
    {
        switch (tab)
        {
            case MainNavigationTab.GenelIsTakibi:
                _suppressTaskDirtyTracking = true;
                try
                {
                    UrgentBoard.ReplaceAll(Array.Empty<TaskItem>());
                    GeneralBoard.ReplaceAll(Array.Empty<TaskItem>());
                    UrgentBoard.SelectedTask = null;
                    GeneralBoard.SelectedTask = null;
                    DetailPanel.Close();
                    SearchOverlay.Close();
                }
                finally
                {
                    _suppressTaskDirtyTracking = false;
                }

                HasUnsavedChanges = true;
                break;

            case MainNavigationTab.Aksiyon:
                ActionModule.LoadFromBackup(Array.Empty<ActionEntry>());
                MarkModuleStateLoadedFromSnapshot(tab);
                break;

            case MainNavigationTab.EksikProje:
                MissingProjectModule.LoadFromBackup(Array.Empty<MissingProjectEntry>(), Array.Empty<MissingProjectCellState>());
                MarkModuleStateLoadedFromSnapshot(tab);
                break;

            case MainNavigationTab.KarotTakibi:
                KarotModule.LoadFromBackup(Array.Empty<KarotEntry>(), Array.Empty<KarotCellState>());
                MarkModuleStateLoadedFromSnapshot(tab);
                break;

            case MainNavigationTab.TadilatTakibi:
                TadilatModule.LoadFromBackup(Array.Empty<TadilatEntry>(), Array.Empty<TadilatCellState>());
                MarkModuleStateLoadedFromSnapshot(tab);
                break;

            case MainNavigationTab.YibfAnaBilgi:
            {
                var isTakibiEntries = YibfModule.GetIsTakibiEntriesSnapshot();
                var yibfCellStates = YibfModule.GetCellStatesSnapshot();
                YibfModule.LoadFromBackup(Array.Empty<YibfAnaBilgiEntry>(), Array.Empty<YibfAnaBilgiEvent>(), isTakibiEntries, yibfCellStates);
                MarkModuleStateLoadedFromSnapshot(tab);
                break;
            }

            case MainNavigationTab.YibfIsTakibi:
            {
                var anaBilgiEntries = YibfModule.GetAnaBilgiEntriesSnapshot();
                var anaBilgiEvents = YibfModule.GetAnaBilgiEventsSnapshot();
                YibfModule.LoadFromBackup(anaBilgiEntries, anaBilgiEvents, Array.Empty<YibfIsTakibiEntry>(), Array.Empty<YibfCellState>());
                MarkModuleStateLoadedFromSnapshot(tab);
                break;
            }

            default:
                break;
        }

        InvalidateSearchCorpus();
        RefreshDashboard();
        RefreshAcilIsOzet();
        NotifySelectionCommands();
    }

    private async Task CleanupBuildArtifactsAsync()
    {
        var decision = MessageBox.Show(
            "Bu işlem yalnız yeniden üretilebilir bin/obj derleme çıktılarını temizler. SQLite verileri, JSON yedekleri ve Excel dosyaları korunur. Devam edilsin mi?",
            "Geçici Dosyaları Temizle",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (decision != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            IsBusy = true;
            var result = await Task.Run(CleanupBuildArtifactsCore);
            if (result.ReclaimedBytes <= 0)
            {
                _notificationService.ShowToast("Temizlenecek derleme çıktısı bulunamadı.", ToastType.Info);
                return;
            }

            var suffix = result.SkippedCurrentOutput
                ? " Çalışan uygulamanın aktif build klasörü korunmuştur."
                : string.Empty;
            _notificationService.ShowToast($"Derleme çıktıları temizlendi ({FormatBytes(result.ReclaimedBytes)} boşaltıldı).{suffix}", ToastType.Success, TimeSpan.FromSeconds(5));
        }
        catch (Exception ex)
        {
            _notificationService.ShowToast($"Geçici dosya temizleme hatası: {ex.Message}", ToastType.Error, TimeSpan.FromSeconds(5));
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static (long ReclaimedBytes, bool SkippedCurrentOutput) CleanupBuildArtifactsCore()
    {
        var solutionRoot = FindSolutionRoot() ?? throw new DirectoryNotFoundException("Çözüm kökü bulunamadı.");
        var currentBase = Path.GetFullPath(AppContext.BaseDirectory);
        var projectRoot = Path.Combine(solutionRoot, "RizaCanKilicIsTakibi");
        var testRoot = Path.Combine(solutionRoot, "RizaCanKilicIsTakibi.Tests");

        var skippedCurrentOutput = false;
        long reclaimedBytes = 0;
        reclaimedBytes += DeleteBuildArtifactContents(Path.Combine(projectRoot, "bin"), currentBase, ref skippedCurrentOutput);
        reclaimedBytes += DeleteBuildArtifactContents(Path.Combine(projectRoot, "obj"), null, ref skippedCurrentOutput);
        reclaimedBytes += DeleteBuildArtifactContents(Path.Combine(testRoot, "bin"), null, ref skippedCurrentOutput);
        reclaimedBytes += DeleteBuildArtifactContents(Path.Combine(testRoot, "obj"), null, ref skippedCurrentOutput);

        return (reclaimedBytes, skippedCurrentOutput);
    }

    private static string? FindSolutionRoot()
    {
        var current = new DirectoryInfo(Path.GetFullPath(AppContext.BaseDirectory));
        while (current is not null)
        {
            if (current.GetFiles("*.sln").Any(file => file.Name.Equals("RizaCanKilicIsTakibi.sln", StringComparison.OrdinalIgnoreCase)))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        return null;
    }

    private static long DeleteBuildArtifactContents(string rootPath, string? protectedPath, ref bool skippedCurrentOutput)
    {
        if (!Directory.Exists(rootPath))
        {
            return 0;
        }

        long reclaimedBytes = 0;
        foreach (var entry in Directory.EnumerateFileSystemEntries(rootPath))
        {
            var fullEntry = Path.GetFullPath(entry);
            if (!string.IsNullOrWhiteSpace(protectedPath))
            {
                if (IsSameOrAncestor(fullEntry, protectedPath))
                {
                    skippedCurrentOutput = true;
                    if (!PathsEqual(fullEntry, protectedPath))
                    {
                        reclaimedBytes += DeleteBuildArtifactContents(fullEntry, protectedPath, ref skippedCurrentOutput);
                    }

                    continue;
                }

                if (IsSameOrAncestor(protectedPath, fullEntry))
                {
                    skippedCurrentOutput = true;
                    continue;
                }
            }

            reclaimedBytes += GetFileSystemSize(fullEntry);
            DeleteFileSystemEntry(fullEntry);
        }

        return reclaimedBytes;
    }

    private static bool PathsEqual(string left, string right)
        => string.Equals(NormalizePath(left), NormalizePath(right), StringComparison.OrdinalIgnoreCase);

    private static bool IsSameOrAncestor(string candidateAncestor, string path)
    {
        var normalizedAncestor = NormalizePath(candidateAncestor);
        var normalizedPath = NormalizePath(path);
        return normalizedPath.Equals(normalizedAncestor, StringComparison.OrdinalIgnoreCase)
               || normalizedPath.StartsWith(normalizedAncestor + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizePath(string path)
    {
        var fullPath = Path.GetFullPath(path);
        return Path.EndsInDirectorySeparator(fullPath)
            ? fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            : fullPath;
    }

    private static long GetFileSystemSize(string path)
    {
        if (File.Exists(path))
        {
            return new FileInfo(path).Length;
        }

        if (!Directory.Exists(path))
        {
            return 0;
        }

        long total = 0;
        foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
        {
            total += new FileInfo(file).Length;
        }

        return total;
    }

    private static void DeleteFileSystemEntry(string path)
    {
        if (File.Exists(path))
        {
            File.SetAttributes(path, FileAttributes.Normal);
            File.Delete(path);
            return;
        }

        if (!Directory.Exists(path))
        {
            return;
        }

        foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
        {
            File.SetAttributes(file, FileAttributes.Normal);
        }

        Directory.Delete(path, true);
    }

    private static string FormatBytes(long bytes)
    {
        var units = new[] { "B", "KB", "MB", "GB" };
        double size = bytes;
        var unitIndex = 0;
        while (size >= 1024 && unitIndex < units.Length - 1)
        {
            size /= 1024;
            unitIndex++;
        }

        return $"{size:0.#} {units[unitIndex]}";
    }
    private async Task ManualBackupAsync()
        => await RunExclusiveOperationAsync(async () =>
        {
            try
            {
                await EnsureAllModulesInitializedAsync();
                var path = _fileDialogService.ShowSaveDialog("Yedek kaydet", "JSON (*.json)|*.json", ".json");
                if (string.IsNullOrWhiteSpace(path))
                {
                    return;
                }

                var metadata = await _backupService.CreateBackupAsync(
                    AllTasks(),
                    path,
                    ActionModule.GetAllEntriesSnapshot(),
                    MissingProjectModule.GetEntriesSnapshot(),
                    MissingProjectModule.GetCellStatesSnapshot(),
                    KarotModule.GetEntriesSnapshot(),
                    KarotModule.GetCellStatesSnapshot(),
                    TadilatModule.GetEntriesSnapshot(),
                    YibfModule.GetAnaBilgiEntriesSnapshot(),
                    YibfModule.GetAnaBilgiEventsSnapshot(),
                    YibfModule.GetIsTakibiEntriesSnapshot(),
                    YibfModule.GetCellStatesSnapshot(),
                    TadilatModule.GetCellStatesSnapshot(),
                    _quickTaskTemplateRepository?.GetAll());
                _notificationService.ShowToast($"Yedek alındı ({metadata.TaskCount} kayıt).", ToastType.Success);
            }
            catch (Exception ex)
            {
                _notificationService.ShowToast($"Yedekleme hatası: {ex.Message}", ToastType.Error);
            }
        });

    private async Task ImportBackupAsync()
        => await RunExclusiveOperationAsync(async () =>
        {
            try
            {
                if (HasAnyUnsavedChanges)
                {
                    if (!_confirmationService.Confirm(new ConfirmationRequest
                        {
                            Kind = ConfirmationKind.Restore,
                            Title = "JSON Aktar",
                            Message = "Kaydedilmemiş değişiklikler var. JSON aktarımı mevcut verilerin üzerine yazacak. Devam edilsin mi?",
                            IsDestructive = true
                        }))
                    {
                        return;
                    }
                }

                var path = _fileDialogService.ShowOpenDialog("JSON yedek aç", "JSON (*.json)|*.json");
                if (string.IsNullOrWhiteSpace(path))
                {
                    return;
                }

                IsBusy = true;
                var previousState = await CaptureApplicationStateSnapshotAsync();
                var restored = await _backupService.RestoreBackupAsync(path);

                ApplyBackupRestoreData(restored);
                HasUnsavedChanges = true;

                var persistResult = await PersistWithRollbackAsync(previousState, "JSON aktarımı");
                if (persistResult != PersistWithRollbackResult.Succeeded)
                {
                    return;
                }

                _undoRedoService.Clear();
                _notificationService.ShowToast("JSON verisi başarıyla aktarıldı.", ToastType.Success);
            }
            catch (Exception ex)
            {
                _notificationService.ShowToast($"JSON aktarma hatası: {ex.Message}", ToastType.Error);
            }
            finally
            {
                _suppressTaskDirtyTracking = false;
                IsBusy = false;
            }
        });

    private async Task ExportExcelAsync()
    {
        try
        {
            if (SelectedMainTab == MainNavigationTab.Ayarlar)
            {
                _notificationService.ShowToast("Ayarlar sekmesi için Excel dışa aktarma yok.", ToastType.Warning);
                return;
            }

            var workbook = BuildCurrentTabExcelWorkbook();
            if (workbook.Sheets.Count == 0)
            {
                _notificationService.ShowToast("Excel dışa aktarma için geçerli veri bulunamadı.", ToastType.Warning);
                return;
            }

            var path = _fileDialogService.ShowSaveDialog("Excel dışa aktar", "Excel (*.xlsx)|*.xlsx", ".xlsx");
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            await _importExportService.ExportWorkbookAsync(workbook, path);
            _notificationService.ShowToast($"{GetCurrentTabExcelTitle()} Excel olarak kaydedildi.", ToastType.Success);
        }
        catch (Exception ex)
        {
            _notificationService.ShowToast($"Excel dışa aktarma hatası: {ex.Message}", ToastType.Error);
        }
    }

    private ExcelWorkbookExportModel BuildCurrentTabExcelWorkbook()
        => SelectedMainTab switch
        {
            MainNavigationTab.GenelIsTakibi => BuildGeneralTasksExcelWorkbook(),
            MainNavigationTab.Aksiyon => BuildActionExcelWorkbook(),
            MainNavigationTab.EksikProje => BuildMissingProjectExcelWorkbook(),
            MainNavigationTab.KarotTakibi => BuildKarotExcelWorkbook(),
            MainNavigationTab.TadilatTakibi => BuildTadilatExcelWorkbook(),
            MainNavigationTab.YibfAnaBilgi => BuildYibfAnaBilgiExcelWorkbook(),
            MainNavigationTab.YibfBekleyenIsler => BuildYibfPendingExcelWorkbook(),
            MainNavigationTab.YibfIsTakibi => BuildYibfIsTakibiExcelWorkbook(),
            _ => new ExcelWorkbookExportModel()
        };

    private string GetCurrentTabExcelTitle()
        => SelectedMainTab switch
        {
            MainNavigationTab.GenelIsTakibi => "Genel iş takibi",
            MainNavigationTab.Aksiyon => "Aksiyon",
            MainNavigationTab.EksikProje => "Eksik proje",
            MainNavigationTab.KarotTakibi => "Karot takibi",
            MainNavigationTab.TadilatTakibi => "Tadilat takibi",
            MainNavigationTab.YibfAnaBilgi => "YİBF ana bilgi",
            MainNavigationTab.YibfBekleyenIsler => "ACİL İŞ ÖZET",
            MainNavigationTab.YibfIsTakibi => "YİBF iş takibi",
            _ => "Excel"
        };

    private ExcelWorkbookExportModel BuildGeneralTasksExcelWorkbook()
        => new()
        {
            Sheets =
            [
                BuildTaskBoardSheet("Acil İşler", UrgentBoard.Tasks.OrderBy(item => item.SortOrder).ThenBy(item => item.UpdatedAt)),
                BuildTaskBoardSheet("Genel İşler", GeneralBoard.Tasks.OrderBy(item => item.SortOrder).ThenBy(item => item.UpdatedAt))
            ]
        };

    private ExcelSheetExportModel BuildTaskBoardSheet(string name, IEnumerable<TaskItem> tasks)
        => new()
        {
            Name = name,
            Headers = ["Başlık", "Bitiş Tarihi", "Oluşturulma", "Güncellenme"],
            Rows = tasks.Select(task => CreateRow(
                CreateCell(task.Title, BuildTaskNotesComment(task)),
                CreateCell(task.DueDate?.ToString("dd.MM.yyyy") ?? string.Empty),
                CreateCell(task.CreatedAt.ToString("dd.MM.yyyy HH:mm")),
                CreateCell(task.UpdatedAt.ToString("dd.MM.yyyy HH:mm")))).ToList()
        };

    private ExcelWorkbookExportModel BuildActionExcelWorkbook()
    {
        var allEntries = ActionModule.GetAllEntriesSnapshot();
        return new ExcelWorkbookExportModel
        {
            Sheets =
            [
                BuildActionSheet("Aksiyon", allEntries.Where(item => item.Category == ActionEntryCategory.Aksiyon)),
                BuildActionSheet("Aksiyona Eklenecekler", allEntries.Where(item => item.Category == ActionEntryCategory.AksiyonaEklenecekler))
            ]
        };
    }

    private static ExcelSheetExportModel BuildActionSheet(string name, IEnumerable<ActionEntry> entries)
        => new()
        {
            Name = name,
            Headers = ["İlçe", "Ada Parsel Yapı Sahibi", "Yapılacak İş"],
            Rows = entries
                .OrderBy(item => item.District, StringComparer.OrdinalIgnoreCase)
                .ThenBy(item => item.DisplayOrder)
                .Select(entry => CreateRow(
                    CreateCell(entry.District),
                    CreateCell(entry.OwnerParcelText),
                    CreateCell(entry.WorkText)))
                .ToList()
        };

    private ExcelWorkbookExportModel BuildMissingProjectExcelWorkbook()
    {
        var entries = MissingProjectModule.GetEntriesSnapshot();
        var states = BuildCellStateLookup(MissingProjectModule.GetCellStatesSnapshot(), state => state.EntryId, state => state.ColumnKey);

        return new ExcelWorkbookExportModel
        {
            Sheets =
            [
                new ExcelSheetExportModel
                {
                    Name = "Eksik Proje",
                    Headers = ["Ada Parsel", "Yapı Sahibi", "Fiziksel Mi Dijital Mi", "Eksik Proje", "Açıklama"],
                    Rows = entries.Select(entry => CreateRow(
                        CreateStatefulCell(entry.AdaParsel, states, entry.Id, nameof(MissingProjectEntry.AdaParsel)),
                        CreateStatefulCell(entry.YapiSahibi, states, entry.Id, nameof(MissingProjectEntry.YapiSahibi)),
                        CreateStatefulCell(entry.RecordMediumText, states, entry.Id, nameof(MissingProjectEntry.RecordMediumText)),
                        CreateStatefulCell(entry.MissingProjectText, states, entry.Id, nameof(MissingProjectEntry.MissingProjectText)),
                        CreateStatefulCell(entry.Description, states, entry.Id, nameof(MissingProjectEntry.Description))))
                    .ToList()
                }
            ]
        };
    }

    private ExcelWorkbookExportModel BuildKarotExcelWorkbook()
    {
        var entries = KarotModule.GetEntriesSnapshot();
        var states = BuildCellStateLookup(KarotModule.GetCellStatesSnapshot(), state => state.EntryId, state => state.ColumnKey);

        return new ExcelWorkbookExportModel
        {
            Sheets =
            [
                BuildKarotSheet("Bekleyen", entries.Where(entry => entry.Status is not KarotStatus.KarotAlindiOlumlu), states),
                BuildKarotSheet("Yapılan", entries.Where(entry => entry.Status == KarotStatus.KarotAlindiOlumlu), states)
            ]
        };
    }

    private ExcelSheetExportModel BuildKarotSheet(string name, IEnumerable<KarotEntry> entries, IReadOnlyDictionary<string, KarotCellState> states)
        => new()
        {
            Name = name,
            Headers = ["Numune Alınma Tarihi", "YİBF No", "Ada Parsel", "Yapı Sahibi", "Müteahhit", "Kat Bilgisi", "Beton Sınıfı", "28 Günlük Sonuç", "Beton Firması", "Laboratuvar", "Açıklama", "Kayıt Durumu"],
            Rows = entries
                .OrderBy(item => item.DisplayOrder)
                .Select(entry =>
                {
                    var rowBackground = GetKarotRowBackgroundColor(entry.Status);
                    return CreateRow(
                        CreateStatefulCell(entry.SampleReceivedDate?.ToString("dd.MM.yyyy") ?? string.Empty, states, entry.Id, nameof(KarotEntry.SampleReceivedDate), rowBackground),
                        CreateStatefulCell(entry.YibfNo, states, entry.Id, nameof(KarotEntry.YibfNo), rowBackground),
                        CreateStatefulCell(entry.AdaParsel, states, entry.Id, nameof(KarotEntry.AdaParsel), rowBackground),
                        CreateStatefulCell(entry.YapiSahibi, states, entry.Id, nameof(KarotEntry.YapiSahibi), rowBackground),
                        CreateStatefulCell(entry.Muteahhit, states, entry.Id, nameof(KarotEntry.Muteahhit), rowBackground),
                        CreateStatefulCell(entry.KatBilgisi, states, entry.Id, nameof(KarotEntry.KatBilgisi), rowBackground),
                        CreateStatefulCell(entry.BetonSinifi, states, entry.Id, nameof(KarotEntry.BetonSinifi), rowBackground),
                        CreateStatefulCell(entry.TwentyEightDayResult, states, entry.Id, nameof(KarotEntry.TwentyEightDayResult), rowBackground),
                        CreateStatefulCell(entry.BetonFirmasi, states, entry.Id, nameof(KarotEntry.BetonFirmasi), rowBackground),
                        CreateStatefulCell(entry.Laboratuvar, states, entry.Id, nameof(KarotEntry.Laboratuvar), rowBackground),
                        CreateStatefulCell(entry.Aciklama, states, entry.Id, nameof(KarotEntry.Aciklama), rowBackground),
                        CreateCell(GetKarotStatusLabel(entry.Status), backgroundColor: rowBackground));
                })
                .ToList()
        };

    private ExcelWorkbookExportModel BuildTadilatExcelWorkbook()
    {
        var entries = TadilatModule.GetEntriesSnapshot();
        var states = BuildCellStateLookup(TadilatModule.GetCellStatesSnapshot(), state => state.EntryId, state => state.ColumnKey);

        return new ExcelWorkbookExportModel
        {
            Sheets =
            [
                BuildTadilatSheet("AKTİF", entries.Where(entry => entry.SubTab == TadilatSubTab.Aktif), states, bitenSheet: false),
                BuildTadilatSheet("BİTEN", entries.Where(entry => entry.SubTab == TadilatSubTab.Biten), states, bitenSheet: true)
            ]
        };
    }

    private ExcelSheetExportModel BuildTadilatSheet(string name, IEnumerable<TadilatEntry> entries, IReadOnlyDictionary<string, TadilatCellState> states, bool bitenSheet)
        => new()
        {
            Name = name,
            Headers = ["İlçe", "İşin İsmi", "Proje Türü", "Dijital Geldi", "Denetçi Onayladı", "Çıktı / Rapor Geldi", "Üst Yazı Teslim Edildi", "Projeler Arşive Eklendi", "Açıklama 1", "Açıklama 2"],
            Rows = entries
                .OrderBy(item => item.District, StringComparer.OrdinalIgnoreCase)
                .ThenBy(item => item.DisplayOrder)
                .Select(entry =>
                {
                    var defaultBackground = bitenSheet ? "#FFD5E4FF" : string.Empty;
                    return CreateRow(
                        CreateCell(entry.District, backgroundColor: defaultBackground),
                        CreateStatefulCell(entry.JobName, states, entry.Id, nameof(TadilatEntry.JobName), defaultBackground),
                        CreateStatefulCell(entry.ProjectType, states, entry.Id, nameof(TadilatEntry.ProjectType), defaultBackground),
                        CreateStatefulCell(entry.DigitalReceived, states, entry.Id, nameof(TadilatEntry.DigitalReceived), defaultBackground),
                        CreateStatefulCell(entry.InspectorApproved, states, entry.Id, nameof(TadilatEntry.InspectorApproved), defaultBackground),
                        CreateStatefulCell(entry.OutputAndReportArrived, states, entry.Id, nameof(TadilatEntry.OutputAndReportArrived), defaultBackground),
                        CreateStatefulCell(entry.OfficialLetterSubmitted, states, entry.Id, nameof(TadilatEntry.OfficialLetterSubmitted), defaultBackground),
                        CreateStatefulCell(entry.ArchivedFromMunicipality, states, entry.Id, nameof(TadilatEntry.ArchivedFromMunicipality), defaultBackground),
                        CreateStatefulCell(entry.Description1, states, entry.Id, nameof(TadilatEntry.Description1), defaultBackground),
                        CreateStatefulCell(entry.Description2, states, entry.Id, nameof(TadilatEntry.Description2), defaultBackground));
                })
                .ToList()
        };

    private ExcelWorkbookExportModel BuildYibfAnaBilgiExcelWorkbook()
    {
        var entries = YibfModule.GetAnaBilgiEntriesSnapshot();
        var events = YibfModule.GetAnaBilgiEventsSnapshot();
        var entryLookup = entries.ToDictionary(item => item.Id);

        return new ExcelWorkbookExportModel
        {
            Sheets =
            [
                new ExcelSheetExportModel
                {
                    Name = "Ana Bilgi",
                    Headers = ["Ada Parsel", "YİBF No", "İdare", "Yapı Sahibi", "Müteahhit"],
                    Rows = entries
                        .OrderBy(item => item.DisplayOrder)
                        .Select(entry => CreateRow(
                            CreateCell(entry.AdaParsel),
                            CreateCell(entry.YibfNo),
                            CreateCell(entry.Idare),
                            CreateCell(entry.YapiSahibi),
                            CreateCell(entry.Muteahhit)))
                        .ToList()
                },
                new ExcelSheetExportModel
                {
                    Name = "Olaylar",
                    Headers = ["Ada Parsel", "YİBF No", "Tarih", "Açıklama", "Renk"],
                    Rows = events
                        .OrderBy(item => item.EntryId)
                        .ThenBy(item => item.DisplayOrder)
                        .Select(item =>
                        {
                            entryLookup.TryGetValue(item.EntryId, out var parent);
                            return CreateRow(
                                CreateCell(parent?.AdaParsel ?? string.Empty),
                                CreateCell(parent?.YibfNo ?? string.Empty),
                                CreateCell(item.EventDate?.ToString("dd.MM.yyyy") ?? string.Empty),
                                CreateCell(item.Description, item.NoteText, item.BackgroundColor),
                                CreateCell(GetYibfEventColorLabel(item.BackgroundColor), backgroundColor: item.BackgroundColor));
                        })
                        .ToList()
                }
            ]
        };
    }

    private ExcelWorkbookExportModel BuildYibfPendingExcelWorkbook()
        => new()
        {
            Sheets =
            [
                new ExcelSheetExportModel
                {
                    Name = "Acil İş Özet",
                    Headers = ["Kategori", "Öncelik", "Özet"],
                    Rows = AcilIsOzetItems
                        .Select(item => CreateRow(
                            CreateCell(item.Category),
                            CreateCell(item.PriorityLabel, backgroundColor: item.PriorityRank == 0 ? "#FFFF0000" : "#FFFFFF00"),
                            CreateCell(item.Summary)))
                        .ToList()
                },
                new ExcelSheetExportModel
                {
                    Name = "Proje Onay Takibi",
                    Headers = ["Öncelik", "Tarih", "Ada Parsel", "Yapı Sahibi", "Özet"],
                    Rows = YibfModule.BekleyenIsler
                        .Select(item => CreateRow(
                            CreateCell(item.StatusLabel, backgroundColor: item.PriorityRank == 0 ? "#FFFF0000" : "#FFFFFF00"),
                            CreateCell(item.EventDateText),
                            CreateCell(item.Entry.AdaParsel),
                            CreateCell(item.Entry.YapiSahibi),
                            CreateCell(item.Summary, item.PendingEvent.NoteText)))
                        .ToList()
                }
            ]
        };

    private ExcelWorkbookExportModel BuildYibfIsTakibiExcelWorkbook()
    {
        var entries = YibfModule.GetIsTakibiEntriesSnapshot();
        var states = BuildCellStateLookup(YibfModule.GetCellStatesSnapshot(), state => state.EntryId, state => state.ColumnKey);

        return new ExcelWorkbookExportModel
        {
            Sheets =
            [
                new ExcelSheetExportModel
                {
                    Name = "YİBF İş Takibi",
                    Headers = ["İşin İsmi", "Müellif Bilgileri Geldi Mi", "Denetçi Atamaları Yapıldı Mı", "Tüm Projelerin Dijitali Var Mı", "Evraklar Tam Mı", "YİBF Sözleşme Hazırlandı Mı", "Dekont Alındı Mı", "Ruhsat Başvurusu Yapıldı Mı", "Ruhsat Nüshası Alındı Mı", "İşyeri Teslim Tutanağı Hazırlandı Mı", "İSG Yazısı Hazırlandı Mı", "Sağlık Güvenlik Planı Geldi Mi", "Temel Topraklama Tutanağı Hazırlandı Mı"],
                    Rows = entries
                        .OrderBy(item => item.DisplayOrder)
                        .Select(entry => CreateRow(
                            CreateStatefulCell(entry.JobName, states, entry.Id, nameof(YibfIsTakibiEntry.JobName)),
                            CreateStatefulCell(entry.MuellifBilgileriGeldiMi, states, entry.Id, nameof(YibfIsTakibiEntry.MuellifBilgileriGeldiMi)),
                            CreateStatefulCell(entry.DenetciAtamalariYapildiMi, states, entry.Id, nameof(YibfIsTakibiEntry.DenetciAtamalariYapildiMi)),
                            CreateStatefulCell(entry.TumProjelerinDijitaliVarMi, states, entry.Id, nameof(YibfIsTakibiEntry.TumProjelerinDijitaliVarMi)),
                            CreateStatefulCell(entry.EvraklarTamMi, states, entry.Id, nameof(YibfIsTakibiEntry.EvraklarTamMi)),
                            CreateStatefulCell(entry.YibfSozlesmeHazirlandiMi, states, entry.Id, nameof(YibfIsTakibiEntry.YibfSozlesmeHazirlandiMi)),
                            CreateStatefulCell(entry.DekontAlindiMi, states, entry.Id, nameof(YibfIsTakibiEntry.DekontAlindiMi)),
                            CreateStatefulCell(entry.RuhsatBasvurusuYapildiMi, states, entry.Id, nameof(YibfIsTakibiEntry.RuhsatBasvurusuYapildiMi)),
                            CreateStatefulCell(entry.RuhsatNushasiAlindiMi, states, entry.Id, nameof(YibfIsTakibiEntry.RuhsatNushasiAlindiMi)),
                            CreateStatefulCell(entry.IsyeriTeslimTutangiHazirlandiMi, states, entry.Id, nameof(YibfIsTakibiEntry.IsyeriTeslimTutangiHazirlandiMi)),
                            CreateStatefulCell(entry.IsgYazisiHazirlandiMi, states, entry.Id, nameof(YibfIsTakibiEntry.IsgYazisiHazirlandiMi)),
                            CreateStatefulCell(entry.SaglikGuvenlikPlaniGeldiMi, states, entry.Id, nameof(YibfIsTakibiEntry.SaglikGuvenlikPlaniGeldiMi)),
                            CreateStatefulCell(entry.TemelTopraklamaTutanagiHazirlandiMi, states, entry.Id, nameof(YibfIsTakibiEntry.TemelTopraklamaTutanagiHazirlandiMi))))
                        .ToList()
                }
            ]
        };
    }

    private static ExcelCellExportModel CreateCell(string? value, string? comment = null, string? backgroundColor = null)
        => new()
        {
            Value = value ?? string.Empty,
            Comment = comment ?? string.Empty,
            BackgroundColor = backgroundColor ?? string.Empty
        };

    private static ExcelRowExportModel CreateRow(params ExcelCellExportModel[] cells)
        => new() { Cells = cells };

    private static ExcelCellExportModel CreateStatefulCell<TState>(string? value, IReadOnlyDictionary<string, TState> states, Guid entryId, string columnKey, string? fallbackBackgroundColor = null)
        where TState : class
    {
        states.TryGetValue(BuildCellStateKey(entryId, columnKey), out var state);
        var comment = state switch
        {
            MissingProjectCellState missing => missing.NoteText,
            TadilatCellState tadilat => tadilat.NoteText,
            YibfCellState yibf => yibf.NoteText,
            KarotCellState karot => karot.NoteText,
            _ => string.Empty
        };

        var backgroundColor = state switch
        {
            MissingProjectCellState missing => FirstNonEmpty(missing.BackgroundColor, fallbackBackgroundColor, string.Empty),
            TadilatCellState tadilat => FirstNonEmpty(tadilat.BackgroundColor, fallbackBackgroundColor, string.Empty),
            YibfCellState yibf => FirstNonEmpty(yibf.BackgroundColor, fallbackBackgroundColor, string.Empty),
            _ => fallbackBackgroundColor ?? string.Empty
        };

        return CreateCell(value, comment, backgroundColor);
    }

    private static Dictionary<string, TState> BuildCellStateLookup<TState>(IEnumerable<TState> states, Func<TState, Guid> entryIdSelector, Func<TState, string> columnKeySelector)
        where TState : class
        => states.ToDictionary(
            state => BuildCellStateKey(entryIdSelector(state), columnKeySelector(state)),
            state => state,
            StringComparer.OrdinalIgnoreCase);

    private static string BuildCellStateKey(Guid entryId, string columnKey)
        => $"{entryId:N}:{columnKey}";

    private static string BuildTaskNotesComment(TaskItem task)
        => string.Join(
            Environment.NewLine,
            task.Notes
                .Select(note => note.Text?.Trim())
                .Where(text => !string.IsNullOrWhiteSpace(text)));

    private static string GetKarotStatusLabel(KarotStatus status)
        => status switch
        {
            KarotStatus.KarotAlinacak => "Karot Alınacak",
            KarotStatus.KarotAlindiSonucBekleniyor => "Karot Alındı Sonuç Bekleniyor",
            KarotStatus.KarotAlindiOlumlu => "Karot Alındı Olumlu",
            KarotStatus.KarotAlindiOlumsuz => "Karot Alındı Olumsuz",
            _ => string.Empty
        };

    private static string GetKarotRowBackgroundColor(KarotStatus status)
        => status switch
        {
            KarotStatus.KarotAlinacak => "#FFFFE1E1",
            KarotStatus.KarotAlindiSonucBekleniyor => "#FFFFF6CC",
            KarotStatus.KarotAlindiOlumlu => "#FFE1EEFF",
            KarotStatus.KarotAlindiOlumsuz => "#FFFFE1E1",
            _ => string.Empty
        };

    private static string GetYibfEventColorLabel(string? backgroundColor)
    {
        if (string.Equals(backgroundColor, "#FFFF0000", StringComparison.OrdinalIgnoreCase))
        {
            return "ACİL";
        }

        if (string.Equals(backgroundColor, "#FFFFFF00", StringComparison.OrdinalIgnoreCase))
        {
            return "DİKKAT";
        }

        return string.IsNullOrWhiteSpace(backgroundColor) ? "-" : "RENKLİ";
    }

    private async Task ImportExcelAsync()
        => await RunExclusiveOperationAsync(async () =>
        {
            try
            {
                var path = _fileDialogService.ShowOpenDialog("Excel içe aktar", "Excel (*.xlsx)|*.xlsx");
                if (string.IsNullOrWhiteSpace(path))
                {
                    return;
                }

                var imported = (await _importExportService.ImportExcelAsync(path)).ToList();
                if (imported.Count == 0)
                {
                    _notificationService.ShowToast("İçe aktarılacak geçerli kayıt bulunamadı.", ToastType.Warning);
                    return;
                }

                _suppressTaskDirtyTracking = true;
                _ = MigrateTaskDescriptionsToNotes(imported);
                var urgent = imported.Where(task => task.BoardType == TaskBoardType.Acil).ToList();
                var general = imported.Where(task => task.BoardType == TaskBoardType.Genel).ToList();

                UrgentBoard.ReplaceAll(urgent);
                GeneralBoard.ReplaceAll(general);

                _suppressTaskDirtyTracking = false;
                HasUnsavedChanges = true;
                _notificationService.ShowToast($"{imported.Count} kayıt içe aktarıldı.", ToastType.Success);
            }
            catch (Exception ex)
            {
                _suppressTaskDirtyTracking = false;
                _notificationService.ShowToast($"Excel içe aktarma hatası: {ex.Message}", ToastType.Error);
            }
        });

    private async Task ExportPdfAsync()
    {
        try
        {
            var path = _fileDialogService.ShowSaveDialog("PDF dışa aktar", "PDF (*.pdf)|*.pdf", ".pdf");
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            await _importExportService.ExportPdfAsync(AllTasks(), path);
            _notificationService.ShowToast("PDF raporu oluşturuldu.", ToastType.Success);
        }
        catch (Exception ex)
        {
            _notificationService.ShowToast($"PDF dışa aktarma hatası: {ex.Message}", ToastType.Error);
        }
    }

    private async Task ExportUrgentPngAsync(UIElement? visual)
    {
        await ExportPngInternalAsync(visual, "Acil işler");
    }

    private async Task ExportGeneralPngAsync(UIElement? visual)
    {
        await ExportPngInternalAsync(visual, "Genel işler");
    }

    private async Task ExportActionListPngAsync(UIElement? visual)
    {
        if (visual is null)
        {
            _notificationService.ShowToast("PNG dışa aktarma için tablo bulunamadı.", ToastType.Warning);
            return;
        }

        try
        {
            var path = _fileDialogService.ShowSaveDialog("PNG dışa aktar", "PNG (*.png)|*.png", ".png");
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            await _importExportService.ExportScrollablePngAsync(visual, path);
            _notificationService.ShowToast("Aksiyon tablosu PNG olarak kaydedildi.", ToastType.Success);
        }
        catch (Exception ex)
        {
            _notificationService.ShowToast($"PNG dışa aktarma hatası: {ex.Message}", ToastType.Error);
        }
    }

    private async Task ExportMissingProjectPngAsync(UIElement? visual)
    {
        await ExportPngInternalAsync(visual, "Eksik proje listesi");
    }

    private async Task ExportKarotPngAsync(UIElement? visual)
    {
        await ExportPngInternalAsync(visual, "Karot listesi");
    }

    private async Task ExportYibfIsTakibiPngAsync(UIElement? visual)
    {
        await ExportPngInternalAsync(visual, "YİBF iş takibi listesi");
    }

    private async Task ExportPngInternalAsync(UIElement? visual, string title)
    {
        if (visual is null)
        {
            _notificationService.ShowToast("PNG dışa aktarma için tablo bulunamadı.", ToastType.Warning);
            return;
        }

        try
        {
            var path = _fileDialogService.ShowSaveDialog("PNG dışa aktar", "PNG (*.png)|*.png", ".png");
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            await _importExportService.ExportScrollablePngAsync(visual, path);
            _notificationService.ShowToast($"{title} tablosu PNG olarak kaydedildi.", ToastType.Success);
        }
        catch (Exception ex)
        {
            _notificationService.ShowToast($"PNG dışa aktarma hatası: {ex.Message}", ToastType.Error);
        }
    }

    private void Undo()
    {
        _undoRedoService.Undo();
    }

    private void Redo()
    {
        _undoRedoService.Redo();
    }

    // MoveTaskToBoard moved to MainViewModel.TaskManagement.cs

    private void SelectSearchResult(SearchResultItem? item)
    {
        if (item is null)
        {
            return;
        }

        switch (item.Kind)
        {
            case SearchResultKind.GeneralTask:
            {
                if (item.BoardType is null)
                {
                    break;
                }

                SelectMainTab(MainNavigationTab.GenelIsTakibi);
                var board = GetBoard(item.BoardType.Value);
                FocusBoard(board.BoardType);
                board.SelectedTask = board.Tasks.FirstOrDefault(task => task.Id == item.ItemId);
                break;
            }
            case SearchResultKind.ActionEntry:
            {
                var entry = ActionModule.AksiyonEntries.Concat(ActionModule.AksiyonaEkleneceklerEntries).FirstOrDefault(x => x.Id == item.ItemId);
                if (entry is null)
                {
                    break;
                }

                SelectMainTab(MainNavigationTab.Aksiyon);
                ActionModule.SelectedSubTab = entry.Category == ActionEntryCategory.Aksiyon ? ActionSubTab.Aksiyon : ActionSubTab.AksiyonaEklenecekler;
                ActionModule.SelectedEntry = entry;
                break;
            }
            case SearchResultKind.MissingProjectEntry:
            {
                var entry = MissingProjectModule.Entries.FirstOrDefault(x => x.Id == item.ItemId);
                if (entry is null)
                {
                    break;
                }

                SelectMainTab(MainNavigationTab.EksikProje);
                MissingProjectModule.SelectedEntry = entry;
                break;
            }
            case SearchResultKind.KarotEntry:
            {
                var entry = KarotModule.Entries.FirstOrDefault(x => x.Id == item.ItemId);
                if (entry is null)
                {
                    break;
                }

                SelectMainTab(MainNavigationTab.KarotTakibi);
                KarotModule.SelectedSubTab = entry.Status == KarotStatus.KarotAlinacak ? KarotSubTab.Bekleyen : KarotSubTab.Yapilan;
                KarotModule.SelectedEntry = entry;
                break;
            }
            case SearchResultKind.TadilatEntry:
            {
                var entry = TadilatModule.AktifEntries.Concat(TadilatModule.BitenEntries).FirstOrDefault(x => x.Id == item.ItemId);
                if (entry is null)
                {
                    break;
                }

                SelectMainTab(MainNavigationTab.TadilatTakibi);
                TadilatModule.SelectedSubTab = entry.SubTab;
                TadilatModule.SelectedEntry = entry;
                break;
            }
            case SearchResultKind.YibfAnaBilgiEntry:
            {
                var entry = YibfModule.AnaBilgiEntries.FirstOrDefault(x => x.Id == item.ItemId);
                if (entry is null)
                {
                    break;
                }

                SelectMainTab(MainNavigationTab.YibfAnaBilgi);
                YibfModule.SelectedAnaBilgiEntry = entry;
                YibfModule.SelectedAnaBilgiEvent = null;
                break;
            }
            case SearchResultKind.YibfAnaBilgiEvent:
            {
                var entry = YibfModule.AnaBilgiEntries.FirstOrDefault(x => x.Id == item.ParentItemId);
                var eventItem = YibfModule.AnaBilgiEvents.FirstOrDefault(x => x.Id == item.ItemId);
                if (entry is null || eventItem is null)
                {
                    break;
                }

                SelectMainTab(MainNavigationTab.YibfAnaBilgi);
                YibfModule.SelectedAnaBilgiEntry = entry;
                YibfModule.SelectedAnaBilgiEvent = eventItem;
                break;
            }
            case SearchResultKind.YibfIsTakibiEntry:
            {
                var entry = YibfModule.IsTakibiEntries.FirstOrDefault(x => x.Id == item.ItemId);
                if (entry is null)
                {
                    break;
                }

                SelectMainTab(MainNavigationTab.YibfIsTakibi);
                YibfModule.SelectedIsTakibiEntry = entry;
                YibfModule.RequestIsTakibiScroll(entry.Id);
                break;
            }
        }

        SearchOverlay.Close();
    }

    private void SelectEksikItem(EksikItemViewModel? item)
    {
        SelectSearchResult(item?.NavigationTarget);
    }

    // FocusBoard and GetBoard moved to MainViewModel.TaskManagement.cs

    private async Task<bool> PersistGeneralTasksAsync(bool showSuccessToast)
    {
        if (_isSavingGeneralTasks)
        {
            return true;
        }

        try
        {
            _isSavingGeneralTasks = true;
            _suppressTaskDirtyTracking = true;
            var currentTasks = AllTasks().Select(task => task.Clone()).ToList();
            var currentIds = currentTasks.Select(task => task.Id).ToHashSet();
            var storedTasks = await _taskRepository.GetAllAsync();

            foreach (var staleTask in storedTasks.Where(task => !currentIds.Contains(task.Id)))
            {
                await _taskRepository.DeleteAsync(staleTask.Id);
            }

            await _taskRepository.SaveManyAsync(currentTasks);
            HasUnsavedChanges = false;
            RefreshDashboard();
            NotifySelectionCommands();
            if (showSuccessToast)
            {
                _notificationService.ShowToast("Değişiklikler kaydedildi.", ToastType.Success, TimeSpan.FromSeconds(2));
            }

            return true;
        }
        catch (Exception ex)
        {
            _notificationService.ShowToast($"Kayıt hatası: {ex.Message}", ToastType.Error);
            return false;
        }
        finally
        {
            _suppressTaskDirtyTracking = false;
            _isSavingGeneralTasks = false;
        }
    }

    private void RefreshDashboard()
    {
        Dashboard.Refresh(AllTasks());
    }

    private void OnBoardSelectedTaskChanged(object? sender, TaskItem? task)
    {
        if (sender is TaskBoardViewModel board && task is not null)
        {
            _activeBoard = board;
        }

        DetailPanel.CurrentTask = task;
        OnPropertyChanged(nameof(SelectedTask));
        NotifySelectionCommands();
    }

    private void OnBoardTasksChanged(object? sender, EventArgs e)
    {
        InvalidateSearchCorpus();
        RefreshDashboard();
        RefreshAcilIsOzet();
        MoveTaskUpCommand.NotifyCanExecuteChanged();
        MoveTaskDownCommand.NotifyCanExecuteChanged();

        if (!_isInitialized || _suppressTaskDirtyTracking)
        {
            return;
        }

        MarkTaskDirty();
    }

    // MarkTaskDirty moved to MainViewModel.TaskManagement.cs

    // CommitGeneralEdit moved to MainViewModel.TaskManagement.cs

    private void NotifySelectionCommands()
    {
        DeleteGeneralTaskCommand.NotifyCanExecuteChanged();
        DeleteUrgentTaskCommand.NotifyCanExecuteChanged();
        DeleteSelectedTaskCommand.NotifyCanExecuteChanged();
        DeleteTaskCommand.NotifyCanExecuteChanged();
        MoveTaskUpCommand.NotifyCanExecuteChanged();
        MoveTaskDownCommand.NotifyCanExecuteChanged();
        CopyTaskCommand.NotifyCanExecuteChanged();
        CopyTaskFromContextCommand.NotifyCanExecuteChanged();
        PasteTaskCommand.NotifyCanExecuteChanged();
        PasteTaskToBoardCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(SelectedTask));
    }

    private void OnSearchQueryChanged(object? sender, string query)
    {
        if (!SearchOverlay.IsOpen || !SearchOverlay.IsClassicMode)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(query))
        {
            SearchOverlay.SetResults(Array.Empty<SearchResultItem>());
            UrgentBoard.FilterText = string.Empty;
            GeneralBoard.FilterText = string.Empty;
            return;
        }

        var results = _searchService.SearchAll(GetSearchCorpus(), query, SearchOverlay.SelectedScope);
        SearchOverlay.SetResults(results);

        UrgentBoard.FilterText = query;
        GeneralBoard.FilterText = query;
    }

    private void OnSearchScopeChanged(object? sender, SearchScope scope)
    {
        if (!SearchOverlay.IsOpen)
        {
            return;
        }

        OnSearchQueryChanged(this, SearchOverlay.Query);
    }

    private void OnSearchModeChanged(object? sender, SearchOverlayMode mode)
    {
        if (!SearchOverlay.IsOpen)
        {
            return;
        }

        if (mode == SearchOverlayMode.Classic)
        {
            OnSearchQueryChanged(this, SearchOverlay.Query);
        }
    }

    private void RunContextQuery()
    {
        var result = BuildContextQueryResult(SearchOverlay.AssistantQuery);
        SearchOverlay.SetAssistantResult(result);
    }

    private static SearchScope MapSearchScope(MainNavigationTab tab)
        => tab switch
        {
            MainNavigationTab.GenelIsTakibi => SearchScope.GenelIsTakibi,
            MainNavigationTab.Aksiyon => SearchScope.Aksiyon,
            MainNavigationTab.EksikProje => SearchScope.EksikProje,
            MainNavigationTab.KarotTakibi => SearchScope.KarotTakibi,
            MainNavigationTab.TadilatTakibi => SearchScope.TadilatTakibi,
            MainNavigationTab.YibfAnaBilgi => SearchScope.YibfAnaBilgi,
            MainNavigationTab.YibfIsTakibi => SearchScope.YibfIsTakibi,
            MainNavigationTab.YibfBekleyenIsler => SearchScope.YibfAnaBilgi,
            MainNavigationTab.TumEksikler => SearchScope.All,
            MainNavigationTab.Ayarlar => SearchScope.All,
            _ => SearchScope.All
        };

    private IReadOnlyList<SearchResultItem> BuildSearchCorpusCore()
    {
        var results = new List<SearchResultItem>();
        var aliasLookup = BuildSearchAliasLookup();
        var yibfParentLookup = YibfModule.AnaBilgiEntries.ToDictionary(x => x.Id);

        foreach (var task in AllTasks())
        {
            var rawSearchText = CombineSearchText(task.Title, task.Description, string.Join(' ', task.Notes.Select(note => note.Text)));
            results.Add(new SearchResultItem
            {
                Kind = SearchResultKind.GeneralTask,
                TargetTab = MainNavigationTab.GenelIsTakibi,
                ItemId = task.Id,
                BoardType = task.BoardType,
                BoardLabel = task.BoardType == TaskBoardType.Acil ? "Genel İş Takibi / Acil İşler" : "Genel İş Takibi / Genel İşler",
                Title = string.IsNullOrWhiteSpace(task.Title) ? "(Başlıksız görev)" : task.Title,
                Summary = FirstNonEmpty(task.Description, task.Notes.FirstOrDefault()?.Text, task.Title),
                SearchText = SearchContextAliasBuilder.EnrichSearchText(rawSearchText, aliasLookup),
                RawSearchText = rawSearchText
            });
        }

        foreach (var entry in ActionModule.AksiyonEntries)
        {
            var rawSearchText = CombineSearchText(entry.District, entry.OwnerParcelText, entry.WorkText);
            results.Add(new SearchResultItem
            {
                Kind = SearchResultKind.ActionEntry,
                TargetTab = MainNavigationTab.Aksiyon,
                ItemId = entry.Id,
                BoardLabel = "Aksiyon / Aksiyon",
                Title = FirstNonEmpty(entry.OwnerParcelText, entry.WorkText, "(Boş aksiyon kaydı)"),
                Summary = FirstNonEmpty(entry.WorkText, entry.OwnerParcelText, entry.District),
                SearchText = SearchContextAliasBuilder.EnrichSearchText(rawSearchText, aliasLookup),
                RawSearchText = rawSearchText
            });
        }

        foreach (var entry in ActionModule.AksiyonaEkleneceklerEntries)
        {
            var rawSearchText = CombineSearchText(entry.District, entry.OwnerParcelText, entry.WorkText);
            results.Add(new SearchResultItem
            {
                Kind = SearchResultKind.ActionEntry,
                TargetTab = MainNavigationTab.Aksiyon,
                ItemId = entry.Id,
                BoardLabel = "Aksiyon / Aksiyona Eklenecekler",
                Title = FirstNonEmpty(entry.OwnerParcelText, entry.WorkText, "(Boş aksiyon kaydı)"),
                Summary = FirstNonEmpty(entry.WorkText, entry.OwnerParcelText, entry.District),
                SearchText = SearchContextAliasBuilder.EnrichSearchText(rawSearchText, aliasLookup),
                RawSearchText = rawSearchText
            });
        }

        foreach (var entry in MissingProjectModule.Entries)
        {
            var rawSearchText = CombineSearchText(entry.AdaParsel, entry.YapiSahibi, entry.RecordMediumText, entry.MissingProjectText, entry.Description);
            results.Add(new SearchResultItem
            {
                Kind = SearchResultKind.MissingProjectEntry,
                TargetTab = MainNavigationTab.EksikProje,
                ItemId = entry.Id,
                BoardLabel = "Eksik Proje",
                Title = FirstNonEmpty(entry.AdaParsel, entry.YapiSahibi, "(Boş eksik proje kaydı)"),
                Summary = FirstNonEmpty(entry.MissingProjectText, entry.Description, entry.RecordMediumText),
                SearchText = SearchContextAliasBuilder.EnrichSearchText(rawSearchText, aliasLookup),
                RawSearchText = rawSearchText
            });
        }

        foreach (var entry in KarotModule.Entries)
        {
            var rawSearchText = CombineSearchText(entry.YibfNo, entry.AdaParsel, entry.YapiSahibi, entry.Muteahhit, entry.KatBilgisi, entry.BetonSinifi, entry.TwentyEightDayResult, entry.BetonFirmasi, entry.Laboratuvar, entry.Aciklama);
            results.Add(new SearchResultItem
            {
                Kind = SearchResultKind.KarotEntry,
                TargetTab = MainNavigationTab.KarotTakibi,
                ItemId = entry.Id,
                BoardLabel = entry.Status == KarotStatus.KarotAlinacak ? "Karot / Bekleyen" : "Karot / Yapılan",
                Title = FirstNonEmpty(entry.YibfNo, entry.AdaParsel, "(Boş karot kaydı)"),
                Summary = FirstNonEmpty(entry.AdaParsel, entry.YapiSahibi, entry.Aciklama),
                SearchText = SearchContextAliasBuilder.EnrichSearchText(rawSearchText, aliasLookup),
                RawSearchText = rawSearchText
            });
        }

        foreach (var entry in TadilatModule.AktifEntries)
        {
            var rawSearchText = CombineSearchText(entry.District, entry.JobName, entry.ProjectType, entry.DigitalReceived, entry.InspectorApproved, entry.OutputAndReportArrived, entry.OfficialLetterSubmitted, entry.ArchivedFromMunicipality, entry.Description1, entry.Description2);
            results.Add(new SearchResultItem
            {
                Kind = SearchResultKind.TadilatEntry,
                TargetTab = MainNavigationTab.TadilatTakibi,
                ItemId = entry.Id,
                BoardLabel = "Tadilat / Aktif",
                Title = FirstNonEmpty(entry.JobName, entry.ProjectType, "(Boş tadilat kaydı)"),
                Summary = FirstNonEmpty(entry.Description1, entry.Description2, entry.District),
                SearchText = SearchContextAliasBuilder.EnrichSearchText(rawSearchText, aliasLookup),
                RawSearchText = rawSearchText
            });
        }

        foreach (var entry in TadilatModule.BitenEntries)
        {
            var rawSearchText = CombineSearchText(entry.District, entry.JobName, entry.ProjectType, entry.DigitalReceived, entry.InspectorApproved, entry.OutputAndReportArrived, entry.OfficialLetterSubmitted, entry.ArchivedFromMunicipality, entry.Description1, entry.Description2);
            results.Add(new SearchResultItem
            {
                Kind = SearchResultKind.TadilatEntry,
                TargetTab = MainNavigationTab.TadilatTakibi,
                ItemId = entry.Id,
                BoardLabel = "Tadilat / Biten",
                Title = FirstNonEmpty(entry.JobName, entry.ProjectType, "(Boş tadilat kaydı)"),
                Summary = FirstNonEmpty(entry.Description1, entry.Description2, entry.District),
                SearchText = SearchContextAliasBuilder.EnrichSearchText(rawSearchText, aliasLookup),
                RawSearchText = rawSearchText
            });
        }

        foreach (var entry in YibfModule.AnaBilgiEntries)
        {
            var rawSearchText = CombineSearchText(entry.AdaParsel, entry.YibfNo, entry.Idare, entry.YapiSahibi, entry.Muteahhit);
            results.Add(new SearchResultItem
            {
                Kind = SearchResultKind.YibfAnaBilgiEntry,
                TargetTab = MainNavigationTab.YibfAnaBilgi,
                ItemId = entry.Id,
                BoardLabel = "YİBF Ana Bilgi",
                Title = FirstNonEmpty(entry.AdaParsel, entry.YibfNo, "(Boş YİBF kaydı)"),
                Summary = FirstNonEmpty(entry.YapiSahibi, entry.Muteahhit, entry.Idare),
                SearchText = SearchContextAliasBuilder.EnrichSearchText(rawSearchText, aliasLookup),
                RawSearchText = rawSearchText
            });
        }

        foreach (var eventItem in YibfModule.AnaBilgiEvents)
        {
            yibfParentLookup.TryGetValue(eventItem.EntryId, out var parent);
            var rawSearchText = CombineSearchText(parent?.AdaParsel, parent?.YibfNo, parent?.Idare, parent?.YapiSahibi, parent?.Muteahhit, eventItem.Description, eventItem.NoteText, eventItem.EventDate?.ToString("dd.MM.yyyy"));
            results.Add(new SearchResultItem
            {
                Kind = SearchResultKind.YibfAnaBilgiEvent,
                TargetTab = MainNavigationTab.YibfAnaBilgi,
                ItemId = eventItem.Id,
                ParentItemId = eventItem.EntryId,
                BoardLabel = "YİBF Ana Bilgi / Olay Akışı",
                Title = FirstNonEmpty(eventItem.Description, parent?.AdaParsel, "(Boş olay)"),
                Summary = FirstNonEmpty(parent?.AdaParsel, parent?.YibfNo, eventItem.NoteText),
                SearchText = SearchContextAliasBuilder.EnrichSearchText(rawSearchText, aliasLookup),
                RawSearchText = rawSearchText
            });
        }

        foreach (var entry in YibfModule.IsTakibiEntries)
        {
            var rawSearchText = CombineSearchText(entry.JobName, entry.MuellifBilgileriGeldiMi, entry.DenetciAtamalariYapildiMi, entry.TumProjelerinDijitaliVarMi, entry.EvraklarTamMi, entry.YibfSozlesmeHazirlandiMi, entry.DekontAlindiMi, entry.RuhsatBasvurusuYapildiMi, entry.RuhsatNushasiAlindiMi, entry.IsyeriTeslimTutangiHazirlandiMi, entry.IsgYazisiHazirlandiMi, entry.SaglikGuvenlikPlaniGeldiMi, entry.TemelTopraklamaTutanagiHazirlandiMi);
            results.Add(new SearchResultItem
            {
                Kind = SearchResultKind.YibfIsTakibiEntry,
                TargetTab = MainNavigationTab.YibfIsTakibi,
                ItemId = entry.Id,
                BoardLabel = "YİBF İş Takibi",
                Title = FirstNonEmpty(entry.JobName, "(Boş YİBF iş takibi kaydı)"),
                Summary = FirstNonEmpty(entry.EvraklarTamMi, entry.RuhsatBasvurusuYapildiMi, entry.MuellifBilgileriGeldiMi),
                SearchText = SearchContextAliasBuilder.EnrichSearchText(rawSearchText, aliasLookup),
                RawSearchText = rawSearchText
            });
        }

        return results;
    }

    private QueryInsightResult BuildContextQueryResult(string? question)
    {
        var trimmedQuestion = question?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(trimmedQuestion))
        {
            return new QueryInsightResult
            {
                AnswerText = "Lütfen ada parsel, yapı sahibi, müteahhit ya da YİBF no içeren bir soru yazın."
            };
        }

        var corpus = GetSearchCorpus();
        var match = _contextQueryService.ExtractMatch(trimmedQuestion, corpus);
        if (!match.HasMatch)
        {
            return new QueryInsightResult
            {
                AnswerText = "Sorgudan aranacak anahtar çıkarılamadı. Ada parsel, yapı sahibi veya YİBF no ile tekrar deneyin."
            };
        }

        var result = _contextInsightBuilder.Build(
            match,
            corpus,
            AllTasks(),
            ActionModule.AksiyonEntries.Concat(ActionModule.AksiyonaEkleneceklerEntries).ToList(),
            MissingProjectModule.Entries.ToList(),
            KarotModule.Entries.ToList(),
            TadilatModule.AktifEntries.ToList(),
            TadilatModule.CellStates.ToList(),
            YibfModule.AnaBilgiEntries.ToList(),
            YibfModule.AnaBilgiEvents.ToList(),
            YibfModule.IsTakibiEntries.ToList(),
            YibfModule.CellStates.ToList());

        return new QueryInsightResult
        {
            MatchedKey = result.MatchedKey,
            SummaryText = result.SummaryText,
            ExplanationText = ContextQueryExplanationBuilder.Build(match)
                ,
            AnswerText = result.AnswerText,
            Sections = result.Sections,
            Sources = result.Sources
        };
    }

    private static string FirstNonEmpty(params string?[] values)
        => StringHelpers.FirstNonEmpty(values);

    private SearchContextAliasLookup BuildSearchAliasLookup()
    {
        var seeds = MissingProjectModule.Entries
            .Select(entry => new SearchContextIdentitySeed(entry.AdaParsel, entry.YapiSahibi))
            .Concat(KarotModule.Entries.Select(entry => new SearchContextIdentitySeed(entry.AdaParsel, entry.YapiSahibi, entry.Muteahhit, entry.YibfNo)))
            .Concat(YibfModule.AnaBilgiEntries.Select(entry => new SearchContextIdentitySeed(entry.AdaParsel, entry.YapiSahibi, entry.Muteahhit, entry.YibfNo)))
            .ToList();

        return SearchContextAliasBuilder.BuildAliasLookup(seeds);
    }

    private static string CombineSearchText(params string?[] values)
        => StringHelpers.CombineNonEmpty(values);
}
