using CommunityToolkit.Mvvm.ComponentModel;

namespace RizaCanKilicIsTakibi.Models;

public sealed class TadilatEntry : ObservableObject
{
    private Guid _id = Guid.NewGuid();
    private Guid? _projectId;
    private TadilatSubTab _subTab = TadilatSubTab.Aktif;
    private string _district = string.Empty;
    private string _jobName = string.Empty;
    private string _projectType = string.Empty;
    private string _digitalReceived = string.Empty;
    private string _inspectorApproved = string.Empty;
    private string _outputAndReportArrived = string.Empty;
    private string _officialLetterSubmitted = string.Empty;
    private string _archivedFromMunicipality = string.Empty;
    private string _description1 = string.Empty;
    private string _description2 = string.Empty;
    private int _displayOrder;
    private DateTime _createdAt = DateTime.Now;
    private DateTime _updatedAt = DateTime.Now;

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

    public TadilatSubTab SubTab
    {
        get => _subTab;
        set => SetProperty(ref _subTab, value);
    }

    public string District
    {
        get => _district;
        set => SetProperty(ref _district, value);
    }

    public string JobName
    {
        get => _jobName;
        set => SetProperty(ref _jobName, value);
    }

    public string ProjectType
    {
        get => _projectType;
        set => SetProperty(ref _projectType, value);
    }

    public string DigitalReceived
    {
        get => _digitalReceived;
        set => SetProperty(ref _digitalReceived, value);
    }

    public string InspectorApproved
    {
        get => _inspectorApproved;
        set => SetProperty(ref _inspectorApproved, value);
    }

    public string OutputAndReportArrived
    {
        get => _outputAndReportArrived;
        set => SetProperty(ref _outputAndReportArrived, value);
    }

    public string OfficialLetterSubmitted
    {
        get => _officialLetterSubmitted;
        set => SetProperty(ref _officialLetterSubmitted, value);
    }

    public string ArchivedFromMunicipality
    {
        get => _archivedFromMunicipality;
        set => SetProperty(ref _archivedFromMunicipality, value);
    }

    public string Description1
    {
        get => _description1;
        set => SetProperty(ref _description1, value);
    }

    public string Description2
    {
        get => _description2;
        set => SetProperty(ref _description2, value);
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
}
