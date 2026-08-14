using CommunityToolkit.Mvvm.ComponentModel;

namespace RizaCanKilicIsTakibi.Models;

public sealed class PersonnelAssignment : ObservableObject
{
    private Guid _id = Guid.NewGuid();
    private Guid? _personnelId;
    private PersonnelAssignmentSourceModule _sourceModule;
    private Guid _sourceEntryId;
    private string? _sourceColumnKey;
    private PersonnelAssignmentStatus _status = PersonnelAssignmentStatus.Open;
    private DateTime _assignedAt = DateTime.Now;
    private DateTime? _completedAt;
    private PersonnelAssignmentPriority _prioritySnapshot = PersonnelAssignmentPriority.None;
    private string _fieldLabelSnapshot = string.Empty;
    private string _summarySnapshot = string.Empty;
    private string _projectIdentitySnapshot = string.Empty;
    private string _moduleLabelSnapshot = string.Empty;

    public Guid Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    public Guid? PersonnelId
    {
        get => _personnelId;
        set => SetProperty(ref _personnelId, value);
    }

    public PersonnelAssignmentSourceModule SourceModule
    {
        get => _sourceModule;
        set => SetProperty(ref _sourceModule, value);
    }

    public Guid SourceEntryId
    {
        get => _sourceEntryId;
        set => SetProperty(ref _sourceEntryId, value);
    }

    public string? SourceColumnKey
    {
        get => _sourceColumnKey;
        set => SetProperty(ref _sourceColumnKey, value);
    }

    public PersonnelAssignmentStatus Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    public DateTime AssignedAt
    {
        get => _assignedAt;
        set => SetProperty(ref _assignedAt, value);
    }

    public DateTime? CompletedAt
    {
        get => _completedAt;
        set => SetProperty(ref _completedAt, value);
    }

    public PersonnelAssignmentPriority PrioritySnapshot
    {
        get => _prioritySnapshot;
        set => SetProperty(ref _prioritySnapshot, value);
    }

    public string FieldLabelSnapshot
    {
        get => _fieldLabelSnapshot;
        set => SetProperty(ref _fieldLabelSnapshot, value);
    }

    public string SummarySnapshot
    {
        get => _summarySnapshot;
        set => SetProperty(ref _summarySnapshot, value);
    }

    public string ProjectIdentitySnapshot
    {
        get => _projectIdentitySnapshot;
        set => SetProperty(ref _projectIdentitySnapshot, value);
    }

    public string ModuleLabelSnapshot
    {
        get => _moduleLabelSnapshot;
        set => SetProperty(ref _moduleLabelSnapshot, value);
    }

    public PersonnelAssignment Clone()
        => new()
        {
            Id = Id,
            PersonnelId = PersonnelId,
            SourceModule = SourceModule,
            SourceEntryId = SourceEntryId,
            SourceColumnKey = SourceColumnKey,
            Status = Status,
            AssignedAt = AssignedAt,
            CompletedAt = CompletedAt,
            PrioritySnapshot = PrioritySnapshot,
            FieldLabelSnapshot = FieldLabelSnapshot,
            SummarySnapshot = SummarySnapshot,
            ProjectIdentitySnapshot = ProjectIdentitySnapshot,
            ModuleLabelSnapshot = ModuleLabelSnapshot
        };
}
