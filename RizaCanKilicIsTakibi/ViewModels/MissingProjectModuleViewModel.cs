using CommunityToolkit.Mvvm.Input;
using RizaCanKilicIsTakibi.Commands;
using RizaCanKilicIsTakibi.Helpers;
using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services;
using RizaCanKilicIsTakibi.Services.Abstractions;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Media;

namespace RizaCanKilicIsTakibi.ViewModels;

public sealed class MissingProjectModuleViewModel : ViewModelBase
{
    private static readonly HashSet<string> PersistedProperties =
    [
        nameof(MissingProjectEntry.AdaParsel),
        nameof(MissingProjectEntry.YapiSahibi),
        nameof(MissingProjectEntry.RecordMediumText),
        nameof(MissingProjectEntry.MissingProjectText),
        nameof(MissingProjectEntry.Description)
    ];

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

    private readonly IMissingProjectRepository _repository;
    private readonly INotificationService _notificationService;
    private readonly IConfirmationService _confirmationService;
    private readonly ITadilatCellNoteDialogService _noteDialogService;
    private readonly IUndoRedoService _undoRedoService;
    private readonly AppSettings _settings;
    private readonly IClipboardService _clipboardService;
    private readonly Dictionary<string, MissingProjectCellState> _cellStateLookup = new(StringComparer.OrdinalIgnoreCase);
    private bool _isInitialized;
    private bool _isPersisting;
    private bool _hasUnsavedChanges;
    private MissingProjectEntry? _selectedEntry;
    private MissingProjectRowViewModel? _selectedRow;
    private sealed record MissingProjectUndoSnapshot(
        IReadOnlyList<MissingProjectEntry> Entries,
        IReadOnlyList<MissingProjectCellState> CellStates,
        Guid? SelectedEntryId,
        bool HasUnsavedChanges);

    public MissingProjectModuleViewModel(
        IMissingProjectRepository repository,
        INotificationService notificationService,
        IConfirmationService confirmationService,
        ITadilatCellNoteDialogService noteDialogService,
        IUndoRedoService undoRedoService,
        AppSettings settings,
        IClipboardService? clipboardService = null)
    {
        _repository = repository;
        _notificationService = notificationService;
        _confirmationService = confirmationService;
        _noteDialogService = noteDialogService;
        _undoRedoService = undoRedoService;
        _settings = settings;
        _clipboardService = clipboardService ?? new ClipboardService();

        Entries = [];
        Entries.CollectionChanged += OnEntriesCollectionChanged;
        CellStates = [];
        Rows = [];

        MediumOptionItems = Enum
            .GetValues<MissingProjectMedium>()
            .Select(medium => new MissingProjectMediumOption(medium, MissingProjectMediumLabelProvider.GetLabel(medium)))
            .ToList();

        AddEntryCommand = new AsyncRelayCommand(AddEntryAsync);
        DeleteEntryCommand = new AsyncRelayCommand<MissingProjectEntry?>(DeleteSelectedAsync, CanDeleteEntry);
        SaveEntryCommand = new AsyncRelayCommand<MissingProjectEntry?>(SaveEntryAsync, CanSaveEntry);
        BeginCellEditCommand = new RelayCommand<MissingProjectCellViewModel?>(BeginCellEdit);
        CommitCellEditCommand = new RelayCommand<MissingProjectCellViewModel?>(CommitCellEdit);
        CancelCellEditCommand = new RelayCommand<MissingProjectCellViewModel?>(CancelCellEdit);
        EditCellNoteCommand = new AsyncRelayCommand<MissingProjectCellViewModel?>(EditCellNoteAsync);
        SetCellColorRedCommand = new RelayCommand<MissingProjectCellViewModel?>(cell => SetCellColor(cell, StrongRedColor));
        SetCellColorYellowCommand = new RelayCommand<MissingProjectCellViewModel?>(cell => SetCellColor(cell, StrongYellowColor));
        SetCellColorGreenCommand = new RelayCommand<MissingProjectCellViewModel?>(cell => SetCellColor(cell, StrongGreenColor));
        SetCellColorBlueCommand = new RelayCommand<MissingProjectCellViewModel?>(cell => SetCellColor(cell, StrongBlueColor));
        SetCellColorGrayCommand = new RelayCommand<MissingProjectCellViewModel?>(cell => SetCellColor(cell, StrongGrayColor));
        ClearCellColorCommand = new RelayCommand<MissingProjectCellViewModel?>(cell => SetCellColor(cell, string.Empty));
        CopyCellCommand = new RelayCommand<MissingProjectCellViewModel?>(CopyCell);
        PasteCellCommand = new RelayCommand<MissingProjectCellViewModel?>(PasteCell, cell => cell?.IsInteractive == true);
    }

    public ObservableRangeCollection<MissingProjectEntry> Entries { get; }
    public ObservableRangeCollection<MissingProjectCellState> CellStates { get; }
    public ObservableRangeCollection<MissingProjectRowViewModel> Rows { get; }
    public IReadOnlyList<MissingProjectMediumOption> MediumOptionItems { get; }

    public bool HasUnsavedChanges
    {
        get => _hasUnsavedChanges;
        private set
        {
            if (SetProperty(ref _hasUnsavedChanges, value))
            {
                SaveEntryCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public MissingProjectEntry? SelectedEntry
    {
        get => _selectedEntry;
        set
        {
            if (SetProperty(ref _selectedEntry, value))
            {
                if (_selectedRow?.Entry != value)
                {
                    _selectedRow = value is null ? null : Rows.FirstOrDefault(row => row.Entry.Id == value.Id);
                }

                RefreshRowSelection();
                DeleteEntryCommand.NotifyCanExecuteChanged();
                SaveEntryCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public MissingProjectRowViewModel? SelectedRow
    {
        get => _selectedRow;
        set
        {
            if (SetProperty(ref _selectedRow, value))
            {
                if (_selectedEntry != value?.Entry)
                {
                    _selectedEntry = value?.Entry;
                    OnPropertyChanged(nameof(SelectedEntry));
                }

                RefreshRowSelection();
                DeleteEntryCommand.NotifyCanExecuteChanged();
                SaveEntryCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public AsyncRelayCommand AddEntryCommand { get; }
    public AsyncRelayCommand<MissingProjectEntry?> DeleteEntryCommand { get; }
    public AsyncRelayCommand<MissingProjectEntry?> SaveEntryCommand { get; }
    public RelayCommand<MissingProjectCellViewModel?> BeginCellEditCommand { get; }
    public RelayCommand<MissingProjectCellViewModel?> CommitCellEditCommand { get; }
    public RelayCommand<MissingProjectCellViewModel?> CancelCellEditCommand { get; }
    public AsyncRelayCommand<MissingProjectCellViewModel?> EditCellNoteCommand { get; }
    public RelayCommand<MissingProjectCellViewModel?> SetCellColorRedCommand { get; }
    public RelayCommand<MissingProjectCellViewModel?> SetCellColorYellowCommand { get; }
    public RelayCommand<MissingProjectCellViewModel?> SetCellColorGreenCommand { get; }
    public RelayCommand<MissingProjectCellViewModel?> SetCellColorBlueCommand { get; }
    public RelayCommand<MissingProjectCellViewModel?> SetCellColorGrayCommand { get; }
    public RelayCommand<MissingProjectCellViewModel?> ClearCellColorCommand { get; }
    public RelayCommand<MissingProjectCellViewModel?> CopyCellCommand { get; }
    public RelayCommand<MissingProjectCellViewModel?> PasteCellCommand { get; }

    public async Task InitializeAsync()
    {
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;

        try
        {
            var entries = (await _repository.GetAllAsync()).ToList();
            var cellStates = (await _repository.GetCellStatesAsync()).ToList();
            if (_settings.SeedSampleDataOnEmpty && entries.Count == 0)
            {
                entries = BuildSeedEntries();
                await _repository.SaveManyAsync(entries, Array.Empty<MissingProjectCellState>());
            }

            ReplaceEntries(entries);
            ReplaceCellStates(cellStates);
            RefreshRows();
            HasUnsavedChanges = false;
        }
        catch (Exception ex)
        {
            _isInitialized = false;
            _notificationService.ShowToast($"Eksik proje yükleme hatası: {ex.Message}", ToastType.Error, TimeSpan.FromSeconds(5));
        }
    }

    public IReadOnlyList<MissingProjectEntry> GetEntriesSnapshot()
        => Entries
            .OrderBy(item => item.DisplayOrder)
            .Select(CloneEntry)
            .ToList();

    public IReadOnlyList<MissingProjectCellState> GetCellStatesSnapshot()
        => CellStates.Select(CloneCellState).ToList();

    public void LoadFromBackup(IEnumerable<MissingProjectEntry> entries, IEnumerable<MissingProjectCellState>? cellStates = null, bool markDirty = true)
    {
        var source = entries ?? Array.Empty<MissingProjectEntry>();
        ReplaceEntries(source.Select(CloneEntry).OrderBy(item => item.DisplayOrder).ThenBy(item => item.UpdatedAt));
        ReplaceCellStates((cellStates ?? Array.Empty<MissingProjectCellState>()).Select(CloneCellState));
        RefreshRows();
        HasUnsavedChanges = markDirty;
    }

    public async Task PersistAsync(bool showErrorToast = true)
    {
        try
        {
            _isPersisting = true;
            await _repository.SaveManyAsync(
                Entries.OrderBy(item => item.DisplayOrder).Select(CloneEntry),
                GetCellStatesSnapshot());
            HasUnsavedChanges = false;
        }
        catch (Exception ex)
        {
            HasUnsavedChanges = true;
            if (showErrorToast)
            {
                _notificationService.ShowToast($"Eksik proje kayıt hatası: {ex.Message}", ToastType.Error);
            }
        }
        finally
        {
            _isPersisting = false;
        }
    }

    public void CommitPendingEdits()
    {
        foreach (var row in Rows.ToList())
        {
            CommitPendingEdit(row.AdaParselCell);
            CommitPendingEdit(row.YapiSahibiCell);
            CommitPendingEdit(row.RecordMediumCell);
            CommitPendingEdit(row.MissingProjectCell);
            CommitPendingEdit(row.DescriptionCell);
        }
    }

    private async Task AddEntryAsync()
    {
        ExecuteUndoableMutation("Eksik proje kayıt ekle", () =>
        {
            var entry = new MissingProjectEntry
            {
                AdaParsel = string.Empty,
                YapiSahibi = string.Empty,
                RecordMedium = MissingProjectMedium.Fiziki,
                RecordMediumText = MissingProjectMediumLabelProvider.GetLabel(MissingProjectMedium.Fiziki),
                MissingProjectText = string.Empty,
                Description = string.Empty,
                DisplayOrder = Entries.Count,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            Entries.Add(entry);
            RefreshRows();
            SelectedEntry = entry;
            HasUnsavedChanges = true;
        });
        _notificationService.ShowToast("Eksik proje kaydı eklendi.", ToastType.Success);
        await Task.CompletedTask;
    }

    private async Task DeleteSelectedAsync(MissingProjectEntry? entry)
    {
        var selected = entry ?? SelectedEntry;
        if (selected is null)
        {
            return;
        }

        if (!_confirmationService.Confirm(new ConfirmationRequest
            {
                Kind = ConfirmationKind.Delete,
                Title = "Eksik Proje Kaydını Sil",
                Message = $"\"{selected.AdaParsel}\" kaydı silinecek.\n\nDevam edilsin mi?",
                IsDestructive = true
            }))
        {
            return;
        }

        ExecuteUndoableMutation("Eksik proje kayıt sil", () =>
        {
            RemoveCellStates(selected.Id);
            Entries.Remove(selected);
            NormalizeDisplayOrder();
            RefreshRows();
            HasUnsavedChanges = true;
            SelectedEntry = Entries.FirstOrDefault();
        });
        _notificationService.ShowToast("Eksik proje kaydı silindi.", ToastType.Warning);
        await Task.CompletedTask;
    }

    private async Task SaveEntryAsync(MissingProjectEntry? entry)
    {
        entry ??= SelectedEntry;
        if (entry is null && !HasUnsavedChanges)
        {
            return;
        }

        try
        {
            await PersistAsync(showErrorToast: true);
            if (!HasUnsavedChanges)
            {
                _notificationService.ShowToast("Eksik proje kaydı güncellendi.", ToastType.Info, TimeSpan.FromSeconds(2));
            }
        }
        catch
        {
            // PersistAsync already handles toast + dirty state.
        }
    }

    private void BeginCellEdit(MissingProjectCellViewModel? cell)
    {
        if (cell is null || !cell.IsInteractive)
        {
            return;
        }

        foreach (var row in Rows)
        {
            row.CloseEditors();
        }

        SelectedEntry = cell.Row.Entry;
        cell.DraftText = cell.Text;
        cell.IsEditing = true;
    }

    private void CommitPendingEdit(MissingProjectCellViewModel cell)
    {
        if (cell.IsEditing)
        {
            CommitCellEdit(cell);
        }
    }

    private void CommitCellEdit(MissingProjectCellViewModel? cell)
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

        ExecuteUndoableMutation("Eksik proje hücre düzenle", () =>
        {
            cell.Row.SetCellValue(cell.ColumnKey, newValue);
            cell.Text = newValue;
            cell.IsEditing = false;
            cell.Row.Entry.UpdatedAt = DateTime.Now;
            HasUnsavedChanges = true;
        });
    }

    private void CancelCellEdit(MissingProjectCellViewModel? cell)
    {
        if (cell is null)
        {
            return;
        }

        cell.DraftText = cell.Text;
        cell.IsEditing = false;
    }

    private async Task EditCellNoteAsync(MissingProjectCellViewModel? cell)
    {
        if (cell is null || !cell.IsInteractive || cell.Row.Entry is null)
        {
            return;
        }

        SelectedEntry = cell.Row.Entry;
        var result = await _noteDialogService.ShowDialogAsync(cell.NoteText);
        if (result is null)
        {
            return;
        }

        ExecuteUndoableMutation("Eksik proje hücre notu", () =>
        {
            var state = GetOrCreateCellState(cell.Row.Entry.Id, cell.ColumnKey);
            state.NoteText = result.DeleteRequested ? string.Empty : result.NoteText.Trim();
            CleanupCellStateIfEmpty(state);
            cell.NoteText = state.NoteText;
            HasUnsavedChanges = true;
        });
    }

    private void SetCellColor(MissingProjectCellViewModel? cell, string color)
    {
        if (cell is null || !cell.IsInteractive || cell.Row.Entry is null)
        {
            return;
        }

        SelectedEntry = cell.Row.Entry;
        ExecuteUndoableMutation("Eksik proje hücre rengi", () =>
        {
            var state = GetOrCreateCellState(cell.Row.Entry.Id, cell.ColumnKey);
            state.BackgroundColor = NormalizeCellColor(color);
            CleanupCellStateIfEmpty(state);
            cell.BackgroundColor = state.BackgroundColor;
            HasUnsavedChanges = true;
        });
    }

    private void CopyCell(MissingProjectCellViewModel? cell)
    {
        if (cell is null || cell.Row.Entry is null)
        {
            return;
        }

        SelectedEntry = cell.Row.Entry;
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

    private void PasteCell(MissingProjectCellViewModel? cell)
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

        foreach (var row in Rows)
        {
            row.CloseEditors();
        }

        SelectedEntry = cell.Row.Entry;
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

        ExecuteUndoableMutation("Eksik proje hücre yapıştır", () =>
        {
            cell.Row.SetCellValue(cell.ColumnKey, normalizedText);
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

    private MissingProjectUndoSnapshot CaptureUndoSnapshot()
        => new(
            GetEntriesSnapshot(),
            GetCellStatesSnapshot(),
            SelectedEntry?.Id,
            HasUnsavedChanges);

    private void ApplyUndoSnapshot(MissingProjectUndoSnapshot snapshot)
    {
        LoadFromBackup(snapshot.Entries, snapshot.CellStates);
        HasUnsavedChanges = snapshot.HasUnsavedChanges;
        SelectedEntry = Entries.FirstOrDefault(item => item.Id == snapshot.SelectedEntryId) ?? Entries.FirstOrDefault();
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

    private void OnEntriesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (MissingProjectEntry item in e.OldItems)
            {
                item.PropertyChanged -= OnEntryPropertyChanged;
            }
        }

        if (e.NewItems is not null)
        {
            foreach (MissingProjectEntry item in e.NewItems)
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
    }

    private void OnEntryPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!_isInitialized || _isPersisting || sender is not MissingProjectEntry)
        {
            return;
        }

        if (!PersistedProperties.Contains(e.PropertyName ?? string.Empty))
        {
            return;
        }

        HasUnsavedChanges = true;
    }

    private void ReplaceEntries(IEnumerable<MissingProjectEntry> source)
    {
        Entries.ReplaceRange(source.OrderBy(item => item.DisplayOrder));
        SelectedEntry = Entries.FirstOrDefault();
    }

    private void ReplaceCellStates(IEnumerable<MissingProjectCellState> states)
    {
        _cellStateLookup.Clear();
        var clonedList = new List<MissingProjectCellState>();

        foreach (var state in states)
        {
            var cloned = CloneCellState(state);
            clonedList.Add(cloned);
            _cellStateLookup[BuildCellStateKey(cloned.EntryId, cloned.ColumnKey)] = cloned;
        }
        CellStates.ReplaceRange(clonedList);
    }

    private void RefreshRows()
    {
        var selectedId = SelectedEntry?.Id;

        var newRows = new List<MissingProjectRowViewModel>();
        foreach (var entry in Entries.OrderBy(item => item.DisplayOrder))
        {
            var row = new MissingProjectRowViewModel(
                entry,
                BuildCell(entry, MissingProjectColumnKeys.AdaParsel, entry.AdaParsel),
                BuildCell(entry, MissingProjectColumnKeys.YapiSahibi, entry.YapiSahibi),
                BuildCell(entry, MissingProjectColumnKeys.RecordMediumText, entry.RecordMediumText),
                BuildCell(entry, MissingProjectColumnKeys.MissingProjectText, entry.MissingProjectText),
                BuildCell(entry, MissingProjectColumnKeys.Description, entry.Description));
            row.IsSelected = selectedId == entry.Id;
            newRows.Add(row);
        }
        Rows.ReplaceRange(newRows);

        _selectedRow = selectedId is null ? null : Rows.FirstOrDefault(row => row.Entry.Id == selectedId.Value);
        OnPropertyChanged(nameof(SelectedRow));
    }

    private MissingProjectCellViewModel BuildCell(MissingProjectEntry entry, string columnKey, string text)
    {
        var state = GetCellState(entry.Id, columnKey);
        return new MissingProjectCellViewModel(
            columnKey,
            text,
            NormalizeCellColor(state?.BackgroundColor),
            state?.NoteText ?? string.Empty);
    }

    private MissingProjectCellState? GetCellState(Guid entryId, string columnKey)
        => _cellStateLookup.TryGetValue(BuildCellStateKey(entryId, columnKey), out var state) ? state : null;

    private MissingProjectCellState GetOrCreateCellState(Guid entryId, string columnKey)
    {
        var key = BuildCellStateKey(entryId, columnKey);
        if (_cellStateLookup.TryGetValue(key, out var existing))
        {
            return existing;
        }

        var created = new MissingProjectCellState
        {
            EntryId = entryId,
            ColumnKey = columnKey
        };
        CellStates.Add(created);
        _cellStateLookup[key] = created;
        return created;
    }

    private void CleanupCellStateIfEmpty(MissingProjectCellState state)
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
        => $"{entryId:N}:{columnKey}";

    private void NormalizeDisplayOrder()
    {
        for (var index = 0; index < Entries.Count; index++)
        {
            Entries[index].DisplayOrder = index;
            Entries[index].UpdatedAt = DateTime.Now;
        }
    }

    private void RefreshRowSelection()
    {
        var selectedId = SelectedEntry?.Id;
        foreach (var row in Rows)
        {
            row.IsSelected = row.Entry.Id == selectedId;
        }
    }

    private bool CanDeleteEntry(MissingProjectEntry? entry)
        => entry is not null || SelectedEntry is not null;

    private bool CanSaveEntry(MissingProjectEntry? entry)
        => entry is not null || SelectedEntry is not null || HasUnsavedChanges;

    private static MissingProjectEntry CloneEntry(MissingProjectEntry entry)
    {
        return new MissingProjectEntry
        {
            Id = entry.Id,
            AdaParsel = entry.AdaParsel,
            YapiSahibi = entry.YapiSahibi,
            RecordMedium = entry.RecordMedium,
            RecordMediumText = entry.RecordMediumText,
            MissingProjectText = entry.MissingProjectText,
            Description = entry.Description,
            DisplayOrder = entry.DisplayOrder,
            CreatedAt = entry.CreatedAt,
            UpdatedAt = entry.UpdatedAt
        };
    }

    private static MissingProjectCellState CloneCellState(MissingProjectCellState state)
    {
        return new MissingProjectCellState
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

    private static MissingProjectMedium ParseMediumLabel(string text, MissingProjectMedium fallback)
    {
        if (SearchTextNormalizer.EqualsNormalized(text, MissingProjectMediumLabelProvider.GetLabel(MissingProjectMedium.Dijital)))
        {
            return MissingProjectMedium.Dijital;
        }

        if (SearchTextNormalizer.EqualsNormalized(text, MissingProjectMediumLabelProvider.GetLabel(MissingProjectMedium.Fiziki)))
        {
            return MissingProjectMedium.Fiziki;
        }

        if (SearchTextNormalizer.EqualsNormalized(text, MissingProjectMediumLabelProvider.GetLabel(MissingProjectMedium.FizikiVeDijital)))
        {
            return MissingProjectMedium.FizikiVeDijital;
        }

        return fallback;
    }

    private static List<MissingProjectEntry> BuildSeedEntries()
    {
        return
        [
            new MissingProjectEntry
            {
                AdaParsel = "104-7",
                YapiSahibi = "KADİR ÇOK GRUP MADENCİLİK",
                RecordMedium = MissingProjectMedium.Fiziki,
                RecordMediumText = "Fiziksel",
                MissingProjectText = "ELEKTRİK PROJELERİ",
                Description = "BOYABAT OSBYE GÖNDERİLDİ.",
                DisplayOrder = 0,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            },
            new MissingProjectEntry
            {
                AdaParsel = "815-5",
                YapiSahibi = "YASİN ERGÜN",
                RecordMedium = MissingProjectMedium.Fiziki,
                RecordMediumText = "Fiziksel",
                MissingProjectText = "ZEMİN ETÜDÜ",
                Description = "GÖKHAN BAL",
                DisplayOrder = 1,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            },
            new MissingProjectEntry
            {
                AdaParsel = "787-5",
                YapiSahibi = "GERZE GÜVEN KEKLİK",
                RecordMedium = MissingProjectMedium.Dijital,
                RecordMediumText = "Dijital",
                MissingProjectText = "MİMARİ SON REVİZE",
                Description = "SERKAN KIVRAK (KURANGLEZ VE ZEMİN TERASI İPTALİ)",
                DisplayOrder = 2,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            },
            new MissingProjectEntry
            {
                AdaParsel = "397-18",
                YapiSahibi = "GERZE GÜVEN KEKLİK",
                RecordMedium = MissingProjectMedium.Dijital,
                RecordMediumText = "Dijital",
                MissingProjectText = "STATİK DÜZELTME",
                Description = "ERSAN ALTUN",
                DisplayOrder = 3,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            },
            new MissingProjectEntry
            {
                AdaParsel = "439-4",
                YapiSahibi = "FAHRETTİN GENÇGÜN",
                RecordMedium = MissingProjectMedium.FizikiVeDijital,
                RecordMediumText = "Fiziksel + Dijital",
                MissingProjectText = "ELEKTRİK RUHSAT PROJELERİ",
                Description = "AHMET SEFA AÇIKGÖZ-PELİN ARSLAN",
                DisplayOrder = 4,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            }
        ];
    }

    public sealed class MissingProjectRowViewModel : ViewModelBase
    {
        private bool _isSelected;

        public MissingProjectRowViewModel(
            MissingProjectEntry entry,
            MissingProjectCellViewModel adaParselCell,
            MissingProjectCellViewModel yapiSahibiCell,
            MissingProjectCellViewModel recordMediumCell,
            MissingProjectCellViewModel missingProjectCell,
            MissingProjectCellViewModel descriptionCell)
        {
            Entry = entry;
            AdaParselCell = Attach(adaParselCell);
            YapiSahibiCell = Attach(yapiSahibiCell);
            RecordMediumCell = Attach(recordMediumCell);
            MissingProjectCell = Attach(missingProjectCell);
            DescriptionCell = Attach(descriptionCell);
        }

        public MissingProjectEntry Entry { get; }

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public MissingProjectCellViewModel AdaParselCell { get; }
        public MissingProjectCellViewModel YapiSahibiCell { get; }
        public MissingProjectCellViewModel RecordMediumCell { get; }
        public MissingProjectCellViewModel MissingProjectCell { get; }
        public MissingProjectCellViewModel DescriptionCell { get; }

        public void SetCellValue(string columnKey, string value)
        {
            switch (columnKey)
            {
                case MissingProjectColumnKeys.AdaParsel:
                    Entry.AdaParsel = value;
                    break;
                case MissingProjectColumnKeys.YapiSahibi:
                    Entry.YapiSahibi = value;
                    break;
                case MissingProjectColumnKeys.RecordMediumText:
                    Entry.RecordMediumText = value;
                    Entry.RecordMedium = ParseMediumLabel(value, Entry.RecordMedium);
                    break;
                case MissingProjectColumnKeys.MissingProjectText:
                    Entry.MissingProjectText = value;
                    break;
                case MissingProjectColumnKeys.Description:
                    Entry.Description = value;
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

        private IEnumerable<MissingProjectCellViewModel> GetCells()
        {
            yield return AdaParselCell;
            yield return YapiSahibiCell;
            yield return RecordMediumCell;
            yield return MissingProjectCell;
            yield return DescriptionCell;
        }

        private MissingProjectCellViewModel Attach(MissingProjectCellViewModel cell)
        {
            cell.Row = this;
            return cell;
        }
    }

    public sealed class MissingProjectCellViewModel : ViewModelBase
    {
        private static readonly BrushConverter BrushConverter = new();

        private string _text;
        private string _draftText;
        private string _backgroundColor;
        private string _noteText;
        private bool _isEditing;

        public MissingProjectCellViewModel(string columnKey, string text, string backgroundColor, string noteText, bool isInteractive = true)
        {
            ColumnKey = columnKey;
            _text = text;
            _draftText = text;
            _backgroundColor = backgroundColor;
            _noteText = noteText;
            IsInteractive = isInteractive;
        }

        public MissingProjectRowViewModel Row { get; set; } = null!;
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
                    return Brushes.Transparent;
                }

                return BrushConverter.ConvertFromString(BackgroundColor) as Brush ?? Brushes.Transparent;
            }
        }
    }
}
