using CommunityToolkit.Mvvm.Input;
using RizaCanKilicIsTakibi.Helpers;
using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services.Abstractions;
using System.Collections.ObjectModel;

namespace RizaCanKilicIsTakibi.ViewModels;

public sealed class PersonnelGorevRowViewModel : ViewModelBase
{
    public PersonnelGorevRowViewModel(PersonnelAssignment assignment, string personnelName)
    {
        Assignment = assignment;
        PersonnelName = string.IsNullOrWhiteSpace(personnelName) ? "Atanmamış" : personnelName;
        ModuleLabel = string.IsNullOrWhiteSpace(assignment.ModuleLabelSnapshot)
            ? IPersonnelAssignmentService.ModuleLabel(assignment.SourceModule)
            : assignment.ModuleLabelSnapshot;
        Summary = assignment.SummarySnapshot;
        FieldLabel = assignment.FieldLabelSnapshot;
        ProjectIdentity = assignment.ProjectIdentitySnapshot;
        PriorityLabel = IPersonnelAssignmentService.PriorityLabel(assignment.PrioritySnapshot);
        StatusLabel = assignment.Status == PersonnelAssignmentStatus.Completed ? "Tamamlandı" : "Açık";
        AssignedAtText = assignment.AssignedAt.ToString("g");
        IsOpen = assignment.Status == PersonnelAssignmentStatus.Open;
    }

    public PersonnelAssignment Assignment { get; }
    public string PersonnelName { get; }
    public string ModuleLabel { get; }
    public string Summary { get; }
    public string FieldLabel { get; }
    public string ProjectIdentity { get; }
    public string PriorityLabel { get; }
    public string StatusLabel { get; }
    public string AssignedAtText { get; }
    public bool IsOpen { get; }
}

public sealed class PersonnelFilterChipViewModel : ViewModelBase
{
    public PersonnelFilterChipViewModel(string key, string label)
    {
        Key = key;
        Label = label;
    }

    public string Key { get; }
    public string Label { get; }
    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}

public sealed class PersonnelGorevViewModel : ViewModelBase
{
    public const string FilterAll = "all";
    public const string FilterUnassigned = "unassigned";
    public const string FilterOpenOnly = "open";

    private readonly IPersonnelAssignmentService _service;
    private readonly IPersonnelSettingsDialogService? _settingsDialog;
    private readonly IPersonnelAssignmentEditDialogService? _editDialog;
    private string _selectedFilterKey = FilterAll;
    private bool _showCompleted;
    private PersonnelGorevRowViewModel? _selectedRow;

    public PersonnelGorevViewModel(
        IPersonnelAssignmentService service,
        IPersonnelSettingsDialogService? settingsDialog = null,
        IPersonnelAssignmentEditDialogService? editDialog = null)
    {
        _service = service;
        _settingsDialog = settingsDialog;
        _editDialog = editDialog;
        Rows = new ObservableCollection<PersonnelGorevRowViewModel>();
        FilterChips = new ObservableCollection<PersonnelFilterChipViewModel>();
        OpenPersonnelSettingsCommand = new AsyncRelayCommand(OpenSettingsAsync);
        ToggleStatusCommand = new AsyncRelayCommand(ToggleStatusAsync, () => SelectedRow is not null);
        EditAssignmentCommand = new AsyncRelayCommand<PersonnelGorevRowViewModel?>(EditAssignmentAsync, row => row is not null || SelectedRow is not null);
        SelectFilterCommand = new RelayCommand<string>(SelectFilter);
        RefreshCommand = new RelayCommand(Refresh);
        _service.Changed += (_, _) => Refresh();
        Refresh();
    }

    public ObservableCollection<PersonnelGorevRowViewModel> Rows { get; }
    public ObservableCollection<PersonnelFilterChipViewModel> FilterChips { get; }
    public IRelayCommand OpenPersonnelSettingsCommand { get; }
    public IAsyncRelayCommand ToggleStatusCommand { get; }
    public IAsyncRelayCommand EditAssignmentCommand { get; }
    public IRelayCommand SelectFilterCommand { get; }
    public IRelayCommand RefreshCommand { get; }

    public string SelectedFilterKey
    {
        get => _selectedFilterKey;
        set
        {
            if (SetProperty(ref _selectedFilterKey, value))
            {
                Refresh();
            }
        }
    }

    public bool ShowCompleted
    {
        get => _showCompleted;
        set
        {
            if (SetProperty(ref _showCompleted, value))
            {
                Refresh();
            }
        }
    }

    public PersonnelGorevRowViewModel? SelectedRow
    {
        get => _selectedRow;
        set
        {
            if (SetProperty(ref _selectedRow, value))
            {
                ToggleStatusCommand.NotifyCanExecuteChanged();
                EditAssignmentCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public void Refresh()
    {
        var people = _service.GetPersonnel();
        var assignments = _service.GetAssignments();

        FilterChips.Clear();
        FilterChips.Add(new PersonnelFilterChipViewModel(FilterAll, "Tümü") { IsSelected = _selectedFilterKey == FilterAll });
        FilterChips.Add(new PersonnelFilterChipViewModel(FilterUnassigned, "Atanmamış") { IsSelected = _selectedFilterKey == FilterUnassigned });
        foreach (var person in people)
        {
            FilterChips.Add(new PersonnelFilterChipViewModel(person.Id.ToString("N"), person.Name)
            {
                IsSelected = string.Equals(_selectedFilterKey, person.Id.ToString("N"), StringComparison.OrdinalIgnoreCase)
            });
        }

        IEnumerable<PersonnelAssignment> filtered = assignments;
        if (!_showCompleted)
        {
            filtered = filtered.Where(a => a.Status == PersonnelAssignmentStatus.Open);
        }

        if (_selectedFilterKey == FilterUnassigned)
        {
            filtered = filtered.Where(a => a.PersonnelId is null);
        }
        else if (_selectedFilterKey != FilterAll && Guid.TryParse(_selectedFilterKey, out var personId))
        {
            filtered = filtered.Where(a => a.PersonnelId == personId);
        }

        var rows = filtered
            .OrderBy(a => a.Status)
            .ThenByDescending(a => a.AssignedAt)
            .Select(a => new PersonnelGorevRowViewModel(a, _service.GetPersonnelName(a.PersonnelId) ?? string.Empty))
            .ToList();

        Rows.Clear();
        foreach (var row in rows)
        {
            Rows.Add(row);
        }
    }

    private void SelectFilter(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        SelectedFilterKey = key;
    }

    private async Task OpenSettingsAsync()
    {
        if (_settingsDialog is null)
        {
            return;
        }

        await _settingsDialog.ShowDialogAsync();
        Refresh();
    }

    private async Task ToggleStatusAsync()
    {
        if (SelectedRow is null)
        {
            return;
        }

        var next = SelectedRow.Assignment.Status == PersonnelAssignmentStatus.Open
            ? PersonnelAssignmentStatus.Completed
            : PersonnelAssignmentStatus.Open;
        await _service.SetStatusAsync(SelectedRow.Assignment.Id, next);
        Refresh();
    }

    private async Task EditAssignmentAsync(PersonnelGorevRowViewModel? row)
    {
        var target = row ?? SelectedRow;
        if (target is null || _editDialog is null)
        {
            return;
        }

        SelectedRow = target;
        var saved = await _editDialog.ShowDialogAsync(target.Assignment);
        if (saved)
        {
            Refresh();
        }
    }
}
