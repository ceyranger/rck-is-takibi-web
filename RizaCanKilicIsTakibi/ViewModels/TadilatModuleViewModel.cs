using CommunityToolkit.Mvvm.Input;
using RizaCanKilicIsTakibi.Commands;
using RizaCanKilicIsTakibi.Helpers;
using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services;
using RizaCanKilicIsTakibi.Services.Abstractions;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Media;

namespace RizaCanKilicIsTakibi.ViewModels;

public sealed class TadilatModuleViewModel : ViewModelBase
{
    private const string StrongRedColor = "#FFFF0000";
    private const string StrongYellowColor = "#FFFFFF00";
    private const string StrongGreenColor = "#FF92D050";
    private const string StrongBlueColor = "#FF4F81BD";
    private const string StrongGrayColor = "#FFD9D9D9";
    private const string SinopDistrict = "SİNOP";
    private const string MerkezDistrict = "MERKEZ";

    private const string LegacyPaleRedColor = "#FFF4C4C4";
    private const string LegacyPaleYellowColor = "#FFF7EDB3";
    private const string LegacyPaleGreenColor = "#FFDCEECE";
    private const string LegacyPaleBlueColor = "#FFD5E4FF";
    private const string LegacyPaleGrayColor = "#FFE8ECF2";

    private static readonly IReadOnlyList<string> DefaultDistricts =
    [
        "AYANCIK",
        "BOYABAT",
        "DURAĞAN",
        "ERFELEK",
        "GERZE",
        "SARAYDÜZÜ",
        "SİNOP",
        "SİNOP OSB",
        "TÜRKELİ",
        "MERKEZ"
    ];

    private static readonly StringComparer TurkishDistrictComparer = StringComparer.Create(new CultureInfo("tr-TR"), ignoreCase: true);

    private static readonly IReadOnlyList<string> RowColorColumnKeys =
    [
        TadilatColumnKeys.JobName,
        TadilatColumnKeys.ProjectType,
        TadilatColumnKeys.DigitalReceived,
        TadilatColumnKeys.InspectorApproved,
        TadilatColumnKeys.OutputAndReportArrived,
        TadilatColumnKeys.OfficialLetterSubmitted,
        TadilatColumnKeys.ArchivedFromMunicipality,
        TadilatColumnKeys.Description1,
        TadilatColumnKeys.Description2
    ];

    private readonly ITadilatRepository _repository;
    private readonly ITadilatImportService _importService;
    private readonly IFileDialogService _fileDialogService;
    private readonly INotificationService _notificationService;
    private readonly IConfirmationService _confirmationService;
    private readonly ITadilatCellNoteDialogService _noteDialogService;
    private readonly IUndoRedoService _undoRedoService;
    private readonly IClipboardService _clipboardService;

    private bool _isInitialized;
    private bool _hasUnsavedChanges;
    private TadilatSubTab _selectedSubTab = TadilatSubTab.Aktif;
    private TadilatEntry? _selectedEntry;
    private Guid? _lastSelectedEntryId;
    private readonly Dictionary<string, TadilatDistrictGroup> _districtGroupLookup = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<Guid, TadilatEntryRow> _rowLookup = [];
    private readonly Dictionary<string, TadilatCellState> _cellStateLookup = new(StringComparer.OrdinalIgnoreCase);
    private sealed record TadilatUndoSnapshot(
        IReadOnlyList<TadilatEntry> Entries,
        IReadOnlyList<TadilatCellState> CellStates,
        TadilatSubTab SelectedSubTab,
        Guid? SelectedEntryId,
        bool HasUnsavedChanges);

    public TadilatModuleViewModel(
        ITadilatRepository repository,
        ITadilatImportService importService,
        IFileDialogService fileDialogService,
        INotificationService notificationService,
        IConfirmationService confirmationService,
        ITadilatCellNoteDialogService noteDialogService,
        IUndoRedoService undoRedoService,
        IClipboardService? clipboardService = null)
    {
        _repository = repository;
        _importService = importService;
        _fileDialogService = fileDialogService;
        _notificationService = notificationService;
        _confirmationService = confirmationService;
        _noteDialogService = noteDialogService;
        _undoRedoService = undoRedoService;
        _clipboardService = clipboardService ?? new ClipboardService();

        AktifEntries = [];
        BitenEntries = [];
        CellStates = [];
        DistrictGroups = [];
        Districts = [];
        DistrictCounts = [];
        ReplaceDistricts(DefaultDistricts);

        SelectSubTabCommand = new RelayCommand<TadilatSubTab>(tab => SelectedSubTab = tab);
        SelectEntryCommand = new RelayCommand<TadilatEntry?>(entry => SelectEntry(entry));
        AddEntryCommand = new AsyncRelayCommand<string?>(AddEntryAsync);
        DeleteEntryCommand = new AsyncRelayCommand(DeleteSelectedAsync, () => SelectedEntry is not null);
        ImportExcelCommand = new AsyncRelayCommand(ImportExcelAsync);

        BeginCellEditCommand = new RelayCommand<TadilatCellViewModel?>(BeginCellEdit);
        CommitCellEditCommand = new RelayCommand<TadilatCellViewModel?>(CommitCellEdit);
        CancelCellEditCommand = new RelayCommand<TadilatCellViewModel?>(CancelCellEdit);
        EditCellNoteCommand = new AsyncRelayCommand<TadilatCellViewModel?>(EditCellNoteAsync);
        SetCellColorRedCommand = new RelayCommand<TadilatCellViewModel?>(cell => SetCellColor(cell, StrongRedColor));
        SetCellColorYellowCommand = new RelayCommand<TadilatCellViewModel?>(cell => SetCellColor(cell, StrongYellowColor));
        SetCellColorGreenCommand = new RelayCommand<TadilatCellViewModel?>(cell => SetCellColor(cell, StrongGreenColor));
        SetCellColorBlueCommand = new RelayCommand<TadilatCellViewModel?>(cell => SetCellColor(cell, StrongBlueColor));
        SetCellColorGrayCommand = new RelayCommand<TadilatCellViewModel?>(cell => SetCellColor(cell, StrongGrayColor));
        ClearCellColorCommand = new RelayCommand<TadilatCellViewModel?>(cell => SetCellColor(cell, string.Empty));
        CopyCellCommand = new RelayCommand<TadilatCellViewModel?>(CopyCell);
        PasteCellCommand = new RelayCommand<TadilatCellViewModel?>(PasteCell, cell => cell?.IsInteractive == true);
        SetRowColorRedCommand = new RelayCommand<TadilatEntry?>(entry => SetRowColor(entry, StrongRedColor));
        SetRowColorYellowCommand = new RelayCommand<TadilatEntry?>(entry => SetRowColor(entry, StrongYellowColor));
        SetRowColorGreenCommand = new RelayCommand<TadilatEntry?>(entry => SetRowColor(entry, StrongGreenColor));
        SetRowColorBlueCommand = new RelayCommand<TadilatEntry?>(entry => SetRowColor(entry, StrongBlueColor));
        SetRowColorGrayCommand = new RelayCommand<TadilatEntry?>(entry => SetRowColor(entry, StrongGrayColor));
        ClearRowColorCommand = new RelayCommand<TadilatEntry?>(entry => SetRowColor(entry, string.Empty));
        MoveToBitenCommand = new AsyncRelayCommand<TadilatEntry?>(entry => MoveEntryToSubTabAsync(entry, TadilatSubTab.Biten));
        MoveToAktifCommand = new AsyncRelayCommand<TadilatEntry?>(entry => MoveEntryToSubTabAsync(entry, TadilatSubTab.Aktif));
        MoveEntryUpCommand = new AsyncRelayCommand<TadilatEntry?>(entry => MoveEntryAsync(entry, -1), CanMoveEntryUp);
        MoveEntryDownCommand = new AsyncRelayCommand<TadilatEntry?>(entry => MoveEntryAsync(entry, 1), CanMoveEntryDown);
    }

    public ObservableRangeCollection<TadilatEntry> AktifEntries { get; }
    public ObservableRangeCollection<TadilatEntry> BitenEntries { get; }
    public ObservableRangeCollection<TadilatCellState> CellStates { get; }
    public ObservableRangeCollection<string> Districts { get; }
    public ObservableRangeCollection<TadilatDistrictCountItem> DistrictCounts { get; }
    public ObservableRangeCollection<TadilatDistrictGroup> DistrictGroups { get; }

    public bool HasUnsavedChanges
    {
        get => _hasUnsavedChanges;
        private set => SetProperty(ref _hasUnsavedChanges, value);
    }

    public TadilatSubTab SelectedSubTab
    {
        get => _selectedSubTab;
        set
        {
            if (SetProperty(ref _selectedSubTab, value))
            {
                RefreshDistrictGroups();
                MoveEntryUpCommand.NotifyCanExecuteChanged();
                MoveEntryDownCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public TadilatEntry? SelectedEntry
    {
        get => _selectedEntry;
        set
        {
            if (SetProperty(ref _selectedEntry, value))
            {
                UpdateRowSelections();
                DeleteEntryCommand.NotifyCanExecuteChanged();
                MoveEntryUpCommand.NotifyCanExecuteChanged();
                MoveEntryDownCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public int VisibleEntryCount => GetCurrentCollection().Count;

    public RelayCommand<TadilatSubTab> SelectSubTabCommand { get; }
    public RelayCommand<TadilatEntry?> SelectEntryCommand { get; }
    public AsyncRelayCommand<string?> AddEntryCommand { get; }
    public AsyncRelayCommand DeleteEntryCommand { get; }
    public AsyncRelayCommand ImportExcelCommand { get; }
    public RelayCommand<TadilatCellViewModel?> BeginCellEditCommand { get; }
    public RelayCommand<TadilatCellViewModel?> CommitCellEditCommand { get; }
    public RelayCommand<TadilatCellViewModel?> CancelCellEditCommand { get; }
    public AsyncRelayCommand<TadilatCellViewModel?> EditCellNoteCommand { get; }
    public RelayCommand<TadilatCellViewModel?> SetCellColorRedCommand { get; }
    public RelayCommand<TadilatCellViewModel?> SetCellColorYellowCommand { get; }
    public RelayCommand<TadilatCellViewModel?> SetCellColorGreenCommand { get; }
    public RelayCommand<TadilatCellViewModel?> SetCellColorBlueCommand { get; }
    public RelayCommand<TadilatCellViewModel?> SetCellColorGrayCommand { get; }
    public RelayCommand<TadilatCellViewModel?> ClearCellColorCommand { get; }
    public RelayCommand<TadilatCellViewModel?> CopyCellCommand { get; }
    public RelayCommand<TadilatCellViewModel?> PasteCellCommand { get; }
    public RelayCommand<TadilatEntry?> SetRowColorRedCommand { get; }
    public RelayCommand<TadilatEntry?> SetRowColorYellowCommand { get; }
    public RelayCommand<TadilatEntry?> SetRowColorGreenCommand { get; }
    public RelayCommand<TadilatEntry?> SetRowColorBlueCommand { get; }
    public RelayCommand<TadilatEntry?> SetRowColorGrayCommand { get; }
    public RelayCommand<TadilatEntry?> ClearRowColorCommand { get; }
    public AsyncRelayCommand<TadilatEntry?> MoveToBitenCommand { get; }
    public AsyncRelayCommand<TadilatEntry?> MoveToAktifCommand { get; }
    public AsyncRelayCommand<TadilatEntry?> MoveEntryUpCommand { get; }
    public AsyncRelayCommand<TadilatEntry?> MoveEntryDownCommand { get; }

    public async Task InitializeAsync()
    {
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;

        try
        {
            var allEntries = (await _repository.GetAllAsync()).Select(CloneEntry).ToList();
            var hasDistrictMigration = NormalizeDistricts(allEntries);
            ReplaceEntries(AktifEntries, allEntries.Where(item => item.SubTab == TadilatSubTab.Aktif));
            ReplaceEntries(BitenEntries, allEntries.Where(item => item.SubTab == TadilatSubTab.Biten));
            ReplaceCellStates(await _repository.GetCellStatesAsync());
            RefreshDistrictGroups();
            HasUnsavedChanges = false;

            if (hasDistrictMigration)
            {
                await PersistAsync(showErrorToast: true);
            }
        }
        catch (Exception ex)
        {
            _isInitialized = false;
            _notificationService.ShowToast($"Tadilat yükleme hatası: {ex.Message}", ToastType.Error, TimeSpan.FromSeconds(5));
        }
    }

    public IReadOnlyList<TadilatEntry> GetEntriesSnapshot()
        => AktifEntries.Concat(BitenEntries).Select(CloneEntry).ToList();

    public IReadOnlyList<TadilatCellState> GetCellStatesSnapshot()
        => CellStates.Select(CloneCellState).ToList();

    public void LoadFromBackup(IEnumerable<TadilatEntry> entries, IEnumerable<TadilatCellState> cellStates, bool markDirty = true)
    {
        var sourceEntries = (entries ?? Array.Empty<TadilatEntry>()).Select(CloneEntry).ToList();
        NormalizeDistricts(sourceEntries);
        ReplaceEntries(AktifEntries, sourceEntries.Where(item => item.SubTab == TadilatSubTab.Aktif));
        ReplaceEntries(BitenEntries, sourceEntries.Where(item => item.SubTab == TadilatSubTab.Biten));
        ReplaceCellStates((cellStates ?? Array.Empty<TadilatCellState>()).Select(CloneCellState));
        HasUnsavedChanges = markDirty;
        RefreshDistrictGroups();
    }

    public async Task PersistAsync(bool showErrorToast = true)
    {
        try
        {
            await _repository.SaveManyAsync(GetEntriesSnapshot(), GetCellStatesSnapshot());
            HasUnsavedChanges = false;
        }
        catch (Exception ex)
        {
            HasUnsavedChanges = true;
            if (showErrorToast)
            {
                _notificationService.ShowToast($"Tadilat kayıt hatası: {ex.Message}", ToastType.Error);
            }
        }
    }

    public void CommitPendingEdits()
    {
        foreach (var row in DistrictGroups.SelectMany(group => group.Rows).Where(row => !row.IsPlaceholder).ToList())
        {
            CommitPendingEdit(row.JobNameCell);
            CommitPendingEdit(row.ProjectTypeCell);
            CommitPendingEdit(row.DigitalReceivedCell);
            CommitPendingEdit(row.InspectorApprovedCell);
            CommitPendingEdit(row.OutputAndReportArrivedCell);
            CommitPendingEdit(row.OfficialLetterSubmittedCell);
            CommitPendingEdit(row.ArchivedFromMunicipalityCell);
            CommitPendingEdit(row.Description1Cell);
            CommitPendingEdit(row.Description2Cell);
        }
    }

    private async Task ImportExcelAsync()
    {
        var path = _fileDialogService.ShowOpenDialog("Tadilat Excel içe aktar", "Excel (*.xlsx)|*.xlsx");
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        try
        {
            var imported = await _importService.ImportAsync(path);
            var validEntries = imported.Entries
                .Where(entry => !string.IsNullOrWhiteSpace(NormalizeDistrictName(entry.District)))
                .ToList();
            var validEntryIds = validEntries.Select(entry => entry.Id).ToHashSet();
            var validCellStates = imported.CellStates
                .Where(state => validEntryIds.Contains(state.EntryId))
                .ToList();
            var skippedCount = imported.Entries.Count - validEntries.Count;

            if (validEntries.Count == 0)
            {
                _notificationService.ShowToast("İçe aktarılacak geçerli tadilat satırı bulunamadı. İlçe alanı zorunludur.", ToastType.Warning, TimeSpan.FromSeconds(4));
                return;
            }

            LoadFromBackup(validEntries, validCellStates);
            await PersistAsync(showErrorToast: true);
            if (HasUnsavedChanges)
            {
                _notificationService.ShowToast("Tadilat verileri içe aktarıldı ancak kalıcı kaydetme tamamlanamadı.", ToastType.Warning, TimeSpan.FromSeconds(5));
                return;
            }

            if (skippedCount > 0)
            {
                _notificationService.ShowToast($"Tadilat Excel verileri içe aktarıldı, {skippedCount} satır boş ilçe nedeniyle atlandı.", ToastType.Warning, TimeSpan.FromSeconds(5));
                return;
            }

            _notificationService.ShowToast(
                $"Tadilat Excel verileri içe aktarıldı ve kaydedildi ({validEntries.Count} satır).",
                ToastType.Success);
        }
        catch (Exception ex)
        {
            _notificationService.ShowToast($"Tadilat import hatası: {ex.Message}", ToastType.Error, TimeSpan.FromSeconds(5));
        }
    }

    private async Task AddEntryAsync(string? district)
    {
        var targetDistrict = string.IsNullOrWhiteSpace(district)
            ? Districts.FirstOrDefault() ?? string.Empty
            : NormalizeDistrictName(district);

        if (string.IsNullOrWhiteSpace(targetDistrict))
        {
            return;
        }

        EnsureDistrictExists(targetDistrict);

        ExecuteUndoableMutation("Tadilat satır ekle", () =>
        {
            var collection = GetCurrentCollection();
            var entry = new TadilatEntry
            {
                Id = Guid.NewGuid(),
                SubTab = SelectedSubTab,
                District = targetDistrict,
                DisplayOrder = NextDisplayOrder(collection, targetDistrict),
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            collection.Add(entry);
            NormalizeDistrictOrder(collection, targetDistrict);
            SelectedEntry = entry;
            HasUnsavedChanges = true;
            RefreshDistrictGroups();
        });
        _notificationService.ShowToast("Tadilat satırı eklendi.", ToastType.Success, TimeSpan.FromSeconds(2));
        await Task.CompletedTask;
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
                Title = "Tadilat Satırını Sil",
                Message = $"\"{target.District}\" satırı silinecek.\n\nDevam edilsin mi?",
                IsDestructive = true
            }))
        {
            return;
        }

        ExecuteUndoableMutation("Tadilat satır sil", () =>
        {
            var collection = GetCollection(target.SubTab);
            collection.Remove(target);
            RemoveCellStates(target.Id);
            NormalizeDistrictOrder(collection, target.District);
            SelectedEntry = collection.FirstOrDefault(item => item.District.Equals(target.District, StringComparison.OrdinalIgnoreCase))
                ?? GetCurrentCollection().FirstOrDefault();
            HasUnsavedChanges = true;
            RefreshDistrictGroups();
        });
        _notificationService.ShowToast("Tadilat satırı silindi.", ToastType.Warning, TimeSpan.FromSeconds(2));
        await Task.CompletedTask;
    }

    private async Task MoveEntryToSubTabAsync(TadilatEntry? entry, TadilatSubTab targetSubTab)
    {
        var targetEntry = entry ?? SelectedEntry;
        if (targetEntry is null || targetEntry.SubTab == targetSubTab)
        {
            return;
        }

        ExecuteUndoableMutation("Tadilat sekme taşı", () =>
        {
            CloseAllEditors();

            var sourceCollection = GetCollection(targetEntry.SubTab);
            var targetCollection = GetCollection(targetSubTab);
            sourceCollection.Remove(targetEntry);

            targetEntry.SubTab = targetSubTab;
            targetEntry.DisplayOrder = NextDisplayOrder(targetCollection, targetEntry.District);
            targetEntry.UpdatedAt = DateTime.Now;
            targetCollection.Add(targetEntry);

            NormalizeDistrictOrder(sourceCollection, targetEntry.District);
            NormalizeDistrictOrder(targetCollection, targetEntry.District);

            HasUnsavedChanges = true;
            RefreshDistrictGroups();

            if (SelectedSubTab == targetSubTab)
            {
                SelectedEntry = targetEntry;
            }
            else
            {
                SelectedEntry = GetCurrentCollection().FirstOrDefault(item => item.District.Equals(targetEntry.District, StringComparison.OrdinalIgnoreCase))
                    ?? GetCurrentCollection().FirstOrDefault();
            }
        });

        var destinationLabel = targetSubTab == TadilatSubTab.Biten ? "BİTEN" : "AKTİF";
        _notificationService.ShowToast($"Tadilat satırı {destinationLabel} sekmesine taşındı.", ToastType.Success, TimeSpan.FromSeconds(2));
        await Task.CompletedTask;
    }

    private async Task MoveEntryAsync(TadilatEntry? entry, int direction)
    {
        var targetEntry = ResolveCurrentEntry(entry ?? SelectedEntry);
        if (targetEntry is null || targetEntry.SubTab != SelectedSubTab)
        {
            return;
        }

        var collection = GetCollection(targetEntry.SubTab);
        var ordered = GetOrderedDistrictEntries(collection, targetEntry.District);
        var currentIndex = ordered.FindIndex(item => item.Id == targetEntry.Id);
        var targetIndex = currentIndex + direction;
        if (currentIndex < 0 || targetIndex < 0 || targetIndex >= ordered.Count)
        {
            return;
        }

        ExecuteUndoableMutation("Tadilat sıralama değiştir", () =>
        {
            CloseAllEditors();

            var currentCollection = GetCollection(targetEntry.SubTab);
            var currentOrdered = GetOrderedDistrictEntries(currentCollection, targetEntry.District);
            var sourceIndex = currentOrdered.FindIndex(item => item.Id == targetEntry.Id);
            var destinationIndex = sourceIndex + direction;
            if (sourceIndex < 0 || destinationIndex < 0 || destinationIndex >= currentOrdered.Count)
            {
                return;
            }

            (currentOrdered[sourceIndex], currentOrdered[destinationIndex]) = (currentOrdered[destinationIndex], currentOrdered[sourceIndex]);
            for (var index = 0; index < currentOrdered.Count; index++)
            {
                currentOrdered[index].DisplayOrder = index;
                currentOrdered[index].UpdatedAt = DateTime.Now;
            }

            SelectedEntry = currentCollection.FirstOrDefault(item => item.Id == targetEntry.Id);
            HasUnsavedChanges = true;
            RefreshDistrictGroups();
        });

        await Task.CompletedTask;
    }

    private bool CanMoveEntryUp(TadilatEntry? entry)
        => CanMoveEntry(entry ?? SelectedEntry, -1);

    private bool CanMoveEntryDown(TadilatEntry? entry)
        => CanMoveEntry(entry ?? SelectedEntry, 1);

    private bool CanMoveEntry(TadilatEntry? entry, int direction)
    {
        var targetEntry = ResolveCurrentEntry(entry);
        if (targetEntry is null || targetEntry.SubTab != SelectedSubTab)
        {
            return false;
        }

        var ordered = GetOrderedDistrictEntries(GetCollection(targetEntry.SubTab), targetEntry.District);
        var currentIndex = ordered.FindIndex(item => item.Id == targetEntry.Id);
        var targetIndex = currentIndex + direction;
        return currentIndex >= 0 && targetIndex >= 0 && targetIndex < ordered.Count;
    }

    private void BeginCellEdit(TadilatCellViewModel? cell)
    {
        if (cell is null || !cell.IsInteractive)
        {
            return;
        }

        CloseAllEditors();
        SelectEntry(cell.Row.Entry);
        cell.DraftText = cell.Text;
        cell.IsEditing = true;
    }

    private void CommitPendingEdit(TadilatCellViewModel cell)
    {
        if (cell.IsEditing)
        {
            CommitCellEdit(cell);
        }
    }

    private void CommitCellEdit(TadilatCellViewModel? cell)
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

        if (cell.ColumnKey == TadilatColumnKeys.JobName && string.IsNullOrWhiteSpace(newValue))
        {
            cell.DraftText = cell.Text;
            cell.IsEditing = false;
            _notificationService.ShowToast("İş adı alanı zorunludur.", ToastType.Warning, TimeSpan.FromSeconds(2));
            return;
        }

        ExecuteUndoableMutation("Tadilat hücre düzenle", () =>
        {
            cell.Row.SetCellValue(cell.ColumnKey, newValue);
            cell.Text = newValue;
            cell.IsEditing = false;
            cell.Row.Entry.UpdatedAt = DateTime.Now;
            HasUnsavedChanges = true;
        });
    }

    private void CancelCellEdit(TadilatCellViewModel? cell)
    {
        if (cell is null)
        {
            return;
        }

        cell.DraftText = cell.Text;
        cell.IsEditing = false;
    }

    private void CopyCell(TadilatCellViewModel? cell)
    {
        if (cell is null || cell.Row.Entry is null)
        {
            return;
        }

        SelectEntry(cell.Row.Entry);
        var payload = new CellClipboardPayload
        {
            Text = cell.Text,
            BackgroundColor = NormalizeCellColor(GetCellState(cell.Row.Entry.Id, cell.ColumnKey)?.BackgroundColor),
            NoteText = cell.NoteText
        };

        if (_clipboardService.TrySetCellPayload(payload))
        {
            _notificationService.ShowToast("Hücre panoya kopyalandı.", ToastType.Info, TimeSpan.FromSeconds(2));
            return;
        }

        _notificationService.ShowToast("Pano erişimi sağlanamadı.", ToastType.Warning, TimeSpan.FromSeconds(3));
    }

    private void PasteCell(TadilatCellViewModel? cell)
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

        CloseAllEditors();
        SelectEntry(cell.Row.Entry);

        var sourcePayload = payload ?? new CellClipboardPayload { Text = text ?? string.Empty };
        var normalizedText = sourcePayload.Text ?? string.Empty;
        var normalizedBackgroundColor = NormalizeCellColor(sourcePayload.BackgroundColor);
        var normalizedNoteText = sourcePayload.NoteText?.Trim() ?? string.Empty;
        var currentStoredState = GetCellState(cell.Row.Entry.Id, cell.ColumnKey);
        var currentBackgroundColor = NormalizeCellColor(currentStoredState?.BackgroundColor);
        var currentNoteText = currentStoredState?.NoteText ?? string.Empty;
        if (string.Equals(cell.Text, normalizedText, StringComparison.Ordinal)
            && string.Equals(currentBackgroundColor, normalizedBackgroundColor, StringComparison.Ordinal)
            && string.Equals(currentNoteText, normalizedNoteText, StringComparison.Ordinal))
        {
            return;
        }

        if (cell.ColumnKey == TadilatColumnKeys.JobName && string.IsNullOrWhiteSpace(normalizedText))
        {
            _notificationService.ShowToast("İş adı alanı zorunludur.", ToastType.Warning, TimeSpan.FromSeconds(2));
            return;
        }

        ExecuteUndoableMutation("Tadilat hücre yapıştır", () =>
        {
            cell.Row.SetCellValue(cell.ColumnKey, normalizedText);
            cell.Text = normalizedText;
            cell.DraftText = normalizedText;
            var state = GetOrCreateCellState(cell.Row.Entry.Id, cell.ColumnKey);
            state.BackgroundColor = normalizedBackgroundColor;
            state.NoteText = normalizedNoteText;
            CleanupCellStateIfEmpty(state);
            var effectiveState = GetCellState(cell.Row.Entry.Id, cell.ColumnKey);
            cell.BackgroundColor = ResolveEffectiveCellBackground(cell.Row.Entry, effectiveState?.BackgroundColor);
            cell.NoteText = effectiveState?.NoteText ?? string.Empty;
            cell.Row.Entry.UpdatedAt = DateTime.Now;
            HasUnsavedChanges = true;
        });
    }

    private async Task EditCellNoteAsync(TadilatCellViewModel? cell)
    {
        if (cell is null || !cell.IsInteractive || cell.Row.Entry is null)
        {
            return;
        }

        SelectEntry(cell.Row.Entry);
        var result = await _noteDialogService.ShowDialogAsync(cell.NoteText);
        if (result is null)
        {
            return;
        }

        ExecuteUndoableMutation("Tadilat hücre notu", () =>
        {
            var state = GetOrCreateCellState(cell.Row.Entry.Id, cell.ColumnKey);
            state.NoteText = result.DeleteRequested ? string.Empty : result.NoteText.Trim();
            CleanupCellStateIfEmpty(state);
            cell.NoteText = state.NoteText;
            HasUnsavedChanges = true;
        });
    }

    private void SetCellColor(TadilatCellViewModel? cell, string color)
    {
        if (cell is null || !cell.IsInteractive || cell.Row.Entry is null)
        {
            return;
        }

        ExecuteUndoableMutation("Tadilat hücre rengi", () =>
        {
            SelectEntry(cell.Row.Entry);
            var state = GetOrCreateCellState(cell.Row.Entry.Id, cell.ColumnKey);
            state.BackgroundColor = NormalizeCellColor(color);
            CleanupCellStateIfEmpty(state);
            cell.BackgroundColor = state.BackgroundColor;
            HasUnsavedChanges = true;
        });
    }

    private void SetRowColor(TadilatEntry? entry, string color)
    {
        var targetEntry = entry ?? SelectedEntry;
        if (targetEntry is null || targetEntry.SubTab != TadilatSubTab.Aktif)
        {
            return;
        }

        ExecuteUndoableMutation("Tadilat satır rengi", () =>
        {
            var normalizedColor = NormalizeCellColor(color);
            foreach (var columnKey in RowColorColumnKeys)
            {
                var state = GetCellState(targetEntry.Id, columnKey);
                if (string.IsNullOrWhiteSpace(normalizedColor))
                {
                    if (state is null)
                    {
                        continue;
                    }

                    state.BackgroundColor = string.Empty;
                    CleanupCellStateIfEmpty(state);
                    continue;
                }

                state ??= GetOrCreateCellState(targetEntry.Id, columnKey);
                state.BackgroundColor = normalizedColor;
            }

            targetEntry.UpdatedAt = DateTime.Now;
            if (_rowLookup.TryGetValue(targetEntry.Id, out var row))
            {
                UpdateRow(row, targetEntry);
            }

            HasUnsavedChanges = true;
        });
    }

    private TadilatUndoSnapshot CaptureUndoSnapshot()
        => new(
            GetEntriesSnapshot(),
            GetCellStatesSnapshot(),
            SelectedSubTab,
            SelectedEntry?.Id,
            HasUnsavedChanges);

    private void ApplyUndoSnapshot(TadilatUndoSnapshot snapshot)
    {
        SelectedSubTab = snapshot.SelectedSubTab;
        LoadFromBackup(snapshot.Entries, snapshot.CellStates);
        HasUnsavedChanges = snapshot.HasUnsavedChanges;
        SelectedEntry = GetCurrentCollection().FirstOrDefault(item => item.Id == snapshot.SelectedEntryId)
            ?? GetCurrentCollection().FirstOrDefault();
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

    private void RefreshDistrictGroups()
    {
        var collection = GetCurrentCollection();
        var orderedDistricts = GetOrderedDistricts(collection);
        var activeDistricts = new HashSet<string>(orderedDistricts, StringComparer.OrdinalIgnoreCase);

        foreach (var obsoleteDistrict in _districtGroupLookup.Keys.Where(key => !activeDistricts.Contains(key)).ToList())
        {
            var obsoleteGroup = _districtGroupLookup[obsoleteDistrict];
            foreach (var obsoleteRow in obsoleteGroup.Rows.Where(row => row.Entry is not null).ToList())
            {
                _rowLookup.Remove(obsoleteRow.Entry!.Id);
            }

            DistrictGroups.Remove(obsoleteGroup);
            _districtGroupLookup.Remove(obsoleteDistrict);
        }

        for (var index = 0; index < orderedDistricts.Count; index++)
        {
            var district = orderedDistricts[index];
            if (!_districtGroupLookup.TryGetValue(district, out var group))
            {
                group = new TadilatDistrictGroup(district, new ObservableCollection<TadilatEntryRow>(), false);
                _districtGroupLookup[district] = group;
                DistrictGroups.Insert(Math.Min(index, DistrictGroups.Count), group);
            }

            var items = collection
                .Where(item => item.District.Equals(district, StringComparison.OrdinalIgnoreCase))
                .OrderBy(item => item.DisplayOrder)
                .ThenBy(item => item.UpdatedAt)
                .ToList();

            SyncDistrictGroup(group, items);

            var currentIndex = DistrictGroups.IndexOf(group);
            if (currentIndex >= 0 && currentIndex != index)
            {
                DistrictGroups.Move(currentIndex, index);
            }
        }

        if (SelectedEntry is not null && !collection.Any(item => item.Id == SelectedEntry.Id))
        {
            _selectedEntry = null;
            OnPropertyChanged(nameof(SelectedEntry));
        }

        UpdateRowSelections();
        RefreshDistrictCounts(collection);
        OnPropertyChanged(nameof(VisibleEntryCount));
        DeleteEntryCommand.NotifyCanExecuteChanged();
        MoveEntryUpCommand.NotifyCanExecuteChanged();
        MoveEntryDownCommand.NotifyCanExecuteChanged();
    }

    private List<string> GetOrderedDistricts(IEnumerable<TadilatEntry> collection)
    {
        var orderedDistricts = Districts
            .Concat(collection.Select(item => NormalizeDistrictName(item.District)))
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(item => item, TurkishDistrictComparer)
            .ToList();
        return orderedDistricts;
    }

    private void SyncDistrictGroup(TadilatDistrictGroup group, IReadOnlyList<TadilatEntry> items)
    {
        var rows = group.Rows;

        if (items.Count == 0)
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

        var desiredIds = items.Select(item => item.Id).ToHashSet();
        foreach (var obsoleteRow in rows.Where(row => row.Entry is not null && !desiredIds.Contains(row.Entry.Id)).ToList())
        {
            rows.Remove(obsoleteRow);
            _rowLookup.Remove(obsoleteRow.Entry!.Id);
        }

        for (var index = 0; index < items.Count; index++)
        {
            var item = items[index];
            if (!_rowLookup.TryGetValue(item.Id, out var row))
            {
                row = BuildRow(item);
                _rowLookup[item.Id] = row;
            }
            else
            {
                UpdateRow(row, item);
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

        while (rows.Count > items.Count)
        {
            var extraRow = rows[rows.Count - 1];
            rows.RemoveAt(rows.Count - 1);
            if (extraRow.Entry is not null)
            {
                _rowLookup.Remove(extraRow.Entry.Id);
            }
        }

        group.HasItems = true;
    }

    private void UpdateRow(TadilatEntryRow row, TadilatEntry entry)
    {
        row.UpdateEntry(entry);
        UpdateCell(row.JobNameCell, entry, TadilatColumnKeys.JobName, entry.JobName);
        UpdateCell(row.ProjectTypeCell, entry, TadilatColumnKeys.ProjectType, entry.ProjectType);
        UpdateCell(row.DigitalReceivedCell, entry, TadilatColumnKeys.DigitalReceived, entry.DigitalReceived);
        UpdateCell(row.InspectorApprovedCell, entry, TadilatColumnKeys.InspectorApproved, entry.InspectorApproved);
        UpdateCell(row.OutputAndReportArrivedCell, entry, TadilatColumnKeys.OutputAndReportArrived, entry.OutputAndReportArrived);
        UpdateCell(row.OfficialLetterSubmittedCell, entry, TadilatColumnKeys.OfficialLetterSubmitted, entry.OfficialLetterSubmitted);
        UpdateCell(row.ArchivedFromMunicipalityCell, entry, TadilatColumnKeys.ArchivedFromMunicipality, entry.ArchivedFromMunicipality);
        UpdateCell(row.Description1Cell, entry, TadilatColumnKeys.Description1, entry.Description1);
        UpdateCell(row.Description2Cell, entry, TadilatColumnKeys.Description2, entry.Description2);
    }

    private void UpdateCell(TadilatCellViewModel cell, TadilatEntry entry, string columnKey, string text)
    {
        if (!cell.IsEditing)
        {
            cell.Text = text;
            cell.DraftText = text;
        }

        var state = GetCellState(entry.Id, columnKey);
        cell.IsInteractive = IsCellInteractive(entry);
        cell.BackgroundColor = ResolveEffectiveCellBackground(entry, state?.BackgroundColor);
        cell.NoteText = state?.NoteText ?? string.Empty;
    }

    private TadilatCellState? GetCellState(Guid entryId, string columnKey)
        => _cellStateLookup.TryGetValue(BuildCellStateKey(entryId, columnKey), out var state) ? state : null;

    private TadilatEntryRow BuildRow(TadilatEntry entry)
    {
        return new TadilatEntryRow(
            entry,
            CreateCell(entry, TadilatColumnKeys.JobName, entry.JobName),
            CreateCell(entry, TadilatColumnKeys.ProjectType, entry.ProjectType),
            CreateCell(entry, TadilatColumnKeys.DigitalReceived, entry.DigitalReceived),
            CreateCell(entry, TadilatColumnKeys.InspectorApproved, entry.InspectorApproved),
            CreateCell(entry, TadilatColumnKeys.OutputAndReportArrived, entry.OutputAndReportArrived),
            CreateCell(entry, TadilatColumnKeys.OfficialLetterSubmitted, entry.OfficialLetterSubmitted),
            CreateCell(entry, TadilatColumnKeys.ArchivedFromMunicipality, entry.ArchivedFromMunicipality),
            CreateCell(entry, TadilatColumnKeys.Description1, entry.Description1),
            CreateCell(entry, TadilatColumnKeys.Description2, entry.Description2));
    }

    private TadilatCellViewModel CreateCell(TadilatEntry entry, string columnKey, string text)
    {
        var state = GetCellState(entry.Id, columnKey);
        return new TadilatCellViewModel(
            columnKey,
            text,
            ResolveEffectiveCellBackground(entry, state?.BackgroundColor),
            state?.NoteText ?? string.Empty,
            IsCellInteractive(entry));
    }

    private TadilatCellState GetOrCreateCellState(Guid entryId, string columnKey)
    {
        var key = BuildCellStateKey(entryId, columnKey);
        if (_cellStateLookup.TryGetValue(key, out var existing))
        {
            return existing;
        }

        var created = new TadilatCellState
        {
            EntryId = entryId,
            ColumnKey = columnKey
        };
        CellStates.Add(created);
        _cellStateLookup[key] = created;
        return created;
    }

    private void CleanupCellStateIfEmpty(TadilatCellState state)
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
        var items = CellStates.Where(item => item.EntryId == entryId).ToList();
        foreach (var item in items)
        {
            CellStates.Remove(item);
            _cellStateLookup.Remove(BuildCellStateKey(item.EntryId, item.ColumnKey));
        }
    }

    private static string BuildCellStateKey(Guid entryId, string columnKey)
        => $"{entryId:N}|{columnKey}";

    private static bool IsCellInteractive(TadilatEntry entry)
        => entry.SubTab == TadilatSubTab.Aktif;

    private static string ResolveEffectiveCellBackground(TadilatEntry entry, string? storedColor)
    {
        if (entry.SubTab == TadilatSubTab.Biten)
        {
            return StrongBlueColor;
        }

        return storedColor ?? string.Empty;
    }

    private bool NormalizeDistricts(IEnumerable<TadilatEntry> entries)
    {
        var hasChange = false;
        foreach (var entry in entries)
        {
            var normalizedDistrict = NormalizeDistrictName(entry.District);
            if (!string.Equals(entry.District, normalizedDistrict, StringComparison.Ordinal))
            {
                entry.District = normalizedDistrict;
                entry.UpdatedAt = DateTime.Now;
                hasChange = true;
            }
        }

        return hasChange;
    }

    private static string NormalizeDistrictName(string? district)
    {
        var value = district?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        if (value.Equals(MerkezDistrict, StringComparison.OrdinalIgnoreCase))
        {
            return SinopDistrict;
        }

        return value.ToUpper(new CultureInfo("tr-TR"));
    }

    private void ReplaceDistricts(IEnumerable<string> source)
    {
        var normalized = source
            .Select(NormalizeDistrictName)
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(item => item, TurkishDistrictComparer)
            .ToList();

        Districts.ReplaceRange(normalized);
    }

    private void RefreshDistrictCounts(IEnumerable<TadilatEntry> source)
    {
        var counts = source
            .GroupBy(item => NormalizeDistrictName(item.District), StringComparer.OrdinalIgnoreCase)
            .Select(group => new TadilatDistrictCountItem(group.Key, group.Count()))
            .OrderBy(item => item.District, TurkishDistrictComparer)
            .ToList();

        DistrictCounts.ReplaceRange(counts);
    }

    private void ReplaceEntries(ObservableRangeCollection<TadilatEntry> target, IEnumerable<TadilatEntry> source)
    {
        var newItems = new List<TadilatEntry>();
        foreach (var item in source.OrderBy(entry => entry.District, StringComparer.OrdinalIgnoreCase).ThenBy(entry => entry.DisplayOrder).ThenBy(entry => entry.UpdatedAt))
        {
            newItems.Add(CloneEntry(item));
            EnsureDistrictExists(item.District);
        }
        target.ReplaceRange(newItems);
    }

    private void ReplaceCellStates(IEnumerable<TadilatCellState> states)
    {
        _cellStateLookup.Clear();
        var clonedList = new List<TadilatCellState>();
        foreach (var state in states)
        {
            var cloned = CloneCellState(state);
            clonedList.Add(cloned);
            _cellStateLookup[BuildCellStateKey(cloned.EntryId, cloned.ColumnKey)] = cloned;
        }
        CellStates.ReplaceRange(clonedList);
    }

    private void EnsureDistrictExists(string district)
    {
        district = NormalizeDistrictName(district);
        if (string.IsNullOrWhiteSpace(district))
        {
            return;
        }

        if (!Districts.Any(item => item.Equals(district, StringComparison.OrdinalIgnoreCase)))
        {
            Districts.Add(district);
            ReplaceDistricts(Districts);
        }
    }

    private void CloseAllEditors()
    {
        foreach (var group in DistrictGroups)
        {
            foreach (var row in group.Rows)
            {
                row.CloseEditors();
            }
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

    private void SelectEntry(TadilatEntry? entry)
    {
        if (entry is not null)
        {
            SelectedEntry = entry;
        }
    }

    private ObservableRangeCollection<TadilatEntry> GetCurrentCollection()
        => SelectedSubTab == TadilatSubTab.Aktif ? AktifEntries : BitenEntries;

    private ObservableRangeCollection<TadilatEntry> GetCollection(TadilatSubTab subTab)
        => subTab == TadilatSubTab.Aktif ? AktifEntries : BitenEntries;

    private TadilatEntry? ResolveCurrentEntry(TadilatEntry? entry)
        => entry is null ? null : GetCollection(entry.SubTab).FirstOrDefault(item => item.Id == entry.Id);

    private static List<TadilatEntry> GetOrderedDistrictEntries(IEnumerable<TadilatEntry> entries, string district)
        => entries.Where(item => item.District.Equals(district, StringComparison.OrdinalIgnoreCase))
            .OrderBy(item => item.DisplayOrder)
            .ThenBy(item => item.UpdatedAt)
            .ToList();

    private static int NextDisplayOrder(IEnumerable<TadilatEntry> entries, string district)
        => entries.Where(item => item.District.Equals(district, StringComparison.OrdinalIgnoreCase))
            .Select(item => item.DisplayOrder)
            .DefaultIfEmpty(-1)
            .Max() + 1;

    private static void NormalizeDistrictOrder(IEnumerable<TadilatEntry> entries, string district)
    {
        var items = entries.Where(item => item.District.Equals(district, StringComparison.OrdinalIgnoreCase))
            .OrderBy(item => item.DisplayOrder)
            .ThenBy(item => item.UpdatedAt)
            .ToList();

        for (var index = 0; index < items.Count; index++)
        {
            items[index].DisplayOrder = index;
            items[index].UpdatedAt = DateTime.Now;
        }
    }

    private static TadilatEntry CloneEntry(TadilatEntry entry)
    {
        return new TadilatEntry
        {
            Id = entry.Id,
            SubTab = entry.SubTab,
            District = entry.District,
            JobName = entry.JobName,
            ProjectType = entry.ProjectType,
            DigitalReceived = entry.DigitalReceived,
            InspectorApproved = entry.InspectorApproved,
            OutputAndReportArrived = entry.OutputAndReportArrived,
            OfficialLetterSubmitted = entry.OfficialLetterSubmitted,
            ArchivedFromMunicipality = entry.ArchivedFromMunicipality,
            Description1 = entry.Description1,
            Description2 = entry.Description2,
            DisplayOrder = entry.DisplayOrder,
            CreatedAt = entry.CreatedAt,
            UpdatedAt = entry.UpdatedAt
        };
    }

    private static TadilatCellState CloneCellState(TadilatCellState state)
    {
        return new TadilatCellState
        {
            EntryId = state.EntryId,
            ColumnKey = state.ColumnKey,
            BackgroundColor = NormalizeCellColor(state.BackgroundColor),
            NoteText = state.NoteText
        };
    }

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
}

public sealed class TadilatDistrictGroup : ViewModelBase
{
    private bool _hasItems;

    public TadilatDistrictGroup(string district, ObservableCollection<TadilatEntryRow> rows, bool hasItems)
    {
        District = district;
        Rows = rows;
        _hasItems = hasItems;
    }

    public string District { get; }
    public ObservableCollection<TadilatEntryRow> Rows { get; }

    public bool HasItems
    {
        get => _hasItems;
        set => SetProperty(ref _hasItems, value);
    }
}

public sealed class TadilatEntryRow : ViewModelBase
{
    private bool _isSelected;

    public TadilatEntryRow(
        TadilatEntry entry,
        TadilatCellViewModel jobNameCell,
        TadilatCellViewModel projectTypeCell,
        TadilatCellViewModel digitalReceivedCell,
        TadilatCellViewModel inspectorApprovedCell,
        TadilatCellViewModel outputAndReportArrivedCell,
        TadilatCellViewModel officialLetterSubmittedCell,
        TadilatCellViewModel archivedFromMunicipalityCell,
        TadilatCellViewModel description1Cell,
        TadilatCellViewModel description2Cell)
    {
        Entry = entry;
        District = entry.District;
        JobNameCell = Attach(jobNameCell);
        ProjectTypeCell = Attach(projectTypeCell);
        DigitalReceivedCell = Attach(digitalReceivedCell);
        InspectorApprovedCell = Attach(inspectorApprovedCell);
        OutputAndReportArrivedCell = Attach(outputAndReportArrivedCell);
        OfficialLetterSubmittedCell = Attach(officialLetterSubmittedCell);
        ArchivedFromMunicipalityCell = Attach(archivedFromMunicipalityCell);
        Description1Cell = Attach(description1Cell);
        Description2Cell = Attach(description2Cell);
    }

    private TadilatEntryRow(string district)
    {
        District = district;
        IsPlaceholder = true;
        JobNameCell = Attach(new TadilatCellViewModel(TadilatColumnKeys.JobName, string.Empty, string.Empty, string.Empty, false));
        ProjectTypeCell = Attach(new TadilatCellViewModel(TadilatColumnKeys.ProjectType, string.Empty, string.Empty, string.Empty, false));
        DigitalReceivedCell = Attach(new TadilatCellViewModel(TadilatColumnKeys.DigitalReceived, string.Empty, string.Empty, string.Empty, false));
        InspectorApprovedCell = Attach(new TadilatCellViewModel(TadilatColumnKeys.InspectorApproved, string.Empty, string.Empty, string.Empty, false));
        OutputAndReportArrivedCell = Attach(new TadilatCellViewModel(TadilatColumnKeys.OutputAndReportArrived, string.Empty, string.Empty, string.Empty, false));
        OfficialLetterSubmittedCell = Attach(new TadilatCellViewModel(TadilatColumnKeys.OfficialLetterSubmitted, string.Empty, string.Empty, string.Empty, false));
        ArchivedFromMunicipalityCell = Attach(new TadilatCellViewModel(TadilatColumnKeys.ArchivedFromMunicipality, string.Empty, string.Empty, string.Empty, false));
        Description1Cell = Attach(new TadilatCellViewModel(TadilatColumnKeys.Description1, string.Empty, string.Empty, string.Empty, false));
        Description2Cell = Attach(new TadilatCellViewModel(TadilatColumnKeys.Description2, string.Empty, string.Empty, string.Empty, false));
    }

    public static TadilatEntryRow CreatePlaceholder(string district) => new(district);

    public TadilatEntry? Entry { get; private set; }
    public string District { get; }
    public bool IsPlaceholder { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public TadilatCellViewModel JobNameCell { get; }
    public TadilatCellViewModel ProjectTypeCell { get; }
    public TadilatCellViewModel DigitalReceivedCell { get; }
    public TadilatCellViewModel InspectorApprovedCell { get; }
    public TadilatCellViewModel OutputAndReportArrivedCell { get; }
    public TadilatCellViewModel OfficialLetterSubmittedCell { get; }
    public TadilatCellViewModel ArchivedFromMunicipalityCell { get; }
    public TadilatCellViewModel Description1Cell { get; }
    public TadilatCellViewModel Description2Cell { get; }

    public void UpdateEntry(TadilatEntry entry)
    {
        Entry = entry;
    }

    public void SetCellValue(string columnKey, string value)
    {
        if (Entry is null)
        {
            return;
        }

        switch (columnKey)
        {
            case TadilatColumnKeys.JobName:
                Entry.JobName = value;
                break;
            case TadilatColumnKeys.ProjectType:
                Entry.ProjectType = value;
                break;
            case TadilatColumnKeys.DigitalReceived:
                Entry.DigitalReceived = value;
                break;
            case TadilatColumnKeys.InspectorApproved:
                Entry.InspectorApproved = value;
                break;
            case TadilatColumnKeys.OutputAndReportArrived:
                Entry.OutputAndReportArrived = value;
                break;
            case TadilatColumnKeys.OfficialLetterSubmitted:
                Entry.OfficialLetterSubmitted = value;
                break;
            case TadilatColumnKeys.ArchivedFromMunicipality:
                Entry.ArchivedFromMunicipality = value;
                break;
            case TadilatColumnKeys.Description1:
                Entry.Description1 = value;
                break;
            case TadilatColumnKeys.Description2:
                Entry.Description2 = value;
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

    private IEnumerable<TadilatCellViewModel> GetCells()
    {
        yield return JobNameCell;
        yield return ProjectTypeCell;
        yield return DigitalReceivedCell;
        yield return InspectorApprovedCell;
        yield return OutputAndReportArrivedCell;
        yield return OfficialLetterSubmittedCell;
        yield return ArchivedFromMunicipalityCell;
        yield return Description1Cell;
        yield return Description2Cell;
    }

    private TadilatCellViewModel Attach(TadilatCellViewModel cell)
    {
        cell.Row = this;
        return cell;
    }
}

public sealed class TadilatCellViewModel : ViewModelBase
{
    private static readonly BrushConverter BrushConverter = new();

    private string _text;
    private string _draftText;
    private string _backgroundColor;
    private string _noteText;
    private bool _isEditing;
    private bool _isInteractive;

    public TadilatCellViewModel(string columnKey, string text, string backgroundColor, string noteText, bool isInteractive = true)
    {
        ColumnKey = columnKey;
        _text = text;
        _draftText = text;
        _backgroundColor = backgroundColor;
        _noteText = noteText;
        _isInteractive = isInteractive;
    }

    public TadilatEntryRow Row { get; set; } = null!;
    public string ColumnKey { get; }
    public bool IsInteractive
    {
        get => _isInteractive;
        set => SetProperty(ref _isInteractive, value);
    }

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

public sealed class TadilatDistrictCountItem
{
    public TadilatDistrictCountItem(string district, int count)
    {
        District = district;
        Count = count;
    }

    public string District { get; }
    public int Count { get; }
    public string DisplayText => $"{District}: {Count}";
}











