using CommunityToolkit.Mvvm.Input;
using RizaCanKilicIsTakibi.Commands;
using RizaCanKilicIsTakibi.Helpers;
using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services.Abstractions;
using System.Collections.ObjectModel;

namespace RizaCanKilicIsTakibi.ViewModels;

public sealed class ActionModuleViewModel : ViewModelBase
{
    private static readonly ActionDistrictVisualStyle DefaultStyle = new("#FFE7ECF3", "#FFF8FAFD", "#FFFCFDFE", "#FFC8D3E2", "#FF223142");
    private static readonly IReadOnlyDictionary<string, ActionDistrictVisualStyle> Palette = new Dictionary<string, ActionDistrictVisualStyle>(StringComparer.OrdinalIgnoreCase)
    {
        ["GERZE"] = new("#FFD7E8D0", "#FFEEF7EA", "#FFF7FBF4", "#FF96B286", "#FF1E3B1A"),
        ["BOYABAT"] = new("#FFD2E1F0", "#FFEBF3FA", "#FFF6FAFD", "#FF90AFCE", "#FF18324F"),
        ["BOYABAT OSB"] = new("#FFC8D9EA", "#FFE5EFF9", "#FFF4F8FD", "#FF7E9DBD", "#FF17304A"),
        ["SARAYDÜZÜ"] = new("#FFCDD8F0", "#FFEAF0FB", "#FFF5F8FE", "#FF8FA4CC", "#FF222E49"),
        ["DURAĞAN"] = new("#FFD4E7C4", "#FFEDF6E5", "#FFF8FBEF", "#FF98B778", "#FF263A1B"),
        ["AYANCIK"] = new("#FFC8E2B8", "#FFE7F4DE", "#FFF5FAF1", "#FF8FB272", "#FF1F3414"),
        ["TÜRKELİ"] = new("#FFF1E0A7", "#FFFAF1D4", "#FFFDF8EA", "#FFD1B45F", "#FF4D3A08"),
        ["MERKEZ"] = new("#FFF9C83A", "#FFFEF4C7", "#FFFFF9E4", "#FFD8A820", "#FF4E3900"),
        ["SİNOP OSB"] = new("#FFF0C24E", "#FFFBEBC5", "#FFFEF6E2", "#FFD4A439", "#FF4B3500"),
        ["ERFELEK"] = new("#FFF0BE9A", "#FFFBE9DE", "#FFFEF5EF", "#FFD69A72", "#FF4A2A16")
    };

    private readonly IActionRepository _repo;
    private readonly IAddActionEntryDialogService _dialog;
    private readonly INotificationService _notifier;
    private readonly IConfirmationService _confirmationService;
    private readonly IUndoRedoService _undoRedo;
    private readonly AppSettings _settings;

    private bool _isInitialized;
    private bool _isBusy;
    private bool _hasUnsavedChanges;
    private ActionSubTab _selectedSubTab = ActionSubTab.Aksiyon;
    private string _selectedDistrict;
    private ActionEntry? _selectedEntry;
    private Guid? _lastSelectedEntryId;
    private readonly Dictionary<string, ActionDistrictGroup> _districtGroupLookup = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<Guid, ActionEntryRow> _rowLookup = [];

    public ActionModuleViewModel(
        IActionRepository actionRepository,
        IAddActionEntryDialogService dialogService,
        INotificationService notificationService,
        IConfirmationService confirmationService,
        IUndoRedoService undoRedoService,
        AppSettings settings)
    {
        _repo = actionRepository;
        _dialog = dialogService;
        _notifier = notificationService;
        _confirmationService = confirmationService;
        _undoRedo = undoRedoService;
        _settings = settings;

        Districts =
        [
            "GERZE", "BOYABAT", "BOYABAT OSB", "SARAYDÜZÜ", "DURAĞAN",
            "AYANCIK", "TÜRKELİ", "MERKEZ", "SİNOP OSB", "ERFELEK"
        ];
        _selectedDistrict = Districts.First();

        AksiyonEntries = [];
        AksiyonaEkleneceklerEntries = [];
        DistrictGroups = [];

        SelectActionSubTabCommand = new RelayCommand<ActionSubTab>(tab => SelectedSubTab = tab);
        SelectDistrictCommand = new RelayCommand<string>(district => { if (!string.IsNullOrWhiteSpace(district)) { SelectedDistrict = district; } });
        SelectEntryCommand = new RelayCommand<ActionEntry?>(entry => { if (entry is not null) { SelectedEntry = entry; } });

        BeginOwnerParcelEditCommand = new RelayCommand<ActionEntryRow?>(row => BeginEdit(row, owner: true));
        BeginWorkEditCommand = new RelayCommand<ActionEntryRow?>(row => BeginEdit(row, owner: false));
        CommitOwnerParcelEditCommand = new AsyncRelayCommand<ActionEntryRow?>(row => CommitEditAsync(row, owner: true));
        CommitWorkEditCommand = new AsyncRelayCommand<ActionEntryRow?>(row => CommitEditAsync(row, owner: false));
        CancelOwnerParcelEditCommand = new RelayCommand<ActionEntryRow?>(row => CancelEdit(row, owner: true));
        CancelWorkEditCommand = new RelayCommand<ActionEntryRow?>(row => CancelEdit(row, owner: false));

        OpenAddActionEntryDialogCommand = new AsyncRelayCommand<string>(OpenAddDialogAsync);
        InsertActionEntryAboveCommand = new AsyncRelayCommand<ActionEntry?>(entry => InsertEntryRelativeAsync(entry, insertAfter: false), entry => entry is not null);
        InsertActionEntryBelowCommand = new AsyncRelayCommand<ActionEntry?>(entry => InsertEntryRelativeAsync(entry, insertAfter: true), entry => entry is not null);
        DeleteActionEntryCommand = new AsyncRelayCommand(DeleteSelectedAsync, () => SelectedEntry is not null);
        MoveActionEntryUpCommand = new AsyncRelayCommand(() => MoveSelectedAsync(-1), CanMoveUp);
        MoveActionEntryDownCommand = new AsyncRelayCommand(() => MoveSelectedAsync(1), CanMoveDown);
        MoveToAksiyonCommand = new AsyncRelayCommand(MoveToAksiyonAsync, CanMoveToAksiyon);

        RefreshDistrictGroups();
    }

    public ObservableCollection<string> Districts { get; }
    public ObservableRangeCollection<ActionEntry> AksiyonEntries { get; }
    public ObservableRangeCollection<ActionEntry> AksiyonaEkleneceklerEntries { get; }
    public ObservableRangeCollection<ActionDistrictGroup> DistrictGroups { get; }

    public bool IsBusy { get => _isBusy; private set => SetProperty(ref _isBusy, value); }
    public bool HasUnsavedChanges { get => _hasUnsavedChanges; private set => SetProperty(ref _hasUnsavedChanges, value); }
    public ActionSubTab SelectedSubTab
    {
        get => _selectedSubTab;
        set
        {
            if (SetProperty(ref _selectedSubTab, value))
            {
                RefreshDistrictGroups();
                MoveToAksiyonCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string SelectedDistrict { get => _selectedDistrict; set => SetProperty(ref _selectedDistrict, value); }
    public ActionEntry? SelectedEntry
    {
        get => _selectedEntry;
        set
        {
            if (SetProperty(ref _selectedEntry, value))
            {
                if (value is not null) { SelectedDistrict = value.District; }
                UpdateRowSelections();
                InsertActionEntryAboveCommand.NotifyCanExecuteChanged();
                InsertActionEntryBelowCommand.NotifyCanExecuteChanged();
                DeleteActionEntryCommand.NotifyCanExecuteChanged();
                MoveActionEntryUpCommand.NotifyCanExecuteChanged();
                MoveActionEntryDownCommand.NotifyCanExecuteChanged();
                MoveToAksiyonCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public RelayCommand<ActionSubTab> SelectActionSubTabCommand { get; }
    public RelayCommand<string> SelectDistrictCommand { get; }
    public RelayCommand<ActionEntry?> SelectEntryCommand { get; }

    public RelayCommand<ActionEntryRow?> BeginOwnerParcelEditCommand { get; }
    public RelayCommand<ActionEntryRow?> BeginWorkEditCommand { get; }
    public AsyncRelayCommand<ActionEntryRow?> CommitOwnerParcelEditCommand { get; }
    public AsyncRelayCommand<ActionEntryRow?> CommitWorkEditCommand { get; }
    public RelayCommand<ActionEntryRow?> CancelOwnerParcelEditCommand { get; }
    public RelayCommand<ActionEntryRow?> CancelWorkEditCommand { get; }

    public AsyncRelayCommand<string> OpenAddActionEntryDialogCommand { get; }
    public AsyncRelayCommand<ActionEntry?> InsertActionEntryAboveCommand { get; }
    public AsyncRelayCommand<ActionEntry?> InsertActionEntryBelowCommand { get; }
    public AsyncRelayCommand DeleteActionEntryCommand { get; }
    public AsyncRelayCommand MoveActionEntryUpCommand { get; }
    public AsyncRelayCommand MoveActionEntryDownCommand { get; }
    public AsyncRelayCommand MoveToAksiyonCommand { get; }

    public async Task InitializeAsync()
    {
        if (_isInitialized) { return; }
        _isInitialized = true;
        IsBusy = true;
        try
        {
            var aksiyon = await _repo.GetByCategoryAsync(ActionEntryCategory.Aksiyon);
            var eklenecekler = await _repo.GetByCategoryAsync(ActionEntryCategory.AksiyonaEklenecekler);
            if (_settings.SeedSampleDataOnEmpty && aksiyon.Count == 0 && eklenecekler.Count == 0)
            {
                var seed = BuildSeedEntries();
                await _repo.SaveManyAsync(seed);
                aksiyon = seed.Where(x => x.Category == ActionEntryCategory.Aksiyon).ToList();
                eklenecekler = seed.Where(x => x.Category == ActionEntryCategory.AksiyonaEklenecekler).ToList();
            }
            ReplaceCollection(AksiyonEntries, aksiyon);
            ReplaceCollection(AksiyonaEkleneceklerEntries, eklenecekler);
            RefreshDistrictGroups();
            HasUnsavedChanges = false;
        }
        catch (Exception ex)
        {
            _isInitialized = false;
            _notifier.ShowToast($"Aksiyon yükleme hatası: {ex.Message}", ToastType.Error, TimeSpan.FromSeconds(5));
        }
        finally { IsBusy = false; }
    }

    public IReadOnlyList<ActionEntry> GetAllEntriesSnapshot() => AksiyonEntries.Concat(AksiyonaEkleneceklerEntries).Select(CloneEntry).ToList();

    public void LoadFromBackup(IEnumerable<ActionEntry> entries, bool markDirty = true)
    {
        var source = entries ?? Array.Empty<ActionEntry>();
        var aksiyon = source
            .Where(item => item.Category == ActionEntryCategory.Aksiyon)
            .OrderBy(item => item.District, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.DisplayOrder)
            .ThenBy(item => item.UpdatedAt)
            .Select(CloneEntry)
            .ToList();

        var eklenecekler = source
            .Where(item => item.Category == ActionEntryCategory.AksiyonaEklenecekler)
            .OrderBy(item => item.District, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.DisplayOrder)
            .ThenBy(item => item.UpdatedAt)
            .Select(CloneEntry)
            .ToList();

        ReplaceCollection(AksiyonEntries, aksiyon);
        ReplaceCollection(AksiyonaEkleneceklerEntries, eklenecekler);
        SelectedEntry = null;
        RefreshDistrictGroups();
        HasUnsavedChanges = markDirty;
    }

    public async Task PersistAsync(bool showErrorToast = true)
    {
        try
        {
            var snapshot = CaptureSnapshot();
            var current = NormalizeEntriesForPersist(snapshot.AksiyonEntries, ActionEntryCategory.Aksiyon)
                .Concat(NormalizeEntriesForPersist(snapshot.AksiyonaEkleneceklerEntries, ActionEntryCategory.AksiyonaEklenecekler))
                .ToList();

            await _repo.SaveManyAsync(current);

            HasUnsavedChanges = false;
        }
        catch (Exception ex)
        {
            HasUnsavedChanges = true;
            if (showErrorToast) { _notifier.ShowToast($"Aksiyon kayıt hatası: {ex.Message}", ToastType.Error); }
        }
    }

    public async Task CommitPendingEditsAsync()
    {
        var rows = DistrictGroups
            .SelectMany(group => group.Rows)
            .Where(row => !row.IsPlaceholder && row.IsInteractive && (row.IsEditingOwnerParcel || row.IsEditingWork))
            .ToList();

        foreach (var row in rows)
        {
            if (row.IsEditingOwnerParcel)
            {
                await CommitEditAsync(row, owner: true);
            }

            if (row.IsEditingWork)
            {
                await CommitEditAsync(row, owner: false);
            }
        }
    }

    private void BeginEdit(ActionEntryRow? row, bool owner)
    {
        if (row?.Entry is null || !row.IsInteractive) { return; }
        SelectEntry(row.Entry);
        if (owner)
        {
            row.OwnerParcelDraft = row.OwnerParcelText;
            row.IsEditingWork = false;
            row.IsEditingOwnerParcel = true;
        }
        else
        {
            row.WorkDraft = row.WorkText;
            row.IsEditingOwnerParcel = false;
            row.IsEditingWork = true;
        }
    }

    private void CancelEdit(ActionEntryRow? row, bool owner)
    {
        if (row is null) { return; }
        if (owner) { row.OwnerParcelDraft = row.OwnerParcelText; row.IsEditingOwnerParcel = false; }
        else { row.WorkDraft = row.WorkText; row.IsEditingWork = false; }
    }

    private async Task CommitEditAsync(ActionEntryRow? row, bool owner)
    {
        if (row?.Entry is null || !row.IsInteractive) { return; }
        if (owner && !row.IsEditingOwnerParcel) { return; }
        if (!owner && !row.IsEditingWork) { return; }

        var draft = (owner ? row.OwnerParcelDraft : row.WorkDraft).Trim();
        var current = owner ? row.OwnerParcelText : row.WorkText;
        if (string.IsNullOrWhiteSpace(draft))
        {
            _notifier.ShowToast("Boş değer kaydedilemez.", ToastType.Warning);
            CancelEdit(row, owner);
            return;
        }
        if (string.Equals(draft, current, StringComparison.Ordinal)) { CancelEdit(row, owner); return; }

        var before = CaptureSnapshot();
        var after = CloneSnapshot(before);
        var target = FindSnapshotEntry(after, row.Entry.Id);
        if (target is null) { return; }
        if (owner) { target.OwnerParcelText = draft; } else { target.WorkText = draft; }
        target.UpdatedAt = DateTime.Now;

        if (owner)
        {
            row.OwnerParcelText = draft;
            row.OwnerParcelDraft = draft;
            row.IsEditingOwnerParcel = false;
        }
        else
        {
            row.WorkText = draft;
            row.WorkDraft = draft;
            row.IsEditingWork = false;
        }

        await ExecuteSnapshotActionAsync("Aksiyon hücre güncelle", before, after, row.Entry.Id, SelectedEntry?.Id);
        _notifier.ShowToast("Hücre güncellendi.", ToastType.Success, TimeSpan.FromSeconds(2));
    }

    private async Task OpenAddDialogAsync(string? district)
    {
        var fallback = Districts.FirstOrDefault() ?? string.Empty;
        var targetDistrict = string.IsNullOrWhiteSpace(district) ? SelectedDistrict : district;
        targetDistrict = string.IsNullOrWhiteSpace(targetDistrict) ? fallback : targetDistrict;
        if (string.IsNullOrWhiteSpace(targetDistrict)) { return; }
        SelectDistrict(targetDistrict);

        var category = MapCategory(SelectedSubTab);
        var created = await _dialog.ShowDialogAsync(targetDistrict, category);
        if (created is null) { return; }

        created.Category = category;
        created.District = targetDistrict;
        created.CreatedAt = DateTime.Now;
        created.UpdatedAt = DateTime.Now;

        var before = CaptureSnapshot();
        var after = CloneSnapshot(before);
        var list = GetSnapshotCollection(after, category);
        created.DisplayOrder = NextDisplayOrder(list, targetDistrict);
        list.Add(CloneEntry(created));
        NormalizeDistrictOrder(list, targetDistrict);

        await ExecuteSnapshotActionAsync("Aksiyon kayıt ekle", before, after, created.Id, SelectedEntry?.Id);
        _notifier.ShowToast("Aksiyon kaydı eklendi.", ToastType.Success);
    }

    private async Task InsertEntryRelativeAsync(ActionEntry? anchorEntry, bool insertAfter)
    {
        if (anchorEntry is null)
        {
            return;
        }

        var currentEntry = AksiyonEntries.Concat(AksiyonaEkleneceklerEntries).FirstOrDefault(item => item.Id == anchorEntry.Id);
        if (currentEntry is null)
        {
            return;
        }

        SelectEntry(currentEntry);
        var created = await _dialog.ShowDialogAsync(currentEntry.District, currentEntry.Category);
        if (created is null)
        {
            return;
        }

        created.Category = currentEntry.Category;
        created.District = currentEntry.District;
        created.CreatedAt = DateTime.Now;
        created.UpdatedAt = DateTime.Now;

        var before = CaptureSnapshot();
        var after = CloneSnapshot(before);
        var list = GetSnapshotCollection(after, currentEntry.Category);
        var target = list.FirstOrDefault(item => item.Id == currentEntry.Id);
        if (target is null)
        {
            return;
        }

        var insertDisplayOrder = target.DisplayOrder + (insertAfter ? 1 : 0);
        foreach (var item in list.Where(item =>
                     item.District.Equals(currentEntry.District, StringComparison.OrdinalIgnoreCase)
                     && item.DisplayOrder >= insertDisplayOrder))
        {
            item.DisplayOrder++;
            item.UpdatedAt = DateTime.Now;
        }

        created.DisplayOrder = insertDisplayOrder;
        list.Add(CloneEntry(created));
        NormalizeDistrictOrder(list, currentEntry.District);

        await ExecuteSnapshotActionAsync(
            insertAfter ? "Aksiyon alta kayıt ekle" : "Aksiyon üste kayıt ekle",
            before,
            after,
            created.Id,
            currentEntry.Id);

        _notifier.ShowToast(insertAfter ? "Seçili satırın altına kayıt eklendi." : "Seçili satırın üstüne kayıt eklendi.", ToastType.Success);
    }

    private async Task DeleteSelectedAsync()
    {
        var entry = SelectedEntry;
        if (entry is null) { return; }

        if (!_confirmationService.Confirm(new ConfirmationRequest
            {
                Kind = ConfirmationKind.Delete,
                Title = "Aksiyon Kaydını Sil",
                Message = $"\"{entry.OwnerParcelText}\" kaydı silinecek.\n\nDevam edilsin mi?",
                IsDestructive = true
            }))
        {
            return;
        }

        var before = CaptureSnapshot();
        var after = CloneSnapshot(before);
        var list = GetSnapshotCollection(after, entry.Category);
        if (list.RemoveAll(x => x.Id == entry.Id) == 0) { return; }
        NormalizeDistrictOrder(list, entry.District);

        await ExecuteSnapshotActionAsync("Aksiyon kayıt sil", before, after, null, entry.Id);
        _notifier.ShowToast("Aksiyon kaydı silindi.", ToastType.Warning);
    }

    private async Task MoveSelectedAsync(int direction)
    {
        var selected = SelectedEntry;
        if (selected is null) { return; }

        var before = CaptureSnapshot();
        var after = CloneSnapshot(before);
        var list = GetSnapshotCollection(after, selected.Category);
        var districtItems = list.Where(x => x.District.Equals(selected.District, StringComparison.OrdinalIgnoreCase)).OrderBy(x => x.DisplayOrder).ToList();
        var i = districtItems.FindIndex(x => x.Id == selected.Id);
        var t = i + direction;
        if (i < 0 || t < 0 || t >= districtItems.Count) { return; }
        (districtItems[i], districtItems[t]) = (districtItems[t], districtItems[i]);
        for (var idx = 0; idx < districtItems.Count; idx++) { districtItems[idx].DisplayOrder = idx; districtItems[idx].UpdatedAt = DateTime.Now; }

        await ExecuteSnapshotActionAsync("Aksiyon sıralama değiştir", before, after, selected.Id, selected.Id);
    }

    private async Task MoveToAksiyonAsync()
    {
        var selected = SelectedEntry;
        if (selected is null || selected.Category != ActionEntryCategory.AksiyonaEklenecekler) { return; }

        var before = CaptureSnapshot();
        var after = CloneSnapshot(before);
        var source = after.AksiyonaEkleneceklerEntries;
        var target = after.AksiyonEntries;
        var moving = source.FirstOrDefault(x => x.Id == selected.Id);
        if (moving is null) { return; }

        source.Remove(moving);
        NormalizeDistrictOrder(source, moving.District);
        moving.Category = ActionEntryCategory.Aksiyon;
        moving.DisplayOrder = NextDisplayOrder(target, moving.District);
        moving.UpdatedAt = DateTime.Now;
        target.Add(moving);
        NormalizeDistrictOrder(target, moving.District);

        await ExecuteSnapshotActionAsync("Aksiyona aktar", before, after, null, selected.Id);
        _notifier.ShowToast("Kayıt aksiyon listesine aktarıldı.", ToastType.Success);
    }

    private bool CanMoveUp()
    {
        if (SelectedEntry is null) { return false; }
        var districtItems = GetCurrentCollection().Where(x => x.District.Equals(SelectedEntry.District, StringComparison.OrdinalIgnoreCase)).OrderBy(x => x.DisplayOrder).ToList();
        return districtItems.FindIndex(x => x.Id == SelectedEntry.Id) > 0;
    }

    private bool CanMoveDown()
    {
        if (SelectedEntry is null) { return false; }
        var districtItems = GetCurrentCollection().Where(x => x.District.Equals(SelectedEntry.District, StringComparison.OrdinalIgnoreCase)).OrderBy(x => x.DisplayOrder).ToList();
        var i = districtItems.FindIndex(x => x.Id == SelectedEntry.Id);
        return i >= 0 && i < districtItems.Count - 1;
    }

    private bool CanMoveToAksiyon() => SelectedEntry is not null && SelectedSubTab == ActionSubTab.AksiyonaEklenecekler && SelectedEntry.Category == ActionEntryCategory.AksiyonaEklenecekler;

    private Task ExecuteSnapshotActionAsync(string description, ActionCollectionsSnapshot before, ActionCollectionsSnapshot after, Guid? executeSelectedId, Guid? undoSelectedId)
    {
        _undoRedo.Execute(new DelegateUndoableAction(
            description,
            () =>
            {
                ApplySnapshot(after, executeSelectedId);
                HasUnsavedChanges = true;
            },
            () =>
            {
                ApplySnapshot(before, undoSelectedId);
                HasUnsavedChanges = true;
            }));
        return Task.CompletedTask;
    }

    private void ApplySnapshot(ActionCollectionsSnapshot snapshot, Guid? selectedId)
    {
        ReplaceCollection(AksiyonEntries, snapshot.AksiyonEntries.Select(CloneEntry));
        ReplaceCollection(AksiyonaEkleneceklerEntries, snapshot.AksiyonaEkleneceklerEntries.Select(CloneEntry));
        RefreshDistrictGroups();
        if (selectedId.HasValue)
        {
            SelectedEntry = AksiyonEntries.Concat(AksiyonaEkleneceklerEntries).FirstOrDefault(x => x.Id == selectedId.Value);
        }
        else { SelectedEntry = null; }
    }

    private ActionCollectionsSnapshot CaptureSnapshot() => new(AksiyonEntries.Select(CloneEntry).ToList(), AksiyonaEkleneceklerEntries.Select(CloneEntry).ToList());

    private static IReadOnlyList<ActionEntry> NormalizeEntriesForPersist(IEnumerable<ActionEntry> entries, ActionEntryCategory category)
    {
        var normalized = new List<ActionEntry>();
        foreach (var districtGroup in entries
                     .GroupBy(item => item.District, StringComparer.OrdinalIgnoreCase)
                     .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase))
        {
            foreach (var entry in districtGroup.OrderBy(item => item.DisplayOrder).ThenBy(item => item.UpdatedAt).Select(CloneEntry).Select((item, index) =>
                     {
                         item.Category = category;
                         item.DisplayOrder = index;
                         return item;
                     }))
            {
                normalized.Add(entry);
            }
        }

        return normalized;
    }
    private static ActionCollectionsSnapshot CloneSnapshot(ActionCollectionsSnapshot snapshot) => new(snapshot.AksiyonEntries.Select(CloneEntry).ToList(), snapshot.AksiyonaEkleneceklerEntries.Select(CloneEntry).ToList());

    private ObservableRangeCollection<ActionEntry> GetCurrentCollection() => SelectedSubTab == ActionSubTab.Aksiyon ? AksiyonEntries : AksiyonaEkleneceklerEntries;
    private static ActionEntryCategory MapCategory(ActionSubTab tab) => tab == ActionSubTab.Aksiyon ? ActionEntryCategory.Aksiyon : ActionEntryCategory.AksiyonaEklenecekler;
    private static List<ActionEntry> GetSnapshotCollection(ActionCollectionsSnapshot snapshot, ActionEntryCategory category) => category == ActionEntryCategory.Aksiyon ? snapshot.AksiyonEntries : snapshot.AksiyonaEkleneceklerEntries;
    private static ActionEntry? FindSnapshotEntry(ActionCollectionsSnapshot snapshot, Guid id) => snapshot.AksiyonEntries.Concat(snapshot.AksiyonaEkleneceklerEntries).FirstOrDefault(x => x.Id == id);
    private static int NextDisplayOrder(IEnumerable<ActionEntry> entries, string district) => entries.Where(x => x.District.Equals(district, StringComparison.OrdinalIgnoreCase)).Select(x => x.DisplayOrder).DefaultIfEmpty(-1).Max() + 1;

    private static void NormalizeDistrictOrder(List<ActionEntry> entries, string district)
    {
        var items = entries.Where(x => x.District.Equals(district, StringComparison.OrdinalIgnoreCase)).OrderBy(x => x.DisplayOrder).ThenBy(x => x.UpdatedAt).ToList();
        for (var i = 0; i < items.Count; i++) { items[i].DisplayOrder = i; items[i].UpdatedAt = DateTime.Now; }
    }

    private static void ReplaceCollection(ObservableRangeCollection<ActionEntry> target, IEnumerable<ActionEntry> source)
    {
        target.ReplaceRange(source);
    }

    private static ActionEntry CloneEntry(ActionEntry entry) => new()
    {
        Id = entry.Id,
        Category = entry.Category,
        District = entry.District,
        OwnerParcelText = entry.OwnerParcelText,
        WorkText = entry.WorkText,
        DisplayOrder = entry.DisplayOrder,
        CreatedAt = entry.CreatedAt,
        UpdatedAt = entry.UpdatedAt
    };

    private void SelectEntry(ActionEntry? entry)
    {
        if (entry is not null)
        {
            SelectedEntry = entry;
        }
    }

    private void SelectDistrict(string? district)
    {
        if (!string.IsNullOrWhiteSpace(district))
        {
            SelectedDistrict = district;
        }
    }

    private static ActionDistrictVisualStyle ResolveStyle(string district) => Palette.TryGetValue(district, out var style) ? style : DefaultStyle;

    private void RefreshDistrictGroups()
    {
        var current = GetCurrentCollection();
        var orderedDistricts = GetOrderedDistricts(current);
        var activeDistricts = new HashSet<string>(orderedDistricts, StringComparer.OrdinalIgnoreCase);

        foreach (var obsoleteDistrict in _districtGroupLookup.Keys.Where(key => !activeDistricts.Contains(key)).ToList())
        {
            var obsoleteGroup = _districtGroupLookup[obsoleteDistrict];
            foreach (var obsoleteRow in obsoleteGroup.Rows.Where(row => !row.IsPlaceholder && row.Entry is not null).ToList())
            {
                _rowLookup.Remove(obsoleteRow.Entry!.Id);
            }

            DistrictGroups.Remove(obsoleteGroup);
            _districtGroupLookup.Remove(obsoleteDistrict);
        }

        for (var index = 0; index < orderedDistricts.Count; index++)
        {
            var district = orderedDistricts[index];
            var style = ResolveStyle(district);
            if (!_districtGroupLookup.TryGetValue(district, out var group))
            {
                group = new ActionDistrictGroup(district, new ObservableCollection<ActionEntryRow>(), false, style.DistrictBackground, style.BorderBrush, style.Foreground);
                _districtGroupLookup[district] = group;
                DistrictGroups.Insert(Math.Min(index, DistrictGroups.Count), group);
            }

            var districtEntries = current
                .Where(x => x.District.Equals(district, StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.UpdatedAt)
                .ToList();

            SyncDistrictGroup(group, districtEntries, style);

            var currentIndex = DistrictGroups.IndexOf(group);
            if (currentIndex >= 0 && currentIndex != index)
            {
                DistrictGroups.Move(currentIndex, index);
            }
        }

        if (SelectedEntry is not null && !current.Any(x => x.Id == SelectedEntry.Id))
        {
            _selectedEntry = null;
            OnPropertyChanged(nameof(SelectedEntry));
        }

        UpdateRowSelections();
        InsertActionEntryAboveCommand.NotifyCanExecuteChanged();
        InsertActionEntryBelowCommand.NotifyCanExecuteChanged();
        DeleteActionEntryCommand.NotifyCanExecuteChanged();
        MoveActionEntryUpCommand.NotifyCanExecuteChanged();
        MoveActionEntryDownCommand.NotifyCanExecuteChanged();
        MoveToAksiyonCommand.NotifyCanExecuteChanged();
    }

    private List<string> GetOrderedDistricts(IEnumerable<ActionEntry> current)
    {
        var known = new HashSet<string>(Districts, StringComparer.OrdinalIgnoreCase);
        var orderedDistricts = Districts.ToList();
        orderedDistricts.AddRange(current.Select(x => x.District).Where(d => !known.Contains(d)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(d => d));
        return orderedDistricts;
    }

    private void SyncDistrictGroup(ActionDistrictGroup group, IReadOnlyList<ActionEntry> districtEntries, ActionDistrictVisualStyle style)
    {
        var rows = group.Rows;

        if (districtEntries.Count == 0)
        {
            foreach (var obsoleteRow in rows.Where(row => row.Entry is not null).ToList())
            {
                rows.Remove(obsoleteRow);
                _rowLookup.Remove(obsoleteRow.Entry!.Id);
            }

            rows.Clear();
            group.HasItems = false;
            return;
        }

        var desiredIds = districtEntries.Select(x => x.Id).ToHashSet();
        foreach (var obsoleteRow in rows.Where(row => row.Entry is not null && !desiredIds.Contains(row.Entry.Id)).ToList())
        {
            rows.Remove(obsoleteRow);
            _rowLookup.Remove(obsoleteRow.Entry!.Id);
        }

        for (var index = 0; index < districtEntries.Count; index++)
        {
            var entry = districtEntries[index];
            if (!_rowLookup.TryGetValue(entry.Id, out var row))
            {
                row = ActionEntryRow.CreateEntry(entry, style);
                _rowLookup[entry.Id] = row;
            }
            else
            {
                UpdateRow(row, entry);
            }

            if (index >= rows.Count)
            {
                rows.Add(row);
                continue;
            }

            if (!ReferenceEquals(rows[index], row))
            {
                var currentIndex = rows.IndexOf(row);
                if (currentIndex >= 0)
                {
                    rows.Move(currentIndex, index);
                }
                else
                {
                    rows.Insert(index, row);
                }
            }
        }

        while (rows.Count > districtEntries.Count)
        {
            var extraRow = rows[^1];
            rows.RemoveAt(rows.Count - 1);
            if (extraRow.Entry is not null)
            {
                _rowLookup.Remove(extraRow.Entry.Id);
            }
        }

        group.HasItems = true;
    }

    private static void UpdateRow(ActionEntryRow row, ActionEntry entry)
    {
        row.UpdateEntry(entry);
        if (!row.IsEditingOwnerParcel)
        {
            row.OwnerParcelText = entry.OwnerParcelText;
            row.OwnerParcelDraft = entry.OwnerParcelText;
        }

        if (!row.IsEditingWork)
        {
            row.WorkText = entry.WorkText;
            row.WorkDraft = entry.WorkText;
        }
    }

    private void UpdateRowSelections()
    {
        if (_lastSelectedEntryId.HasValue)
        {
            if (_rowLookup.TryGetValue(_lastSelectedEntryId.Value, out var previousRow))
            {
                previousRow.IsSelected = false;
            }
        }

        var selectedId = SelectedEntry?.Id;
        if (selectedId.HasValue && _rowLookup.TryGetValue(selectedId.Value, out var selectedRow))
        {
            selectedRow.IsSelected = true;
        }

        _lastSelectedEntryId = selectedId;
    }

    private static List<ActionEntry> BuildSeedEntries() =>
    [
        new ActionEntry
        {
            Category = ActionEntryCategory.Aksiyon,
            District = "GERZE",
            OwnerParcelText = "457-6 ALAATTİN BEYAZ İSTİNAT",
            WorkText = "İSTİNAT YİBF TAAHHÜTNAME SÖZLEŞME ALINACAK.",
            DisplayOrder = 0,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        },
        new ActionEntry
        {
            Category = ActionEntryCategory.Aksiyon,
            District = "MERKEZ",
            OwnerParcelText = "430-11 HİF OTEL",
            WorkText = "BODRUM KAT TAVAN KAROT ALDIRILACAK.",
            DisplayOrder = 0,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        },
        new ActionEntry
        {
            Category = ActionEntryCategory.AksiyonaEklenecekler,
            District = "BOYABAT",
            OwnerParcelText = "815-5 YASİN ERGÜN",
            WorkText = "YAPI SAHİBİ MÜT SÖZLEŞMESİ VE RUHSAT EKLERİ ALINACAK.",
            DisplayOrder = 0,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        },
        new ActionEntry
        {
            Category = ActionEntryCategory.AksiyonaEklenecekler,
            District = "TÜRKELİ",
            OwnerParcelText = "216-9 GÖKSU YAPI",
            WorkText = "YİBF TAAHHÜT SÖZLEŞME EKSİK. PROJELER ALINACAK.",
            DisplayOrder = 0,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        }
    ];

    private sealed record ActionCollectionsSnapshot(List<ActionEntry> AksiyonEntries, List<ActionEntry> AksiyonaEkleneceklerEntries);
}

public sealed record ActionDistrictVisualStyle(
    string DistrictBackground,
    string RowBackground,
    string PlaceholderBackground,
    string BorderBrush,
    string Foreground);

public sealed class ActionDistrictGroup : ViewModelBase
{
    private bool _hasItems;

    public ActionDistrictGroup(string district, ObservableCollection<ActionEntryRow> rows, bool hasItems, string districtBackground, string districtBorderBrush, string districtForeground)
    {
        District = district;
        Rows = rows;
        _hasItems = hasItems;
        DistrictBackground = districtBackground;
        DistrictBorderBrush = districtBorderBrush;
        DistrictForeground = districtForeground;
    }

    public string District { get; }
    public ObservableCollection<ActionEntryRow> Rows { get; }
    public bool HasItems
    {
        get => _hasItems;
        set => SetProperty(ref _hasItems, value);
    }
    public string DistrictBackground { get; }
    public string DistrictBorderBrush { get; }
    public string DistrictForeground { get; }
}

public sealed class ActionEntryRow : ViewModelBase
{
    private bool _isSelected;
    private bool _isEditingOwnerParcel;
    private bool _isEditingWork;
    private string _ownerParcelText;
    private string _workText;
    private string _ownerParcelDraft;
    private string _workDraft;

    private ActionEntryRow(ActionEntry? entry, bool isPlaceholder, string rowBackground, string borderBrush, string foreground)
    {
        Entry = entry;
        IsPlaceholder = isPlaceholder;
        IsInteractive = !isPlaceholder;
        RowBackground = rowBackground;
        RowBorderBrush = borderBrush;
        RowForeground = foreground;
        _ownerParcelText = entry?.OwnerParcelText ?? string.Empty;
        _workText = entry?.WorkText ?? string.Empty;
        _ownerParcelDraft = _ownerParcelText;
        _workDraft = _workText;
    }

    public ActionEntry? Entry { get; private set; }
    public bool IsPlaceholder { get; }
    public bool IsInteractive { get; }
    public string RowBackground { get; }
    public string RowBorderBrush { get; }
    public string RowForeground { get; }

    public bool IsSelected { get => _isSelected; set => SetProperty(ref _isSelected, value); }
    public bool IsEditingOwnerParcel { get => _isEditingOwnerParcel; set => SetProperty(ref _isEditingOwnerParcel, value); }
    public bool IsEditingWork { get => _isEditingWork; set => SetProperty(ref _isEditingWork, value); }
    public string OwnerParcelText { get => _ownerParcelText; set => SetProperty(ref _ownerParcelText, value); }
    public string WorkText { get => _workText; set => SetProperty(ref _workText, value); }
    public string OwnerParcelDraft { get => _ownerParcelDraft; set => SetProperty(ref _ownerParcelDraft, value); }
    public string WorkDraft { get => _workDraft; set => SetProperty(ref _workDraft, value); }

    public static ActionEntryRow CreateEntry(ActionEntry entry, ActionDistrictVisualStyle style) => new(entry, false, style.RowBackground, style.BorderBrush, style.Foreground);
    public static ActionEntryRow CreatePlaceholder(ActionDistrictVisualStyle style) => new(null, true, style.PlaceholderBackground, style.BorderBrush, style.Foreground);

    public void UpdateEntry(ActionEntry entry)
    {
        Entry = entry;
    }
}

