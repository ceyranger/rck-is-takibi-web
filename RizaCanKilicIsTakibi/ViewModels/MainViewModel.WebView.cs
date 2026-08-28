using CommunityToolkit.Mvvm.Input;
using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services;
using RizaCanKilicIsTakibi.Services.Abstractions;
using System.IO;

namespace RizaCanKilicIsTakibi.ViewModels;

public sealed partial class MainViewModel
{
    private const string DefaultWebPin = "271179";
    private readonly WebViewGitHubPublishService _webViewGitHubPublishService = new();
    private string _lastWebViewExportStatus = "Henüz dışa aktarılmadı.";

    private void InitializeWebViewExportFeature()
    {
        PickWebViewExportDirectoryCommand = new RelayCommand(PickWebViewExportDirectory);
        ExportWebViewNowCommand = new AsyncRelayCommand(ExportWebViewNowAsync, CanExportWebViewNow);
        RefreshWebViewExportStatusFromDisk();
    }

    public RelayCommand PickWebViewExportDirectoryCommand { get; private set; } = null!;
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

    public string WebViewExportDirectory
    {
        get => _settings.WebViewExportDirectory;
        private set
        {
            var normalized = value?.Trim() ?? string.Empty;
            if (string.Equals(_settings.WebViewExportDirectory, normalized, StringComparison.Ordinal))
            {
                return;
            }

            _settings.WebViewExportDirectory = normalized;
            OnPropertyChanged();
            OnPropertyChanged(nameof(WebViewExportDirectoryDisplay));
            HasUnsavedSettings = true;
            ExportWebViewNowCommand.NotifyCanExecuteChanged();
            RefreshWebViewExportStatusFromDisk();
        }
    }

    public string WebViewExportDirectoryDisplay
        => string.IsNullOrWhiteSpace(WebViewExportDirectory)
            ? "(Klasör seçilmedi)"
            : WebViewExportDirectory;

    public string LastWebViewExportStatus
    {
        get => _lastWebViewExportStatus;
        private set => SetProperty(ref _lastWebViewExportStatus, value);
    }

    private bool CanExportWebViewNow()
        => _webViewSnapshotService is not null
           && !string.IsNullOrWhiteSpace(WebViewExportDirectory);

    private void PickWebViewExportDirectory()
    {
        var selected = _fileDialogService.ShowFolderDialog(
            "Web JSON klasörü seç (web-view-latest.json buraya yazılır)");
        if (string.IsNullOrWhiteSpace(selected))
        {
            return;
        }

        WebViewExportDirectory = selected;
    }

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
        if (_webViewSnapshotService is null || !WebViewExportEnabled || string.IsNullOrWhiteSpace(WebViewExportDirectory))
        {
            return;
        }

        RunSafeBackgroundTask(TryExportWebViewSnapshotAsync(showSuccessToast: false), "Web görüntüleme dosyası güncellenemedi.");
    }

    private async Task<WebViewSnapshotExportResult?> TryExportWebViewSnapshotAsync(bool showSuccessToast)
    {
        if (_webViewSnapshotService is null || string.IsNullOrWhiteSpace(WebViewExportDirectory))
        {
            return null;
        }

        try
        {
            await EnsureAllModulesInitializedAsync();
            RefreshTumEksikler();
            PersonnelGorev?.Refresh();

            var derived = WebViewSnapshotDerivedBuilder.Build(
                WebViewSnapshotDerivedBuilder.GetAllTumEksiklerGroups(TumEksikler),
                YibfModule.BekleyenGruplar,
                PersonnelGorev?.Rows ?? []);

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
                WebViewExportDirectory);

            if (result is null)
            {
                return null;
            }

            var jsonPath = Path.Combine(WebViewExportDirectory, IWebViewSnapshotService.LatestFileName);
            var publishNote = string.Empty;
            if (_settings.WebViewGitHubPublishEnabled)
            {
                var repo = string.IsNullOrWhiteSpace(_settings.WebViewGitHubRepository)
                    ? WebViewGitHubPublishService.DefaultRepo
                    : _settings.WebViewGitHubRepository.Trim();
                var publishResult = await _webViewGitHubPublishService.TryPublishAsync(jsonPath, repo);
                publishNote = publishResult switch
                {
                    { Success: true } => " Site güncellendi.",
                    { Success: false } => $" Site yüklenemedi: {publishResult.Message}",
                    _ => string.Empty
                };
            }

            UpdateLastWebViewExportStatus(result.ExportedAt, result.FileSizeBytes, publishNote);

            if (showSuccessToast)
            {
                _notificationService.ShowToast(
                    publishNote.StartsWith(" Site güncellendi", StringComparison.Ordinal)
                        ? $"Web sitesi güncellendi ({FormatBytes(result.FileSizeBytes)}). 1-2 dk sonra yenileyin."
                        : $"JSON yazıldı ({FormatBytes(result.FileSizeBytes)}).{publishNote}",
                    publishNote.StartsWith(" Site güncellendi", StringComparison.Ordinal) ? ToastType.Success : ToastType.Warning,
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
        if (string.IsNullOrWhiteSpace(WebViewExportDirectory))
        {
            LastWebViewExportStatus = "Henüz dışa aktarılmadı.";
            return;
        }

        var path = Path.Combine(WebViewExportDirectory, IWebViewSnapshotService.LatestFileName);
        if (!File.Exists(path))
        {
            LastWebViewExportStatus = "Klasör seçildi; dosya henüz oluşturulmadı.";
            return;
        }

        var info = new FileInfo(path);
        UpdateLastWebViewExportStatus(info.LastWriteTime, info.Length);
    }

    private void UpdateLastWebViewExportStatus(DateTime exportedAt, long bytes, string publishNote = "")
        => LastWebViewExportStatus = $"Son dışa aktarma: {exportedAt:g} · {FormatBytes(bytes)} · PIN {DefaultWebPin}{publishNote}";
}
