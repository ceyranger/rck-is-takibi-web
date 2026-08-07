using CommunityToolkit.Mvvm.Input;
using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services.Abstractions;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;

namespace RizaCanKilicIsTakibi.ViewModels;

public sealed partial class MainViewModel
{
    private ProjectLinkDryRunResult? _lastProjectLinkDryRun;
    private IReadOnlyList<UnresolvedLinkResolution> _projectLinkResolutions = [];
    private bool _catalogSeedAttempted;
    private string _catalogSearchQuery = string.Empty;
    private ProjectCatalogListItemViewModel? _selectedProjectCatalogListItem;
    private Guid? _pickerSelectedProjectId;
    private int _unresolvedProjectLinkCount;
    private int _autoLinkCandidateCount;
    private bool _isRefreshingProjectLinkHealth;

    private void InitializeProjectCatalogCommands()
    {
        AddProjectCatalogEntryCommand = new AsyncRelayCommand(AddProjectCatalogEntryAsync);
        EditProjectCatalogEntryCommand = new AsyncRelayCommand(EditProjectCatalogEntryAsync, () => SelectedProjectCatalogEntry is not null);
        DeactivateProjectCatalogEntryCommand = new RelayCommand(DeactivateSelectedProjectCatalogEntry, () => SelectedProjectCatalogEntry is { IsActive: true });
        SeedProjectCatalogCommand = new AsyncRelayCommand(SeedProjectCatalogAsync, () => ProjectCatalogEntries.Count == 0);
        ProjectLinkDryRunCommand = new AsyncRelayCommand(ProjectLinkDryRunAsync);
        ResolveUnresolvedProjectLinksCommand = new AsyncRelayCommand(ResolveUnresolvedProjectLinksAsync, CanResolveUnresolvedProjectLinks);
        ApplyProjectLinksCommand = new AsyncRelayCommand(ApplyProjectLinksAsync, CanApplyProjectLinks);
        OpenProjectLinkHealthCommand = new AsyncRelayCommand(OpenProjectLinkHealthAsync);
        OverwriteLinkedProjectRecordsCommand = new AsyncRelayCommand(
            OverwriteLinkedProjectRecordsAsync,
            () => SelectedProjectCatalogEntry is not null);

        ApplySelectedProjectToKarotCommand = new RelayCommand(ApplySelectedProjectToKarot, () => PickerSelectedProjectId is not null && KarotModule.SelectedEntry is not null);
        ApplySelectedProjectToTadilatCommand = new RelayCommand(ApplySelectedProjectToTadilat, () => PickerSelectedProjectId is not null && TadilatModule.SelectedEntry is not null);
        ApplySelectedProjectToActionCommand = new RelayCommand(ApplySelectedProjectToAction, () => PickerSelectedProjectId is not null && ActionModule.SelectedEntry is not null);
        ApplySelectedProjectToMissingProjectCommand = new RelayCommand(ApplySelectedProjectToMissingProject, () => PickerSelectedProjectId is not null && MissingProjectModule.SelectedEntry is not null);
        ApplySelectedProjectToYibfIsTakibiCommand = new RelayCommand(ApplySelectedProjectToYibfIsTakibi, () => PickerSelectedProjectId is not null && YibfModule.SelectedIsTakibiEntry is not null);

        FilteredProjectCatalogListItems = new ObservableCollection<ProjectCatalogListItemViewModel>();
        RefreshFilteredProjectCatalogList();

        KarotModule.PropertyChanged += OnModuleSelectionChanged;
        TadilatModule.PropertyChanged += OnModuleSelectionChanged;
        ActionModule.PropertyChanged += OnModuleSelectionChanged;
        MissingProjectModule.PropertyChanged += OnModuleSelectionChanged;
        YibfModule.PropertyChanged += OnModuleSelectionChanged;
    }

    private void OnModuleSelectionChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(KarotModuleViewModel.SelectedEntry)
            or nameof(TadilatModuleViewModel.SelectedEntry)
            or nameof(ActionModuleViewModel.SelectedEntry)
            or nameof(MissingProjectModuleViewModel.SelectedEntry)
            or nameof(YibfModuleViewModel.SelectedIsTakibiEntry))
        {
            NotifyProjectApplyCommandsCanExecuteChanged();
        }
    }

    public ObservableCollection<ProjectCatalogListItemViewModel> FilteredProjectCatalogListItems { get; private set; } = [];

    public string CatalogSearchQuery
    {
        get => _catalogSearchQuery;
        set
        {
            if (SetProperty(ref _catalogSearchQuery, value))
            {
                RefreshFilteredProjectCatalogList();
            }
        }
    }

    public ProjectCatalogListItemViewModel? SelectedProjectCatalogListItem
    {
        get => _selectedProjectCatalogListItem;
        set
        {
            if (SetProperty(ref _selectedProjectCatalogListItem, value))
            {
                EditProjectCatalogEntryCommand.NotifyCanExecuteChanged();
                DeactivateProjectCatalogEntryCommand.NotifyCanExecuteChanged();
                OverwriteLinkedProjectRecordsCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public ProjectCatalogEntry? SelectedProjectCatalogEntry => SelectedProjectCatalogListItem?.Entry;

    public Guid? PickerSelectedProjectId
    {
        get => _pickerSelectedProjectId;
        set
        {
            if (SetProperty(ref _pickerSelectedProjectId, value))
            {
                NotifyProjectApplyCommandsCanExecuteChanged();
            }
        }
    }

    public bool IsProjectCatalogEmpty => ProjectCatalogEntries.Count == 0;
    public int UnresolvedProjectLinkCount
    {
        get => _unresolvedProjectLinkCount;
        private set => SetProperty(ref _unresolvedProjectLinkCount, value);
    }

    public int AutoLinkCandidateCount
    {
        get => _autoLinkCandidateCount;
        private set => SetProperty(ref _autoLinkCandidateCount, value);
    }

    public AsyncRelayCommand AddProjectCatalogEntryCommand { get; private set; } = null!;
    public AsyncRelayCommand EditProjectCatalogEntryCommand { get; private set; } = null!;
    public RelayCommand DeactivateProjectCatalogEntryCommand { get; private set; } = null!;
    public AsyncRelayCommand SeedProjectCatalogCommand { get; private set; } = null!;
    public AsyncRelayCommand ProjectLinkDryRunCommand { get; private set; } = null!;
    public AsyncRelayCommand ResolveUnresolvedProjectLinksCommand { get; private set; } = null!;
    public AsyncRelayCommand ApplyProjectLinksCommand { get; private set; } = null!;
    public AsyncRelayCommand OpenProjectLinkHealthCommand { get; private set; } = null!;
    public AsyncRelayCommand OverwriteLinkedProjectRecordsCommand { get; private set; } = null!;

    public RelayCommand ApplySelectedProjectToKarotCommand { get; private set; } = null!;
    public RelayCommand ApplySelectedProjectToTadilatCommand { get; private set; } = null!;
    public RelayCommand ApplySelectedProjectToActionCommand { get; private set; } = null!;
    public RelayCommand ApplySelectedProjectToMissingProjectCommand { get; private set; } = null!;
    public RelayCommand ApplySelectedProjectToYibfIsTakibiCommand { get; private set; } = null!;

    private void RefreshFilteredProjectCatalogList()
    {
        if (_projectCatalogService is null)
        {
            FilteredProjectCatalogListItems.Clear();
            OnPropertyChanged(nameof(IsProjectCatalogEmpty));
            SeedProjectCatalogCommand.NotifyCanExecuteChanged();
            return;
        }

        var lookup = ProjectCatalogEntries.ToDictionary(item => item.Id);
        var filtered = _projectCatalogService.Search(ProjectCatalogEntries, CatalogSearchQuery)
            .Select(entry =>
            {
                var parentName = entry.ParentProjectId is Guid parentId && lookup.TryGetValue(parentId, out var parent)
                    ? parent.DisplayName
                    : string.Empty;
                return new ProjectCatalogListItemViewModel(entry, parentName);
            })
            .ToList();

        FilteredProjectCatalogListItems.Clear();
        foreach (var item in filtered)
        {
            FilteredProjectCatalogListItems.Add(item);
        }

        OnPropertyChanged(nameof(IsProjectCatalogEmpty));
        SeedProjectCatalogCommand.NotifyCanExecuteChanged();
    }

    private async Task TryAutoSeedProjectCatalogAsync()
    {
        if (_catalogSeedAttempted || _projectCatalogService is null || ProjectCatalogEntries.Count > 0)
        {
            return;
        }

        await EnsureYibfModuleInitializedAsync();
        if (YibfModule.AnaBilgiEntries.Count == 0)
        {
            return;
        }

        _catalogSeedAttempted = true;
        await SeedProjectCatalogCoreAsync(showOnlyIfApplied: true);
    }

    private async Task SeedProjectCatalogAsync()
        => await SeedProjectCatalogCoreAsync(showOnlyIfApplied: false);

    private async Task SeedProjectCatalogCoreAsync(bool showOnlyIfApplied)
    {
        if (_projectCatalogService is null)
        {
            return;
        }

        await EnsureYibfModuleInitializedAsync();
        if (YibfModule.AnaBilgiEntries.Count == 0)
        {
            _notificationService.ShowToast("Proje Takibi kaydı bulunamadı.", ToastType.Warning);
            return;
        }

        if (ProjectCatalogEntries.Count > 0 && showOnlyIfApplied)
        {
            return;
        }

        if (ProjectCatalogEntries.Count > 0)
        {
            _notificationService.ShowToast("Katalog zaten dolu.", ToastType.Info);
            return;
        }

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
                quickTaskTemplates: _quickTaskTemplateRepository?.GetAll(),
                projectCatalogEntries: GetProjectCatalogSnapshot());
        }
        catch
        {
            // Seed öncesi yedek alınamasa da bellek içi seed devam edebilir.
        }

        var seed = _projectCatalogService.BuildSeedFromAnaBilgi(YibfModule.AnaBilgiEntries);
        ReplaceProjectCatalogEntries(seed.Select(entry => entry.Clone()));
        MarkCatalogDirty();
        RefreshFilteredProjectCatalogList();
        _notificationService.ShowToast("Proje kataloğu Proje Takibi'den oluşturuldu. Kaydet ile kalıcı olur.", ToastType.Success, TimeSpan.FromSeconds(4));
    }

    private async Task AddProjectCatalogEntryAsync()
    {
        if (_projectCatalogService is null || _projectCatalogEntryDialogService is null)
        {
            return;
        }

        var created = await _projectCatalogEntryDialogService.ShowDialogAsync(null, GetProjectCatalogSnapshot());
        if (created is null)
        {
            return;
        }

        ProjectCatalogEntries.Add(created);
        MarkCatalogDirty();
        RefreshFilteredProjectCatalogList();

        if (created.Kind != ProjectCatalogKind.Special)
        {
            await EnsureYibfModuleInitializedAsync();
            var fanOut = _projectCatalogService.BuildFanOut(created);
            YibfModule.AddStubEntriesFromFanOut(fanOut);
        }

        _notificationService.ShowToast("Proje kataloğa eklendi.", ToastType.Success);
    }

    private async Task EditProjectCatalogEntryAsync()
    {
        if (_projectCatalogService is null || _projectCatalogEntryDialogService is null || SelectedProjectCatalogEntry is null)
        {
            return;
        }

        var edited = await _projectCatalogEntryDialogService.ShowDialogAsync(
            SelectedProjectCatalogEntry.Clone(),
            GetProjectCatalogSnapshot());

        if (edited is null)
        {
            return;
        }

        var target = ProjectCatalogEntries.FirstOrDefault(item => item.Id == edited.Id);
        if (target is null)
        {
            return;
        }

        target.DisplayName = edited.DisplayName;
        target.AdaParsel = edited.AdaParsel;
        target.YapiSahibi = edited.YapiSahibi;
        target.YibfNo = edited.YibfNo;
        target.Belediye = edited.Belediye;
        target.Muteahhit = edited.Muteahhit;
        target.Kind = edited.Kind;
        target.ParentProjectId = edited.ParentProjectId;
        target.IsActive = edited.IsActive;
        target.UpdatedAt = edited.UpdatedAt;

        MarkCatalogDirty();
        RefreshFilteredProjectCatalogList();

        var syncedCount = await SyncLinkedIdentityFromCatalogAsync(target, requireConfirmation: false);
        if (syncedCount > 0)
        {
            _notificationService.ShowToast(
                $"Proje kataloğu güncellendi; {syncedCount} bağlı kayıt senkronize edildi. Kalıcı olması için Kaydet'e basın.",
                ToastType.Success,
                TimeSpan.FromSeconds(4));
            return;
        }

        _notificationService.ShowToast("Proje kataloğu güncellendi.", ToastType.Success);
        await Task.CompletedTask;
    }

    private void DeactivateSelectedProjectCatalogEntry()
    {
        if (SelectedProjectCatalogEntry is null)
        {
            return;
        }

        SelectedProjectCatalogEntry.IsActive = false;
        SelectedProjectCatalogEntry.UpdatedAt = DateTime.Now;
        MarkCatalogDirty();
        RefreshFilteredProjectCatalogList();
        _notificationService.ShowToast("Proje pasife alındı.", ToastType.Info);
    }

    private async Task ProjectLinkDryRunAsync()
    {
        if (_projectLinkingService is null)
        {
            return;
        }

        await EnsureAllModulesInitializedAsync();
        try
        {
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
                quickTaskTemplates: _quickTaskTemplateRepository?.GetAll(),
                projectCatalogEntries: GetProjectCatalogSnapshot());
        }
        catch (Exception ex)
        {
            _notificationService.ShowToast($"Yedek alınamadı: {ex.Message}", ToastType.Warning);
        }

        _lastProjectLinkDryRun = _projectLinkingService.DryRun(
            GetProjectCatalogSnapshot(),
            KarotModule.GetEntriesSnapshot(),
            TadilatModule.GetEntriesSnapshot(),
            ActionModule.GetAllEntriesSnapshot(),
            MissingProjectModule.GetEntriesSnapshot(),
            AllTasks().ToList(),
            YibfModule.GetIsTakibiEntriesSnapshot());
        UpdateProjectLinkHealthCounts(_lastProjectLinkDryRun);

        _projectLinkResolutions = [];
        ResolveUnresolvedProjectLinksCommand.NotifyCanExecuteChanged();
        ApplyProjectLinksCommand.NotifyCanExecuteChanged();

        var summary = new StringBuilder();
        summary.AppendLine($"Otomatik bağlanacak: {_lastProjectLinkDryRun.AutoLinkCount}");
        summary.AppendLine($"Özel iş: {_lastProjectLinkDryRun.SpecialJobCount}");
        summary.AppendLine($"Zaten bağlı/atlanan: {_lastProjectLinkDryRun.SkippedAlreadyLinkedCount}");
        summary.AppendLine($"Çözülmemiş: {_lastProjectLinkDryRun.Unresolved.Count}");

        MessageBox.Show(
            summary.ToString().TrimEnd(),
            "Proje Bağlantısı Önizleme",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private bool CanResolveUnresolvedProjectLinks()
        => _lastProjectLinkDryRun is { Unresolved.Count: > 0 };

    private async Task ResolveUnresolvedProjectLinksAsync()
    {
        if (_projectLinkResolveDialogService is null || _lastProjectLinkDryRun is null || _lastProjectLinkDryRun.Unresolved.Count == 0)
        {
            return;
        }

        var resolutions = await _projectLinkResolveDialogService.ShowDialogAsync(
            _lastProjectLinkDryRun.Unresolved,
            GetProjectCatalogSnapshot());

        if (resolutions is null)
        {
            return;
        }

        _projectLinkResolutions = resolutions;
        ApplyProjectLinksCommand.NotifyCanExecuteChanged();
    }

    private bool CanApplyProjectLinks()
        => _lastProjectLinkDryRun is not null
           && (_lastProjectLinkDryRun.AutoActions.Count > 0 || _projectLinkResolutions.Count > 0);

    private async Task ApplyProjectLinksAsync()
    {
        if (_projectLinkingService is null || _lastProjectLinkDryRun is null)
        {
            return;
        }

        if (!_confirmationService.Confirm(new ConfirmationRequest
            {
                Kind = ConfirmationKind.Save,
                Title = "Proje Bağlantılarını Uygula",
                Message = "Seçilen proje bağlantıları canlı kayıtlara uygulanacak. Devam edilsin mi?"
            }))
        {
            return;
        }

        _projectLinkingService.Apply(
            _lastProjectLinkDryRun.AutoActions,
            _projectLinkResolutions,
            KarotModule.Entries,
            TadilatModule.AktifEntries.Concat(TadilatModule.BitenEntries).ToList(),
            ActionModule.AksiyonEntries.Concat(ActionModule.AksiyonaEkleneceklerEntries).ToList(),
            MissingProjectModule.Entries,
            AllTasksMutable(),
            YibfModule.IsTakibiEntries,
            ProjectCatalogEntries);

        KarotModule.MarkDirty();
        TadilatModule.MarkDirty();
        ActionModule.MarkDirty();
        MissingProjectModule.MarkDirty();
        YibfModule.MarkDirty();
        MarkTaskDirty();
        MarkCatalogDirty();
        RefreshFilteredProjectCatalogList();
        RefreshTumEksikler();
        InvalidateSearchCorpus();

        _lastProjectLinkDryRun = null;
        _projectLinkResolutions = [];
        ResolveUnresolvedProjectLinksCommand.NotifyCanExecuteChanged();
        ApplyProjectLinksCommand.NotifyCanExecuteChanged();
        await RefreshProjectLinkHealthAsync();

        var remaining = UnresolvedProjectLinkCount;
        if (remaining > 0)
        {
            _notificationService.ShowToast(
                $"Bağlantılar uygulandı. {remaining} çözülmemiş kaldı — Şüphelileri çöz ile seçin, sonra Kaydet.",
                ToastType.Warning,
                TimeSpan.FromSeconds(6));
            return;
        }

        _notificationService.ShowToast("Proje bağlantıları uygulandı. Kalıcı olması için Kaydet'e basın.", ToastType.Success, TimeSpan.FromSeconds(4));
    }

    private async Task OpenProjectLinkHealthAsync()
    {
        SelectMainTab(MainNavigationTab.Ayarlar);
        await RefreshProjectLinkHealthAsync();
        if (CanResolveUnresolvedProjectLinks())
        {
            await ResolveUnresolvedProjectLinksAsync();
        }
    }

    private async Task RefreshProjectLinkHealthAsync()
    {
        if (_projectLinkingService is null || _isRefreshingProjectLinkHealth)
        {
            return;
        }

        _isRefreshingProjectLinkHealth = true;
        try
        {
            await EnsureAllModulesInitializedAsync();
            var catalog = GetProjectCatalogSnapshot();
            var karot = KarotModule.GetEntriesSnapshot();
            var tadilat = TadilatModule.GetEntriesSnapshot();
            var action = ActionModule.GetAllEntriesSnapshot();
            var missing = MissingProjectModule.GetEntriesSnapshot();
            var tasks = AllTasks().ToList();
            var yibf = YibfModule.GetIsTakibiEntriesSnapshot();

            _lastProjectLinkDryRun = await Task.Run(() => _projectLinkingService.DryRun(
                catalog,
                karot,
                tadilat,
                action,
                missing,
                tasks,
                yibf));
            _projectLinkResolutions = [];
            UpdateProjectLinkHealthCounts(_lastProjectLinkDryRun);
            ResolveUnresolvedProjectLinksCommand.NotifyCanExecuteChanged();
            ApplyProjectLinksCommand.NotifyCanExecuteChanged();
        }
        finally
        {
            _isRefreshingProjectLinkHealth = false;
        }
    }

    private void UpdateProjectLinkHealthCounts(ProjectLinkDryRunResult result)
    {
        UnresolvedProjectLinkCount = result.Unresolved.Count;
        AutoLinkCandidateCount = result.AutoLinkCount;
    }

    private async Task OverwriteLinkedProjectRecordsAsync()
    {
        if (_projectCatalogService is null || SelectedProjectCatalogEntry is null)
        {
            return;
        }

        await SyncLinkedIdentityFromCatalogAsync(SelectedProjectCatalogEntry, requireConfirmation: true);
    }

    private async Task<int> SyncLinkedIdentityFromCatalogAsync(ProjectCatalogEntry project, bool requireConfirmation)
    {
        if (_projectCatalogService is null)
        {
            return 0;
        }

        await EnsureAllModulesInitializedAsync();
        var anaBilgi = YibfModule.AnaBilgiEntries.ToList();
        var karot = KarotModule.Entries.ToList();
        var missing = MissingProjectModule.Entries.ToList();
        var action = ActionModule.AksiyonEntries.Concat(ActionModule.AksiyonaEkleneceklerEntries).ToList();
        var tadilat = TadilatModule.AktifEntries.Concat(TadilatModule.BitenEntries).ToList();
        var yibf = YibfModule.IsTakibiEntries.ToList();
        var preview = _projectCatalogService.PreviewLinkedIdentityOverwrite(
            project, anaBilgi, karot, missing, action, tadilat, yibf);

        if (preview.TotalCount == 0)
        {
            if (requireConfirmation)
            {
                _notificationService.ShowToast("Güncellenecek bağlı kayıt bulunamadı.", ToastType.Info);
            }

            return 0;
        }

        if (requireConfirmation)
        {
            var message =
                $"Proje Takibi: {preview.YibfAnaBilgiCount}\n" +
                $"Karot: {preview.KarotCount}\n" +
                $"Eksik Proje: {preview.MissingProjectCount}\n" +
                $"Aksiyon: {preview.ActionCount}\n" +
                $"Tadilat: {preview.TadilatCount}\n" +
                $"YİBF İş Takibi: {preview.YibfIsTakibiCount}\n\n" +
                "UYARI: Bağlı kayıtlardaki elle girilmiş proje kimlik bilgileri katalog değerleriyle değiştirilecek. Devam edilsin mi?";
            if (!_confirmationService.Confirm(new ConfirmationRequest
                {
                    Kind = ConfirmationKind.Save,
                    Title = "Bağlı Kayıtları Güncelle",
                    Message = message,
                    IsDestructive = true
                }))
            {
                return 0;
            }
        }

        var result = _projectCatalogService.OverwriteLinkedIdentityFields(
            project, anaBilgi, karot, missing, action, tadilat, yibf);
        if (result.YibfAnaBilgiCount > 0) YibfModule.MarkDirty();
        if (result.KarotCount > 0) KarotModule.MarkDirty();
        if (result.MissingProjectCount > 0) MissingProjectModule.MarkDirty();
        if (result.ActionCount > 0) ActionModule.MarkDirty();
        if (result.TadilatCount > 0) TadilatModule.MarkDirty();
        if (result.YibfIsTakibiCount > 0) YibfModule.MarkDirty();
        RefreshTumEksikler();
        InvalidateSearchCorpus();

        if (requireConfirmation)
        {
            _notificationService.ShowToast(
                $"{result.TotalCount} bağlı kayıt güncellendi. Kalıcı olması için Kaydet'e basın.",
                ToastType.Success,
                TimeSpan.FromSeconds(4));
        }

        return result.TotalCount;
    }

    private ProjectCatalogEntry? FindActiveProject(Guid? projectId)
    {
        if (projectId is not Guid id)
        {
            return null;
        }

        return ProjectCatalogEntries.FirstOrDefault(item => item.Id == id && item.IsActive);
    }

    private void ApplySelectedProjectToKarot()
    {
        if (_projectCatalogService is null || KarotModule.SelectedEntry is null)
        {
            return;
        }

        var project = FindActiveProject(PickerSelectedProjectId);
        if (project is null)
        {
            return;
        }

        _projectCatalogService.ApplyProjectSelection(KarotModule.SelectedEntry, project, ProjectCatalogEntries);
        KarotModule.MarkDirty();
        _notificationService.ShowToast("Proje karot satırına uygulandı.", ToastType.Success);
    }

    private void ApplySelectedProjectToTadilat()
    {
        if (_projectCatalogService is null || TadilatModule.SelectedEntry is null)
        {
            return;
        }

        var project = FindActiveProject(PickerSelectedProjectId);
        if (project is null)
        {
            return;
        }

        _projectCatalogService.ApplyProjectSelection(TadilatModule.SelectedEntry, project, ProjectCatalogEntries);
        TadilatModule.MarkDirty();
        _notificationService.ShowToast("Proje tadilat satırına uygulandı.", ToastType.Success);
    }

    private void ApplySelectedProjectToAction()
    {
        if (_projectCatalogService is null || ActionModule.SelectedEntry is null)
        {
            return;
        }

        var project = FindActiveProject(PickerSelectedProjectId);
        if (project is null)
        {
            return;
        }

        _projectCatalogService.ApplyProjectSelection(ActionModule.SelectedEntry, project, ProjectCatalogEntries);
        ActionModule.MarkDirty();
        _notificationService.ShowToast("Proje aksiyon satırına uygulandı.", ToastType.Success);
    }

    private void ApplySelectedProjectToMissingProject()
    {
        if (_projectCatalogService is null || MissingProjectModule.SelectedEntry is null)
        {
            return;
        }

        var project = FindActiveProject(PickerSelectedProjectId);
        if (project is null)
        {
            return;
        }

        _projectCatalogService.ApplyProjectSelection(MissingProjectModule.SelectedEntry, project, ProjectCatalogEntries);
        MissingProjectModule.MarkDirty();
        _notificationService.ShowToast("Proje eksik proje satırına uygulandı.", ToastType.Success);
    }

    private void ApplySelectedProjectToYibfIsTakibi()
    {
        if (_projectCatalogService is null || YibfModule.SelectedIsTakibiEntry is null)
        {
            return;
        }

        var project = FindActiveProject(PickerSelectedProjectId);
        if (project is null)
        {
            return;
        }

        _projectCatalogService.ApplyProjectSelection(YibfModule.SelectedIsTakibiEntry, project, ProjectCatalogEntries);
        YibfModule.SelectedIsTakibiEntry.UpdatedAt = DateTime.Now;
        YibfModule.MarkDirty();
        _notificationService.ShowToast("Proje YİBF iş takibi satırına uygulandı.", ToastType.Success);
    }

    private void NotifyProjectApplyCommandsCanExecuteChanged()
    {
        ApplySelectedProjectToKarotCommand.NotifyCanExecuteChanged();
        ApplySelectedProjectToTadilatCommand.NotifyCanExecuteChanged();
        ApplySelectedProjectToActionCommand.NotifyCanExecuteChanged();
        ApplySelectedProjectToMissingProjectCommand.NotifyCanExecuteChanged();
        ApplySelectedProjectToYibfIsTakibiCommand.NotifyCanExecuteChanged();
    }

    private List<TaskItem> AllTasksMutable()
        => UrgentBoard.Tasks.Concat(GeneralBoard.Tasks).ToList();
}
