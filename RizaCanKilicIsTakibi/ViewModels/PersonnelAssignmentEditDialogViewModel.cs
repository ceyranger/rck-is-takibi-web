using CommunityToolkit.Mvvm.Input;
using RizaCanKilicIsTakibi.Helpers;
using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services.Abstractions;
using System.Collections.ObjectModel;

namespace RizaCanKilicIsTakibi.ViewModels;

public sealed class PersonnelPriorityOption
{
    public PersonnelPriorityOption(PersonnelAssignmentPriority value, string label)
    {
        Value = value;
        Label = label;
    }

    public PersonnelAssignmentPriority Value { get; }
    public string Label { get; }
}

public sealed class PersonnelStatusOption
{
    public PersonnelStatusOption(PersonnelAssignmentStatus value, string label)
    {
        Value = value;
        Label = label;
    }

    public PersonnelAssignmentStatus Value { get; }
    public string Label { get; }
}

public sealed class PersonnelPickOption
{
    public PersonnelPickOption(Guid? id, string displayName)
    {
        Id = id;
        DisplayName = displayName;
    }

    public Guid? Id { get; }
    public string DisplayName { get; }
}

public sealed class PersonnelAssignmentEditDialogViewModel : ViewModelBase
{
    private readonly PersonnelAssignment _original;
    private PersonnelPickOption _selectedPersonnel;
    private PersonnelStatusOption _selectedStatus;
    private PersonnelPriorityOption _selectedPriority;
    private string _summary;
    private string _fieldLabel;
    private string _projectIdentity;

    public PersonnelAssignmentEditDialogViewModel(
        PersonnelAssignment assignment,
        IEnumerable<Personnel> personnel)
    {
        _original = assignment.Clone();
        PersonnelOptions = new ObservableCollection<PersonnelPickOption>(
            new[] { new PersonnelPickOption(null, "Atanmamış") }
                .Concat(personnel.Select(p => new PersonnelPickOption(p.Id, p.Name))));
        StatusOptions =
        [
            new PersonnelStatusOption(PersonnelAssignmentStatus.Open, "Açık"),
            new PersonnelStatusOption(PersonnelAssignmentStatus.Completed, "Tamamlandı")
        ];
        PriorityOptions =
        [
            new PersonnelPriorityOption(PersonnelAssignmentPriority.None, "Yok"),
            new PersonnelPriorityOption(PersonnelAssignmentPriority.Warning, "Uyarı"),
            new PersonnelPriorityOption(PersonnelAssignmentPriority.Critical, "Kritik"),
            new PersonnelPriorityOption(PersonnelAssignmentPriority.Urgent, "Acil")
        ];

        ModuleLabel = string.IsNullOrWhiteSpace(assignment.ModuleLabelSnapshot)
            ? IPersonnelAssignmentService.ModuleLabel(assignment.SourceModule)
            : assignment.ModuleLabelSnapshot;
        AssignedAtText = assignment.AssignedAt.ToString("g");
        _summary = assignment.SummarySnapshot ?? string.Empty;
        _fieldLabel = assignment.FieldLabelSnapshot ?? string.Empty;
        _projectIdentity = assignment.ProjectIdentitySnapshot ?? string.Empty;
        _selectedPersonnel = PersonnelOptions.FirstOrDefault(p => p.Id == assignment.PersonnelId)
            ?? PersonnelOptions[0];
        _selectedStatus = StatusOptions.First(s => s.Value == assignment.Status);
        _selectedPriority = PriorityOptions.First(p => p.Value == assignment.PrioritySnapshot);

        SaveCommand = new RelayCommand(() => RequestClose?.Invoke(this, true));
        CancelCommand = new RelayCommand(() => RequestClose?.Invoke(this, false));
    }

    public event EventHandler<bool>? RequestClose;

    public ObservableCollection<PersonnelPickOption> PersonnelOptions { get; }
    public IReadOnlyList<PersonnelStatusOption> StatusOptions { get; }
    public IReadOnlyList<PersonnelPriorityOption> PriorityOptions { get; }

    public string ModuleLabel { get; }
    public string AssignedAtText { get; }

    public PersonnelPickOption SelectedPersonnel
    {
        get => _selectedPersonnel;
        set => SetProperty(ref _selectedPersonnel, value);
    }

    public PersonnelStatusOption SelectedStatus
    {
        get => _selectedStatus;
        set => SetProperty(ref _selectedStatus, value);
    }

    public PersonnelPriorityOption SelectedPriority
    {
        get => _selectedPriority;
        set => SetProperty(ref _selectedPriority, value);
    }

    public string Summary
    {
        get => _summary;
        set => SetProperty(ref _summary, value);
    }

    public string FieldLabel
    {
        get => _fieldLabel;
        set => SetProperty(ref _fieldLabel, value);
    }

    public string ProjectIdentity
    {
        get => _projectIdentity;
        set => SetProperty(ref _projectIdentity, value);
    }

    public RelayCommand SaveCommand { get; }
    public RelayCommand CancelCommand { get; }

    public PersonnelAssignment BuildUpdatedAssignment()
    {
        var next = _original.Clone();
        next.PersonnelId = SelectedPersonnel.Id;
        next.Status = SelectedStatus.Value;
        next.CompletedAt = SelectedStatus.Value == PersonnelAssignmentStatus.Completed
            ? (_original.CompletedAt ?? DateTime.Now)
            : null;
        next.PrioritySnapshot = SelectedPriority.Value;
        next.SummarySnapshot = Summary?.Trim() ?? string.Empty;
        next.FieldLabelSnapshot = FieldLabel?.Trim() ?? string.Empty;
        next.ProjectIdentitySnapshot = ProjectIdentity?.Trim() ?? string.Empty;
        next.ModuleLabelSnapshot = ModuleLabel;
        return next;
    }
}
