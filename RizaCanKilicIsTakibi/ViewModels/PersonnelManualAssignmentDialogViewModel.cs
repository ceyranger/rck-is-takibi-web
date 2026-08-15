using CommunityToolkit.Mvvm.Input;
using RizaCanKilicIsTakibi.Helpers;
using RizaCanKilicIsTakibi.Models;
using System.Collections.ObjectModel;

namespace RizaCanKilicIsTakibi.ViewModels;

public sealed class PersonnelManualAssignmentDialogViewModel : ViewModelBase
{
    private PersonnelPickOption? _selectedPersonnel;
    private PersonnelPriorityOption _selectedPriority;
    private string _summary = string.Empty;
    private string _fieldLabel = string.Empty;
    private string _projectIdentity = string.Empty;

    public PersonnelManualAssignmentDialogViewModel(IEnumerable<Personnel> personnel)
    {
        PersonnelOptions = new ObservableCollection<PersonnelPickOption>(
            personnel.Select(p => new PersonnelPickOption(p.Id, p.Name)));
        PriorityOptions =
        [
            new PersonnelPriorityOption(PersonnelAssignmentPriority.None, "Yok"),
            new PersonnelPriorityOption(PersonnelAssignmentPriority.Warning, "Uyarı"),
            new PersonnelPriorityOption(PersonnelAssignmentPriority.Critical, "Kritik"),
            new PersonnelPriorityOption(PersonnelAssignmentPriority.Urgent, "Acil")
        ];
        _selectedPriority = PriorityOptions[0];
        _selectedPersonnel = PersonnelOptions.FirstOrDefault();

        SaveCommand = new RelayCommand(
            () => RequestClose?.Invoke(this, true),
            () => SelectedPersonnel is not null && !string.IsNullOrWhiteSpace(Summary));
        CancelCommand = new RelayCommand(() => RequestClose?.Invoke(this, false));
    }

    public event EventHandler<bool>? RequestClose;

    public ObservableCollection<PersonnelPickOption> PersonnelOptions { get; }
    public IReadOnlyList<PersonnelPriorityOption> PriorityOptions { get; }

    public PersonnelPickOption? SelectedPersonnel
    {
        get => _selectedPersonnel;
        set
        {
            if (SetProperty(ref _selectedPersonnel, value))
            {
                SaveCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public PersonnelPriorityOption SelectedPriority
    {
        get => _selectedPriority;
        set => SetProperty(ref _selectedPriority, value);
    }

    public string Summary
    {
        get => _summary;
        set
        {
            if (SetProperty(ref _summary, value))
            {
                SaveCommand.NotifyCanExecuteChanged();
            }
        }
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

    public PersonnelAssignment BuildAssignment()
    {
        if (SelectedPersonnel?.Id is null)
        {
            throw new InvalidOperationException("Personel seçilmedi.");
        }

        return new PersonnelAssignment
        {
            PersonnelId = SelectedPersonnel.Id,
            SourceModule = PersonnelAssignmentSourceModule.Manual,
            SourceEntryId = Guid.NewGuid(),
            Status = PersonnelAssignmentStatus.Open,
            AssignedAt = DateTime.Now,
            PrioritySnapshot = SelectedPriority.Value,
            SummarySnapshot = Summary.Trim(),
            FieldLabelSnapshot = FieldLabel?.Trim() ?? string.Empty,
            ProjectIdentitySnapshot = ProjectIdentity?.Trim() ?? string.Empty,
            ModuleLabelSnapshot = "Manuel"
        };
    }
}
