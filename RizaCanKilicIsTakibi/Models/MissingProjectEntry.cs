using CommunityToolkit.Mvvm.ComponentModel;

namespace RizaCanKilicIsTakibi.Models;

public sealed class MissingProjectEntry : ObservableObject
{
    private Guid _id = Guid.NewGuid();
    private Guid? _projectId;
    private string _adaParsel = string.Empty;
    private string _yapiSahibi = string.Empty;
    private MissingProjectMedium _recordMedium = MissingProjectMedium.Fiziki;
    private string _recordMediumText = string.Empty;
    private string _missingProjectText = string.Empty;
    private string _description = string.Empty;
    private int _displayOrder;
    private DateTime _createdAt = DateTime.Now;
    private DateTime _updatedAt = DateTime.Now;
    private string _assignedPersonnelBadge = string.Empty;

    public Guid Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    public Guid? ProjectId
    {
        get => _projectId;
        set => SetProperty(ref _projectId, value);
    }

    public string AdaParsel
    {
        get => _adaParsel;
        set => SetProperty(ref _adaParsel, value);
    }

    public string YapiSahibi
    {
        get => _yapiSahibi;
        set => SetProperty(ref _yapiSahibi, value);
    }

    public MissingProjectMedium RecordMedium
    {
        get => _recordMedium;
        set => SetProperty(ref _recordMedium, value);
    }

    public string RecordMediumText
    {
        get => _recordMediumText;
        set => SetProperty(ref _recordMediumText, value);
    }

    public string MissingProjectText
    {
        get => _missingProjectText;
        set => SetProperty(ref _missingProjectText, value);
    }

    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    public int DisplayOrder
    {
        get => _displayOrder;
        set => SetProperty(ref _displayOrder, value);
    }

    public DateTime CreatedAt
    {
        get => _createdAt;
        set => SetProperty(ref _createdAt, value);
    }

    public DateTime UpdatedAt
    {
        get => _updatedAt;
        set => SetProperty(ref _updatedAt, value);
    }

    /// <summary>UI-only badge; not persisted.</summary>
    public string AssignedPersonnelBadge
    {
        get => _assignedPersonnelBadge;
        set => SetProperty(ref _assignedPersonnelBadge, value ?? string.Empty);
    }

    public bool HasAssignedPersonnel => !string.IsNullOrWhiteSpace(AssignedPersonnelBadge);
}
