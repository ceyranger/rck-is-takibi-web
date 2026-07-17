using Microsoft.Extensions.DependencyInjection;
using QuestPDF.Infrastructure;
using RizaCanKilicIsTakibi.Helpers;
using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services;
using RizaCanKilicIsTakibi.Services.Abstractions;
using RizaCanKilicIsTakibi.ViewModels;
using RizaCanKilicIsTakibi.Views;
using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace RizaCanKilicIsTakibi;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;
    private bool _isFatalShutdownRequested;
    private string? _dbPath;
    private AppSettingsLoadResult? _settingsLoadResult;

    internal bool IsFatalShutdownRequested => _isFatalShutdownRequested;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnMainWindowClose;

        QuestPDF.Settings.License = LicenseType.Community;

        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        RegisterGlobalExceptionHandling();

        try
        {
            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            EnsureWindowVisibleOnCurrentScreen(mainWindow);
            MainWindow = mainWindow;
            mainWindow.Show();
            mainWindow.WindowState = WindowState.Normal;
            mainWindow.Topmost = true;
            mainWindow.Topmost = false;
            mainWindow.Activate();
            NotifySettingsRecoveryIfNeeded();
        }
        catch (Exception ex)
        {
            HandleFatalException(ex);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        
        if (!string.IsNullOrWhiteSpace(_dbPath))
        {
            var tempConnectionString = SqliteConnectionSettings.BuildConnectionString(_dbPath);
            SqliteConnectionSettings.TruncateWal(tempConnectionString);
        }

        base.OnExit(e);
    }

    private void ConfigureServices(IServiceCollection services)
    {
        var pathService = new PathService();
        AppExceptionHandler.Initialize(pathService);
        
        _dbPath = pathService.DatabasePath;
        var settingsService = new AppSettingsService(pathService.SettingsPath);
        var lastSaveMetadataService = new LastSaveMetadataService(pathService.LastSaveMetadataPath);
        _settingsLoadResult = settingsService.Load();
        var settings = _settingsLoadResult.Settings;

        // Repository'leri eager initialization ile oluştur
        var taskRepository = new SqliteTaskRepository(_dbPath);
        var actionRepository = new SqliteActionRepository(_dbPath);
        var missingProjectRepository = new SqliteMissingProjectRepository(_dbPath);
        var karotRepository = new SqliteKarotRepository(_dbPath);
        var tadilatRepository = new SqliteTadilatRepository(_dbPath);
        var yibfRepository = new SqliteYibfRepository(_dbPath);
        var quickTaskTemplateRepository = new SqliteQuickTaskTemplateRepository(_dbPath);

        services.AddSingleton(pathService);
        services.AddSingleton<IAppSettingsService>(settingsService);
        services.AddSingleton<ILastSaveMetadataService>(lastSaveMetadataService);
        services.AddSingleton(_settingsLoadResult);
        services.AddSingleton(settings);
        services.AddSingleton<IClipboardService, ClipboardService>();
        services.AddSingleton<ITaskRepository>(taskRepository);
        services.AddSingleton<IActionRepository>(actionRepository);
        services.AddSingleton<IMissingProjectRepository>(missingProjectRepository);
        services.AddSingleton<IKarotRepository>(karotRepository);
        services.AddSingleton<ITadilatRepository>(tadilatRepository);
        services.AddSingleton<IYibfRepository>(yibfRepository);
        services.AddSingleton<IQuickTaskTemplateRepository>(quickTaskTemplateRepository);
        services.AddSingleton<IBackupService>(_ => new BackupService(pathService.BackupDirectory));
        services.AddSingleton<IImportExportService, ImportExportService>();
        services.AddSingleton<IGenelIsTakibiExcelImportService, GenelIsTakibiExcelImportService>();
        services.AddSingleton<ITadilatImportService, TadilatExcelImportService>();
        services.AddSingleton<IYibfImportService, YibfExcelImportService>();
        services.AddSingleton<INotificationService, NotificationService>();
        services.AddSingleton<IConfirmationService, ConfirmationService>();
        services.AddSingleton<IUndoRedoService, UndoRedoService>();
        services.AddSingleton<ISearchService, SearchService>();
        services.AddSingleton<IContextQueryService, ContextQueryService>();
        services.AddSingleton<IContextInsightBuilder, ContextInsightBuilder>();
        services.AddSingleton<IFileDialogService, FileDialogService>();
        services.AddSingleton<IAddActionEntryDialogService, AddActionEntryDialogService>();
        services.AddSingleton<IKarotStatusDialogService, KarotStatusDialogService>();
        services.AddSingleton<ITadilatCellNoteDialogService, TadilatCellNoteDialogService>();
        services.AddSingleton<IYibfAnaBilgiEventDialogService, YibfAnaBilgiEventDialogService>();
        services.AddSingleton<IYibfAnaBilgiEntryDialogService, YibfAnaBilgiEntryDialogService>();
        services.AddSingleton<IQuickTaskTemplateDialogService, QuickTaskTemplateDialogService>();

        services.AddSingleton<DashboardViewModel>();
        services.AddSingleton<SearchOverlayViewModel>();
        services.AddSingleton<TaskDetailViewModel>();
        services.AddSingleton<ToastHostViewModel>();
        services.AddSingleton<ActionModuleViewModel>();
        services.AddSingleton<MissingProjectModuleViewModel>();
        services.AddSingleton<KarotModuleViewModel>();
        services.AddSingleton<TadilatModuleViewModel>();
        services.AddSingleton<YibfModuleViewModel>();
        services.AddSingleton<MainViewModel>();

        services.AddSingleton<MainWindow>();
    }

    private void RegisterGlobalExceptionHandling()
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        HandleException(e.Exception);
        e.Handled = true;
        RequestFatalShutdown();
    }

    private void OnCurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            HandleException(ex);
        }

        RequestFatalShutdown();
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        HandleException(e.Exception);
        e.SetObserved();
    }

    private void HandleException(Exception exception)
    {
        var notifier = _serviceProvider?.GetService<INotificationService>();
        AppExceptionHandler.Handle(exception, notifier);
    }

    private void HandleFatalException(Exception exception)
    {
        HandleException(exception);
        RequestFatalShutdown();
    }

    private void NotifySettingsRecoveryIfNeeded()
    {
        var notifier = _serviceProvider?.GetService<INotificationService>();
        AppSettingsRecoveryNotifier.NotifyIfNeeded(_settingsLoadResult, notifier);
    }

    private void RequestFatalShutdown()
    {
        if (_isFatalShutdownRequested)
        {
            return;
        }

        _isFatalShutdownRequested = true;
        if (Dispatcher.CheckAccess())
        {
            Shutdown(-1);
            return;
        }

        _ = Dispatcher.BeginInvoke(new Action(() => Shutdown(-1)));
    }

    private static void EnsureWindowVisibleOnCurrentScreen(Window window)
    {
        var workArea = SystemParameters.WorkArea;
        const double safeMargin = 24d;
        const double fallbackWidth = 1024d;
        const double fallbackHeight = 640d;

        var maxWidth = Math.Max(fallbackWidth, workArea.Width - safeMargin);
        var maxHeight = Math.Max(fallbackHeight, workArea.Height - safeMargin);

        window.WindowStartupLocation = WindowStartupLocation.Manual;
        window.Width = Math.Min(window.Width, maxWidth);
        window.Height = Math.Min(window.Height, maxHeight);
        window.Left = workArea.Left + Math.Max(0, (workArea.Width - window.Width) / 2);
        window.Top = workArea.Top + Math.Max(0, (workArea.Height - window.Height) / 2);
    }
}
