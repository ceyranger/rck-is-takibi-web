using CommunityToolkit.Mvvm.Input;
using RizaCanKilicIsTakibi.Commands;
using RizaCanKilicIsTakibi.Helpers;
using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services;
using RizaCanKilicIsTakibi.Services.Abstractions;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace RizaCanKilicIsTakibi.ViewModels;

public sealed class KarotModuleViewModel : ViewModelBase
{
    private static readonly HashSet<string> PersistedProperties =
    [
        nameof(KarotEntry.SampleReceivedDate),
        nameof(KarotEntry.YibfNo),
        nameof(KarotEntry.AdaParsel),
        nameof(KarotEntry.YapiSahibi),
        nameof(KarotEntry.Muteahhit),
        nameof(KarotEntry.KatBilgisi),
        nameof(KarotEntry.BetonSinifi),
        nameof(KarotEntry.TwentyEightDayResult),
        nameof(KarotEntry.BetonFirmasi),
        nameof(KarotEntry.Laboratuvar),
        nameof(KarotEntry.Aciklama),
        nameof(KarotEntry.Status),
        nameof(KarotEntry.DisplayOrder)
    ];

    private static readonly CultureInfo TurkishCulture = CultureInfo.GetCultureInfo("tr-TR");
    private static readonly Brush WaitingOrNegativeBrush = CreateBrush("#FFFF0000");
    private static readonly Brush PendingResultBrush = CreateBrush("#FFFFFF00");
    private static readonly Brush PositiveBrush = CreateBrush("#FF4F81BD");

    private readonly IKarotRepository _repository;
    private readonly IKarotStatusDialogService _statusDialogService;
    private readonly INotificationService _notificationService;
    private readonly IConfirmationService _confirmationService;
    private readonly ITadilatCellNoteDialogService _noteDialogService;
    private readonly IUndoRedoService _undoRedoService;
    private readonly IClipboardService _clipboardService;
    private readonly IReadOnlyList<ColumnFilterViewModel> _columnFilters;
    private readonly Dictionary<Guid, KarotEntryRowViewModel> _rowLookup = [];
    private readonly Dictionary<string, KarotCellState> _cellStateLookup = new(StringComparer.OrdinalIgnoreCase);
    private bool _isAddingEntry;
    private bool _isInitialized;
    private bool _isPersisting;
    private bool _isRefreshQueued;
    private bool _hasUnsavedChanges;
    private KarotSubTab _selectedSubTab = KarotSubTab.Bekleyen;
    private KarotEntry? _selectedEntry;
    private sealed record KarotUndoSnapshot(
        IReadOnlyList<KarotEntry> Entries,
        IReadOnlyList<KarotCellState> CellStates,
        KarotSubTab SelectedSubTab,
        Guid? SelectedEntryId,
        bool HasUnsavedChanges);

    public KarotModuleViewModel(
        IKarotRepository repository,
        IKarotStatusDialogService statusDialogService,
        INotificationService notificationService,
        IConfirmationService confirmationService,
        ITadilatCellNoteDialogService noteDialogService,
        IUndoRedoService undoRedoService,
        IClipboardService? clipboardService = null)
    {
        _repository = repository;
        _statusDialogService = statusDialogService;
        _notificationService = notificationService;
        _confirmationService = confirmationService;
        _noteDialogService = noteDialogService;
        _undoRedoService = undoRedoService;
        _clipboardService = clipboardService ?? new ClipboardService();

        Entries = [];
        Entries.CollectionChanged += OnEntriesCollectionChanged;

        VisibleEntries = [];
        VisibleEntries.CollectionChanged += (_, _) => OnPropertyChanged(nameof(VisibleEntryCount));

        VisibleRows = [];
        CellStates = [];

        SampleReceivedDateColumnFilter = new ColumnFilterViewModel("Numune Alınma Tarihi", RefreshVisibleEntries, ApplyColumnSort);
        YibfNoColumnFilter = new ColumnFilterViewModel("YİBF No", RefreshVisibleEntries, ApplyColumnSort);
        AdaParselColumnFilter = new ColumnFilterViewModel("Ada Parsel", RefreshVisibleEntries, ApplyColumnSort);
        YapiSahibiColumnFilter = new ColumnFilterViewModel("Yapı Sahibi", RefreshVisibleEntries, ApplyColumnSort);
        MuteahhitColumnFilter = new ColumnFilterViewModel("Müteahhit", RefreshVisibleEntries, ApplyColumnSort);
        KatBilgisiColumnFilter = new ColumnFilterViewModel("Kat Bilgisi", RefreshVisibleEntries, ApplyColumnSort);
        BetonSinifiColumnFilter = new ColumnFilterViewModel("Beton Sınıfı", RefreshVisibleEntries, ApplyColumnSort);
        TwentyEightDayResultColumnFilter = new ColumnFilterViewModel("28 Günlük Sonuç", RefreshVisibleEntries, ApplyColumnSort);
        BetonFirmasiColumnFilter = new ColumnFilterViewModel("Beton Firması", RefreshVisibleEntries, ApplyColumnSort);
        LaboratuvarColumnFilter = new ColumnFilterViewModel("Laboratuvar", RefreshVisibleEntries, ApplyColumnSort);
        AciklamaColumnFilter = new ColumnFilterViewModel("Açıklama", RefreshVisibleEntries, ApplyColumnSort);
        _columnFilters =
        [
            SampleReceivedDateColumnFilter,
            YibfNoColumnFilter,
            AdaParselColumnFilter,
            YapiSahibiColumnFilter,
            MuteahhitColumnFilter,
            KatBilgisiColumnFilter,
            BetonSinifiColumnFilter,
            TwentyEightDayResultColumnFilter,
            BetonFirmasiColumnFilter,
            LaboratuvarColumnFilter,
            AciklamaColumnFilter
        ];

        ApplyDefaultSort();

        SelectKarotSubTabCommand = new RelayCommand<KarotSubTab>(tab => SelectedSubTab = tab);
        SelectKarotEntryCommand = new RelayCommand<KarotEntry?>(entry => SelectedEntry = entry);
        AddKarotEntryCommand = new AsyncRelayCommand(AddEntryAsync, () => !_isAddingEntry);
        DeleteKarotEntryCommand = new AsyncRelayCommand(DeleteSelectedAsync, () => SelectedEntry is not null);
        MoveKarotEntryUpCommand = new RelayCommand(() => MoveSelected(-1), CanMoveUp);
        MoveKarotEntryDownCommand = new RelayCommand(() => MoveSelected(1), CanMoveDown);
        OpenKarotStatusDialogCommand = new AsyncRelayCommand<KarotEntry?>(OpenStatusDialogAsync, entry => (entry ?? SelectedEntry) is not null);
        EditCellNoteCommand = new AsyncRelayCommand<KarotCellViewModel?>(EditCellNoteAsync);
        CopyCellCommand = new RelayCommand<KarotCellViewModel?>(CopyCell);
        PasteCellCommand = new RelayCommand<KarotCellViewModel?>(PasteCell);
    }

    public ObservableRangeCollection<KarotEntry> Entries { get; }
    public ObservableRangeCollection<KarotEntry> VisibleEntries { get; }
    public ObservableRangeCollection<KarotEntryRowViewModel> VisibleRows { get; }
    public ObservableRangeCollection<KarotCellState> CellStates { get; }

    public ColumnFilterViewModel SampleReceivedDateColumnFilter { get; }
    public ColumnFilterViewModel YibfNoColumnFilter { get; }
    public ColumnFilterViewModel AdaParselColumnFilter { get; }
    public ColumnFilterViewModel YapiSahibiColumnFilter { get; }
    public ColumnFilterViewModel MuteahhitColumnFilter { get; }
    public ColumnFilterViewModel KatBilgisiColumnFilter { get; }
    public ColumnFilterViewModel BetonSinifiColumnFilter { get; }
    public ColumnFilterViewModel TwentyEightDayResultColumnFilter { get; }
    public ColumnFilterViewModel BetonFirmasiColumnFilter { get; }
    public ColumnFilterViewModel LaboratuvarColumnFilter { get; }
    public ColumnFilterViewModel AciklamaColumnFilter { get; }

    public bool HasUnsavedChanges
    {
        get => _hasUnsavedChanges;
        private set => SetProperty(ref _hasUnsavedChanges, value);
    }

    public int VisibleEntryCount => VisibleEntries.Count;

    public KarotSubTab SelectedSubTab
    {
        get => _selectedSubTab;
        set
        {
            if (SetProperty(ref _selectedSubTab, value))
            {
                RefreshColumnFilters();
                NotifyCommands();
            }
        }
    }

    public KarotEntry? SelectedEntry
    {
        get => _selectedEntry;
        set
        {
            if (SetProperty(ref _selectedEntry, value))
            {
                NotifyCommands();
            }
        }
    }

    public AsyncRelayCommand AddKarotEntryCommand { get; }
    public AsyncRelayCommand DeleteKarotEntryCommand { get; }
    public RelayCommand MoveKarotEntryUpCommand { get; }
    public RelayCommand MoveKarotEntryDownCommand { get; }
    public AsyncRelayCommand<KarotEntry?> OpenKarotStatusDialogCommand { get; }
    public AsyncRelayCommand<KarotCellViewModel?> EditCellNoteCommand { get; }
    public RelayCommand<KarotCellViewModel?> CopyCellCommand { get; }
    public RelayCommand<KarotCellViewModel?> PasteCellCommand { get; }
    public RelayCommand<KarotSubTab> SelectKarotSubTabCommand { get; }
    public RelayCommand<KarotEntry?> SelectKarotEntryCommand { get; }

    public async Task InitializeAsync()
    {
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;

        try
        {
            ReplaceEntries(await _repository.GetAllAsync());
            ReplaceCellStates(await _repository.GetCellStatesAsync());
            HasUnsavedChanges = false;
        }
        catch (Exception ex)
        {
            _isInitialized = false;
            _notificationService.ShowToast($"Karot yükleme hatası: {ex.Message}", ToastType.Error, TimeSpan.FromSeconds(5));
        }
    }

    public IReadOnlyList<KarotEntry> GetEntriesSnapshot()
        => Entries.OrderBy(item => item.DisplayOrder).ThenBy(item => item.UpdatedAt).Select(CloneEntry).ToList();

    public IReadOnlyList<KarotCellState> GetCellStatesSnapshot()
        => CellStates.Select(CloneCellState).ToList();

    public void LoadFromBackup(IEnumerable<KarotEntry> entries, IEnumerable<KarotCellState>? cellStates = null, bool markDirty = true)
    {
        ReplaceEntries((entries ?? Array.Empty<KarotEntry>()).Select(CloneEntry));
        ReplaceCellStates((cellStates ?? Array.Empty<KarotCellState>()).Select(CloneCellState));
        HasUnsavedChanges = markDirty;
    }

    public async Task PersistAsync(bool showErrorToast = true)
    {
        try
        {
            _isPersisting = true;
            await _repository.SaveManyAsync(GetEntriesSnapshot(), GetCellStatesSnapshot());
            HasUnsavedChanges = false;
        }
        catch (Exception ex)
        {
            HasUnsavedChanges = true;
            if (showErrorToast)
            {
                _notificationService.ShowToast($"Karot kayıt hatası: {ex.Message}", ToastType.Error);
            }
        }
        finally
        {
            _isPersisting = false;
        }
    }

    private async Task AddEntryAsync()
    {
        if (_isAddingEntry)
        {
            return;
        }

        _isAddingEntry = true;
        AddKarotEntryCommand.NotifyCanExecuteChanged();

        try
        {
            CollapseBlankDraftEntries();
            var reusableEntry = FindReusableDraftEntry();
            if (reusableEntry is not null)
            {
                EnsureEntryVisible(reusableEntry);
                SelectedEntry = reusableEntry;
                return;
            }

            ExecuteUndoableMutation("Karot kayıt ekle", () =>
            {
                var entry = new KarotEntry
                {
                    Status = SelectedSubTab == KarotSubTab.Yapilan ? KarotStatus.KarotAlindiOlumlu : KarotStatus.KarotAlinacak,
                    DisplayOrder = Entries.Count,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                Entries.Add(entry);
                NormalizeDisplayOrder();
                EnsureEntryVisible(entry);
                SelectedEntry = entry;
                HasUnsavedChanges = true;
            });
            _notificationService.ShowToast("Karot kaydı eklendi.", ToastType.Success, TimeSpan.FromSeconds(2));
            await Task.CompletedTask;
        }
        finally
        {
            _isAddingEntry = false;
            AddKarotEntryCommand.NotifyCanExecuteChanged();
        }
    }

    private async Task DeleteSelectedAsync()
    {
        var target = SelectedEntry;
        if (target is null)
        {
            return;
        }

        if (!_confirmationService.Confirm(new ConfirmationRequest
            {
                Kind = ConfirmationKind.Delete,
                Title = "Karot Kaydını Sil",
                Message = $"\"{target.AdaParsel}\" kaydı silinecek.\n\nDevam edilsin mi?",
                IsDestructive = true
            }))
        {
            return;
        }

        ExecuteUndoableMutation("Karot kayıt sil", () =>
        {
            var targetId = target.Id;
            Entries.Remove(target);
            RemoveCellStates(targetId);
            NormalizeDisplayOrder();
            RefreshVisibleEntries();
            SelectedEntry = VisibleEntries.FirstOrDefault();
            HasUnsavedChanges = true;
        });
        _notificationService.ShowToast("Karot kaydı silindi.", ToastType.Warning, TimeSpan.FromSeconds(2));
        await Task.CompletedTask;
    }

    private async Task OpenStatusDialogAsync(KarotEntry? entry)
    {
        var target = entry ?? SelectedEntry;
        if (target is null)
        {
            return;
        }

        SelectedEntry = target;

        var result = await _statusDialogService.ShowDialogAsync(target.Status);
        if (!result.HasValue || result.Value == target.Status)
        {
            return;
        }

        ExecuteUndoableMutation("Karot durum güncelle", () =>
        {
            target.Status = result.Value;
            target.UpdatedAt = DateTime.Now;
            HasUnsavedChanges = true;
            RefreshVisibleEntries();

            if (!MatchesSubTab(target.Status))
            {
                SelectedEntry = null;
            }
        });

        _notificationService.ShowToast("Karot durumu güncellendi.", ToastType.Info, TimeSpan.FromSeconds(2));
    }

    private async Task EditCellNoteAsync(KarotCellViewModel? cell)
    {
        if (cell is null || cell.Row.Entry is null)
        {
            return;
        }

        SelectKarotEntryCommand.Execute(cell.Row.Entry);
        var result = await _noteDialogService.ShowDialogAsync(cell.NoteText);
        if (result is null)
        {
            return;
        }

        ExecuteUndoableMutation("Karot hücre notu", () =>
        {
            var state = GetOrCreateCellState(cell.Row.Entry.Id, cell.ColumnKey);
            state.NoteText = result.DeleteRequested ? string.Empty : result.NoteText.Trim();
            CleanupCellStateIfEmpty(state);
            cell.NoteText = state.NoteText;
            HasUnsavedChanges = true;
        });
    }

    private void CopyCell(KarotCellViewModel? cell)
    {
        if (cell is null)
        {
            return;
        }

        SelectKarotEntryCommand.Execute(cell.Row.Entry);
        if (_clipboardService.TrySetText(cell.Text))
        {
            _notificationService.ShowToast("Hücre metni panoya kopyalandı.", ToastType.Info, TimeSpan.FromSeconds(2));
            return;
        }

        _notificationService.ShowToast("Pano erişimi sağlanamadı.", ToastType.Warning, TimeSpan.FromSeconds(3));
    }

    private void PasteCell(KarotCellViewModel? cell)
    {
        if (cell?.Row.Entry is null)
        {
            return;
        }

        if (!_clipboardService.TryGetText(out var text))
        {
            _notificationService.ShowToast("Panoda yapıştırılacak metin yok.", ToastType.Info, TimeSpan.FromSeconds(2));
            return;
        }

        SelectKarotEntryCommand.Execute(cell.Row.Entry);
        var normalizedText = text ?? string.Empty;
        if (string.Equals(cell.Text, normalizedText, StringComparison.Ordinal))
        {
            return;
        }

        ExecuteUndoableMutation("Karot hücre yapıştır", () =>
        {
            cell.Text = normalizedText;
            cell.Row.Entry.UpdatedAt = DateTime.Now;
            HasUnsavedChanges = true;
        });
    }

    private KarotUndoSnapshot CaptureUndoSnapshot()
        => new(
            GetEntriesSnapshot(),
            GetCellStatesSnapshot(),
            SelectedSubTab,
            SelectedEntry?.Id,
            HasUnsavedChanges);

    private void ApplyUndoSnapshot(KarotUndoSnapshot snapshot)
    {
        SelectedSubTab = snapshot.SelectedSubTab;
        LoadFromBackup(snapshot.Entries, snapshot.CellStates);
        HasUnsavedChanges = snapshot.HasUnsavedChanges;
        RefreshVisibleEntries();
        SelectedEntry = Entries.FirstOrDefault(item => item.Id == snapshot.SelectedEntryId)
            ?? VisibleEntries.FirstOrDefault()
            ?? Entries.FirstOrDefault();
    }

    private void ExecuteUndoableMutation(string description, Action mutate)
    {
        var before = CaptureUndoSnapshot();
        mutate();
        var after = CaptureUndoSnapshot();
        ApplyUndoSnapshot(before);
        _undoRedoService.Execute(new DelegateUndoableAction(
            description,
            () => ApplyUndoSnapshot(after),
            () => ApplyUndoSnapshot(before)));
    }

    private bool MatchesSubTab(KarotStatus status)
        => SelectedSubTab == KarotSubTab.Bekleyen
            ? status is not KarotStatus.KarotAlindiOlumlu
            : status == KarotStatus.KarotAlindiOlumlu;

    private void MoveSelected(int direction)
    {
        if (SelectedEntry is null)
        {
            return;
        }

        var visibleItems = VisibleEntries
            .OrderBy(item => item.DisplayOrder)
            .ToList();

        var currentIndex = visibleItems.FindIndex(item => item.Id == SelectedEntry.Id);
        var targetIndex = currentIndex + direction;
        if (currentIndex < 0 || targetIndex < 0 || targetIndex >= visibleItems.Count)
        {
            return;
        }

        ExecuteUndoableMutation("Karot sıralama değiştir", () =>
        {
            var sourceEntry = visibleItems[currentIndex];
            var targetEntry = visibleItems[targetIndex];
            (sourceEntry.DisplayOrder, targetEntry.DisplayOrder) = (targetEntry.DisplayOrder, sourceEntry.DisplayOrder);
            sourceEntry.UpdatedAt = DateTime.Now;
            targetEntry.UpdatedAt = DateTime.Now;

            NormalizeDisplayOrder();
            HasUnsavedChanges = true;
            RefreshVisibleEntries();
            SelectedEntry = sourceEntry;
        });
    }

    private bool CanMoveUp()
    {
        if (SelectedEntry is null)
        {
            return false;
        }

        var visibleItems = VisibleEntries.OrderBy(item => item.DisplayOrder).ToList();
        return visibleItems.FindIndex(item => item.Id == SelectedEntry.Id) > 0;
    }

    private bool CanMoveDown()
    {
        if (SelectedEntry is null)
        {
            return false;
        }

        var visibleItems = VisibleEntries.OrderBy(item => item.DisplayOrder).ToList();
        var currentIndex = visibleItems.FindIndex(item => item.Id == SelectedEntry.Id);
        return currentIndex >= 0 && currentIndex < visibleItems.Count - 1;
    }

    private void OnEntriesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (KarotEntry item in e.OldItems)
            {
                item.PropertyChanged -= OnEntryPropertyChanged;
            }
        }

        if (e.NewItems is not null)
        {
            foreach (KarotEntry item in e.NewItems)
            {
                item.PropertyChanged -= OnEntryPropertyChanged;
                item.PropertyChanged += OnEntryPropertyChanged;
            }
        }

        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            foreach (var item in Entries)
            {
                item.PropertyChanged -= OnEntryPropertyChanged;
                item.PropertyChanged += OnEntryPropertyChanged;
            }
        }

        RefreshColumnFilters();
    }

    private void OnEntryPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!_isInitialized || _isPersisting || sender is not KarotEntry entry)
        {
            return;
        }

        if (!PersistedProperties.Contains(e.PropertyName ?? string.Empty))
        {
            return;
        }

        if (e.PropertyName == nameof(KarotEntry.Status) && _rowLookup.TryGetValue(entry.Id, out var row))
        {
            row.RefreshStatusBrush();
        }

        HasUnsavedChanges = true;
        QueueRefreshAfterEditCycle();
    }

    private void ReplaceEntries(IEnumerable<KarotEntry> source)
    {
        Entries.ReplaceRange(source.OrderBy(item => item.DisplayOrder).ThenBy(item => item.UpdatedAt));

        var removedBlankDrafts = CollapseBlankDraftEntries();
        NormalizeDisplayOrder();
        RefreshColumnFilters();
        SelectedEntry = VisibleEntries.FirstOrDefault();
        if (removedBlankDrafts > 0)
        {
            HasUnsavedChanges = true;
        }
    }

    private void ReplaceCellStates(IEnumerable<KarotCellState> states)
    {
        _cellStateLookup.Clear();
        var clonedList = new List<KarotCellState>();
        foreach (var state in states)
        {
            var cloned = CloneCellState(state);
            if (cloned.EntryId == Guid.Empty || string.IsNullOrWhiteSpace(cloned.ColumnKey))
            {
                continue;
            }

            clonedList.Add(cloned);
            _cellStateLookup[BuildCellStateKey(cloned.EntryId, cloned.ColumnKey)] = cloned;
        }
        CellStates.ReplaceRange(clonedList);

        RefreshVisibleRows();
    }

    private void NormalizeDisplayOrder()
    {
        var ordered = Entries.OrderBy(item => item.DisplayOrder).ThenBy(item => item.UpdatedAt).ToList();
        for (var index = 0; index < ordered.Count; index++)
        {
            ordered[index].DisplayOrder = index;
        }
    }

    private void RefreshColumnFilters()
    {
        var visibleSource = Entries.Where(entry => MatchesSubTab(entry.Status)).ToList();
        SampleReceivedDateColumnFilter.SetAvailableValues(visibleSource.Select(entry => FormatDate(entry.SampleReceivedDate)));
        YibfNoColumnFilter.SetAvailableValues(visibleSource.Select(entry => entry.YibfNo));
        AdaParselColumnFilter.SetAvailableValues(visibleSource.Select(entry => entry.AdaParsel));
        YapiSahibiColumnFilter.SetAvailableValues(visibleSource.Select(entry => entry.YapiSahibi));
        MuteahhitColumnFilter.SetAvailableValues(visibleSource.Select(entry => entry.Muteahhit));
        KatBilgisiColumnFilter.SetAvailableValues(visibleSource.Select(entry => entry.KatBilgisi));
        BetonSinifiColumnFilter.SetAvailableValues(visibleSource.Select(entry => entry.BetonSinifi));
        TwentyEightDayResultColumnFilter.SetAvailableValues(visibleSource.Select(entry => entry.TwentyEightDayResult));
        BetonFirmasiColumnFilter.SetAvailableValues(visibleSource.Select(entry => entry.BetonFirmasi));
        LaboratuvarColumnFilter.SetAvailableValues(visibleSource.Select(entry => entry.Laboratuvar));
        AciklamaColumnFilter.SetAvailableValues(visibleSource.Select(entry => entry.Aciklama));
        RefreshVisibleEntries();
    }

    private void RefreshVisibleEntries()
    {
        var selectedId = SelectedEntry?.Id;
        var visibleItems = GetSortedVisibleEntries(Entries.Where(entry => MatchesSubTab(entry.Status) && MatchesAllFilters(entry))).ToList();

        VisibleEntries.ReplaceRange(visibleItems);

        RefreshVisibleRows();

        if (selectedId is null)
        {
            return;
        }

        var selectedEntry = VisibleEntries.FirstOrDefault(entry => entry.Id == selectedId.Value);
        if (!ReferenceEquals(selectedEntry, SelectedEntry))
        {
            SelectedEntry = selectedEntry;
        }
    }

    private void RefreshVisibleRows()
    {
        var visibleIds = VisibleEntries.Select(item => item.Id).ToHashSet();
        foreach (var stale in VisibleRows.Where(row => !visibleIds.Contains(row.Entry.Id)).ToList())
        {
            VisibleRows.Remove(stale);
            _rowLookup.Remove(stale.Entry.Id);
        }

        for (var index = 0; index < VisibleEntries.Count; index++)
        {
            var entry = VisibleEntries[index];
            if (!_rowLookup.TryGetValue(entry.Id, out var row))
            {
                row = BuildRow(entry);
                _rowLookup[entry.Id] = row;
            }
            else if (!ReferenceEquals(row.Entry, entry))
            {
                var staleIndex = VisibleRows.IndexOf(row);
                row = BuildRow(entry);
                _rowLookup[entry.Id] = row;
                if (staleIndex >= 0)
                {
                    VisibleRows[staleIndex] = row;
                }
            }
            else
            {
                UpdateRow(row, entry);
            }

            if (index >= VisibleRows.Count)
            {
                VisibleRows.Add(row);
                continue;
            }

            if (!ReferenceEquals(VisibleRows[index], row))
            {
                var currentIndex = VisibleRows.IndexOf(row);
                if (currentIndex >= 0)
                {
                    VisibleRows.Move(currentIndex, index);
                }
                else
                {
                    VisibleRows.Insert(index, row);
                }
            }
        }

        while (VisibleRows.Count > VisibleEntries.Count)
        {
            VisibleRows.RemoveAt(VisibleRows.Count - 1);
        }
    }

    private KarotEntryRowViewModel BuildRow(KarotEntry entry)
    {
        return new KarotEntryRowViewModel(
            entry,
            CreateCell(entry, KarotColumnKeys.SampleReceivedDate, () => FormatDate(entry.SampleReceivedDate), value => ApplySampleReceivedDate(entry, value)),
            CreateCell(entry, KarotColumnKeys.YibfNo, () => entry.YibfNo, value => entry.YibfNo = value),
            CreateCell(entry, KarotColumnKeys.AdaParsel, () => entry.AdaParsel, value => entry.AdaParsel = value),
            CreateCell(entry, KarotColumnKeys.YapiSahibi, () => entry.YapiSahibi, value => entry.YapiSahibi = value),
            CreateCell(entry, KarotColumnKeys.Muteahhit, () => entry.Muteahhit, value => entry.Muteahhit = value),
            CreateCell(entry, KarotColumnKeys.KatBilgisi, () => entry.KatBilgisi, value => entry.KatBilgisi = value),
            CreateCell(entry, KarotColumnKeys.BetonSinifi, () => entry.BetonSinifi, value => entry.BetonSinifi = value),
            CreateCell(entry, KarotColumnKeys.TwentyEightDayResult, () => entry.TwentyEightDayResult, value => entry.TwentyEightDayResult = value),
            CreateCell(entry, KarotColumnKeys.BetonFirmasi, () => entry.BetonFirmasi, value => entry.BetonFirmasi = value),
            CreateCell(entry, KarotColumnKeys.Laboratuvar, () => entry.Laboratuvar, value => entry.Laboratuvar = value),
            CreateCell(entry, KarotColumnKeys.Aciklama, () => entry.Aciklama, value => entry.Aciklama = value));
    }

    private void UpdateRow(KarotEntryRowViewModel row, KarotEntry entry)
    {
        row.UpdateEntry(entry);
        row.SampleReceivedDateCell.RefreshTextFromSource();
        row.YibfNoCell.RefreshTextFromSource();
        row.AdaParselCell.RefreshTextFromSource();
        row.YapiSahibiCell.RefreshTextFromSource();
        row.MuteahhitCell.RefreshTextFromSource();
        row.KatBilgisiCell.RefreshTextFromSource();
        row.BetonSinifiCell.RefreshTextFromSource();
        row.TwentyEightDayResultCell.RefreshTextFromSource();
        row.BetonFirmasiCell.RefreshTextFromSource();
        row.LaboratuvarCell.RefreshTextFromSource();
        row.AciklamaCell.RefreshTextFromSource();
        row.RefreshStatusBrush();

        UpdateCellState(row.SampleReceivedDateCell, entry.Id, KarotColumnKeys.SampleReceivedDate);
        UpdateCellState(row.YibfNoCell, entry.Id, KarotColumnKeys.YibfNo);
        UpdateCellState(row.AdaParselCell, entry.Id, KarotColumnKeys.AdaParsel);
        UpdateCellState(row.YapiSahibiCell, entry.Id, KarotColumnKeys.YapiSahibi);
        UpdateCellState(row.MuteahhitCell, entry.Id, KarotColumnKeys.Muteahhit);
        UpdateCellState(row.KatBilgisiCell, entry.Id, KarotColumnKeys.KatBilgisi);
        UpdateCellState(row.BetonSinifiCell, entry.Id, KarotColumnKeys.BetonSinifi);
        UpdateCellState(row.TwentyEightDayResultCell, entry.Id, KarotColumnKeys.TwentyEightDayResult);
        UpdateCellState(row.BetonFirmasiCell, entry.Id, KarotColumnKeys.BetonFirmasi);
        UpdateCellState(row.LaboratuvarCell, entry.Id, KarotColumnKeys.Laboratuvar);
        UpdateCellState(row.AciklamaCell, entry.Id, KarotColumnKeys.Aciklama);
    }

    private KarotCellViewModel CreateCell(KarotEntry entry, string columnKey, Func<string> readText, Action<string> writeText)
    {
        var state = GetCellState(entry.Id, columnKey);
        return new KarotCellViewModel(columnKey, readText, writeText, state?.NoteText ?? string.Empty);
    }

    private void UpdateCellState(KarotCellViewModel cell, Guid entryId, string columnKey)
    {
        var state = GetCellState(entryId, columnKey);
        cell.NoteText = state?.NoteText ?? string.Empty;
    }

    private KarotCellState? GetCellState(Guid entryId, string columnKey)
        => _cellStateLookup.TryGetValue(BuildCellStateKey(entryId, columnKey), out var state) ? state : null;

    private KarotCellState GetOrCreateCellState(Guid entryId, string columnKey)
    {
        var key = BuildCellStateKey(entryId, columnKey);
        if (_cellStateLookup.TryGetValue(key, out var existing))
        {
            return existing;
        }

        var state = new KarotCellState
        {
            EntryId = entryId,
            ColumnKey = columnKey
        };
        CellStates.Add(state);
        _cellStateLookup[key] = state;
        return state;
    }

    private void CleanupCellStateIfEmpty(KarotCellState state)
    {
        if (!string.IsNullOrWhiteSpace(state.NoteText))
        {
            return;
        }

        CellStates.Remove(state);
        _cellStateLookup.Remove(BuildCellStateKey(state.EntryId, state.ColumnKey));
    }

    private void RemoveCellStates(Guid entryId)
    {
        var items = CellStates.Where(state => state.EntryId == entryId).ToList();
        foreach (var state in items)
        {
            CellStates.Remove(state);
            _cellStateLookup.Remove(BuildCellStateKey(state.EntryId, state.ColumnKey));
        }
    }

    private static string BuildCellStateKey(Guid entryId, string columnKey)
        => $"{entryId:N}|{columnKey}";

    private void EnsureEntryVisible(KarotEntry entry)
    {
        if (VisibleEntries.Any(item => item.Id == entry.Id))
        {
            return;
        }

        foreach (var filter in _columnFilters)
        {
            filter.SelectAllCommand.Execute(null);
            filter.ClearSortSilently();
        }

        ApplyDefaultSort();
        RefreshVisibleEntries();
    }

    private void QueueRefreshAfterEditCycle()
    {
        if (_isRefreshQueued)
        {
            return;
        }

        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null)
        {
            RefreshColumnFilters();
            NotifyCommands();
            return;
        }

        _isRefreshQueued = true;
        dispatcher.BeginInvoke(() =>
        {
            _isRefreshQueued = false;
            RefreshColumnFilters();
            NotifyCommands();
        }, DispatcherPriority.Background);
    }

    private KarotEntry? FindReusableDraftEntry()
    {
        return Entries
            .Where(entry => MatchesSubTab(entry.Status))
            .OrderBy(entry => entry.DisplayOrder)
            .ThenByDescending(entry => entry.UpdatedAt)
            .FirstOrDefault(IsEntryBlank);
    }

    private int CollapseBlankDraftEntries()
    {
        var removedCount = 0;

        foreach (var group in Entries
                     .Where(IsEntryBlank)
                     .GroupBy(entry => entry.Status)
                     .ToList())
        {
            var extras = group
                .OrderBy(entry => entry.DisplayOrder)
                .ThenByDescending(entry => entry.UpdatedAt)
                .Skip(1)
                .ToList();

            foreach (var extra in extras)
            {
                Entries.Remove(extra);
                RemoveCellStates(extra.Id);
                removedCount++;
            }
        }

        return removedCount;
    }

    private static bool IsEntryBlank(KarotEntry entry)
    {
        return entry.SampleReceivedDate is null
               && string.IsNullOrWhiteSpace(entry.YibfNo)
               && string.IsNullOrWhiteSpace(entry.AdaParsel)
               && string.IsNullOrWhiteSpace(entry.YapiSahibi)
               && string.IsNullOrWhiteSpace(entry.Muteahhit)
               && string.IsNullOrWhiteSpace(entry.KatBilgisi)
               && string.IsNullOrWhiteSpace(entry.BetonSinifi)
               && string.IsNullOrWhiteSpace(entry.TwentyEightDayResult)
               && string.IsNullOrWhiteSpace(entry.BetonFirmasi)
               && string.IsNullOrWhiteSpace(entry.Laboratuvar)
               && string.IsNullOrWhiteSpace(entry.Aciklama);
    }

    private void ApplyColumnSort(ColumnFilterViewModel activeFilter)
    {
        foreach (var filter in _columnFilters)
        {
            if (!ReferenceEquals(filter, activeFilter))
            {
                filter.ClearSortSilently();
            }
        }

        RefreshVisibleEntries();
    }

    private void ApplyDefaultSort()
    {
        foreach (var filter in _columnFilters)
        {
            filter.ClearSortSilently();
        }
    }

    private bool MatchesAllFilters(KarotEntry entry)
    {
        return SampleReceivedDateColumnFilter.IsMatch(FormatDate(entry.SampleReceivedDate))
               && YibfNoColumnFilter.IsMatch(entry.YibfNo)
               && AdaParselColumnFilter.IsMatch(entry.AdaParsel)
               && YapiSahibiColumnFilter.IsMatch(entry.YapiSahibi)
               && MuteahhitColumnFilter.IsMatch(entry.Muteahhit)
               && KatBilgisiColumnFilter.IsMatch(entry.KatBilgisi)
               && BetonSinifiColumnFilter.IsMatch(entry.BetonSinifi)
               && TwentyEightDayResultColumnFilter.IsMatch(entry.TwentyEightDayResult)
               && BetonFirmasiColumnFilter.IsMatch(entry.BetonFirmasi)
               && LaboratuvarColumnFilter.IsMatch(entry.Laboratuvar)
               && AciklamaColumnFilter.IsMatch(entry.Aciklama);
    }

    private IEnumerable<KarotEntry> GetSortedVisibleEntries(IEnumerable<KarotEntry> source)
    {
        if (SampleReceivedDateColumnFilter.SortDirection is { } sampleDateDirection)
        {
            return ApplySecondarySort(sampleDateDirection == ListSortDirection.Ascending
                ? source.OrderBy(entry => entry.SampleReceivedDate)
                : source.OrderByDescending(entry => entry.SampleReceivedDate));
        }

        if (YibfNoColumnFilter.SortDirection is { } yibfDirection)
        {
            return ApplySecondarySort(yibfDirection == ListSortDirection.Ascending
                ? source.OrderBy(entry => entry.YibfNo, StringComparer.CurrentCultureIgnoreCase)
                : source.OrderByDescending(entry => entry.YibfNo, StringComparer.CurrentCultureIgnoreCase));
        }

        if (AdaParselColumnFilter.SortDirection is { } adaParselDirection)
        {
            return ApplySecondarySort(adaParselDirection == ListSortDirection.Ascending
                ? source.OrderBy(entry => entry.AdaParsel, StringComparer.CurrentCultureIgnoreCase)
                : source.OrderByDescending(entry => entry.AdaParsel, StringComparer.CurrentCultureIgnoreCase));
        }

        if (YapiSahibiColumnFilter.SortDirection is { } yapiSahibiDirection)
        {
            return ApplySecondarySort(yapiSahibiDirection == ListSortDirection.Ascending
                ? source.OrderBy(entry => entry.YapiSahibi, StringComparer.CurrentCultureIgnoreCase)
                : source.OrderByDescending(entry => entry.YapiSahibi, StringComparer.CurrentCultureIgnoreCase));
        }

        if (MuteahhitColumnFilter.SortDirection is { } muteahhitDirection)
        {
            return ApplySecondarySort(muteahhitDirection == ListSortDirection.Ascending
                ? source.OrderBy(entry => entry.Muteahhit, StringComparer.CurrentCultureIgnoreCase)
                : source.OrderByDescending(entry => entry.Muteahhit, StringComparer.CurrentCultureIgnoreCase));
        }

        if (KatBilgisiColumnFilter.SortDirection is { } katBilgisiDirection)
        {
            return ApplySecondarySort(katBilgisiDirection == ListSortDirection.Ascending
                ? source.OrderBy(entry => entry.KatBilgisi, StringComparer.CurrentCultureIgnoreCase)
                : source.OrderByDescending(entry => entry.KatBilgisi, StringComparer.CurrentCultureIgnoreCase));
        }

        if (BetonSinifiColumnFilter.SortDirection is { } betonSinifiDirection)
        {
            return ApplySecondarySort(betonSinifiDirection == ListSortDirection.Ascending
                ? source.OrderBy(entry => entry.BetonSinifi, StringComparer.CurrentCultureIgnoreCase)
                : source.OrderByDescending(entry => entry.BetonSinifi, StringComparer.CurrentCultureIgnoreCase));
        }

        if (TwentyEightDayResultColumnFilter.SortDirection is { } resultDirection)
        {
            return ApplySecondarySort(resultDirection == ListSortDirection.Ascending
                ? source.OrderBy(entry => entry.TwentyEightDayResult, StringComparer.CurrentCultureIgnoreCase)
                : source.OrderByDescending(entry => entry.TwentyEightDayResult, StringComparer.CurrentCultureIgnoreCase));
        }

        if (BetonFirmasiColumnFilter.SortDirection is { } betonFirmasiDirection)
        {
            return ApplySecondarySort(betonFirmasiDirection == ListSortDirection.Ascending
                ? source.OrderBy(entry => entry.BetonFirmasi, StringComparer.CurrentCultureIgnoreCase)
                : source.OrderByDescending(entry => entry.BetonFirmasi, StringComparer.CurrentCultureIgnoreCase));
        }

        if (LaboratuvarColumnFilter.SortDirection is { } laboratuvarDirection)
        {
            return ApplySecondarySort(laboratuvarDirection == ListSortDirection.Ascending
                ? source.OrderBy(entry => entry.Laboratuvar, StringComparer.CurrentCultureIgnoreCase)
                : source.OrderByDescending(entry => entry.Laboratuvar, StringComparer.CurrentCultureIgnoreCase));
        }

        if (AciklamaColumnFilter.SortDirection is { } aciklamaDirection)
        {
            return ApplySecondarySort(aciklamaDirection == ListSortDirection.Ascending
                ? source.OrderBy(entry => entry.Aciklama, StringComparer.CurrentCultureIgnoreCase)
                : source.OrderByDescending(entry => entry.Aciklama, StringComparer.CurrentCultureIgnoreCase));
        }

        return source
            .OrderBy(entry => entry.DisplayOrder)
            .ThenByDescending(entry => entry.UpdatedAt);
    }

    private static IOrderedEnumerable<KarotEntry> ApplySecondarySort(IOrderedEnumerable<KarotEntry> ordered)
    {
        return ordered
            .ThenBy(entry => entry.DisplayOrder)
            .ThenByDescending(entry => entry.UpdatedAt);
    }

    private static string FormatDate(DateTime? date)
        => date?.ToString("dd.MM.yyyy") ?? string.Empty;

    private static DateTime? ParseDateText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (DateTime.TryParseExact(trimmed, "dd.MM.yyyy", TurkishCulture, DateTimeStyles.None, out var parsedExact))
        {
            return parsedExact.Date;
        }

        return DateTime.TryParse(trimmed, TurkishCulture, DateTimeStyles.AllowWhiteSpaces, out var parsedGeneric)
            ? parsedGeneric.Date
            : null;
    }

    private static void ApplySampleReceivedDate(KarotEntry entry, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            entry.SampleReceivedDate = null;
            return;
        }

        var parsed = ParseDateText(value);
        if (parsed.HasValue)
        {
            entry.SampleReceivedDate = parsed.Value;
        }
    }

    private static Brush CreateBrush(string hexColor)
    {
        var converter = new BrushConverter();
        return converter.ConvertFromString(hexColor) as Brush ?? Brushes.White;
    }

    public static Brush ResolveStatusRowBrush(KarotStatus status)
    {
        return status switch
        {
            KarotStatus.KarotAlinacak => WaitingOrNegativeBrush,
            KarotStatus.KarotAlindiOlumsuz => WaitingOrNegativeBrush,
            KarotStatus.KarotAlindiSonucBekleniyor => PendingResultBrush,
            KarotStatus.KarotAlindiOlumlu => PositiveBrush,
            _ => Brushes.White
        };
    }

    private void NotifyCommands()
    {
        DeleteKarotEntryCommand.NotifyCanExecuteChanged();
        MoveKarotEntryUpCommand.NotifyCanExecuteChanged();
        MoveKarotEntryDownCommand.NotifyCanExecuteChanged();
        OpenKarotStatusDialogCommand.NotifyCanExecuteChanged();
    }

    private static KarotEntry CloneEntry(KarotEntry entry)
    {
        return new KarotEntry
        {
            Id = entry.Id,
            SampleReceivedDate = entry.SampleReceivedDate,
            YibfNo = entry.YibfNo,
            AdaParsel = entry.AdaParsel,
            YapiSahibi = entry.YapiSahibi,
            Muteahhit = entry.Muteahhit,
            KatBilgisi = entry.KatBilgisi,
            BetonSinifi = entry.BetonSinifi,
            TwentyEightDayResult = entry.TwentyEightDayResult,
            BetonFirmasi = entry.BetonFirmasi,
            Laboratuvar = entry.Laboratuvar,
            Aciklama = entry.Aciklama,
            Status = entry.Status,
            DisplayOrder = entry.DisplayOrder,
            CreatedAt = entry.CreatedAt,
            UpdatedAt = entry.UpdatedAt
        };
    }

    private static KarotCellState CloneCellState(KarotCellState state)
    {
        return new KarotCellState
        {
            EntryId = state.EntryId,
            ColumnKey = state.ColumnKey.Trim(),
            NoteText = state.NoteText
        };
    }
}

public sealed class KarotEntryRowViewModel : ViewModelBase
{
    private KarotEntry _entry;
    private Brush _rowBackgroundBrush;

    public KarotEntryRowViewModel(
        KarotEntry entry,
        KarotCellViewModel sampleReceivedDateCell,
        KarotCellViewModel yibfNoCell,
        KarotCellViewModel adaParselCell,
        KarotCellViewModel yapiSahibiCell,
        KarotCellViewModel muteahhitCell,
        KarotCellViewModel katBilgisiCell,
        KarotCellViewModel betonSinifiCell,
        KarotCellViewModel twentyEightDayResultCell,
        KarotCellViewModel betonFirmasiCell,
        KarotCellViewModel laboratuvarCell,
        KarotCellViewModel aciklamaCell)
    {
        _entry = entry;
        _rowBackgroundBrush = KarotModuleViewModel.ResolveStatusRowBrush(entry.Status);
        SampleReceivedDateCell = Attach(sampleReceivedDateCell);
        YibfNoCell = Attach(yibfNoCell);
        AdaParselCell = Attach(adaParselCell);
        YapiSahibiCell = Attach(yapiSahibiCell);
        MuteahhitCell = Attach(muteahhitCell);
        KatBilgisiCell = Attach(katBilgisiCell);
        BetonSinifiCell = Attach(betonSinifiCell);
        TwentyEightDayResultCell = Attach(twentyEightDayResultCell);
        BetonFirmasiCell = Attach(betonFirmasiCell);
        LaboratuvarCell = Attach(laboratuvarCell);
        AciklamaCell = Attach(aciklamaCell);
    }

    public KarotEntry Entry => _entry;
    public Brush RowBackgroundBrush => _rowBackgroundBrush;

    public KarotCellViewModel SampleReceivedDateCell { get; }
    public KarotCellViewModel YibfNoCell { get; }
    public KarotCellViewModel AdaParselCell { get; }
    public KarotCellViewModel YapiSahibiCell { get; }
    public KarotCellViewModel MuteahhitCell { get; }
    public KarotCellViewModel KatBilgisiCell { get; }
    public KarotCellViewModel BetonSinifiCell { get; }
    public KarotCellViewModel TwentyEightDayResultCell { get; }
    public KarotCellViewModel BetonFirmasiCell { get; }
    public KarotCellViewModel LaboratuvarCell { get; }
    public KarotCellViewModel AciklamaCell { get; }

    public void UpdateEntry(KarotEntry entry)
    {
        _entry = entry;
        OnPropertyChanged(nameof(Entry));
        RefreshStatusBrush();
    }

    public void RefreshStatusBrush()
    {
        var nextBrush = KarotModuleViewModel.ResolveStatusRowBrush(_entry.Status);
        if (ReferenceEquals(_rowBackgroundBrush, nextBrush))
        {
            return;
        }

        _rowBackgroundBrush = nextBrush;
        OnPropertyChanged(nameof(RowBackgroundBrush));
    }

    private KarotCellViewModel Attach(KarotCellViewModel cell)
    {
        cell.Row = this;
        return cell;
    }
}

public sealed class KarotCellViewModel : ViewModelBase
{
    private readonly Func<string> _readText;
    private readonly Action<string> _writeText;
    private string _text;
    private string _noteText;
    private bool _suppressWrite;

    public KarotCellViewModel(string columnKey, Func<string> readText, Action<string> writeText, string noteText)
    {
        ColumnKey = columnKey;
        _readText = readText;
        _writeText = writeText;
        _text = readText();
        _noteText = noteText;
    }

    public KarotEntryRowViewModel Row { get; set; } = null!;
    public string ColumnKey { get; }

    public string Text
    {
        get => _text;
        set
        {
            if (!SetProperty(ref _text, value))
            {
                return;
            }

            if (!_suppressWrite)
            {
                _writeText(value);
            }
        }
    }

    public string NoteText
    {
        get => _noteText;
        set
        {
            if (SetProperty(ref _noteText, value))
            {
                OnPropertyChanged(nameof(HasNote));
            }
        }
    }

    public bool HasNote => !string.IsNullOrWhiteSpace(NoteText);

    public void RefreshTextFromSource()
    {
        var next = _readText();
        if (string.Equals(_text, next, StringComparison.Ordinal))
        {
            return;
        }

        _suppressWrite = true;
        _text = next;
        OnPropertyChanged(nameof(Text));
        _suppressWrite = false;
    }
}
