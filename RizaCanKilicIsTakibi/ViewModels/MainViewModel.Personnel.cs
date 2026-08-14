using CommunityToolkit.Mvvm.Input;
using RizaCanKilicIsTakibi.Helpers;
using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services;
using RizaCanKilicIsTakibi.Services.Abstractions;
using System.Text;
using System.Windows;

namespace RizaCanKilicIsTakibi.ViewModels;

public sealed partial class MainViewModel
{
    private IPersonnelAssignmentService? _personnelAssignmentService;
    private IPersonnelPickDialogService? _personnelPickDialogService;
    private IPersonnelCellScopeDialogService? _personnelCellScopeDialogService;
    private bool _isPersonnelGorevViewActivated;

    public PersonnelGorevViewModel? PersonnelGorev { get; private set; }

    public bool IsPersonnelGorevViewActivated
    {
        get => _isPersonnelGorevViewActivated;
        private set => SetProperty(ref _isPersonnelGorevViewActivated, value);
    }

    public IAsyncRelayCommand? AssignPersonnelToUrgentTaskCommand { get; private set; }
    public IAsyncRelayCommand? AssignPersonnelToGeneralTaskCommand { get; private set; }
    public IAsyncRelayCommand? RemovePersonnelFromUrgentTaskCommand { get; private set; }
    public IAsyncRelayCommand? RemovePersonnelFromGeneralTaskCommand { get; private set; }
    public IAsyncRelayCommand? AssignPersonnelToTaskCommand { get; private set; }
    public IAsyncRelayCommand? RemovePersonnelFromTaskCommand { get; private set; }
    public IAsyncRelayCommand? AssignPersonnelToActionEntryCommand { get; private set; }
    public IAsyncRelayCommand? RemovePersonnelFromActionEntryCommand { get; private set; }
    public IAsyncRelayCommand? AssignPersonnelToMissingProjectCommand { get; private set; }
    public IAsyncRelayCommand? RemovePersonnelFromMissingProjectCommand { get; private set; }
    public IAsyncRelayCommand? AssignPersonnelToKarotCommand { get; private set; }
    public IAsyncRelayCommand? RemovePersonnelFromKarotCommand { get; private set; }
    public IAsyncRelayCommand? AssignPersonnelToTadilatCellCommand { get; private set; }
    public IAsyncRelayCommand? AssignPersonnelToTadilatRowCommand { get; private set; }
    public IAsyncRelayCommand? RemovePersonnelFromTadilatCommand { get; private set; }
    public IAsyncRelayCommand? AssignPersonnelToYibfCellCommand { get; private set; }
    public IAsyncRelayCommand? AssignPersonnelToYibfRowCommand { get; private set; }
    public IAsyncRelayCommand? RemovePersonnelFromYibfCommand { get; private set; }
    public IAsyncRelayCommand? AssignPersonnelToYibfEventCommand { get; private set; }
    public IAsyncRelayCommand? RemovePersonnelFromYibfEventCommand { get; private set; }
    public IAsyncRelayCommand? ExportPersonnelExcelCommand { get; private set; }
    public IAsyncRelayCommand? ExportPersonnelPdfCommand { get; private set; }

    private void InitializePersonnelFeature(
        IPersonnelAssignmentService? personnelAssignmentService,
        IPersonnelSettingsDialogService? personnelSettingsDialogService,
        IPersonnelPickDialogService? personnelPickDialogService,
        IPersonnelCellScopeDialogService? personnelCellScopeDialogService,
        PersonnelGorevViewModel? personnelGorevViewModel)
    {
        if (personnelAssignmentService is null)
        {
            return;
        }

        _personnelAssignmentService = personnelAssignmentService;
        _personnelPickDialogService = personnelPickDialogService;
        _personnelCellScopeDialogService = personnelCellScopeDialogService;
        PersonnelGorev = personnelGorevViewModel
            ?? new PersonnelGorevViewModel(personnelAssignmentService, personnelSettingsDialogService);

        AssignPersonnelToUrgentTaskCommand = new AsyncRelayCommand(
            () => AssignPersonnelToSelectedTaskAsync(TaskBoardType.Acil),
            () => UrgentBoard.SelectedTask is not null);
        AssignPersonnelToGeneralTaskCommand = new AsyncRelayCommand(
            () => AssignPersonnelToSelectedTaskAsync(TaskBoardType.Genel),
            () => GeneralBoard.SelectedTask is not null);
        RemovePersonnelFromUrgentTaskCommand = new AsyncRelayCommand(
            () => RemovePersonnelFromSelectedTaskAsync(TaskBoardType.Acil),
            () => UrgentBoard.SelectedTask is not null);
        RemovePersonnelFromGeneralTaskCommand = new AsyncRelayCommand(
            () => RemovePersonnelFromSelectedTaskAsync(TaskBoardType.Genel),
            () => GeneralBoard.SelectedTask is not null);
        AssignPersonnelToTaskCommand = new AsyncRelayCommand<TaskItem?>(AssignPersonnelToTaskAsync, task => task is not null);
        RemovePersonnelFromTaskCommand = new AsyncRelayCommand<TaskItem?>(RemovePersonnelFromTaskAsync, task => task is not null);
        AssignPersonnelToActionEntryCommand = new AsyncRelayCommand<ActionEntry?>(AssignPersonnelToActionAsync, e => e is not null);
        RemovePersonnelFromActionEntryCommand = new AsyncRelayCommand<ActionEntry?>(RemovePersonnelFromActionAsync, e => e is not null);
        AssignPersonnelToMissingProjectCommand = new AsyncRelayCommand<MissingProjectEntry?>(AssignPersonnelToMissingProjectAsync, e => e is not null);
        RemovePersonnelFromMissingProjectCommand = new AsyncRelayCommand<MissingProjectEntry?>(RemovePersonnelFromMissingProjectAsync, e => e is not null);
        AssignPersonnelToKarotCommand = new AsyncRelayCommand<KarotEntry?>(AssignPersonnelToKarotAsync, e => e is not null);
        RemovePersonnelFromKarotCommand = new AsyncRelayCommand<KarotEntry?>(RemovePersonnelFromKarotAsync, e => e is not null);
        AssignPersonnelToTadilatCellCommand = new AsyncRelayCommand<object?>(AssignPersonnelToTadilatCellAsync);
        AssignPersonnelToTadilatRowCommand = new AsyncRelayCommand<TadilatEntry?>(AssignPersonnelToTadilatRowAsync, e => e is not null);
        RemovePersonnelFromTadilatCommand = new AsyncRelayCommand<TadilatEntry?>(RemovePersonnelFromTadilatAsync, e => e is not null);
        AssignPersonnelToYibfCellCommand = new AsyncRelayCommand<object?>(AssignPersonnelToYibfCellAsync);
        AssignPersonnelToYibfRowCommand = new AsyncRelayCommand<YibfIsTakibiEntry?>(AssignPersonnelToYibfRowAsync, e => e is not null);
        RemovePersonnelFromYibfCommand = new AsyncRelayCommand<YibfIsTakibiEntry?>(RemovePersonnelFromYibfAsync, e => e is not null);
        AssignPersonnelToYibfEventCommand = new AsyncRelayCommand<YibfAnaBilgiEvent?>(AssignPersonnelToYibfEventAsync, e => e is not null);
        RemovePersonnelFromYibfEventCommand = new AsyncRelayCommand<YibfAnaBilgiEvent?>(RemovePersonnelFromYibfEventAsync, e => e is not null);
        ExportPersonnelExcelCommand = new AsyncRelayCommand(ExportPersonnelExcelAsync);
        ExportPersonnelPdfCommand = new AsyncRelayCommand(ExportPersonnelPdfAsync);

        _personnelAssignmentService.Changed += (_, _) =>
        {
            RefreshPersonnelBadges();
            PersonnelGorev?.Refresh();
            SyncPersonnelAssignmentCompletion();
        };

        RefreshPersonnelBadges();
    }

    private void SyncPersonnelAssignmentCompletion()
    {
        if (_personnelAssignmentService is null)
        {
            return;
        }

        try
        {
            _personnelAssignmentService.SyncCompletionFromSources(
                AllTasks(),
                ActionModule.GetAllEntriesSnapshot(),
                MissingProjectModule.GetEntriesSnapshot(),
                KarotModule.GetEntriesSnapshot(),
                TadilatModule.GetEntriesSnapshot(),
                TadilatModule.GetCellStatesSnapshot(),
                YibfModule.GetAnaBilgiEventsSnapshot(),
                YibfModule.GetIsTakibiEntriesSnapshot(),
                YibfModule.GetCellStatesSnapshot());
        }
        catch
        {
            // Sync best-effort; do not break UI.
        }
    }

    private void RefreshPersonnelBadges()
    {
        if (_personnelAssignmentService is null)
        {
            return;
        }

        foreach (var task in AllTasks())
        {
            var module = task.BoardType == TaskBoardType.Acil
                ? PersonnelAssignmentSourceModule.AcilTask
                : PersonnelAssignmentSourceModule.GenelTask;
            task.AssignedPersonnelBadge = _personnelAssignmentService.GetBadgeText(module, task.Id);
        }

        foreach (var entry in ActionModule.GetAllEntriesSnapshot())
        {
            // Action entries from snapshot are clones; refresh live collections instead.
        }

        RefreshLiveModuleBadges();
    }

    private void RefreshLiveModuleBadges()
    {
        if (_personnelAssignmentService is null)
        {
            return;
        }

        foreach (var group in ActionModule.DistrictGroups)
        {
            foreach (var row in group.Rows)
            {
                if (row.Entry is null)
                {
                    continue;
                }

                row.Entry.AssignedPersonnelBadge = _personnelAssignmentService.GetBadgeText(PersonnelAssignmentSourceModule.Action, row.Entry.Id);
            }
        }

        foreach (var entry in MissingProjectModule.Entries)
        {
            entry.AssignedPersonnelBadge = _personnelAssignmentService.GetBadgeText(PersonnelAssignmentSourceModule.MissingProject, entry.Id);
        }

        foreach (var entry in KarotModule.Entries)
        {
            entry.AssignedPersonnelBadge = _personnelAssignmentService.GetBadgeText(PersonnelAssignmentSourceModule.Karot, entry.Id);
        }

        RefreshTadilatYibfBadges();
    }

    private void RefreshTadilatYibfBadges()
    {
        if (_personnelAssignmentService is null)
        {
            return;
        }

        // Tadilat / YİBF expose entries through module view models.
        TadilatModule.RefreshPersonnelBadges(_personnelAssignmentService);
        YibfModule.RefreshPersonnelBadges(_personnelAssignmentService);
    }

    private async Task AssignPersonnelToSelectedTaskAsync(TaskBoardType boardType)
    {
        var task = boardType == TaskBoardType.Acil ? UrgentBoard.SelectedTask : GeneralBoard.SelectedTask;
        await AssignPersonnelToTaskAsync(task);
    }

    private async Task RemovePersonnelFromSelectedTaskAsync(TaskBoardType boardType)
    {
        var task = boardType == TaskBoardType.Acil ? UrgentBoard.SelectedTask : GeneralBoard.SelectedTask;
        await RemovePersonnelFromTaskAsync(task);
    }

    private async Task AssignPersonnelToTaskAsync(TaskItem? task)
    {
        if (task is null || _personnelAssignmentService is null || _personnelPickDialogService is null)
        {
            return;
        }

        var personnelId = await _personnelPickDialogService.ShowDialogAsync();
        if (personnelId is null)
        {
            return;
        }

        var module = task.BoardType == TaskBoardType.Acil
            ? PersonnelAssignmentSourceModule.AcilTask
            : PersonnelAssignmentSourceModule.GenelTask;

        await _personnelAssignmentService.AssignAsync(new PersonnelAssignment
        {
            PersonnelId = personnelId,
            SourceModule = module,
            SourceEntryId = task.Id,
            Status = PersonnelAssignmentStatus.Open,
            AssignedAt = DateTime.Now,
            PrioritySnapshot = module == PersonnelAssignmentSourceModule.AcilTask
                ? PersonnelAssignmentPriority.Urgent
                : PersonnelAssignmentPriority.None,
            SummarySnapshot = task.Title,
            ProjectIdentitySnapshot = string.Empty,
            ModuleLabelSnapshot = IPersonnelAssignmentService.ModuleLabel(module)
        });
        _notificationService.ShowToast("Personel atandı.", ToastType.Success);
    }

    private async Task RemovePersonnelFromTaskAsync(TaskItem? task)
    {
        if (task is null || _personnelAssignmentService is null)
        {
            return;
        }

        var module = task.BoardType == TaskBoardType.Acil
            ? PersonnelAssignmentSourceModule.AcilTask
            : PersonnelAssignmentSourceModule.GenelTask;
        await _personnelAssignmentService.RemoveAssignmentsForSourceAsync(module, task.Id);
        _notificationService.ShowToast("Personel ataması kaldırıldı.", ToastType.Info);
    }

    private async Task AssignPersonnelToActionAsync(ActionEntry? entry)
    {
        if (entry is null || _personnelAssignmentService is null || _personnelPickDialogService is null)
        {
            return;
        }

        var personnelId = await _personnelPickDialogService.ShowDialogAsync();
        if (personnelId is null)
        {
            return;
        }

        var category = entry.Category == ActionEntryCategory.AksiyonaEklenecekler
            ? "Aksiyona Eklenecekler"
            : "Aksiyon";

        await _personnelAssignmentService.AssignAsync(new PersonnelAssignment
        {
            PersonnelId = personnelId,
            SourceModule = PersonnelAssignmentSourceModule.Action,
            SourceEntryId = entry.Id,
            Status = PersonnelAssignmentStatus.Open,
            AssignedAt = DateTime.Now,
            SummarySnapshot = $"{category}: {entry.WorkText}",
            ProjectIdentitySnapshot = entry.OwnerParcelText,
            FieldLabelSnapshot = category,
            ModuleLabelSnapshot = IPersonnelAssignmentService.ModuleLabel(PersonnelAssignmentSourceModule.Action)
        });
        _notificationService.ShowToast("Personel atandı.", ToastType.Success);
    }

    private async Task RemovePersonnelFromActionAsync(ActionEntry? entry)
    {
        if (entry is null || _personnelAssignmentService is null)
        {
            return;
        }

        await _personnelAssignmentService.RemoveAssignmentsForSourceAsync(PersonnelAssignmentSourceModule.Action, entry.Id);
        _notificationService.ShowToast("Personel ataması kaldırıldı.", ToastType.Info);
    }

    private async Task AssignPersonnelToMissingProjectAsync(MissingProjectEntry? entry)
    {
        if (entry is null || _personnelAssignmentService is null || _personnelPickDialogService is null)
        {
            return;
        }

        var personnelId = await _personnelPickDialogService.ShowDialogAsync();
        if (personnelId is null)
        {
            return;
        }

        await _personnelAssignmentService.AssignAsync(new PersonnelAssignment
        {
            PersonnelId = personnelId,
            SourceModule = PersonnelAssignmentSourceModule.MissingProject,
            SourceEntryId = entry.Id,
            Status = PersonnelAssignmentStatus.Open,
            AssignedAt = DateTime.Now,
            PrioritySnapshot = PersonnelAssignmentPriority.Warning,
            SummarySnapshot = entry.MissingProjectText,
            ProjectIdentitySnapshot = entry.AdaParsel,
            ModuleLabelSnapshot = IPersonnelAssignmentService.ModuleLabel(PersonnelAssignmentSourceModule.MissingProject)
        });
        _notificationService.ShowToast("Personel atandı.", ToastType.Success);
    }

    private async Task RemovePersonnelFromMissingProjectAsync(MissingProjectEntry? entry)
    {
        if (entry is null || _personnelAssignmentService is null)
        {
            return;
        }

        await _personnelAssignmentService.RemoveAssignmentsForSourceAsync(PersonnelAssignmentSourceModule.MissingProject, entry.Id);
        _notificationService.ShowToast("Personel ataması kaldırıldı.", ToastType.Info);
    }

    private async Task AssignPersonnelToKarotAsync(KarotEntry? entry)
    {
        if (entry is null || _personnelAssignmentService is null || _personnelPickDialogService is null)
        {
            return;
        }

        if (!IPersonnelAssignmentService.IsAssignableKarotStatus(entry.Status))
        {
            _notificationService.ShowToast("Bu Karot durumunda personel atanamaz.", ToastType.Warning);
            return;
        }

        var personnelId = await _personnelPickDialogService.ShowDialogAsync();
        if (personnelId is null)
        {
            return;
        }

        var priority = entry.Status == KarotStatus.KarotAlindiOlumsuz
            ? PersonnelAssignmentPriority.Critical
            : PersonnelAssignmentPriority.Warning;

        await _personnelAssignmentService.AssignAsync(new PersonnelAssignment
        {
            PersonnelId = personnelId,
            SourceModule = PersonnelAssignmentSourceModule.Karot,
            SourceEntryId = entry.Id,
            Status = PersonnelAssignmentStatus.Open,
            AssignedAt = DateTime.Now,
            PrioritySnapshot = priority,
            SummarySnapshot = $"{entry.Status}: {entry.KatBilgisi}",
            ProjectIdentitySnapshot = FirstNonEmptyLocal(entry.AdaParsel, entry.YibfNo),
            ModuleLabelSnapshot = IPersonnelAssignmentService.ModuleLabel(PersonnelAssignmentSourceModule.Karot)
        });
        _notificationService.ShowToast("Personel atandı.", ToastType.Success);
    }

    private async Task RemovePersonnelFromKarotAsync(KarotEntry? entry)
    {
        if (entry is null || _personnelAssignmentService is null)
        {
            return;
        }

        await _personnelAssignmentService.RemoveAssignmentsForSourceAsync(PersonnelAssignmentSourceModule.Karot, entry.Id);
        _notificationService.ShowToast("Personel ataması kaldırıldı.", ToastType.Info);
    }

    private async Task AssignPersonnelToTadilatRowAsync(TadilatEntry? entry)
    {
        if (entry is null || entry.SubTab != TadilatSubTab.Aktif || _personnelAssignmentService is null || _personnelPickDialogService is null)
        {
            return;
        }

        var personnelId = await _personnelPickDialogService.ShowDialogAsync();
        if (personnelId is null)
        {
            return;
        }

        await _personnelAssignmentService.AssignAsync(BuildTadilatAssignment(entry, personnelId.Value, columnKey: null));
        _notificationService.ShowToast("Personel atandı.", ToastType.Success);
    }

    private async Task AssignPersonnelToTadilatCellAsync(object? parameter)
    {
        if (_personnelAssignmentService is null || _personnelPickDialogService is null || _personnelCellScopeDialogService is null)
        {
            return;
        }

        if (!TryResolveCellContext(parameter, out var entryObj, out var columnKey) || entryObj is not TadilatEntry entry)
        {
            return;
        }

        if (entry.SubTab != TadilatSubTab.Aktif)
        {
            return;
        }

        var label = PersonnelAssignmentService.GetTadilatFieldLabel(columnKey);
        var scope = _personnelCellScopeDialogService.ShowDialog(label);
        if (scope == PersonnelCellScopeChoice.Cancel)
        {
            return;
        }

        var personnelId = await _personnelPickDialogService.ShowDialogAsync();
        if (personnelId is null)
        {
            return;
        }

        if (scope == PersonnelCellScopeChoice.ThisCell)
        {
            await _personnelAssignmentService.AssignAsync(BuildTadilatAssignment(entry, personnelId.Value, columnKey));
        }
        else
        {
            var drafts = TadilatModule.GetRedYellowColumnKeys(entry.Id)
                .Select(key => BuildTadilatAssignment(entry, personnelId.Value, key))
                .ToList();
            if (drafts.Count == 0)
            {
                drafts.Add(BuildTadilatAssignment(entry, personnelId.Value, columnKey));
            }

            await _personnelAssignmentService.AssignManyAsync(drafts);
        }

        _notificationService.ShowToast("Personel atandı.", ToastType.Success);
    }

    private async Task RemovePersonnelFromTadilatAsync(TadilatEntry? entry)
    {
        if (entry is null || _personnelAssignmentService is null)
        {
            return;
        }

        await _personnelAssignmentService.RemoveAssignmentsForSourceAsync(PersonnelAssignmentSourceModule.Tadilat, entry.Id);
        _notificationService.ShowToast("Personel ataması kaldırıldı.", ToastType.Info);
    }

    private async Task AssignPersonnelToYibfRowAsync(YibfIsTakibiEntry? entry)
    {
        if (entry is null || _personnelAssignmentService is null || _personnelPickDialogService is null)
        {
            return;
        }

        var personnelId = await _personnelPickDialogService.ShowDialogAsync();
        if (personnelId is null)
        {
            return;
        }

        await _personnelAssignmentService.AssignAsync(BuildYibfAssignment(entry, personnelId.Value, null));
        _notificationService.ShowToast("Personel atandı.", ToastType.Success);
    }

    private async Task AssignPersonnelToYibfCellAsync(object? parameter)
    {
        if (_personnelAssignmentService is null || _personnelPickDialogService is null || _personnelCellScopeDialogService is null)
        {
            return;
        }

        if (!TryResolveCellContext(parameter, out var entryObj, out var columnKey) || entryObj is not YibfIsTakibiEntry entry)
        {
            return;
        }

        var label = PersonnelAssignmentService.GetYibfFieldLabel(columnKey);
        var scope = _personnelCellScopeDialogService.ShowDialog(label);
        if (scope == PersonnelCellScopeChoice.Cancel)
        {
            return;
        }

        var personnelId = await _personnelPickDialogService.ShowDialogAsync();
        if (personnelId is null)
        {
            return;
        }

        if (scope == PersonnelCellScopeChoice.ThisCell)
        {
            await _personnelAssignmentService.AssignAsync(BuildYibfAssignment(entry, personnelId.Value, columnKey));
        }
        else
        {
            var drafts = YibfModule.GetRedYellowColumnKeys(entry.Id)
                .Select(key => BuildYibfAssignment(entry, personnelId.Value, key))
                .ToList();
            if (drafts.Count == 0)
            {
                drafts.Add(BuildYibfAssignment(entry, personnelId.Value, columnKey));
            }

            await _personnelAssignmentService.AssignManyAsync(drafts);
        }

        _notificationService.ShowToast("Personel atandı.", ToastType.Success);
    }

    private async Task RemovePersonnelFromYibfAsync(YibfIsTakibiEntry? entry)
    {
        if (entry is null || _personnelAssignmentService is null)
        {
            return;
        }

        await _personnelAssignmentService.RemoveAssignmentsForSourceAsync(PersonnelAssignmentSourceModule.YibfIsTakibi, entry.Id);
        _notificationService.ShowToast("Personel ataması kaldırıldı.", ToastType.Info);
    }

    private async Task AssignPersonnelToYibfEventAsync(YibfAnaBilgiEvent? evt)
    {
        if (evt is null || _personnelAssignmentService is null || _personnelPickDialogService is null)
        {
            return;
        }

        var personnelId = await _personnelPickDialogService.ShowDialogAsync();
        if (personnelId is null)
        {
            return;
        }

        var priority = PersonnelPendingColorHelper.IsCriticalColor(evt.BackgroundColor)
            ? PersonnelAssignmentPriority.Critical
            : PersonnelPendingColorHelper.IsPendingColor(evt.BackgroundColor)
                ? PersonnelAssignmentPriority.Warning
                : PersonnelAssignmentPriority.None;

        await _personnelAssignmentService.AssignAsync(new PersonnelAssignment
        {
            PersonnelId = personnelId,
            SourceModule = PersonnelAssignmentSourceModule.YibfAnaBilgiEvent,
            SourceEntryId = evt.Id,
            Status = PersonnelAssignmentStatus.Open,
            AssignedAt = DateTime.Now,
            PrioritySnapshot = priority,
            SummarySnapshot = $"{evt.ApprovalStatus}: {evt.Description}",
            FieldLabelSnapshot = evt.ApprovalStatus,
            ModuleLabelSnapshot = IPersonnelAssignmentService.ModuleLabel(PersonnelAssignmentSourceModule.YibfAnaBilgiEvent)
        });
        _notificationService.ShowToast("Personel atandı.", ToastType.Success);
    }

    private async Task RemovePersonnelFromYibfEventAsync(YibfAnaBilgiEvent? evt)
    {
        if (evt is null || _personnelAssignmentService is null)
        {
            return;
        }

        await _personnelAssignmentService.RemoveAssignmentsForSourceAsync(PersonnelAssignmentSourceModule.YibfAnaBilgiEvent, evt.Id);
        _notificationService.ShowToast("Personel ataması kaldırıldı.", ToastType.Info);
    }

    private PersonnelAssignment BuildTadilatAssignment(TadilatEntry entry, Guid personnelId, string? columnKey)
    {
        var color = string.IsNullOrWhiteSpace(columnKey) ? null : TadilatModule.GetCellBackgroundColor(entry.Id, columnKey);
        var priority = PersonnelPendingColorHelper.IsCriticalColor(color)
            ? PersonnelAssignmentPriority.Critical
            : PersonnelPendingColorHelper.IsPendingColor(color)
                ? PersonnelAssignmentPriority.Warning
                : PersonnelAssignmentPriority.None;

        return new PersonnelAssignment
        {
            PersonnelId = personnelId,
            SourceModule = PersonnelAssignmentSourceModule.Tadilat,
            SourceEntryId = entry.Id,
            SourceColumnKey = columnKey,
            Status = PersonnelAssignmentStatus.Open,
            AssignedAt = DateTime.Now,
            PrioritySnapshot = priority,
            FieldLabelSnapshot = string.IsNullOrWhiteSpace(columnKey)
                ? "Satır"
                : PersonnelAssignmentService.GetTadilatFieldLabel(columnKey),
            SummarySnapshot = entry.JobName,
            ProjectIdentitySnapshot = entry.JobName,
            ModuleLabelSnapshot = IPersonnelAssignmentService.ModuleLabel(PersonnelAssignmentSourceModule.Tadilat)
        };
    }

    private PersonnelAssignment BuildYibfAssignment(YibfIsTakibiEntry entry, Guid personnelId, string? columnKey)
    {
        var color = string.IsNullOrWhiteSpace(columnKey) ? null : YibfModule.GetCellBackgroundColor(entry.Id, columnKey);
        var priority = PersonnelPendingColorHelper.IsCriticalColor(color)
            ? PersonnelAssignmentPriority.Critical
            : PersonnelPendingColorHelper.IsPendingColor(color)
                ? PersonnelAssignmentPriority.Warning
                : PersonnelAssignmentPriority.None;

        return new PersonnelAssignment
        {
            PersonnelId = personnelId,
            SourceModule = PersonnelAssignmentSourceModule.YibfIsTakibi,
            SourceEntryId = entry.Id,
            SourceColumnKey = columnKey,
            Status = PersonnelAssignmentStatus.Open,
            AssignedAt = DateTime.Now,
            PrioritySnapshot = priority,
            FieldLabelSnapshot = string.IsNullOrWhiteSpace(columnKey)
                ? "Satır"
                : PersonnelAssignmentService.GetYibfFieldLabel(columnKey),
            SummarySnapshot = entry.JobName,
            ProjectIdentitySnapshot = entry.JobName,
            ModuleLabelSnapshot = IPersonnelAssignmentService.ModuleLabel(PersonnelAssignmentSourceModule.YibfIsTakibi)
        };
    }

    private static bool TryResolveCellContext(object? parameter, out object? entry, out string columnKey)
    {
        entry = null;
        columnKey = string.Empty;
        if (parameter is null)
        {
            return false;
        }

        if (parameter is YibfCellViewModel yibfCell)
        {
            entry = yibfCell.Row?.Entry;
            columnKey = yibfCell.ColumnKey;
            return entry is not null && !string.IsNullOrWhiteSpace(columnKey);
        }

        if (parameter is TadilatCellViewModel tadilatCell)
        {
            entry = tadilatCell.Row?.Entry;
            columnKey = tadilatCell.ColumnKey;
            return entry is not null && !string.IsNullOrWhiteSpace(columnKey);
        }

        var type = parameter.GetType();
        var entryProp = type.GetProperty("Entry") ?? type.GetProperty("Row") ?? type.GetProperty("DataContext");
        var keyProp = type.GetProperty("ColumnKey") ?? type.GetProperty("Key");
        var entryValue = entryProp?.GetValue(parameter);
        if (entryValue is not null && entryValue.GetType().GetProperty("Entry") is { } nestedEntry)
        {
            entry = nestedEntry.GetValue(entryValue);
        }
        else
        {
            entry = entryValue;
        }

        columnKey = keyProp?.GetValue(parameter) as string ?? string.Empty;

        if (entry is null && parameter is FrameworkElement fe)
        {
            entry = fe.DataContext;
            columnKey = fe.Tag as string ?? columnKey;
        }

        return entry is not null && !string.IsNullOrWhiteSpace(columnKey);
    }

    private async Task ExportPersonnelExcelAsync()
    {
        if (_personnelAssignmentService is null)
        {
            return;
        }

        var path = _fileDialogService.ShowSaveDialog("Personel görevleri Excel", "Excel (*.xlsx)|*.xlsx", ".xlsx");
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        var workbook = BuildPersonnelOpenTasksWorkbook();
        await _importExportService.ExportWorkbookAsync(workbook, path);
        _notificationService.ShowToast("Personel Excel çıktısı alındı.", ToastType.Success);
    }

    private async Task ExportPersonnelPdfAsync()
    {
        if (_personnelAssignmentService is null)
        {
            return;
        }

        var path = _fileDialogService.ShowSaveDialog("Personel görevleri PDF", "PDF (*.pdf)|*.pdf", ".pdf");
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        var workbook = BuildPersonnelOpenTasksWorkbook();
        await _importExportService.ExportWorkbookAsPdfAsync(workbook, path);
        _notificationService.ShowToast("Personel PDF çıktısı alındı.", ToastType.Success);
    }

    private ExcelWorkbookExportModel BuildPersonnelOpenTasksWorkbook()
    {
        var people = _personnelAssignmentService!.GetPersonnel().ToDictionary(p => p.Id);
        var rows = _personnelAssignmentService.GetAssignments()
            .Where(a => a.Status == PersonnelAssignmentStatus.Open)
            .OrderBy(a => people.TryGetValue(a.PersonnelId ?? Guid.Empty, out var p) ? p.Name : "Atanmamış")
            .ThenByDescending(a => a.AssignedAt)
            .Select(a => new ExcelRowExportModel
            {
                Cells =
                [
                    new ExcelCellExportModel { Value = a.PersonnelId is Guid id && people.TryGetValue(id, out var p) ? p.Name : "Atanmamış" },
                    new ExcelCellExportModel { Value = string.IsNullOrWhiteSpace(a.ModuleLabelSnapshot) ? IPersonnelAssignmentService.ModuleLabel(a.SourceModule) : a.ModuleLabelSnapshot },
                    new ExcelCellExportModel { Value = a.SummarySnapshot },
                    new ExcelCellExportModel { Value = "Açık" },
                    new ExcelCellExportModel { Value = a.ProjectIdentitySnapshot },
                    new ExcelCellExportModel { Value = a.AssignedAt.ToString("g") },
                    new ExcelCellExportModel { Value = IPersonnelAssignmentService.PriorityLabel(a.PrioritySnapshot) },
                    new ExcelCellExportModel { Value = a.FieldLabelSnapshot }
                ]
            })
            .ToList();

        return new ExcelWorkbookExportModel
        {
            Sheets =
            [
                new ExcelSheetExportModel
                {
                    Name = "Personel Görevleri",
                    Headers =
                    [
                        "Personel", "Sekme", "İş özeti", "Durum", "Proje kimliği", "Atama tarihi", "Öncelik", "Kaynak alan"
                    ],
                    Rows = rows
                }
            ]
        };
    }

    private static string FirstNonEmptyLocal(params string?[] values)
        => values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))?.Trim() ?? string.Empty;

    private async Task NotifySourceDeletedAsync(PersonnelAssignmentSourceModule module, Guid sourceEntryId)
    {
        if (_personnelAssignmentService is null)
        {
            return;
        }

        await _personnelAssignmentService.RemoveAssignmentsForSourceAsync(module, sourceEntryId);
    }
}
