using CommunityToolkit.Mvvm.Input;
using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services;
using RizaCanKilicIsTakibi.Services.Abstractions;
using System.IO;

namespace RizaCanKilicIsTakibi.ViewModels;

public sealed partial class MainViewModel
{
    private const string DefaultWebPin = "271179";
    private const string WebViewSiteUrl = "https://ceyranger.github.io/rck-is-takibi-web/";
    private readonly WebViewGitSyncService _webViewGitSyncService = new();
    private string _lastWebViewExportStatus = "Henüz dışa aktarılmadı.";

    private void InitializeWebViewExportFeature()
    {
        _settings.WebViewRepoRoot = WebViewRepoPaths.NormalizeRepoRoot(_settings.WebViewRepoRoot);
        ExportWebViewNowCommand = new AsyncRelayCommand(ExportWebViewNowAsync, CanExportWebViewNow);
        RefreshWebViewExportStatusFromDisk();
    }

    public AsyncRelayCommand ExportWebViewNowCommand { get; private set; } = null!;

    public bool WebViewExportEnabled
    {
        get => _settings.WebViewExportEnabled;
        set
        {
            if (_settings.WebViewExportEnabled == value)
            {
                return;
            }

            _settings.WebViewExportEnabled = value;
            OnPropertyChanged();
            HasUnsavedSettings = true;
            ExportWebViewNowCommand.NotifyCanExecuteChanged();
        }
    }

    public bool WebViewGitSyncEnabled
    {
        get => _settings.WebViewGitSyncEnabled;
        set
        {
            if (_settings.WebViewGitSyncEnabled == value)
            {
                return;
            }

            _settings.WebViewGitSyncEnabled = value;
            OnPropertyChanged();
            HasUnsavedSettings = true;
        }
    }

    public string WebViewRepoRoot
    {
        get => _settings.WebViewRepoRoot;
        set
        {
            var normalized = WebViewRepoPaths.NormalizeRepoRoot(value);
            if (string.Equals(_settings.WebViewRepoRoot, normalized, StringComparison.Ordinal))
            {
                return;
            }

            _settings.WebViewRepoRoot = normalized;
            OnPropertyChanged();
            OnPropertyChanged(nameof(WebViewExportDirectoryDisplay));
            HasUnsavedSettings = true;
            ExportWebViewNowCommand.NotifyCanExecuteChanged();
            RefreshWebViewExportStatusFromDisk();
        }
    }

    public string WebViewExportDirectoryDisplay
        => WebViewRepoPaths.GetExportFilePath(WebViewRepoRoot);

    public string LastWebViewExportStatus
    {
        get => _lastWebViewExportStatus;
        private set => SetProperty(ref _lastWebViewExportStatus, value);
    }

    private string WebViewExportDirectory => WebViewRepoPaths.GetExportDirectory(WebViewRepoRoot);

    private bool CanExportWebViewNow()
        => _webViewSnapshotService is not null
           && WebViewRepoPaths.ValidateRepoRoot(WebViewRepoRoot).IsValid;

    private async Task ExportWebViewNowAsync()
    {
        var result = await TryExportWebViewSnapshotAsync(showSuccessToast: true);
        if (result is null && CanExportWebViewNow())
        {
            _notificationService.ShowToast("Web görüntüleme dosyası oluşturulamadı.", ToastType.Warning, TimeSpan.FromSeconds(4));
        }
    }

    private void TryExportWebViewSnapshotFireAndForget()
    {
        if (_webViewSnapshotService is null || !WebViewExportEnabled || !CanExportWebViewNow())
        {
            return;
        }

        RunSafeBackgroundTask(TryExportWebViewSnapshotAsync(showSuccessToast: false), "Web görüntüleme dosyası güncellenemedi.");
    }

    private async Task<WebViewSnapshotExportResult?> TryExportWebViewSnapshotAsync(bool showSuccessToast)
    {
        if (_webViewSnapshotService is null || !WebViewRepoPaths.ValidateRepoRoot(WebViewRepoRoot).IsValid)
        {
            return null;
        }

        try
        {
            await EnsureAllModulesInitializedAsync();
            RefreshTumEksikler();
            RefreshAcilIsOzet();
            PersonnelGorev?.Refresh();

            var derived = WebViewSnapshotDerivedBuilder.Build(
                WebViewSnapshotDerivedBuilder.GetAllTumEksiklerGroups(TumEksikler),
                YibfModule.BekleyenGruplar,
                PersonnelGorev?.Rows ?? [],
                AcilIsOzetItems);

            var exportDirectory = WebViewExportDirectory;
            var result = await _webViewSnapshotService.TryExportLatestAsync(
                new WebViewSnapshotExportRequest
                {
                    Tasks = AllTasks(),
                    ActionEntries = ActionModule.GetAllEntriesSnapshot(),
                    MissingProjectEntries = MissingProjectModule.GetEntriesSnapshot(),
                    MissingProjectCellStates = MissingProjectModule.GetCellStatesSnapshot(),
                    KarotEntries = KarotModule.GetEntriesSnapshot(),
                    KarotCellStates = KarotModule.GetCellStatesSnapshot(),
                    TadilatEntries = TadilatModule.GetEntriesSnapshot(),
                    YibfAnaBilgiEntries = YibfModule.GetAnaBilgiEntriesSnapshot(),
                    YibfAnaBilgiEvents = YibfModule.GetAnaBilgiEventsSnapshot(),
                    YibfIsTakibiEntries = YibfModule.GetIsTakibiEntriesSnapshot(),
                    YibfCellStates = YibfModule.GetCellStatesSnapshot(),
                    TadilatCellStates = TadilatModule.GetCellStatesSnapshot(),
                    QuickTaskTemplates = _quickTaskTemplateRepository?.GetAll(),
                    ProjectCatalogEntries = GetProjectCatalogSnapshot(),
                    Personnel = _personnelAssignmentService?.GetPersonnel(),
                    PersonnelAssignments = _personnelAssignmentService?.GetAssignments(),
                    Derived = derived
                },
                exportDirectory);

            if (result is null)
            {
                return null;
            }

            var jsonPath = result.FilePath;
            var publishNote = string.Empty;
            if (WebViewGitSyncEnabled)
            {
                var syncResult = await _webViewGitSyncService.TrySyncAsync(WebViewRepoRoot, jsonPath);
                publishNote = syncResult switch
                {
                    { Success: true } => $" {syncResult.Message}",
                    { Success: false } => $" Site güncellenemedi: {syncResult.Message}",
                    _ => string.Empty
                };
            }

            UpdateLastWebViewExportStatus(result.ExportedAt, result.FileSizeBytes, publishNote);

            if (showSuccessToast)
            {
                var siteUpdated = publishNote.Contains("güncellendi", StringComparison.OrdinalIgnoreCase)
                                  || publishNote.Contains("güncel", StringComparison.OrdinalIgnoreCase);
                _notificationService.ShowToast(
                    siteUpdated
                        ? $"Web sitesi güncellendi ({FormatBytes(result.FileSizeBytes)}). 1-2 dk sonra yenileyin."
                        : $"JSON yazıldı ({FormatBytes(result.FileSizeBytes)}).{publishNote}",
                    siteUpdated ? ToastType.Success : ToastType.Warning,
                    TimeSpan.FromSeconds(5));
            }

            return result;
        }
        catch (Exception ex)
        {
            LastWebViewExportStatus = $"Son dışa aktarma başarısız: {ex.Message}";
            if (showSuccessToast)
            {
                _notificationService.ShowToast($"Web dışa aktarma hatası: {ex.Message}", ToastType.Error, TimeSpan.FromSeconds(4));
            }

            return null;
        }
    }

    private void RefreshWebViewExportStatusFromDisk()
    {
        var path = WebViewRepoPaths.GetExportFilePath(WebViewRepoRoot);
        if (!File.Exists(path))
        {
            var validation = WebViewRepoPaths.ValidateRepoRoot(WebViewRepoRoot);
            LastWebViewExportStatus = validation.IsValid
                ? "Repo hazır; dosya henüz oluşturulmadı."
                : validation.ErrorMessage ?? "Henüz dışa aktarılmadı.";
            return;
        }

        var info = new FileInfo(path);
        UpdateLastWebViewExportStatus(info.LastWriteTime, info.Length, $" · {WebViewSiteUrl}");
    }

    private void UpdateLastWebViewExportStatus(DateTime exportedAt, long bytes, string publishNote = "")
        => LastWebViewExportStatus = $"Son dışa aktarma: {exportedAt:g} · {FormatBytes(bytes)} · PIN {DefaultWebPin}{publishNote}";
}
