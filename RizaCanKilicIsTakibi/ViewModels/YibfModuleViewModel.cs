using CommunityToolkit.Mvvm.Input;
using RizaCanKilicIsTakibi.Commands;
using RizaCanKilicIsTakibi.Helpers;
using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services;
using RizaCanKilicIsTakibi.Services.Abstractions;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Media;

namespace RizaCanKilicIsTakibi.ViewModels;

public sealed class YibfModuleViewModel : ViewModelBase
{
    private const string StrongRedColor = "#FFFF0000";
    private const string StrongYellowColor = "#FFFFFF00";
    private const string StrongGreenColor = "#FF92D050";
    private const string StrongBlueColor = "#FF4F81BD";
    private const string StrongGrayColor = "#FFD9D9D9";

    private const string LegacyPaleRedColor = "#FFF4C4C4";
    private const string LegacyPaleYellowColor = "#FFF7EDB3";
    private const string LegacyPaleGreenColor = "#FFDCEECE";
    private const string LegacyPaleBlueColor = "#FFD5E4FF";
    private const string LegacyPaleGrayColor = "#FFE8ECF2";

    private readonly IYibfRepository _repository;
    private readonly IYibfImportService _importService;
    private readonly IFileDialogService _fileDialogService;
    private readonly INotificationService _notificationService;
    private readonly IConfirmationService _confirmationService;
    private readonly ITadilatCellNoteDialogService _noteDialogService;
    private readonly IYibfAnaBilgiEventDialogService _anaBilgiEventDialogService;
    private readonly IYibfAnaBilgiEntryDialogService _anaBilgiEntryDialogService;
    private readonly IYibfIsTakibiEntryDialogService? _isTakibiEntryDialogService;
    private readonly IProjectCatalogUiState? _projectCatalogUiState;
    private readonly IProjectCatalogService? _projectCatalogService;
    private readonly IUndoRedoService _undoRedoService;
    private readonly IClipboardService _clipboardService;

    private bool _isInitialized;
    private bool _hasUnsavedChanges;
    private string _searchQuery = string.Empty;
    private string _isTakibiSearchText = string.Empty;
    private string _pendingSearchText = string.Empty;
    private YibfAnaBilgiEntry? _selectedAnaBilgiEntry;
    private YibfAnaBilgiEvent? _selectedAnaBilgiEvent;
    private YibfIsTakibiEntry? _selectedIsTakibiEntry;
    private Guid? _pendingIsTakibiScrollTargetId;
    private Guid? _lastSelectedAnaBilgiEntryId;
    private Guid? _lastSelectedAnaBilgiEventId;
    private Guid? _lastSelectedIsTakibiEntryId;
    private string _pendingApprovalFilter = YibfAnaBilgiApprovalStatuses.FilterAll;
    private readonly Dictionary<Guid, YibfAnaBilgiListItemViewModel> _allJobLookup = [];
    private readonly Dictionary<Guid, YibfPendingItemViewModel> _pendingLookup = [];
    private readonly Dictionary<Guid, YibfPendingGroupViewModel> _pendingGroupLookup = [];
    private readonly Dictionary<Guid, YibfTimelineEventViewModel> _visibleEventLookup = [];
    private readonly Dictionary<Guid, YibfIsTakibiRow> _isTakibiRowLookup = [];
    private readonly Dictionary<string, YibfCellState> _cellStateLookup = new(StringComparer.OrdinalIgnoreCase);
    private sealed record YibfUndoSnapshot(
        IReadOnlyList<YibfAnaBilgiEntry> AnaBilgiEntries,
        IReadOnlyList<YibfAnaBilgiEvent> AnaBilgiEvents,
        IReadOnlyList<YibfIsTakibiEntry> IsTakibiEntries,
        IReadOnlyList<YibfCellState> CellStates,
        Guid? SelectedAnaBilgiEntryId,
        Guid? SelectedAnaBilgiEventId,
        Guid? SelectedIsTakibiEntryId,
        bool HasUnsavedChanges);

    public YibfModuleViewModel(
        IYibfRepository repository,
        IYibfImportService importService,
        IFileDialogService fileDialogService,
        INotificationService notificationService,
        IConfirmationService confirmationService,
        ITadilatCellNoteDialogService noteDialogService,
        IYibfAnaBilgiEventDialogService anaBilgiEventDialogService,
        IYibfAnaBilgiEntryDialogService anaBilgiEntryDialogService,
        IUndoRedoService undoRedoService,
        IClipboardService? clipboardService = null,
        IYibfIsTakibiEntryDialogService? isTakibiEntryDialogService = null,
        IProjectCatalogUiState? projectCatalogUiState = null,
        IProjectCatalogService? projectCatalogService = null)
    {
        _repository = repository;
        _importService = importService;
        _fileDialogService = fileDialogService;
        _notificationService = notificationService;
        _confirmationService = confirmationService;
        _noteDialogService = noteDialogService;
        _anaBilgiEventDialogService = anaBilgiEventDialogService;
        _anaBilgiEntryDialogService = anaBilgiEntryDialogService;
        _isTakibiEntryDialogService = isTakibiEntryDialogService;
        _projectCatalogUiState = projectCatalogUiState;
        _projectCatalogService = projectCatalogService;
        _undoRedoService = undoRedoService;
        _clipboardService = clipboardService ?? new ClipboardService();

        AnaBilgiEntries = [];
        AnaBilgiEvents = [];
        IsTakibiEntries = [];
        CellStates = [];
        BekleyenIsler = [];
        BekleyenGruplar = [];
        TumIsler = [];
        VisibleEvents = [];
        IsTakibiRows = [];

        TumIslerView = CollectionViewSource.GetDefaultView(TumIsler);
        TumIslerView.Filter = FilterAllJobs;

        FilteredBekleyenIslerView = CollectionViewSource.GetDefaultView(BekleyenIsler);
        FilteredBekleyenIslerView.Filter = FilterPendingApprovalItems;

        FilteredBekleyenGruplarView = CollectionViewSource.GetDefaultView(BekleyenGruplar);
        FilteredBekleyenGruplarView.Filter = FilterPendingApprovalGroups;

        SelectAnaBilgiEntryCommand = new RelayCommand<YibfAnaBilgiEntry?>(entry => SelectedAnaBilgiEntry = entry);
        SelectAnaBilgiEventCommand = new RelayCommand<YibfAnaBilgiEvent?>(item => SelectedAnaBilgiEvent = item);
        SelectPendingItemCommand = new RelayCommand<YibfPendingItemViewModel?>(SelectPendingItem);
        EditPendingItemCommand = new AsyncRelayCommand<YibfPendingItemViewModel?>(EditPendingItemAsync);
        SelectPendingApprovalFilterCommand = new RelayCommand<string?>(SelectPendingApprovalFilter);
        SelectIsTakibiEntryCommand = new RelayCommand<YibfIsTakibiEntry?>(entry => SelectedIsTakibiEntry = entry);
        ImportExcelCommand = new AsyncRelayCommand(ImportExcelAsync);
        AddAnaBilgiEntryCommand = new AsyncRelayCommand(AddAnaBilgiEntryAsync);
        EditAnaBilgiEntryCommand = new AsyncRelayCommand(EditSelectedAnaBilgiEntryAsync, () => SelectedAnaBilgiEntry is not null);
        AddAnaBilgiEventCommand = new AsyncRelayCommand(AddAnaBilgiEventAsync, () => SelectedAnaBilgiEntry is not null);
        EditAnaBilgiEventCommand = new AsyncRelayCommand(EditSelectedAnaBilgiEventAsync, () => SelectedAnaBilgiEvent is not null);
        DeleteAnaBilgiEventCommand = new AsyncRelayCommand(DeleteSelectedAnaBilgiEventAsync, () => SelectedAnaBilgiEvent is not null);
        DeleteAnaBilgiEntryCommand = new AsyncRelayCommand(DeleteSelectedAnaBilgiEntryAsync, () => SelectedAnaBilgiEntry is not null);
        MoveAnaBilgiEntryUpCommand = new AsyncRelayCommand<YibfAnaBilgiEntry?>(entry => MoveAnaBilgiEntryAsync(entry, -1), CanMoveAnaBilgiEntryUp);
        MoveAnaBilgiEntryDownCommand = new AsyncRelayCommand<YibfAnaBilgiEntry?>(entry => MoveAnaBilgiEntryAsync(entry, 1), CanMoveAnaBilgiEntryDown);
        AddIsTakibiEntryCommand = new AsyncRelayCommand(AddIsTakibiEntryAsync);
        DeleteIsTakibiEntryCommand = new AsyncRelayCommand(DeleteSelectedIsTakibiAsync, () => SelectedIsTakibiEntry is not null);
        MoveIsTakibiEntryUpCommand = new AsyncRelayCommand<YibfIsTakibiEntry?>(entry => MoveIsTakibiEntryAsync(entry, -1), CanMoveIsTakibiEntryUp);
        MoveIsTakibiEntryDownCommand = new AsyncRelayCommand<YibfIsTakibiEntry?>(entry => MoveIsTakibiEntryAsync(entry, 1), CanMoveIsTakibiEntryDown);
        ClearIsTakibiSearchCommand = new RelayCommand(ClearIsTakibiSearch, () => HasActiveIsTakibiSearch);
        ClearPendingSearchCommand = new RelayCommand(() => PendingSearchText = string.Empty, () => HasActivePendingSearch);
        DeleteActiveSelectionCommand = new AsyncRelayCommand(DeleteActiveSelectionAsync, CanDeleteActiveSelection);
        BeginCellEditCommand = new RelayCommand<YibfCellViewModel?>(BeginCellEdit);
        CommitCellEditCommand = new RelayCommand<YibfCellViewModel?>(CommitCellEdit);
        CancelCellEditCommand = new RelayCommand<YibfCellViewModel?>(CancelCellEdit);
        EditCellNoteCommand = new AsyncRelayCommand<YibfCellViewModel?>(EditCellNoteAsync);
        SetCellColorRedCommand = new RelayCommand<YibfCellViewModel?>(cell => SetCellColor(cell, StrongRedColor));
        SetCellColorYellowCommand = new RelayCommand<YibfCellViewModel?>(cell => SetCellColor(cell, StrongYellowColor));
        SetCellColorGreenCommand = new RelayCommand<YibfCellViewModel?>(cell => SetCellColor(cell, StrongGreenColor));
        SetCellColorBlueCommand = new RelayCommand<YibfCellViewModel?>(cell => SetCellColor(cell, StrongBlueColor));
        SetCellColorGrayCommand = new RelayCommand<YibfCellViewModel?>(cell => SetCellColor(cell, StrongGrayColor));
        ClearCellColorCommand = new RelayCommand<YibfCellViewModel?>(cell => SetCellColor(cell, string.Empty));
        CopyCellCommand = new RelayCommand<YibfCellViewModel?>(CopyCell);
        PasteCellCommand = new RelayCommand<YibfCellViewModel?>(PasteCell, cell => cell?.IsInteractive == true);
    }

    public ObservableRangeCollection<YibfAnaBilgiEntry> AnaBilgiEntries { get; }
    public ObservableRangeCollection<YibfAnaBilgiEvent> AnaBilgiEvents { get; }
    public ObservableRangeCollection<YibfIsTakibiEntry> IsTakibiEntries { get; }
    public ObservableRangeCollection<YibfCellState> CellStates { get; }
    public ObservableRangeCollection<YibfPendingItemViewModel> BekleyenIsler { get; }
    public ObservableRangeCollection<YibfPendingGroupViewModel> BekleyenGruplar { get; }
    public ObservableRangeCollection<YibfAnaBilgiListItemViewModel> TumIsler { get; }
    public ObservableRangeCollection<YibfTimelineEventViewModel> VisibleEvents { get; }
    public ObservableRangeCollection<YibfIsTakibiRow> IsTakibiRows { get; }
    public ICollectionView TumIslerView { get; }
    public ICollectionView FilteredBekleyenIslerView { get; }
    public ICollectionView FilteredBekleyenGruplarView { get; }

    public bool HasUnsavedChanges
    {
        get => _hasUnsavedChanges;
        private set => SetProperty(ref _hasUnsavedChanges, value);
    }

    public void MarkDirty() => HasUnsavedChanges = true;


    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            if (SetProperty(ref _searchQuery, value))
            {
                TumIslerView.Refresh();
            }
        }
    }

    public string IsTakibiSearchText
    {
        get => _isTakibiSearchText;
        set
        {
            if (SetProperty(ref _isTakibiSearchText, value))
            {
                RefreshIsTakibiRows();
                ClearIsTakibiSearchCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public bool HasActiveIsTakibiSearch => !string.IsNullOrWhiteSpace(IsTakibiSearchText);

    public string PendingSearchText
    {
        get => _pendingSearchText;
        set
        {
            if (SetProperty(ref _pendingSearchText, value ?? string.Empty))
            {
                FilteredBekleyenIslerView.Refresh();
                ApplyPendingFilterToGroups();
                FilteredBekleyenGruplarView.Refresh();
                OnPropertyChanged(nameof(HasActivePendingSearch));
                OnPropertyChanged(nameof(FilteredBekleyenIslerCount));
                OnPropertyChanged(nameof(FilteredBekleyenGruplarCount));
                ClearPendingSearchCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public bool HasActivePendingSearch => !string.IsNullOrWhiteSpace(PendingSearchText);

    public bool HasNoVisibleIsTakibiResults => HasActiveIsTakibiSearch && IsTakibiRows.Count == 0;

    public int TotalIsTakibiCount => IsTakibiEntries.Count;

    public string IsTakibiEntryCountDisplay => HasActiveIsTakibiSearch
        ? $"Görünen: {VisibleIsTakibiCount} / {TotalIsTakibiCount}"
        : $"Kayıt: {TotalIsTakibiCount}";

    public YibfAnaBilgiEntry? SelectedAnaBilgiEntry
    {
        get => _selectedAnaBilgiEntry;
        set
        {
            if (SetProperty(ref _selectedAnaBilgiEntry, value))
            {
                RefreshVisibleEvents();
                RefreshAllJobSelection();
                EditAnaBilgiEntryCommand.NotifyCanExecuteChanged();
                AddAnaBilgiEventCommand.NotifyCanExecuteChanged();
                DeleteAnaBilgiEntryCommand.NotifyCanExecuteChanged();
                MoveAnaBilgiEntryUpCommand.NotifyCanExecuteChanged();
                MoveAnaBilgiEntryDownCommand.NotifyCanExecuteChanged();
                DeleteActiveSelectionCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public YibfAnaBilgiEvent? SelectedAnaBilgiEvent
    {
        get => _selectedAnaBilgiEvent;
        set
        {
            if (SetProperty(ref _selectedAnaBilgiEvent, value))
            {
                RefreshVisibleEventSelection();
                EditAnaBilgiEventCommand.NotifyCanExecuteChanged();
                DeleteAnaBilgiEventCommand.NotifyCanExecuteChanged();
                DeleteActiveSelectionCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public YibfIsTakibiEntry? SelectedIsTakibiEntry
    {
        get => _selectedIsTakibiEntry;
        set
        {
            if (SetProperty(ref _selectedIsTakibiEntry, value))
            {
                RefreshIsTakibiSelection();
                DeleteIsTakibiEntryCommand.NotifyCanExecuteChanged();
                MoveIsTakibiEntryUpCommand.NotifyCanExecuteChanged();
                MoveIsTakibiEntryDownCommand.NotifyCanExecuteChanged();
                DeleteActiveSelectionCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public Guid? PendingIsTakibiScrollTargetId
    {
        get => _pendingIsTakibiScrollTargetId;
        private set => SetProperty(ref _pendingIsTakibiScrollTargetId, value);
    }

    public int VisibleIsTakibiCount => IsTakibiRows.Count;

    public string PendingApprovalFilter
    {
        get => _pendingApprovalFilter;
        set
        {
            var normalized = value ?? YibfAnaBilgiApprovalStatuses.FilterAll;
            if (!SetProperty(ref _pendingApprovalFilter, normalized))
            {
                return;
            }

            FilteredBekleyenIslerView.Refresh();
            ApplyPendingFilterToGroups();
            FilteredBekleyenGruplarView.Refresh();
            OnPropertyChanged(nameof(FilteredBekleyenIslerCount));
            OnPropertyChanged(nameof(FilteredBekleyenGruplarCount));
            OnPropertyChanged(nameof(IsPendingFilterAllSelected));
            OnPropertyChanged(nameof(IsPendingFilterIncelenecekSelected));
            OnPropertyChanged(nameof(IsPendingFilterDenetcidenDonusSelected));
            OnPropertyChanged(nameof(IsPendingFilterMuelliftenRevizeSelected));
            OnPropertyChanged(nameof(IsPendingFilterBeklenenSelected));
            OnPropertyChanged(nameof(IsPendingFilterKategorisizSelected));
        }
    }

    public int FilteredBekleyenIslerCount
        => BekleyenIsler.Count(MatchesPendingApprovalFilter);

    public int FilteredBekleyenGruplarCount
        => BekleyenGruplar.Count(MatchesPendingApprovalGroup);

    public int PendingFilterAllCount => BekleyenIsler.Count;
    public int PendingFilterIncelenecekCount
        => BekleyenIsler.Count(item => string.Equals(item.FilterKey, YibfAnaBilgiApprovalStatuses.Incelenecek, StringComparison.Ordinal));
    public int PendingFilterDenetcidenDonusCount
        => BekleyenIsler.Count(item => string.Equals(item.FilterKey, YibfAnaBilgiApprovalStatuses.DenetcidenDonus, StringComparison.Ordinal));
    public int PendingFilterMuelliftenRevizeCount
        => BekleyenIsler.Count(item => string.Equals(item.FilterKey, YibfAnaBilgiApprovalStatuses.MuelliftenRevize, StringComparison.Ordinal));
    public int PendingFilterBeklenenCount
        => BekleyenIsler.Count(item => string.Equals(item.FilterKey, YibfAnaBilgiApprovalStatuses.Beklenen, StringComparison.Ordinal));
    public int PendingFilterKategorisizCount
        => BekleyenIsler.Count(item => string.Equals(item.FilterKey, YibfAnaBilgiApprovalStatuses.FilterKategorisiz, StringComparison.Ordinal));

    public bool IsPendingFilterAllSelected
        => string.Equals(PendingApprovalFilter, YibfAnaBilgiApprovalStatuses.FilterAll, StringComparison.Ordinal);
    public bool IsPendingFilterIncelenecekSelected
        => string.Equals(PendingApprovalFilter, YibfAnaBilgiApprovalStatuses.Incelenecek, StringComparison.Ordinal);
    public bool IsPendingFilterDenetcidenDonusSelected
        => string.Equals(PendingApprovalFilter, YibfAnaBilgiApprovalStatuses.DenetcidenDonus, StringComparison.Ordinal);
    public bool IsPendingFilterMuelliftenRevizeSelected
        => string.Equals(PendingApprovalFilter, YibfAnaBilgiApprovalStatuses.MuelliftenRevize, StringComparison.Ordinal);
    public bool IsPendingFilterBeklenenSelected
        => string.Equals(PendingApprovalFilter, YibfAnaBilgiApprovalStatuses.Beklenen, StringComparison.Ordinal);
    public bool IsPendingFilterKategorisizSelected
        => string.Equals(PendingApprovalFilter, YibfAnaBilgiApprovalStatuses.FilterKategorisiz, StringComparison.Ordinal);

    public RelayCommand<YibfAnaBilgiEntry?> SelectAnaBilgiEntryCommand { get; }
    public RelayCommand<YibfAnaBilgiEvent?> SelectAnaBilgiEventCommand { get; }
    public RelayCommand<YibfPendingItemViewModel?> SelectPendingItemCommand { get; }
    public AsyncRelayCommand<YibfPendingItemViewModel?> EditPendingItemCommand { get; }
    public RelayCommand<string?> SelectPendingApprovalFilterCommand { get; }
    public RelayCommand<YibfIsTakibiEntry?> SelectIsTakibiEntryCommand { get; }
    public AsyncRelayCommand ImportExcelCommand { get; }
    public AsyncRelayCommand AddAnaBilgiEntryCommand { get; }
    public AsyncRelayCommand EditAnaBilgiEntryCommand { get; }
    public AsyncRelayCommand AddAnaBilgiEventCommand { get; }
    public AsyncRelayCommand EditAnaBilgiEventCommand { get; }
    public AsyncRelayCommand DeleteAnaBilgiEventCommand { get; }
    public AsyncRelayCommand DeleteAnaBilgiEntryCommand { get; }
    public AsyncRelayCommand<YibfAnaBilgiEntry?> MoveAnaBilgiEntryUpCommand { get; }
    public AsyncRelayCommand<YibfAnaBilgiEntry?> MoveAnaBilgiEntryDownCommand { get; }
    public AsyncRelayCommand AddIsTakibiEntryCommand { get; }
    public AsyncRelayCommand DeleteIsTakibiEntryCommand { get; }
    public AsyncRelayCommand<YibfIsTakibiEntry?> MoveIsTakibiEntryUpCommand { get; }
    public AsyncRelayCommand<YibfIsTakibiEntry?> MoveIsTakibiEntryDownCommand { get; }
    public RelayCommand ClearIsTakibiSearchCommand { get; }
    public RelayCommand ClearPendingSearchCommand { get; }
    public AsyncRelayCommand DeleteActiveSelectionCommand { get; }
    public RelayCommand<YibfCellViewModel?> BeginCellEditCommand { get; }
    public RelayCommand<YibfCellViewModel?> CommitCellEditCommand { get; }
    public RelayCommand<YibfCellViewModel?> CancelCellEditCommand { get; }
    public AsyncRelayCommand<YibfCellViewModel?> EditCellNoteCommand { get; }
    public RelayCommand<YibfCellViewModel?> SetCellColorRedCommand { get; }
    public RelayCommand<YibfCellViewModel?> SetCellColorYellowCommand { get; }
    public RelayCommand<YibfCellViewModel?> SetCellColorGreenCommand { get; }
    public RelayCommand<YibfCellViewModel?> SetCellColorBlueCommand { get; }
    public RelayCommand<YibfCellViewModel?> SetCellColorGrayCommand { get; }
    public RelayCommand<YibfCellViewModel?> ClearCellColorCommand { get; }
    public RelayCommand<YibfCellViewModel?> CopyCellCommand { get; }
    public RelayCommand<YibfCellViewModel?> PasteCellCommand { get; }

    public async Task InitializeAsync()
    {
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;

        try
        {
            ReplaceAnaBilgiEntries(await _repository.GetAnaBilgiEntriesAsync());
            ReplaceAnaBilgiEvents(await _repository.GetAnaBilgiEventsAsync());
            ReplaceIsTakibiEntries(await _repository.GetIsTakibiEntriesAsync());
            ReplaceCellStates(await _repository.GetCellStatesAsync());
            NormalizeWorkIdentities();
            RefreshAnaBilgiCollections();
            RefreshIsTakibiRows();
            HasUnsavedChanges = false;
        }
        catch (Exception ex)
        {
            _isInitialized = false;
            _notificationService.ShowToast($"YİBF yükleme hatası: {ex.Message}", ToastType.Error, TimeSpan.FromSeconds(5));
        }
    }

    public IReadOnlyList<YibfAnaBilgiEntry> GetAnaBilgiEntriesSnapshot()
        => AnaBilgiEntries.OrderBy(item => item.DisplayOrder).Select(CloneAnaBilgiEntry).ToList();

    public IReadOnlyList<YibfAnaBilgiEvent> GetAnaBilgiEventsSnapshot()
        => AnaBilgiEvents.OrderBy(item => item.EntryId).ThenBy(item => item.DisplayOrder).Select(CloneAnaBilgiEvent).ToList();

    public IReadOnlyList<YibfIsTakibiEntry> GetIsTakibiEntriesSnapshot()
        => IsTakibiEntries.OrderBy(item => item.DisplayOrder).Select(CloneIsTakibiEntry).ToList();

    public IReadOnlyList<YibfCellState> GetCellStatesSnapshot()
        => CellStates.OrderBy(item => item.EntryId).ThenBy(item => item.ColumnKey, StringComparer.OrdinalIgnoreCase).Select(CloneCellState).ToList();

    public void RefreshPersonnelBadges(IPersonnelAssignmentService service)
    {
        foreach (var entry in IsTakibiEntries)
        {
            entry.AssignedPersonnelBadge = service.GetBadgeText(PersonnelAssignmentSourceModule.YibfIsTakibi, entry.Id);
        }

        foreach (var evt in AnaBilgiEvents)
        {
            evt.AssignedPersonnelBadge = service.GetBadgeText(PersonnelAssignmentSourceModule.YibfAnaBilgiEvent, evt.Id);
        }
    }

    public string? GetCellBackgroundColor(Guid entryId, string columnKey)
    {
        var color = NormalizeCellColor(GetCellState(entryId, columnKey)?.BackgroundColor);
        return string.IsNullOrWhiteSpace(color) ? null : color;
    }

    public IReadOnlyList<string> GetRedYellowColumnKeys(Guid entryId)
    {
        return CellStates
            .Where(state => state.EntryId == entryId)
            .Where(state =>
            {
                var color = NormalizeCellColor(state.BackgroundColor);
                return string.Equals(color, StrongRedColor, StringComparison.OrdinalIgnoreCase)
                       || string.Equals(color, StrongYellowColor, StringComparison.OrdinalIgnoreCase);
            })
            .Select(state => state.ColumnKey)
            .ToList();
    }

    public void LoadFromBackup(
        IEnumerable<YibfAnaBilgiEntry> anaBilgiEntries,
        IEnumerable<YibfAnaBilgiEvent> anaBilgiEvents,
        IEnumerable<YibfIsTakibiEntry> isTakibiEntries,
        IEnumerable<YibfCellState> cellStates,
        bool markDirty = true)
    {
        ReplaceAnaBilgiEntries((anaBilgiEntries ?? Array.Empty<YibfAnaBilgiEntry>()).Select(CloneAnaBilgiEntry));
        ReplaceAnaBilgiEvents((anaBilgiEvents ?? Array.Empty<YibfAnaBilgiEvent>()).Select(CloneAnaBilgiEvent));
        ReplaceIsTakibiEntries((isTakibiEntries ?? Array.Empty<YibfIsTakibiEntry>()).Select(CloneIsTakibiEntry));
        ReplaceCellStates((cellStates ?? Array.Empty<YibfCellState>()).Select(CloneCellState));
        NormalizeWorkIdentities();
        RefreshAnaBilgiCollections();
        RefreshIsTakibiRows();
        HasUnsavedChanges = markDirty;
    }

    public async Task PersistAsync(bool showErrorToast = true)
    {
        try
        {
            NormalizeWorkIdentities();
            await _repository.SaveManyAsync(GetAnaBilgiEntriesSnapshot(), GetAnaBilgiEventsSnapshot(), GetIsTakibiEntriesSnapshot(), GetCellStatesSnapshot());
            HasUnsavedChanges = false;
        }
        catch (Exception ex)
        {
            HasUnsavedChanges = true;
            if (showErrorToast)
            {
                _notificationService.ShowToast($"YİBF kayıt hatası: {ex.Message}", ToastType.Error);
            }
        }
    }

    public void CommitPendingEdits()
    {
        foreach (var row in _isTakibiRowLookup.Values.ToList())
        {
            CommitPendingEdit(row.JobNameCell);
            CommitPendingEdit(row.MuellifCell);
            CommitPendingEdit(row.DenetciCell);
            CommitPendingEdit(row.TumDijitalCell);
            CommitPendingEdit(row.EvrakCell);
            CommitPendingEdit(row.SozlesmeCell);
            CommitPendingEdit(row.DekontCell);
            CommitPendingEdit(row.RuhsatBasvuruCell);
            CommitPendingEdit(row.RuhsatNushaCell);
            CommitPendingEdit(row.IsyeriTeslimCell);
            CommitPendingEdit(row.IsgCell);
            CommitPendingEdit(row.SaglikCell);
            CommitPendingEdit(row.TopraklamaCell);
        }
    }

    public void RequestIsTakibiScroll(Guid? entryId)
    {
        if (HasActiveIsTakibiSearch)
        {
            IsTakibiSearchText = string.Empty;
        }

        if (PendingIsTakibiScrollTargetId == entryId)
        {
            PendingIsTakibiScrollTargetId = null;
        }

        PendingIsTakibiScrollTargetId = entryId;
    }

    public void ClearPendingIsTakibiScrollTarget()
    {
        PendingIsTakibiScrollTargetId = null;
    }

    private async Task ImportExcelAsync()
    {
        var path = _fileDialogService.ShowOpenDialog("YİBF Excel içe aktar", "Excel (*.xlsx)|*.xlsx");
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        try
        {
            var imported = await _importService.ImportAsync(path);
            var validAnaBilgiEntries = imported.AnaBilgiEntries
                .Where(entry => IsValidAnaBilgiEntryInput(entry.AdaParsel, entry.YapiSahibi))
                .ToList();
            var validAnaBilgiEntryIds = validAnaBilgiEntries.Select(entry => entry.Id).ToHashSet();
            var validAnaBilgiEvents = imported.AnaBilgiEvents
                .Where(item => validAnaBilgiEntryIds.Contains(item.EntryId))
                .ToList();
            var validIsTakibiEntries = imported.IsTakibiEntries
                .Where(entry => !string.IsNullOrWhiteSpace(entry.JobName))
                .ToList();
            var validIsTakibiEntryIds = validIsTakibiEntries.Select(entry => entry.Id).ToHashSet();
            var validCellStates = imported.CellStates
                .Where(item => validIsTakibiEntryIds.Contains(item.EntryId))
                .ToList();
            var skippedCount =
                (imported.AnaBilgiEntries.Count - validAnaBilgiEntries.Count) +
                (imported.IsTakibiEntries.Count - validIsTakibiEntries.Count);

            LoadFromBackup(validAnaBilgiEntries, validAnaBilgiEvents, validIsTakibiEntries, validCellStates);

            if (skippedCount > 0)
            {
                _notificationService.ShowToast($"YİBF Excel verileri içe aktarıldı, {skippedCount} satır eksik zorunlu alan nedeniyle atlandı.", ToastType.Warning, TimeSpan.FromSeconds(5));
                return;
            }

            _notificationService.ShowToast("YİBF Excel verileri içe aktarıldı.", ToastType.Success);
        }
        catch (Exception ex)
        {
            _notificationService.ShowToast($"YİBF import hatası: {ex.Message}", ToastType.Error, TimeSpan.FromSeconds(5));
        }
    }

    private async Task AddAnaBilgiEventAsync()
    {
        var targetEntry = SelectedAnaBilgiEntry;
        if (targetEntry is null)
        {
            _notificationService.ShowToast("Önce bir proje takibi kaydı seçin.", ToastType.Warning, TimeSpan.FromSeconds(2));
            return;
        }

        var result = await _anaBilgiEventDialogService.ShowDialogAsync(DateTime.Today, string.Empty, string.Empty, string.Empty);
        if (result is null || IsEmptyAnaBilgiEvent(result.EventDate, result.Description, result.BackgroundColor, result.NoteText))
        {
            return;
        }

        if (SelectedAnaBilgiEntry?.Id != targetEntry.Id || !AnaBilgiEntries.Any(item => item.Id == targetEntry.Id))
        {
            _notificationService.ShowToast("Seçili proje takibi kaydı artık mevcut değil.", ToastType.Warning, TimeSpan.FromSeconds(2));
            return;
        }

        ExecuteUndoableMutation("Proje takibi olay ekle", () =>
        {
            var item = new YibfAnaBilgiEvent
            {
                Id = Guid.NewGuid(),
                EntryId = targetEntry.Id,
                EventDate = result.EventDate,
                Description = result.Description.Trim(),
                BackgroundColor = NormalizeCellColor(result.BackgroundColor),
                NoteText = result.NoteText.Trim(),
                ApprovalStatus = YibfAnaBilgiApprovalStatuses.Normalize(result.ApprovalStatus),
                DisplayOrder = AnaBilgiEvents.Count(evt => evt.EntryId == targetEntry.Id)
            };

            AnaBilgiEvents.Add(item);
            targetEntry.UpdatedAt = DateTime.Now;
            NormalizeAnaBilgiEventOrder(item.EntryId);
            HasUnsavedChanges = true;
            RefreshAnaBilgiCollections();
            SelectedAnaBilgiEvent = item;
        });
        _notificationService.ShowToast("Proje takibi olayı eklendi.", ToastType.Success, TimeSpan.FromSeconds(2));
    }

    private async Task AddAnaBilgiEntryAsync()
    {
        var result = await _anaBilgiEntryDialogService.ShowDialogAsync();
        if (result is null)
        {
            return;
        }

        if (!TryValidateAnaBilgiEntryInput(result.AdaParsel, result.YapiSahibi))
        {
            return;
        }

        ExecuteUndoableMutation("Proje takibi kayıt ekle", () =>
        {
            var entry = new YibfAnaBilgiEntry
            {
                Id = Guid.NewGuid(),
                AdaParsel = result.AdaParsel,
                YibfNo = result.YibfNo,
                Idare = result.Idare,
                YapiSahibi = result.YapiSahibi,
                Muteahhit = result.Muteahhit,
                DisplayOrder = AnaBilgiEntries.Count == 0 ? 0 : AnaBilgiEntries.Max(item => item.DisplayOrder) + 1,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            if (result.WorkGroupId is Guid workGroupId || result.ProjectId is Guid projectId)
            {
                var groupId = result.WorkGroupId ?? result.ProjectId ?? entry.Id;
                var identityId = result.ProjectId ?? result.WorkGroupId ?? entry.Id;
                entry.WorkGroupId = groupId;
                entry.WorkIdentityId = identityId;
            }
            else
            {
                entry.WorkGroupId = entry.Id;
                entry.WorkIdentityId = entry.Id;
            }

            AnaBilgiEntries.Add(entry);
            NormalizeWorkIdentities();
            HasUnsavedChanges = true;
            RefreshAnaBilgiCollections();
            SelectedAnaBilgiEntry = entry;
        });
        _notificationService.ShowToast("Proje takibi kaydı eklendi.", ToastType.Success, TimeSpan.FromSeconds(2));
        await Task.CompletedTask;
    }

    private async Task EditSelectedAnaBilgiEntryAsync()
    {
        if (SelectedAnaBilgiEntry is null)
        {
            return;
        }

        var target = SelectedAnaBilgiEntry;
        var result = await _anaBilgiEntryDialogService.ShowDialogAsync(
            new YibfAnaBilgiEntryDialogResult
            {
                AdaParsel = target.AdaParsel,
                YibfNo = target.YibfNo,
                Idare = target.Idare,
                YapiSahibi = target.YapiSahibi,
                Muteahhit = target.Muteahhit
            },
            isEditMode: true);

        if (result is null)
        {
            return;
        }

        if (!TryValidateAnaBilgiEntryInput(result.AdaParsel, result.YapiSahibi))
        {
            return;
        }

        if (SelectedAnaBilgiEntry?.Id != target.Id || !AnaBilgiEntries.Any(item => item.Id == target.Id))
        {
            _notificationService.ShowToast("Seçili proje takibi kaydı artık mevcut değil.", ToastType.Warning, TimeSpan.FromSeconds(2));
            return;
        }

        ExecuteUndoableMutation("Proje takibi kayıt düzenle", () =>
        {
            target.AdaParsel = result.AdaParsel;
            target.YibfNo = result.YibfNo;
            target.Idare = result.Idare;
            target.YapiSahibi = result.YapiSahibi;
            target.Muteahhit = result.Muteahhit;
            target.UpdatedAt = DateTime.Now;

            NormalizeWorkIdentities();
            HasUnsavedChanges = true;
            RefreshAnaBilgiCollections();
            RefreshVisibleEvents();
        });
        _notificationService.ShowToast("Proje takibi kaydı güncellendi.", ToastType.Success, TimeSpan.FromSeconds(2));
        await Task.CompletedTask;
    }

    private async Task EditSelectedAnaBilgiEventAsync()
    {
        var targetEvent = SelectedAnaBilgiEvent;
        if (targetEvent is null)
        {
            return;
        }

        var result = await _anaBilgiEventDialogService.ShowDialogAsync(
            targetEvent.EventDate,
            targetEvent.Description,
            NormalizeCellColor(targetEvent.BackgroundColor),
            targetEvent.NoteText,
            targetEvent.ApprovalStatus);

        if (result is null)
        {
            return;
        }

        if (SelectedAnaBilgiEvent?.Id != targetEvent.Id || !AnaBilgiEvents.Any(item => item.Id == targetEvent.Id))
        {
            _notificationService.ShowToast("Seçili proje takibi olayı artık mevcut değil.", ToastType.Warning, TimeSpan.FromSeconds(2));
            return;
        }

        if (IsEmptyAnaBilgiEvent(result.EventDate, result.Description, result.BackgroundColor, result.NoteText))
        {
            await DeleteSelectedAnaBilgiEventAsync();
            return;
        }

        ExecuteUndoableMutation("Proje takibi olay düzenle", () =>
        {
            targetEvent.EventDate = result.EventDate;
            targetEvent.Description = result.Description.Trim();
            targetEvent.BackgroundColor = NormalizeCellColor(result.BackgroundColor);
            targetEvent.NoteText = result.NoteText.Trim();
            targetEvent.ApprovalStatus = YibfAnaBilgiApprovalStatuses.Normalize(result.ApprovalStatus);

            var entry = AnaBilgiEntries.FirstOrDefault(item => item.Id == targetEvent.EntryId);
            if (entry is not null)
            {
                entry.UpdatedAt = DateTime.Now;
            }

            HasUnsavedChanges = true;
            RefreshAnaBilgiCollections();
        });
        _notificationService.ShowToast("Proje takibi olayı güncellendi.", ToastType.Success, TimeSpan.FromSeconds(2));
    }

    private async Task DeleteSelectedAnaBilgiEntryAsync()
    {
        var target = SelectedAnaBilgiEntry;
        if (target is null)
        {
            return;
        }

        if (!_confirmationService.Confirm(new ConfirmationRequest
            {
                Kind = ConfirmationKind.Delete,
                Title = "Proje Takibi Kaydını Sil",
                Message = $"\"{target.AdaParsel}\" kaydı silinecek.\n\nDevam edilsin mi?",
                IsDestructive = true
            }))
        {
            return;
        }

        ExecuteUndoableMutation("Proje takibi kayıt sil", () =>
        {
            var nextSelection = AnaBilgiEntries
                .Where(item => item.Id != target.Id)
                .OrderByDescending(item => item.DisplayOrder)
                .FirstOrDefault();

            foreach (var item in AnaBilgiEvents.Where(item => item.EntryId == target.Id).ToList())
            {
                AnaBilgiEvents.Remove(item);
            }

            AnaBilgiEntries.Remove(target);
            NormalizeAnaBilgiEntryOrder();
            HasUnsavedChanges = true;
            RefreshAnaBilgiCollections();
            SelectedAnaBilgiEntry = nextSelection;
        });
        _notificationService.ShowToast("Proje takibi kaydı silindi.", ToastType.Warning, TimeSpan.FromSeconds(2));
        await Task.CompletedTask;
    }

    private async Task MoveAnaBilgiEntryAsync(YibfAnaBilgiEntry? entry, int direction)
    {
        var target = ResolveAnaBilgiEntry(entry ?? SelectedAnaBilgiEntry);
        if (target is null)
        {
            return;
        }

        var ordered = GetAnaBilgiVisualOrder();
        var currentIndex = ordered.FindIndex(item => item.Id == target.Id);
        var targetIndex = currentIndex + direction;
        if (currentIndex < 0 || targetIndex < 0 || targetIndex >= ordered.Count)
        {
            return;
        }

        ExecuteUndoableMutation("Proje takibi sıralama değiştir", () =>
        {
            var currentTarget = ResolveAnaBilgiEntry(target);
            if (currentTarget is null)
            {
                return;
            }

            var currentOrdered = GetAnaBilgiVisualOrder();
            var sourceIndex = currentOrdered.FindIndex(item => item.Id == currentTarget.Id);
            var destinationIndex = sourceIndex + direction;
            if (sourceIndex < 0 || destinationIndex < 0 || destinationIndex >= currentOrdered.Count)
            {
                return;
            }

            (currentOrdered[sourceIndex], currentOrdered[destinationIndex]) = (currentOrdered[destinationIndex], currentOrdered[sourceIndex]);
            ApplyAnaBilgiVisualOrder(currentOrdered);

            SelectedAnaBilgiEntry = AnaBilgiEntries.FirstOrDefault(item => item.Id == currentTarget.Id);
            HasUnsavedChanges = true;
            RefreshAnaBilgiCollections();
        });

        await Task.CompletedTask;
    }

    private bool CanMoveAnaBilgiEntryUp(YibfAnaBilgiEntry? entry)
        => CanMoveAnaBilgiEntry(entry ?? SelectedAnaBilgiEntry, -1);

    private bool CanMoveAnaBilgiEntryDown(YibfAnaBilgiEntry? entry)
        => CanMoveAnaBilgiEntry(entry ?? SelectedAnaBilgiEntry, 1);

    private bool CanMoveAnaBilgiEntry(YibfAnaBilgiEntry? entry, int direction)
    {
        var target = ResolveAnaBilgiEntry(entry);
        if (target is null)
        {
            return false;
        }

        var ordered = GetAnaBilgiVisualOrder();
        var currentIndex = ordered.FindIndex(item => item.Id == target.Id);
        var targetIndex = currentIndex + direction;
        return currentIndex >= 0 && targetIndex >= 0 && targetIndex < ordered.Count;
    }

    private async Task DeleteSelectedAnaBilgiEventAsync()
    {
        var target = SelectedAnaBilgiEvent;
        if (target is null)
        {
            return;
        }

        if (!_confirmationService.Confirm(new ConfirmationRequest
            {
                Kind = ConfirmationKind.Delete,
                Title = "Proje Takibi Olayını Sil",
                Message = $"\"{target.Description}\" olayı silinecek.\n\nDevam edilsin mi?",
                IsDestructive = true
            }))
        {
            return;
        }

        ExecuteUndoableMutation("Proje takibi olay sil", () =>
        {
            var currentTarget = AnaBilgiEvents.FirstOrDefault(item => item.Id == target.Id);
            if (currentTarget is null)
            {
                RefreshVisibleEvents();
                return;
            }

            var entryId = currentTarget.EntryId;
            AnaBilgiEvents.Remove(currentTarget);
            NormalizeAnaBilgiEventOrder(entryId);

            var entry = AnaBilgiEntries.FirstOrDefault(item => item.Id == entryId);
            if (entry is not null)
            {
                entry.UpdatedAt = DateTime.Now;
            }

            HasUnsavedChanges = true;
            RefreshAnaBilgiCollections();
        });
        await Task.CompletedTask;
        _notificationService.ShowToast("Proje takibi olayı silindi.", ToastType.Warning, TimeSpan.FromSeconds(2));
    }

    private async Task AddIsTakibiEntryAsync()
    {
        if (_isTakibiEntryDialogService is not null)
        {
            var created = await _isTakibiEntryDialogService.ShowDialogAsync();
            if (created is null)
            {
                return;
            }

            ExecuteUndoableMutation("YİBF iş takibi satır ekle", () =>
            {
                created.DisplayOrder = IsTakibiEntries.Count;
                IsTakibiEntries.Add(created);
                SyncAnaBilgiFromCatalogForIsTakibi(created);
                NormalizeWorkIdentities();
                NormalizeIsTakibiOrder();
                RefreshAnaBilgiCollections();
                RefreshIsTakibiRows();
                SelectedIsTakibiEntry = created;
                RequestIsTakibiScroll(created.Id);
                HasUnsavedChanges = true;
            });
            _notificationService.ShowToast("YİBF iş takibi satırı eklendi.", ToastType.Success, TimeSpan.FromSeconds(2));
            await Task.CompletedTask;
            return;
        }

        ExecuteUndoableMutation("YİBF iş takibi satır ekle", () =>
        {
            var entry = new YibfIsTakibiEntry
            {
                Id = Guid.NewGuid(),
                DisplayOrder = IsTakibiEntries.Count,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
            entry.WorkGroupId = entry.Id;
            entry.WorkIdentityId = entry.Id;

            IsTakibiEntries.Add(entry);
            NormalizeWorkIdentities();
            NormalizeIsTakibiOrder();
            RefreshIsTakibiRows();
            SelectedIsTakibiEntry = entry;
            RequestIsTakibiScroll(entry.Id);
            HasUnsavedChanges = true;
        });
        _notificationService.ShowToast("YİBF iş takibi satırı eklendi.", ToastType.Success, TimeSpan.FromSeconds(2));
        await Task.CompletedTask;
    }

    public void AddStubEntriesFromFanOut(ProjectCatalogFanOutResult fanOut)
    {
        if (fanOut.AnaBilgiStub is null && fanOut.IsTakibiStub is null)
        {
            return;
        }

        ExecuteUndoableMutation("Katalog fan-out", () =>
        {
            if (fanOut.AnaBilgiStub is { } anaStub
                && !AnaBilgiEntries.Any(item =>
                    item.Id == anaStub.Id
                    || item.WorkGroupId == anaStub.WorkGroupId
                    || (HasSameAdaSahip(item, anaStub))))
            {
                anaStub.DisplayOrder = AnaBilgiEntries.Count == 0 ? 0 : AnaBilgiEntries.Max(item => item.DisplayOrder) + 1;
                AnaBilgiEntries.Add(anaStub);
            }

            if (fanOut.IsTakibiStub is { } isTakibiStub
                && !IsTakibiEntries.Any(item =>
                    item.WorkIdentityId == isTakibiStub.WorkIdentityId
                    || (item.WorkGroupId == isTakibiStub.WorkGroupId
                        && item.WorkIdentityId == isTakibiStub.WorkGroupId
                        && string.Equals(item.JobName?.Trim(), isTakibiStub.JobName?.Trim(), StringComparison.OrdinalIgnoreCase))))
            {
                isTakibiStub.Id = Guid.NewGuid();
                isTakibiStub.DisplayOrder = IsTakibiEntries.Count == 0 ? 0 : IsTakibiEntries.Max(item => item.DisplayOrder) + 1;
                IsTakibiEntries.Add(isTakibiStub);
                SyncAnaBilgiFromCatalogForIsTakibi(isTakibiStub);
            }

            NormalizeWorkIdentities();
            NormalizeIsTakibiOrder();
            RefreshAnaBilgiCollections();
            RefreshIsTakibiRows();
            HasUnsavedChanges = true;
        });
    }

    private void SyncAnaBilgiFromCatalogForIsTakibi(YibfIsTakibiEntry isTakibiEntry)
    {
        if (_projectCatalogService is null || _projectCatalogUiState is null)
        {
            return;
        }

        var catalog = _projectCatalogUiState.GetActiveEntries();
        var project = catalog.FirstOrDefault(item => item.Id == isTakibiEntry.WorkIdentityId)
                      ?? catalog.FirstOrDefault(item => item.Id == isTakibiEntry.WorkGroupId);
        if (project is null)
        {
            return;
        }

        // İstinat işlerinde belediye/müteahhit üst projeden alınır.
        if (project.Kind == ProjectCatalogKind.Istinat
            && project.ParentProjectId is Guid parentId
            && parentId != Guid.Empty)
        {
            project = catalog.FirstOrDefault(item => item.Id == parentId) ?? project;
        }

        var anaBilgi = AnaBilgiEntries.FirstOrDefault(item =>
            item.WorkGroupId == isTakibiEntry.WorkGroupId
            || item.Id == isTakibiEntry.WorkGroupId
            || item.WorkIdentityId == isTakibiEntry.WorkGroupId);

        if (anaBilgi is null && project.Kind == ProjectCatalogKind.Normal)
        {
            var stub = _projectCatalogService.BuildFanOut(project).AnaBilgiStub;
            if (stub is not null
                && !AnaBilgiEntries.Any(item => item.Id == stub.Id || item.WorkGroupId == stub.WorkGroupId))
            {
                stub.DisplayOrder = AnaBilgiEntries.Count == 0 ? 0 : AnaBilgiEntries.Max(item => item.DisplayOrder) + 1;
                AnaBilgiEntries.Add(stub);
            }

            return;
        }

        if (anaBilgi is not null)
        {
            _projectCatalogService.ApplyProjectSelection(anaBilgi, project);
        }
    }

    private static bool HasSameAdaSahip(YibfAnaBilgiEntry left, YibfAnaBilgiEntry right)
        => !string.IsNullOrWhiteSpace(left.AdaParsel)
           && !string.IsNullOrWhiteSpace(left.YapiSahibi)
           && string.Equals(left.AdaParsel.Trim(), right.AdaParsel?.Trim(), StringComparison.OrdinalIgnoreCase)
           && string.Equals(left.YapiSahibi.Trim(), right.YapiSahibi?.Trim(), StringComparison.OrdinalIgnoreCase);

    private async Task DeleteSelectedIsTakibiAsync()
    {
        var target = SelectedIsTakibiEntry;
        if (target is null)
        {
            return;
        }

        if (!_confirmationService.Confirm(new ConfirmationRequest
            {
                Kind = ConfirmationKind.Delete,
                Title = "YİBF İş Takibi Satırını Sil",
                Message = $"\"{target.JobName}\" satırı silinecek.\n\nDevam edilsin mi?",
                IsDestructive = true
            }))
        {
            return;
        }

        ExecuteUndoableMutation("YİBF iş takibi satır sil", () =>
        {
            IsTakibiEntries.Remove(target);
            RemoveCellStates(target.Id);
            NormalizeIsTakibiOrder();
            RefreshIsTakibiRows();
            SelectedIsTakibiEntry = IsTakibiEntries.FirstOrDefault();
            HasUnsavedChanges = true;
        });
        _notificationService.ShowToast("YİBF iş takibi satırı silindi.", ToastType.Warning, TimeSpan.FromSeconds(2));
        await Task.CompletedTask;
    }

    private async Task MoveIsTakibiEntryAsync(YibfIsTakibiEntry? entry, int direction)
    {
        var target = ResolveIsTakibiEntry(entry ?? SelectedIsTakibiEntry);
        if (target is null)
        {
            return;
        }

        var ordered = GetIsTakibiVisualOrder();
        var currentIndex = ordered.FindIndex(item => item.Id == target.Id);
        var targetIndex = currentIndex + direction;
        if (currentIndex < 0 || targetIndex < 0 || targetIndex >= ordered.Count)
        {
            return;
        }

        ExecuteUndoableMutation("YİBF iş takibi sıralama değiştir", () =>
        {
            CloseAllEditors();

            var currentTarget = ResolveIsTakibiEntry(target);
            if (currentTarget is null)
            {
                return;
            }

            var currentOrdered = GetIsTakibiVisualOrder();
            var sourceIndex = currentOrdered.FindIndex(item => item.Id == currentTarget.Id);
            var destinationIndex = sourceIndex + direction;
            if (sourceIndex < 0 || destinationIndex < 0 || destinationIndex >= currentOrdered.Count)
            {
                return;
            }

            (currentOrdered[sourceIndex], currentOrdered[destinationIndex]) = (currentOrdered[destinationIndex], currentOrdered[sourceIndex]);
            ApplyIsTakibiVisualOrder(currentOrdered);

            RefreshIsTakibiRows();
            SelectedIsTakibiEntry = IsTakibiEntries.FirstOrDefault(item => item.Id == currentTarget.Id);
            HasUnsavedChanges = true;
        });

        await Task.CompletedTask;
    }

    private bool CanMoveIsTakibiEntryUp(YibfIsTakibiEntry? entry)
        => CanMoveIsTakibiEntry(entry ?? SelectedIsTakibiEntry, -1);

    private bool CanMoveIsTakibiEntryDown(YibfIsTakibiEntry? entry)
        => CanMoveIsTakibiEntry(entry ?? SelectedIsTakibiEntry, 1);

    private bool CanMoveIsTakibiEntry(YibfIsTakibiEntry? entry, int direction)
    {
        var target = ResolveIsTakibiEntry(entry);
        if (target is null)
        {
            return false;
        }

        var ordered = GetIsTakibiVisualOrder();
        var currentIndex = ordered.FindIndex(item => item.Id == target.Id);
        var targetIndex = currentIndex + direction;
        return currentIndex >= 0 && targetIndex >= 0 && targetIndex < ordered.Count;
    }

    private async Task DeleteActiveSelectionAsync()
    {
        if (SelectedAnaBilgiEvent is not null)
        {
            await DeleteSelectedAnaBilgiEventAsync();
            return;
        }

        if (SelectedAnaBilgiEntry is not null)
        {
            await DeleteSelectedAnaBilgiEntryAsync();
            return;
        }

        await DeleteSelectedIsTakibiAsync();
    }

    private bool CanDeleteActiveSelection()
        => SelectedAnaBilgiEvent is not null || SelectedAnaBilgiEntry is not null || SelectedIsTakibiEntry is not null;

    private void BeginCellEdit(YibfCellViewModel? cell)
    {
        if (cell is null || !cell.IsInteractive)
        {
            return;
        }

        CloseAllEditors();
        SelectedIsTakibiEntry = cell.Row.Entry;
        cell.DraftText = cell.Text;
        cell.IsEditing = true;
    }

    private void CommitPendingEdit(YibfCellViewModel cell)
    {
        if (cell.IsEditing)
        {
            CommitCellEdit(cell);
        }
    }

    private void CommitCellEdit(YibfCellViewModel? cell)
    {
        if (cell is null || !cell.IsInteractive || cell.Row.Entry is null)
        {
            return;
        }

        var newValue = cell.DraftText.Trim();
        if (string.Equals(newValue, cell.Text, StringComparison.Ordinal))
        {
            cell.IsEditing = false;
            return;
        }

        if (cell.ColumnKey == YibfIsTakibiColumnKeys.JobName && string.IsNullOrWhiteSpace(newValue))
        {
            cell.DraftText = cell.Text;
            cell.IsEditing = false;
            _notificationService.ShowToast("İş adı alanı zorunludur.", ToastType.Warning, TimeSpan.FromSeconds(2));
            return;
        }

        ExecuteUndoableMutation("YİBF hücre düzenle", () =>
        {
            cell.Row.SetCellValue(cell.ColumnKey, newValue);
            if (cell.ColumnKey == YibfIsTakibiColumnKeys.JobName)
            {
                NormalizeWorkIdentities();
            }

            cell.Text = newValue;
            cell.IsEditing = false;
            cell.Row.Entry.UpdatedAt = DateTime.Now;
            HasUnsavedChanges = true;
        });
    }

    private void CancelCellEdit(YibfCellViewModel? cell)
    {
        if (cell is null)
        {
            return;
        }

        cell.DraftText = cell.Text;
        cell.IsEditing = false;
    }

    private void CopyCell(YibfCellViewModel? cell)
    {
        if (cell is null || cell.Row.Entry is null)
        {
            return;
        }

        SelectedIsTakibiEntry = cell.Row.Entry;
        var payload = new CellClipboardPayload
        {
            Text = cell.Text,
            BackgroundColor = NormalizeCellColor(cell.BackgroundColor),
            NoteText = cell.NoteText
        };

        if (_clipboardService.TrySetCellPayload(payload))
        {
            _notificationService.ShowToast("Hücre panoya kopyalandı.", ToastType.Info, TimeSpan.FromSeconds(2));
            return;
        }

        _notificationService.ShowToast("Pano erişimi sağlanamadı.", ToastType.Warning, TimeSpan.FromSeconds(3));
    }

    private void PasteCell(YibfCellViewModel? cell)
    {
        if (cell is null || !cell.IsInteractive || cell.Row.Entry is null)
        {
            return;
        }

        string? text = null;
        if (!_clipboardService.TryGetCellPayload(out var payload) && !_clipboardService.TryGetText(out text))
        {
            _notificationService.ShowToast("Panoda yapıştırılacak metin yok.", ToastType.Info, TimeSpan.FromSeconds(2));
            return;
        }

        foreach (var row in IsTakibiRows)
        {
            row.CloseEditors();
        }

        SelectedIsTakibiEntry = cell.Row.Entry;
        var sourcePayload = payload ?? new CellClipboardPayload { Text = text ?? string.Empty };
        var normalizedText = sourcePayload.Text ?? string.Empty;
        var normalizedBackgroundColor = NormalizeCellColor(sourcePayload.BackgroundColor);
        var normalizedNoteText = sourcePayload.NoteText?.Trim() ?? string.Empty;
        if (string.Equals(cell.Text, normalizedText, StringComparison.Ordinal)
            && string.Equals(cell.BackgroundColor, normalizedBackgroundColor, StringComparison.Ordinal)
            && string.Equals(cell.NoteText, normalizedNoteText, StringComparison.Ordinal))
        {
            return;
        }

        if (cell.ColumnKey == YibfIsTakibiColumnKeys.JobName && string.IsNullOrWhiteSpace(normalizedText))
        {
            _notificationService.ShowToast("İş adı alanı zorunludur.", ToastType.Warning, TimeSpan.FromSeconds(2));
            return;
        }

        ExecuteUndoableMutation("YİBF hücre yapıştır", () =>
        {
            cell.Row.SetCellValue(cell.ColumnKey, normalizedText);
            if (cell.ColumnKey == YibfIsTakibiColumnKeys.JobName)
            {
                NormalizeWorkIdentities();
            }

            cell.Text = normalizedText;
            cell.DraftText = normalizedText;
            var state = GetOrCreateCellState(cell.Row.Entry.Id, cell.ColumnKey);
            state.BackgroundColor = normalizedBackgroundColor;
            state.NoteText = normalizedNoteText;
            CleanupCellStateIfEmpty(state);
            var currentState = GetCellState(cell.Row.Entry.Id, cell.ColumnKey);
            cell.BackgroundColor = currentState?.BackgroundColor ?? string.Empty;
            cell.NoteText = currentState?.NoteText ?? string.Empty;
            cell.Row.Entry.UpdatedAt = DateTime.Now;
            HasUnsavedChanges = true;
        });
    }

    private async Task EditCellNoteAsync(YibfCellViewModel? cell)
    {
        if (cell is null || !cell.IsInteractive || cell.Row.Entry is null)
        {
            return;
        }

        SelectedIsTakibiEntry = cell.Row.Entry;
        var result = await _noteDialogService.ShowDialogAsync(cell.NoteText);
        if (result is null)
        {
            return;
        }

        ExecuteUndoableMutation("YİBF hücre notu", () =>
        {
            var state = GetOrCreateCellState(cell.Row.Entry.Id, cell.ColumnKey);
            state.NoteText = result.DeleteRequested ? string.Empty : result.NoteText.Trim();
            CleanupCellStateIfEmpty(state);
            cell.NoteText = state.NoteText;
            HasUnsavedChanges = true;
        });
    }

    private void SetCellColor(YibfCellViewModel? cell, string color)
    {
        if (cell is null || !cell.IsInteractive || cell.Row.Entry is null)
        {
            return;
        }

        SelectedIsTakibiEntry = cell.Row.Entry;
        ExecuteUndoableMutation("YİBF hücre rengi", () =>
        {
            var state = GetOrCreateCellState(cell.Row.Entry.Id, cell.ColumnKey);
            state.BackgroundColor = NormalizeCellColor(color);
            CleanupCellStateIfEmpty(state);
            cell.BackgroundColor = state.BackgroundColor;
            HasUnsavedChanges = true;
        });
    }

    private YibfUndoSnapshot CaptureUndoSnapshot()
        => new(
            GetAnaBilgiEntriesSnapshot(),
            GetAnaBilgiEventsSnapshot(),
            GetIsTakibiEntriesSnapshot(),
            GetCellStatesSnapshot(),
            SelectedAnaBilgiEntry?.Id,
            SelectedAnaBilgiEvent?.Id,
            SelectedIsTakibiEntry?.Id,
            HasUnsavedChanges);

    private void ApplyUndoSnapshot(YibfUndoSnapshot snapshot)
    {
        LoadFromBackup(snapshot.AnaBilgiEntries, snapshot.AnaBilgiEvents, snapshot.IsTakibiEntries, snapshot.CellStates);
        HasUnsavedChanges = snapshot.HasUnsavedChanges;
        SelectedAnaBilgiEntry = AnaBilgiEntries.FirstOrDefault(item => item.Id == snapshot.SelectedAnaBilgiEntryId);
        RefreshVisibleEvents();
        SelectedAnaBilgiEvent = VisibleEvents.FirstOrDefault(item => item.Model?.Id == snapshot.SelectedAnaBilgiEventId)?.Model
            ?? AnaBilgiEvents.FirstOrDefault(item => item.Id == snapshot.SelectedAnaBilgiEventId);
        SelectedIsTakibiEntry = IsTakibiEntries.FirstOrDefault(item => item.Id == snapshot.SelectedIsTakibiEntryId)
            ?? IsTakibiEntries.FirstOrDefault();
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

    private bool FilterAllJobs(object item)
    {
        if (item is not YibfAnaBilgiListItemViewModel job)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            return true;
        }

        var query = SearchQuery.Trim();
        return Contains(job.Entry.AdaParsel, query)
            || Contains(job.Entry.YibfNo, query)
            || Contains(job.Entry.Idare, query)
            || Contains(job.Entry.YapiSahibi, query)
            || Contains(job.Entry.Muteahhit, query)
            || Contains(job.LastSummary, query);
    }

    private void RefreshAnaBilgiCollections()
    {
        var orderedEntries = AnaBilgiEntries.OrderByDescending(item => item.DisplayOrder).ToList();
        var latestPendingByEntryId = orderedEntries.ToDictionary(
            entry => entry.Id,
            entry => AnaBilgiEvents
                .Where(item => item.EntryId == entry.Id && !IsEmptyAnaBilgiEvent(item))
                .OrderBy(item => item.DisplayOrder)
                .LastOrDefault(item => IsPendingApprovalEvent(item)));

        var activeEntryIds = orderedEntries.Select(item => item.Id).ToHashSet();
        foreach (var obsoleteId in _allJobLookup.Keys.Where(id => !activeEntryIds.Contains(id)).ToList())
        {
            var item = _allJobLookup[obsoleteId];
            TumIsler.Remove(item);
            _allJobLookup.Remove(obsoleteId);
        }

        for (var index = 0; index < orderedEntries.Count; index++)
        {
            var entry = orderedEntries[index];
            latestPendingByEntryId.TryGetValue(entry.Id, out var latestPending);
            if (!_allJobLookup.TryGetValue(entry.Id, out var listItem))
            {
                listItem = new YibfAnaBilgiListItemViewModel(entry, latestPending);
                _allJobLookup[entry.Id] = listItem;
                TumIsler.Insert(Math.Min(index, TumIsler.Count), listItem);
            }
            else
            {
                listItem.Update(entry, latestPending);
                var currentIndex = TumIsler.IndexOf(listItem);
                if (currentIndex >= 0 && currentIndex != index)
                {
                    TumIsler.Move(currentIndex, index);
                }
            }
        }

        var orderedPending = orderedEntries
            .SelectMany(entry => AnaBilgiEvents
                .Where(evt => evt.EntryId == entry.Id && !IsEmptyAnaBilgiEvent(evt) && IsPendingApprovalEvent(evt))
                .Select(evt => new { entry, pending = evt }))
            .OrderBy(item => item.pending!.EventDate ?? DateTime.MaxValue)
            .ThenBy(item => item.pending!.DisplayOrder)
            .ThenBy(item => item.entry.DisplayOrder)
            .ToList();

        var activePendingIds = orderedPending.Select(item => item.pending.Id).ToHashSet();
        foreach (var obsoleteId in _pendingLookup.Keys.Where(id => !activePendingIds.Contains(id)).ToList())
        {
            var item = _pendingLookup[obsoleteId];
            BekleyenIsler.Remove(item);
            _pendingLookup.Remove(obsoleteId);
        }

        for (var index = 0; index < orderedPending.Count; index++)
        {
            var item = orderedPending[index];
            if (!_pendingLookup.TryGetValue(item.pending.Id, out var pendingItem))
            {
                pendingItem = new YibfPendingItemViewModel(item.entry, item.pending!);
                _pendingLookup[item.pending.Id] = pendingItem;
                BekleyenIsler.Insert(Math.Min(index, BekleyenIsler.Count), pendingItem);
            }
            else
            {
                pendingItem.Update(item.entry, item.pending!);
                var currentIndex = BekleyenIsler.IndexOf(pendingItem);
                if (currentIndex >= 0 && currentIndex != index)
                {
                    BekleyenIsler.Move(currentIndex, index);
                }
            }
        }

        NotifyPendingFilterProperties();
        RebuildPendingGroups();
        FilteredBekleyenIslerView.Refresh();
        TumIslerView.Refresh();
        if (SelectedAnaBilgiEntry is null || !AnaBilgiEntries.Any(item => item.Id == SelectedAnaBilgiEntry.Id))
        {
            SelectedAnaBilgiEntry = orderedEntries.FirstOrDefault();
        }
        else
        {
            RefreshVisibleEvents();
            RefreshAllJobSelection();
        }

        MoveAnaBilgiEntryUpCommand.NotifyCanExecuteChanged();
        MoveAnaBilgiEntryDownCommand.NotifyCanExecuteChanged();
    }
    private void RefreshVisibleEvents()
    {
        var currentEventId = SelectedAnaBilgiEvent?.Id;
        if (SelectedAnaBilgiEntry is null)
        {
            VisibleEvents.Clear();
            _visibleEventLookup.Clear();
            SelectedAnaBilgiEvent = null;
            return;
        }

        var events = AnaBilgiEvents
            .Where(evt => evt.EntryId == SelectedAnaBilgiEntry.Id && !IsEmptyAnaBilgiEvent(evt))
            .OrderByDescending(evt => evt.EventDate ?? DateTime.MinValue)
            .ThenByDescending(evt => evt.DisplayOrder)
            .ToList();

        var activeIds = events.Select(evt => evt.Id).ToHashSet();
        foreach (var obsoleteId in _visibleEventLookup.Keys.Where(id => !activeIds.Contains(id)).ToList())
        {
            var item = _visibleEventLookup[obsoleteId];
            VisibleEvents.Remove(item);
            _visibleEventLookup.Remove(obsoleteId);
        }

        for (var index = 0; index < events.Count; index++)
        {
            var item = events[index];
            if (!_visibleEventLookup.TryGetValue(item.Id, out var timelineItem))
            {
                timelineItem = new YibfTimelineEventViewModel(item);
                _visibleEventLookup[item.Id] = timelineItem;
                VisibleEvents.Insert(Math.Min(index, VisibleEvents.Count), timelineItem);
            }
            else
            {
                timelineItem.Update(item);
                var currentIndex = VisibleEvents.IndexOf(timelineItem);
                if (currentIndex >= 0 && currentIndex != index)
                {
                    VisibleEvents.Move(currentIndex, index);
                }
            }
        }

        SelectedAnaBilgiEvent = events.FirstOrDefault(evt => evt.Id == currentEventId) ?? events.FirstOrDefault();
        RefreshVisibleEventSelection();
    }

    private bool TryValidateAnaBilgiEntryInput(string? adaParsel, string? yapiSahibi)
    {
        if (IsValidAnaBilgiEntryInput(adaParsel, yapiSahibi))
        {
            return true;
        }

        _notificationService.ShowToast("Ada Parsel ve Yapı Sahibi alanları zorunludur.", ToastType.Warning, TimeSpan.FromSeconds(3));
        return false;
    }

    private static bool IsValidAnaBilgiEntryInput(string? adaParsel, string? yapiSahibi)
        => !string.IsNullOrWhiteSpace(adaParsel) && !string.IsNullOrWhiteSpace(yapiSahibi);

    private void RefreshAllJobSelection()
    {
        if (_lastSelectedAnaBilgiEntryId.HasValue)
        {
            if (_allJobLookup.TryGetValue(_lastSelectedAnaBilgiEntryId.Value, out var previousItem))
            {
                previousItem.IsSelected = false;
            }
        }

        var selectedId = SelectedAnaBilgiEntry?.Id;
        if (selectedId.HasValue && _allJobLookup.TryGetValue(selectedId.Value, out var selectedItem))
        {
            selectedItem.IsSelected = true;
        }

        _lastSelectedAnaBilgiEntryId = selectedId;
    }

    private void RefreshVisibleEventSelection()
    {
        if (_lastSelectedAnaBilgiEventId.HasValue)
        {
            if (_visibleEventLookup.TryGetValue(_lastSelectedAnaBilgiEventId.Value, out var previousItem))
            {
                previousItem.IsSelected = false;
            }
        }

        var selectedId = SelectedAnaBilgiEvent?.Id;
        if (selectedId.HasValue && _visibleEventLookup.TryGetValue(selectedId.Value, out var selectedItem))
        {
            selectedItem.IsSelected = true;
        }

        _lastSelectedAnaBilgiEventId = selectedId;
    }

    private void SelectPendingItem(YibfPendingItemViewModel? item)
    {
        if (item is null)
        {
            return;
        }

        SelectedAnaBilgiEntry = item.Entry;
        SelectedAnaBilgiEvent = AnaBilgiEvents.FirstOrDefault(evt => evt.Id == item.PendingEvent.Id);
    }

    private async Task EditPendingItemAsync(YibfPendingItemViewModel? item)
    {
        if (item is null)
        {
            return;
        }

        SelectPendingItem(item);

        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        if (dispatcher is not null)
        {
            await dispatcher.InvokeAsync(() => { }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        }

        await EditSelectedAnaBilgiEventAsync();
    }

    private void ClearIsTakibiSearch()
    {
        IsTakibiSearchText = string.Empty;
    }

    private bool EntryMatchesIsTakibiSearch(YibfIsTakibiEntry entry)
    {
        if (!HasActiveIsTakibiSearch)
        {
            return true;
        }

        var query = IsTakibiSearchText;
        if (Contains(entry.JobName, query)
            || Contains(entry.WorkVariantLabel, query)
            || Contains(entry.MuellifBilgileriGeldiMi, query)
            || Contains(entry.DenetciAtamalariYapildiMi, query)
            || Contains(entry.TumProjelerinDijitaliVarMi, query)
            || Contains(entry.EvraklarTamMi, query)
            || Contains(entry.YibfSozlesmeHazirlandiMi, query)
            || Contains(entry.DekontAlindiMi, query)
            || Contains(entry.RuhsatBasvurusuYapildiMi, query)
            || Contains(entry.RuhsatNushasiAlindiMi, query)
            || Contains(entry.IsyeriTeslimTutangiHazirlandiMi, query)
            || Contains(entry.IsgYazisiHazirlandiMi, query)
            || Contains(entry.SaglikGuvenlikPlaniGeldiMi, query)
            || Contains(entry.TemelTopraklamaTutanagiHazirlandiMi, query))
        {
            return true;
        }

        foreach (var state in CellStates)
        {
            if (state.EntryId != entry.Id || string.IsNullOrWhiteSpace(state.NoteText))
            {
                continue;
            }

            if (Contains(state.NoteText, query))
            {
                return true;
            }
        }

        return false;
    }

    private void RefreshIsTakibiRows()
    {
        var allOrderedEntries = IsTakibiEntries.OrderBy(item => item.DisplayOrder).ToList();
        var visibleOrderedEntries = allOrderedEntries.Where(EntryMatchesIsTakibiSearch).ToList();
        var allIds = allOrderedEntries.Select(item => item.Id).ToHashSet();
        var visibleIds = visibleOrderedEntries.Select(item => item.Id).ToHashSet();

        foreach (var obsoleteId in _isTakibiRowLookup.Keys.Where(id => !allIds.Contains(id)).ToList())
        {
            var row = _isTakibiRowLookup[obsoleteId];
            IsTakibiRows.Remove(row);
            _isTakibiRowLookup.Remove(obsoleteId);
        }

        foreach (var entry in allOrderedEntries)
        {
            if (!_isTakibiRowLookup.TryGetValue(entry.Id, out var row))
            {
                row = BuildRow(entry);
                _isTakibiRowLookup[entry.Id] = row;
            }
            else
            {
                UpdateIsTakibiRow(row, entry);
            }
        }

        foreach (var hiddenId in IsTakibiRows.Select(row => row.Entry.Id).Where(id => !visibleIds.Contains(id)).ToList())
        {
            if (_isTakibiRowLookup.TryGetValue(hiddenId, out var hiddenRow))
            {
                IsTakibiRows.Remove(hiddenRow);
            }
        }

        for (var index = 0; index < visibleOrderedEntries.Count; index++)
        {
            var entry = visibleOrderedEntries[index];
            var row = _isTakibiRowLookup[entry.Id];
            var currentIndex = IsTakibiRows.IndexOf(row);
            if (currentIndex < 0)
            {
                IsTakibiRows.Insert(Math.Min(index, IsTakibiRows.Count), row);
            }
            else if (currentIndex != index)
            {
                IsTakibiRows.Move(currentIndex, index);
            }
        }

        if (SelectedIsTakibiEntry is null
            || !IsTakibiEntries.Any(item => item.Id == SelectedIsTakibiEntry.Id)
            || !visibleIds.Contains(SelectedIsTakibiEntry.Id))
        {
            SelectedIsTakibiEntry = visibleOrderedEntries.FirstOrDefault();
        }
        else
        {
            RefreshIsTakibiSelection();
        }

        OnPropertyChanged(nameof(VisibleIsTakibiCount));
        OnPropertyChanged(nameof(TotalIsTakibiCount));
        OnPropertyChanged(nameof(IsTakibiEntryCountDisplay));
        OnPropertyChanged(nameof(HasActiveIsTakibiSearch));
        OnPropertyChanged(nameof(HasNoVisibleIsTakibiResults));
        ClearIsTakibiSearchCommand.NotifyCanExecuteChanged();
        MoveIsTakibiEntryUpCommand.NotifyCanExecuteChanged();
        MoveIsTakibiEntryDownCommand.NotifyCanExecuteChanged();
    }

    private void UpdateIsTakibiRow(YibfIsTakibiRow row, YibfIsTakibiEntry entry)
    {
        row.UpdateEntry(entry);
        UpdateIsTakibiCell(row.JobNameCell, entry, YibfIsTakibiColumnKeys.JobName, entry.JobName);
        UpdateIsTakibiCell(row.MuellifCell, entry, YibfIsTakibiColumnKeys.MuellifBilgileriGeldiMi, entry.MuellifBilgileriGeldiMi);
        UpdateIsTakibiCell(row.DenetciCell, entry, YibfIsTakibiColumnKeys.DenetciAtamalariYapildiMi, entry.DenetciAtamalariYapildiMi);
        UpdateIsTakibiCell(row.TumDijitalCell, entry, YibfIsTakibiColumnKeys.TumProjelerinDijitaliVarMi, entry.TumProjelerinDijitaliVarMi);
        UpdateIsTakibiCell(row.EvrakCell, entry, YibfIsTakibiColumnKeys.EvraklarTamMi, entry.EvraklarTamMi);
        UpdateIsTakibiCell(row.SozlesmeCell, entry, YibfIsTakibiColumnKeys.YibfSozlesmeHazirlandiMi, entry.YibfSozlesmeHazirlandiMi);
        UpdateIsTakibiCell(row.DekontCell, entry, YibfIsTakibiColumnKeys.DekontAlindiMi, entry.DekontAlindiMi);
        UpdateIsTakibiCell(row.RuhsatBasvuruCell, entry, YibfIsTakibiColumnKeys.RuhsatBasvurusuYapildiMi, entry.RuhsatBasvurusuYapildiMi);
        UpdateIsTakibiCell(row.RuhsatNushaCell, entry, YibfIsTakibiColumnKeys.RuhsatNushasiAlindiMi, entry.RuhsatNushasiAlindiMi);
        UpdateIsTakibiCell(row.IsyeriTeslimCell, entry, YibfIsTakibiColumnKeys.IsyeriTeslimTutangiHazirlandiMi, entry.IsyeriTeslimTutangiHazirlandiMi);
        UpdateIsTakibiCell(row.IsgCell, entry, YibfIsTakibiColumnKeys.IsgYazisiHazirlandiMi, entry.IsgYazisiHazirlandiMi);
        UpdateIsTakibiCell(row.SaglikCell, entry, YibfIsTakibiColumnKeys.SaglikGuvenlikPlaniGeldiMi, entry.SaglikGuvenlikPlaniGeldiMi);
        UpdateIsTakibiCell(row.TopraklamaCell, entry, YibfIsTakibiColumnKeys.TemelTopraklamaTutanagiHazirlandiMi, entry.TemelTopraklamaTutanagiHazirlandiMi);
    }

    private void UpdateIsTakibiCell(YibfCellViewModel cell, YibfIsTakibiEntry entry, string columnKey, string text)
    {
        if (!cell.IsEditing)
        {
            cell.Text = text;
            cell.DraftText = text;
        }

        var state = GetCellState(entry.Id, columnKey);
        cell.BackgroundColor = state?.BackgroundColor ?? string.Empty;
        cell.NoteText = state?.NoteText ?? string.Empty;
    }
    private YibfIsTakibiRow BuildRow(YibfIsTakibiEntry entry)
    {
        return new YibfIsTakibiRow(
            entry,
            CreateCell(entry, YibfIsTakibiColumnKeys.JobName, entry.JobName),
            CreateCell(entry, YibfIsTakibiColumnKeys.MuellifBilgileriGeldiMi, entry.MuellifBilgileriGeldiMi),
            CreateCell(entry, YibfIsTakibiColumnKeys.DenetciAtamalariYapildiMi, entry.DenetciAtamalariYapildiMi),
            CreateCell(entry, YibfIsTakibiColumnKeys.TumProjelerinDijitaliVarMi, entry.TumProjelerinDijitaliVarMi),
            CreateCell(entry, YibfIsTakibiColumnKeys.EvraklarTamMi, entry.EvraklarTamMi),
            CreateCell(entry, YibfIsTakibiColumnKeys.YibfSozlesmeHazirlandiMi, entry.YibfSozlesmeHazirlandiMi),
            CreateCell(entry, YibfIsTakibiColumnKeys.DekontAlindiMi, entry.DekontAlindiMi),
            CreateCell(entry, YibfIsTakibiColumnKeys.RuhsatBasvurusuYapildiMi, entry.RuhsatBasvurusuYapildiMi),
            CreateCell(entry, YibfIsTakibiColumnKeys.RuhsatNushasiAlindiMi, entry.RuhsatNushasiAlindiMi),
            CreateCell(entry, YibfIsTakibiColumnKeys.IsyeriTeslimTutangiHazirlandiMi, entry.IsyeriTeslimTutangiHazirlandiMi),
            CreateCell(entry, YibfIsTakibiColumnKeys.IsgYazisiHazirlandiMi, entry.IsgYazisiHazirlandiMi),
            CreateCell(entry, YibfIsTakibiColumnKeys.SaglikGuvenlikPlaniGeldiMi, entry.SaglikGuvenlikPlaniGeldiMi),
            CreateCell(entry, YibfIsTakibiColumnKeys.TemelTopraklamaTutanagiHazirlandiMi, entry.TemelTopraklamaTutanagiHazirlandiMi));
    }

    private YibfCellViewModel CreateCell(YibfIsTakibiEntry entry, string columnKey, string text)
    {
        var state = GetCellState(entry.Id, columnKey);
        return new YibfCellViewModel(columnKey, text, state?.BackgroundColor ?? string.Empty, state?.NoteText ?? string.Empty);
    }

        private YibfCellState? GetCellState(Guid entryId, string columnKey)
        => _cellStateLookup.TryGetValue(BuildCellStateKey(entryId, columnKey), out var state) ? state : null;

    private YibfCellState GetOrCreateCellState(Guid entryId, string columnKey)
    {
        var key = BuildCellStateKey(entryId, columnKey);
        if (_cellStateLookup.TryGetValue(key, out var existing))
        {
            return existing;
        }

        var created = new YibfCellState
        {
            EntryId = entryId,
            ColumnKey = columnKey
        };
        CellStates.Add(created);
        _cellStateLookup[key] = created;
        return created;
    }

    private void CleanupCellStateIfEmpty(YibfCellState state)
    {
        if (!string.IsNullOrWhiteSpace(state.BackgroundColor) || !string.IsNullOrWhiteSpace(state.NoteText))
        {
            return;
        }

        CellStates.Remove(state);
        _cellStateLookup.Remove(BuildCellStateKey(state.EntryId, state.ColumnKey));
    }

    private void RemoveCellStates(Guid entryId)
    {
        foreach (var state in CellStates.Where(item => item.EntryId == entryId).ToList())
        {
            CellStates.Remove(state);
            _cellStateLookup.Remove(BuildCellStateKey(state.EntryId, state.ColumnKey));
        }
    }

    private static string BuildCellStateKey(Guid entryId, string columnKey)
        => $"{entryId:N}|{columnKey}";

    private void CloseAllEditors()
    {
        foreach (var row in IsTakibiRows)
        {
            row.CloseEditors();
        }
    }

    private void RefreshIsTakibiSelection()
    {
        if (_lastSelectedIsTakibiEntryId.HasValue)
        {
            if (_isTakibiRowLookup.TryGetValue(_lastSelectedIsTakibiEntryId.Value, out var previousRow))
            {
                previousRow.IsSelected = false;
            }
        }

        var selectedId = SelectedIsTakibiEntry?.Id;
        if (selectedId.HasValue && _isTakibiRowLookup.TryGetValue(selectedId.Value, out var selectedRow))
        {
            selectedRow.IsSelected = true;
        }

        _lastSelectedIsTakibiEntryId = selectedId;
    }

    private void ReplaceAnaBilgiEntries(IEnumerable<YibfAnaBilgiEntry> entries)
    {
        AnaBilgiEntries.ReplaceRange(entries.OrderBy(item => item.DisplayOrder).Select(CloneAnaBilgiEntry));
    }

    private YibfAnaBilgiEntry? ResolveAnaBilgiEntry(YibfAnaBilgiEntry? entry)
        => entry is null ? null : AnaBilgiEntries.FirstOrDefault(item => item.Id == entry.Id);

    private List<YibfAnaBilgiEntry> GetAnaBilgiVisualOrder()
        => AnaBilgiEntries.OrderByDescending(item => item.DisplayOrder).ToList();

    private static void ApplyAnaBilgiVisualOrder(IReadOnlyList<YibfAnaBilgiEntry> orderedEntries)
    {
        var now = DateTime.Now;
        for (var index = 0; index < orderedEntries.Count; index++)
        {
            orderedEntries[index].DisplayOrder = orderedEntries.Count - 1 - index;
            orderedEntries[index].UpdatedAt = now;
        }
    }

    private void ReplaceAnaBilgiEvents(IEnumerable<YibfAnaBilgiEvent> events)
    {
        AnaBilgiEvents.ReplaceRange(events.Where(evt => !IsEmptyAnaBilgiEvent(evt)).OrderBy(evt => evt.EntryId).ThenBy(evt => evt.DisplayOrder).Select(CloneAnaBilgiEvent));
    }

    private void ReplaceIsTakibiEntries(IEnumerable<YibfIsTakibiEntry> entries)
    {
        IsTakibiEntries.ReplaceRange(entries.OrderBy(item => item.DisplayOrder).Select(CloneIsTakibiEntry));
        NormalizeIsTakibiOrder();
    }

    private YibfIsTakibiEntry? ResolveIsTakibiEntry(YibfIsTakibiEntry? entry)
        => entry is null ? null : IsTakibiEntries.FirstOrDefault(item => item.Id == entry.Id);

    private List<YibfIsTakibiEntry> GetIsTakibiVisualOrder()
        => IsTakibiEntries.OrderBy(item => item.DisplayOrder).ToList();

    private static void ApplyIsTakibiVisualOrder(IReadOnlyList<YibfIsTakibiEntry> orderedEntries)
    {
        var now = DateTime.Now;
        for (var index = 0; index < orderedEntries.Count; index++)
        {
            orderedEntries[index].DisplayOrder = index;
            orderedEntries[index].UpdatedAt = now;
        }
    }

    private void ReplaceCellStates(IEnumerable<YibfCellState> states)
    {
        _cellStateLookup.Clear();
        var clonedList = new List<YibfCellState>();
        foreach (var state in states)
        {
            var cloned = CloneCellState(state);
            clonedList.Add(cloned);
            _cellStateLookup[BuildCellStateKey(cloned.EntryId, cloned.ColumnKey)] = cloned;
        }
        CellStates.ReplaceRange(clonedList);
    }

    private void NormalizeIsTakibiOrder()
    {
        var ordered = IsTakibiEntries.OrderBy(item => item.DisplayOrder).ThenBy(item => item.UpdatedAt).ToList();
        for (var index = 0; index < ordered.Count; index++)
        {
            ordered[index].DisplayOrder = index;
            ordered[index].UpdatedAt = DateTime.Now;
        }
    }

    private void NormalizeAnaBilgiEventOrder(Guid entryId)
    {
        var ordered = AnaBilgiEvents
            .Where(item => item.EntryId == entryId && !IsEmptyAnaBilgiEvent(item))
            .OrderBy(item => item.DisplayOrder)
            .ThenBy(item => item.EventDate)
            .ToList();

        foreach (var emptyItem in AnaBilgiEvents.Where(item => item.EntryId == entryId && IsEmptyAnaBilgiEvent(item)).ToList())
        {
            AnaBilgiEvents.Remove(emptyItem);
        }

        for (var index = 0; index < ordered.Count; index++)
        {
            ordered[index].DisplayOrder = index;
        }
    }

    private void NormalizeAnaBilgiEntryOrder()
    {
        var ordered = AnaBilgiEntries.OrderBy(item => item.DisplayOrder).ThenBy(item => item.CreatedAt).ToList();
        for (var index = 0; index < ordered.Count; index++)
        {
            ordered[index].DisplayOrder = index;
        }
    }

    private bool NormalizeWorkIdentities()
        => YibfWorkIdentityService.NormalizeIdentities(AnaBilgiEntries.ToList(), IsTakibiEntries.ToList());

    private void SortBekleyenItems()
    {
        var ordered = BekleyenIsler
            .OrderBy(item => item.PendingEvent.EventDate ?? DateTime.MaxValue)
            .ThenBy(item => item.PendingEvent.DisplayOrder)
            .ThenBy(item => item.Entry.DisplayOrder)
            .ToList();

        BekleyenIsler.ReplaceRange(ordered);
        NotifyPendingFilterProperties();
        RebuildPendingGroups();
        FilteredBekleyenIslerView.Refresh();
    }

    private void RebuildPendingGroups()
    {
        var orderedGroups = BekleyenIsler
            .GroupBy(item => item.Entry.Id)
            .Select(group =>
            {
                var events = group
                    .OrderBy(item => item.PendingEvent.EventDate ?? DateTime.MaxValue)
                    .ThenBy(item => item.PendingEvent.DisplayOrder)
                    .ToList();
                var entry = events[0].Entry;
                return new { entry.Id, entry, events };
            })
            .OrderBy(group => group.events.Min(item => item.UrgencyRank))
            .ThenBy(group => group.events.Min(item => item.PendingEvent.EventDate ?? DateTime.MaxValue))
            .ThenBy(group => group.entry.DisplayOrder)
            .ToList();

        var activeIds = orderedGroups.Select(item => item.Id).ToHashSet();
        foreach (var obsoleteId in _pendingGroupLookup.Keys.Where(id => !activeIds.Contains(id)).ToList())
        {
            var item = _pendingGroupLookup[obsoleteId];
            BekleyenGruplar.Remove(item);
            _pendingGroupLookup.Remove(obsoleteId);
        }

        for (var index = 0; index < orderedGroups.Count; index++)
        {
            var item = orderedGroups[index];
            if (!_pendingGroupLookup.TryGetValue(item.Id, out var groupVm))
            {
                groupVm = new YibfPendingGroupViewModel(item.entry, item.events);
                _pendingGroupLookup[item.Id] = groupVm;
                BekleyenGruplar.Insert(Math.Min(index, BekleyenGruplar.Count), groupVm);
            }
            else
            {
                groupVm.Update(item.entry, item.events);
                var currentIndex = BekleyenGruplar.IndexOf(groupVm);
                if (currentIndex >= 0 && currentIndex != index)
                {
                    BekleyenGruplar.Move(currentIndex, index);
                }
            }
        }

        ApplyPendingFilterToGroups();
        FilteredBekleyenGruplarView.Refresh();
        OnPropertyChanged(nameof(FilteredBekleyenGruplarCount));
    }

    private void ApplyPendingFilterToGroups()
    {
        foreach (var group in BekleyenGruplar)
        {
            group.ApplyFilter(PendingApprovalFilter, PendingSearchText);
        }
    }

    private void SelectPendingApprovalFilter(string? filterKey)
    {
        PendingApprovalFilter = filterKey ?? YibfAnaBilgiApprovalStatuses.FilterAll;
    }

    private bool FilterPendingApprovalItems(object item)
        => item is YibfPendingItemViewModel pending && MatchesPendingApprovalFilter(pending);

    private bool FilterPendingApprovalGroups(object item)
        => item is YibfPendingGroupViewModel group && MatchesPendingApprovalGroup(group);

    private bool MatchesPendingApprovalFilter(YibfPendingItemViewModel item)
    {
        if (!string.IsNullOrEmpty(PendingApprovalFilter)
            && !string.Equals(item.FilterKey, PendingApprovalFilter, StringComparison.Ordinal))
        {
            return false;
        }

        return item.MatchesSearch(PendingSearchText);
    }

    private bool MatchesPendingApprovalGroup(YibfPendingGroupViewModel group)
        => group.VisibleEvents.Count > 0;

    private void NotifyPendingFilterProperties()
    {
        OnPropertyChanged(nameof(FilteredBekleyenIslerCount));
        OnPropertyChanged(nameof(FilteredBekleyenGruplarCount));
        OnPropertyChanged(nameof(PendingFilterAllCount));
        OnPropertyChanged(nameof(PendingFilterIncelenecekCount));
        OnPropertyChanged(nameof(PendingFilterDenetcidenDonusCount));
        OnPropertyChanged(nameof(PendingFilterMuelliftenRevizeCount));
        OnPropertyChanged(nameof(PendingFilterBeklenenCount));
        OnPropertyChanged(nameof(PendingFilterKategorisizCount));
        OnPropertyChanged(nameof(IsPendingFilterAllSelected));
        OnPropertyChanged(nameof(IsPendingFilterIncelenecekSelected));
        OnPropertyChanged(nameof(IsPendingFilterDenetcidenDonusSelected));
        OnPropertyChanged(nameof(IsPendingFilterMuelliftenRevizeSelected));
        OnPropertyChanged(nameof(IsPendingFilterBeklenenSelected));
        OnPropertyChanged(nameof(IsPendingFilterKategorisizSelected));
    }

    private static bool Contains(string? source, string value)
        => SearchTextNormalizer.Contains(source, value);

    private static string FirstNonEmpty(params string?[] values)
        => values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? string.Empty;

    private static string CombineSearchText(params string?[] values)
        => string.Join(' ', values.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value!.Trim()));

    private static bool IsPendingColor(string? color)
        => string.Equals(NormalizeCellColor(color), StrongRedColor, StringComparison.OrdinalIgnoreCase)
           || string.Equals(NormalizeCellColor(color), StrongYellowColor, StringComparison.OrdinalIgnoreCase);

    private static bool IsPendingApprovalEvent(YibfAnaBilgiEvent item)
    {
        if (YibfAnaBilgiApprovalStatuses.IsApproved(item.ApprovalStatus)
            || YibfAnaBilgiApprovalStatuses.IsPassive(item.ApprovalStatus))
        {
            return false;
        }

        if (YibfAnaBilgiApprovalStatuses.IsExplicitPending(item.ApprovalStatus))
        {
            return true;
        }

        return IsPendingColor(item.BackgroundColor);
    }

    private static bool IsEmptyAnaBilgiEvent(YibfAnaBilgiEvent item)
        => IsEmptyAnaBilgiEvent(item.EventDate, item.Description, item.BackgroundColor, item.NoteText);

    private static bool IsEmptyAnaBilgiEvent(DateTime? eventDate, string? description, string? backgroundColor, string? noteText)
        => string.IsNullOrWhiteSpace(description);

    private static string NormalizeCellColor(string? color)
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

        if (string.Equals(color, LegacyPaleGreenColor, StringComparison.OrdinalIgnoreCase))
        {
            return StrongGreenColor;
        }

        if (string.Equals(color, LegacyPaleBlueColor, StringComparison.OrdinalIgnoreCase))
        {
            return StrongBlueColor;
        }

        if (string.Equals(color, LegacyPaleGrayColor, StringComparison.OrdinalIgnoreCase))
        {
            return StrongGrayColor;
        }

        return color;
    }

    private static YibfAnaBilgiEntry CloneAnaBilgiEntry(YibfAnaBilgiEntry entry)
    {
        return new YibfAnaBilgiEntry
        {
            Id = entry.Id,
            WorkGroupId = entry.WorkGroupId,
            WorkIdentityId = entry.WorkIdentityId,
            AdaParsel = entry.AdaParsel,
            YibfNo = entry.YibfNo,
            Idare = entry.Idare,
            YapiSahibi = entry.YapiSahibi,
            Muteahhit = entry.Muteahhit,
            DisplayOrder = entry.DisplayOrder,
            CreatedAt = entry.CreatedAt,
            UpdatedAt = entry.UpdatedAt
        };
    }

    private static YibfAnaBilgiEvent CloneAnaBilgiEvent(YibfAnaBilgiEvent item)
    {
        return new YibfAnaBilgiEvent
        {
            Id = item.Id,
            EntryId = item.EntryId,
            EventDate = item.EventDate,
            Description = item.Description,
            BackgroundColor = NormalizeCellColor(item.BackgroundColor),
            NoteText = item.NoteText,
            ApprovalStatus = YibfAnaBilgiApprovalStatuses.Normalize(item.ApprovalStatus),
            DisplayOrder = item.DisplayOrder
        };
    }

    private static YibfIsTakibiEntry CloneIsTakibiEntry(YibfIsTakibiEntry entry)
    {
        return new YibfIsTakibiEntry
        {
            Id = entry.Id,
            WorkGroupId = entry.WorkGroupId,
            WorkIdentityId = entry.WorkIdentityId,
            WorkVariantLabel = entry.WorkVariantLabel,
            JobName = entry.JobName,
            MuellifBilgileriGeldiMi = entry.MuellifBilgileriGeldiMi,
            DenetciAtamalariYapildiMi = entry.DenetciAtamalariYapildiMi,
            TumProjelerinDijitaliVarMi = entry.TumProjelerinDijitaliVarMi,
            EvraklarTamMi = entry.EvraklarTamMi,
            YibfSozlesmeHazirlandiMi = entry.YibfSozlesmeHazirlandiMi,
            DekontAlindiMi = entry.DekontAlindiMi,
            RuhsatBasvurusuYapildiMi = entry.RuhsatBasvurusuYapildiMi,
            RuhsatNushasiAlindiMi = entry.RuhsatNushasiAlindiMi,
            IsyeriTeslimTutangiHazirlandiMi = entry.IsyeriTeslimTutangiHazirlandiMi,
            IsgYazisiHazirlandiMi = entry.IsgYazisiHazirlandiMi,
            SaglikGuvenlikPlaniGeldiMi = entry.SaglikGuvenlikPlaniGeldiMi,
            TemelTopraklamaTutanagiHazirlandiMi = entry.TemelTopraklamaTutanagiHazirlandiMi,
            DisplayOrder = entry.DisplayOrder,
            CreatedAt = entry.CreatedAt,
            UpdatedAt = entry.UpdatedAt
        };
    }

    private static YibfCellState CloneCellState(YibfCellState state)
    {
        return new YibfCellState
        {
            EntryId = state.EntryId,
            ColumnKey = state.ColumnKey,
            BackgroundColor = NormalizeCellColor(state.BackgroundColor),
            NoteText = state.NoteText
        };
    }
}

public sealed class YibfPendingItemViewModel : ViewModelBase
{
    private const int OverdueDayThreshold = 7;
    private static readonly BrushConverter BrushConverter = new();
    private YibfAnaBilgiEntry _entry;
    private YibfAnaBilgiEvent _pendingEvent;

    public YibfPendingItemViewModel(YibfAnaBilgiEntry entry, YibfAnaBilgiEvent pendingEvent)
    {
        _entry = entry;
        _pendingEvent = pendingEvent;
    }

    public YibfAnaBilgiEntry Entry => _entry;
    public YibfAnaBilgiEvent PendingEvent => _pendingEvent;
    public string StatusLabel => YibfAnaBilgiApprovalStatuses.GetLabel(PendingEvent.ApprovalStatus);
    public string FilterKey => YibfAnaBilgiApprovalStatuses.GetFilterKey(PendingEvent.ApprovalStatus);
    public int UrgencyRank => YibfAnaBilgiApprovalStatuses.GetUrgencyRank(PendingEvent.ApprovalStatus);
    public int PriorityRank => UrgencyRank;
    public string Summary => PendingEvent.Description;
    public string EventDateText => PendingEvent.EventDate?.ToString("dd.MM.yyyy") ?? "-";
    public int? DaysElapsed
        => PendingEvent.EventDate is DateTime date
            ? Math.Max(0, (DateTime.Today - date.Date).Days)
            : null;
    public string DaysElapsedText
        => DaysElapsed is int days ? $"{days} gün" : "—";
    public bool IsOverdue => DaysElapsed >= OverdueDayThreshold;
    public Brush CategoryBrush
    {
        get
        {
            var color = YibfAnaBilgiApprovalStatuses.GetDefaultColorForStatus(PendingEvent.ApprovalStatus);
            return BrushConverter.ConvertFromString(color) as Brush ?? Brushes.LightGray;
        }
    }

    public Brush PriorityBrush => CategoryBrush;
    public Brush StatusBrush => CategoryBrush;

    public bool MatchesSearch(string? query)
        => string.IsNullOrWhiteSpace(query)
           || SearchTextNormalizer.Contains(Entry.AdaParsel, query)
           || SearchTextNormalizer.Contains(Entry.YapiSahibi, query)
           || SearchTextNormalizer.Contains(StatusLabel, query)
           || SearchTextNormalizer.Contains(Summary, query)
           || SearchTextNormalizer.Contains(PendingEvent.NoteText, query)
           || SearchTextNormalizer.Contains(EventDateText, query);

    public void Update(YibfAnaBilgiEntry entry, YibfAnaBilgiEvent pendingEvent)
    {
        _entry = entry;
        _pendingEvent = pendingEvent;
        OnPropertyChanged(nameof(Entry));
        OnPropertyChanged(nameof(PendingEvent));
        OnPropertyChanged(nameof(StatusLabel));
        OnPropertyChanged(nameof(FilterKey));
        OnPropertyChanged(nameof(UrgencyRank));
        OnPropertyChanged(nameof(PriorityRank));
        OnPropertyChanged(nameof(Summary));
        OnPropertyChanged(nameof(EventDateText));
        OnPropertyChanged(nameof(DaysElapsed));
        OnPropertyChanged(nameof(DaysElapsedText));
        OnPropertyChanged(nameof(IsOverdue));
        OnPropertyChanged(nameof(CategoryBrush));
        OnPropertyChanged(nameof(PriorityBrush));
        OnPropertyChanged(nameof(StatusBrush));
    }
}

public sealed class YibfPendingGroupViewModel : ViewModelBase
{
    private YibfAnaBilgiEntry _entry;
    private IReadOnlyList<YibfPendingItemViewModel> _allEvents;
    private IReadOnlyList<YibfPendingItemViewModel> _visibleEvents;
    private string _activeFilter = YibfAnaBilgiApprovalStatuses.FilterAll;
    private string _activeSearchText = string.Empty;

    public YibfPendingGroupViewModel(YibfAnaBilgiEntry entry, IReadOnlyList<YibfPendingItemViewModel> events)
    {
        _entry = entry;
        _allEvents = events;
        _visibleEvents = events;
    }

    public YibfAnaBilgiEntry Entry => _entry;
    public IReadOnlyList<YibfPendingItemViewModel> AllEvents => _allEvents;
    public IReadOnlyList<YibfPendingItemViewModel> Events => _allEvents;
    public IReadOnlyList<YibfPendingItemViewModel> VisibleEvents => _visibleEvents;
    public int EventCount => _visibleEvents.Count;
    public string EventCountText => $"{EventCount} olay";
    public string TitleText => $"{Entry.AdaParsel}   {Entry.YapiSahibi}".Trim();
    public int UrgencyRank => _visibleEvents.Count == 0 ? int.MaxValue : _visibleEvents.Min(item => item.UrgencyRank);
    public bool IsOverdue => _visibleEvents.Any(item => item.IsOverdue);

    public void Update(YibfAnaBilgiEntry entry, IReadOnlyList<YibfPendingItemViewModel> events)
    {
        _entry = entry;
        _allEvents = events;
        OnPropertyChanged(nameof(Entry));
        OnPropertyChanged(nameof(AllEvents));
        OnPropertyChanged(nameof(Events));
        OnPropertyChanged(nameof(TitleText));
        ApplyFilter(_activeFilter, _activeSearchText);
    }

    public void ApplyFilter(string? filterKey, string? searchText = null)
    {
        _activeFilter = filterKey ?? YibfAnaBilgiApprovalStatuses.FilterAll;
        _activeSearchText = searchText ?? string.Empty;
        var groupMatchesSearch = string.IsNullOrWhiteSpace(_activeSearchText)
                                 || SearchTextNormalizer.Contains(TitleText, _activeSearchText);
        _visibleEvents = _allEvents
            .Where(item => string.IsNullOrEmpty(_activeFilter)
                           || string.Equals(item.FilterKey, _activeFilter, StringComparison.Ordinal))
            .Where(item => groupMatchesSearch || item.MatchesSearch(_activeSearchText))
            .ToList();

        OnPropertyChanged(nameof(VisibleEvents));
        OnPropertyChanged(nameof(EventCount));
        OnPropertyChanged(nameof(EventCountText));
        OnPropertyChanged(nameof(UrgencyRank));
        OnPropertyChanged(nameof(IsOverdue));
    }
}

public sealed class YibfAnaBilgiListItemViewModel : ViewModelBase
{
    private static readonly BrushConverter BrushConverter = new();
    private bool _isSelected;
    private YibfAnaBilgiEntry _entry;
    private YibfAnaBilgiEvent? _latestPendingEvent;

    public YibfAnaBilgiListItemViewModel(YibfAnaBilgiEntry entry, YibfAnaBilgiEvent? latestPendingEvent)
    {
        _entry = entry;
        _latestPendingEvent = latestPendingEvent;
    }

    public YibfAnaBilgiEntry Entry => _entry;
    public YibfAnaBilgiEvent? LatestPendingEvent => _latestPendingEvent;
    public string LastSummary => LatestPendingEvent?.Description ?? "Renkli bekleyen olay yok";
    public bool HasPending => LatestPendingEvent is not null;
    public Brush StatusBrush => BrushConverter.ConvertFromString(LatestPendingEvent?.BackgroundColor ?? "#FFD9D9D9") as Brush ?? Brushes.LightGray;

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public void Update(YibfAnaBilgiEntry entry, YibfAnaBilgiEvent? latestPendingEvent)
    {
        _entry = entry;
        _latestPendingEvent = latestPendingEvent;
        OnPropertyChanged(nameof(Entry));
        OnPropertyChanged(nameof(LatestPendingEvent));
        OnPropertyChanged(nameof(LastSummary));
        OnPropertyChanged(nameof(HasPending));
        OnPropertyChanged(nameof(StatusBrush));
    }
}

public sealed class YibfTimelineEventViewModel : ViewModelBase
{
    private static readonly BrushConverter BrushConverter = new();
    private bool _isSelected;

    public YibfTimelineEventViewModel(YibfAnaBilgiEvent model)
    {
        Model = model;
    }

    public YibfAnaBilgiEvent Model { get; private set; }

    public void Update(YibfAnaBilgiEvent model)
    {
        Model = model;
        OnPropertyChanged(nameof(Model));
        Refresh();
    }

    public void Refresh()
    {
        OnPropertyChanged(nameof(DateText));
        OnPropertyChanged(nameof(Description));
        OnPropertyChanged(nameof(HasNote));
        OnPropertyChanged(nameof(NoteText));
        OnPropertyChanged(nameof(AccentBrush));
        OnPropertyChanged(nameof(AccentForegroundBrush));
    }
    public string DateText => Model.EventDate?.ToString("dd.MM.yyyy") ?? "-";
    public string Description => string.IsNullOrWhiteSpace(Model.Description) ? "-" : Model.Description;
    public bool HasNote => !string.IsNullOrWhiteSpace(Model.NoteText);
    public string NoteText => Model.NoteText;
    public Brush AccentBrush => BrushConverter.ConvertFromString(string.IsNullOrWhiteSpace(Model.BackgroundColor) ? "#FFD7E1EC" : Model.BackgroundColor) as Brush ?? Brushes.LightGray;
    public Brush AccentForegroundBrush => IsBrightBackground(Model.BackgroundColor) ? Brushes.Black : Brushes.White;

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    private static bool IsBrightBackground(string? color)
    {
        if (string.IsNullOrWhiteSpace(color))
        {
            return true;
        }

        try
        {
            if (ColorConverter.ConvertFromString(color) is not Color parsed)
            {
                return false;
            }

            var luminance = ((0.299 * parsed.R) + (0.587 * parsed.G) + (0.114 * parsed.B)) / 255d;
            return luminance >= 0.62d;
        }
        catch
        {
            return false;
        }
    }
}
public sealed class YibfIsTakibiRow : ViewModelBase
{
    private bool _isSelected;

    public YibfIsTakibiRow(
        YibfIsTakibiEntry entry,
        YibfCellViewModel jobNameCell,
        YibfCellViewModel muellifCell,
        YibfCellViewModel denetciCell,
        YibfCellViewModel tumDijitalCell,
        YibfCellViewModel evrakCell,
        YibfCellViewModel sozlesmeCell,
        YibfCellViewModel dekontCell,
        YibfCellViewModel ruhsatBasvuruCell,
        YibfCellViewModel ruhsatNushaCell,
        YibfCellViewModel isyeriTeslimCell,
        YibfCellViewModel isgCell,
        YibfCellViewModel saglikCell,
        YibfCellViewModel topraklamaCell)
    {
        Entry = entry;
        JobNameCell = Attach(jobNameCell);
        MuellifCell = Attach(muellifCell);
        DenetciCell = Attach(denetciCell);
        TumDijitalCell = Attach(tumDijitalCell);
        EvrakCell = Attach(evrakCell);
        SozlesmeCell = Attach(sozlesmeCell);
        DekontCell = Attach(dekontCell);
        RuhsatBasvuruCell = Attach(ruhsatBasvuruCell);
        RuhsatNushaCell = Attach(ruhsatNushaCell);
        IsyeriTeslimCell = Attach(isyeriTeslimCell);
        IsgCell = Attach(isgCell);
        SaglikCell = Attach(saglikCell);
        TopraklamaCell = Attach(topraklamaCell);
    }

    public YibfIsTakibiEntry Entry { get; private set; }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public YibfCellViewModel JobNameCell { get; }
    public YibfCellViewModel MuellifCell { get; }
    public YibfCellViewModel DenetciCell { get; }
    public YibfCellViewModel TumDijitalCell { get; }
    public YibfCellViewModel EvrakCell { get; }
    public YibfCellViewModel SozlesmeCell { get; }
    public YibfCellViewModel DekontCell { get; }
    public YibfCellViewModel RuhsatBasvuruCell { get; }
    public YibfCellViewModel RuhsatNushaCell { get; }
    public YibfCellViewModel IsyeriTeslimCell { get; }
    public YibfCellViewModel IsgCell { get; }
    public YibfCellViewModel SaglikCell { get; }
    public YibfCellViewModel TopraklamaCell { get; }

    public void UpdateEntry(YibfIsTakibiEntry entry)
    {
        Entry = entry;
    }

    public void SetCellValue(string columnKey, string value)
    {
        switch (columnKey)
        {
            case YibfIsTakibiColumnKeys.JobName:
                Entry.JobName = value;
                break;
            case YibfIsTakibiColumnKeys.MuellifBilgileriGeldiMi:
                Entry.MuellifBilgileriGeldiMi = value;
                break;
            case YibfIsTakibiColumnKeys.DenetciAtamalariYapildiMi:
                Entry.DenetciAtamalariYapildiMi = value;
                break;
            case YibfIsTakibiColumnKeys.TumProjelerinDijitaliVarMi:
                Entry.TumProjelerinDijitaliVarMi = value;
                break;
            case YibfIsTakibiColumnKeys.EvraklarTamMi:
                Entry.EvraklarTamMi = value;
                break;
            case YibfIsTakibiColumnKeys.YibfSozlesmeHazirlandiMi:
                Entry.YibfSozlesmeHazirlandiMi = value;
                break;
            case YibfIsTakibiColumnKeys.DekontAlindiMi:
                Entry.DekontAlindiMi = value;
                break;
            case YibfIsTakibiColumnKeys.RuhsatBasvurusuYapildiMi:
                Entry.RuhsatBasvurusuYapildiMi = value;
                break;
            case YibfIsTakibiColumnKeys.RuhsatNushasiAlindiMi:
                Entry.RuhsatNushasiAlindiMi = value;
                break;
            case YibfIsTakibiColumnKeys.IsyeriTeslimTutangiHazirlandiMi:
                Entry.IsyeriTeslimTutangiHazirlandiMi = value;
                break;
            case YibfIsTakibiColumnKeys.IsgYazisiHazirlandiMi:
                Entry.IsgYazisiHazirlandiMi = value;
                break;
            case YibfIsTakibiColumnKeys.SaglikGuvenlikPlaniGeldiMi:
                Entry.SaglikGuvenlikPlaniGeldiMi = value;
                break;
            case YibfIsTakibiColumnKeys.TemelTopraklamaTutanagiHazirlandiMi:
                Entry.TemelTopraklamaTutanagiHazirlandiMi = value;
                break;
        }
    }

    public void CloseEditors()
    {
        foreach (var cell in GetCells())
        {
            cell.IsEditing = false;
            cell.DraftText = cell.Text;
        }
    }

    private IEnumerable<YibfCellViewModel> GetCells()
    {
        yield return JobNameCell;
        yield return MuellifCell;
        yield return DenetciCell;
        yield return TumDijitalCell;
        yield return EvrakCell;
        yield return SozlesmeCell;
        yield return DekontCell;
        yield return RuhsatBasvuruCell;
        yield return RuhsatNushaCell;
        yield return IsyeriTeslimCell;
        yield return IsgCell;
        yield return SaglikCell;
        yield return TopraklamaCell;
    }

    private YibfCellViewModel Attach(YibfCellViewModel cell)
    {
        cell.Row = this;
        return cell;
    }
}

public sealed class YibfCellViewModel : ViewModelBase
{
    private static readonly BrushConverter BrushConverter = new();

    private string _text;
    private string _draftText;
    private string _backgroundColor;
    private string _noteText;
    private bool _isEditing;

    public YibfCellViewModel(string columnKey, string text, string backgroundColor, string noteText, bool isInteractive = true)
    {
        ColumnKey = columnKey;
        _text = text;
        _draftText = text;
        _backgroundColor = backgroundColor;
        _noteText = noteText;
        IsInteractive = isInteractive;
    }

    public YibfIsTakibiRow Row { get; set; } = null!;
    public string ColumnKey { get; }
    public bool IsInteractive { get; }

    public string Text
    {
        get => _text;
        set => SetProperty(ref _text, value);
    }

    public string DraftText
    {
        get => _draftText;
        set => SetProperty(ref _draftText, value);
    }

    public string BackgroundColor
    {
        get => _backgroundColor;
        set
        {
            if (SetProperty(ref _backgroundColor, value))
            {
                OnPropertyChanged(nameof(BackgroundBrush));
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

    public bool IsEditing
    {
        get => _isEditing;
        set => SetProperty(ref _isEditing, value);
    }

    public bool HasNote => !string.IsNullOrWhiteSpace(NoteText);

    public Brush BackgroundBrush
    {
        get
        {
            if (string.IsNullOrWhiteSpace(BackgroundColor))
            {
                return Brushes.White;
            }

            return BrushConverter.ConvertFromString(BackgroundColor) as Brush ?? Brushes.White;
        }
    }
}



