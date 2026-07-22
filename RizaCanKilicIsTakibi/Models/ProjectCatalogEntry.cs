using CommunityToolkit.Mvvm.ComponentModel;

namespace RizaCanKilicIsTakibi.Models;

public sealed class ProjectCatalogEntry : ObservableObject
{
    private Guid _id = Guid.NewGuid();
    private string _displayName = string.Empty;
    private string _adaParsel = string.Empty;
    private string _yapiSahibi = string.Empty;
    private string _yibfNo = string.Empty;
    private string _belediye = string.Empty;
    private string _muteahhit = string.Empty;
    private ProjectCatalogKind _kind = ProjectCatalogKind.Normal;
    private Guid? _parentProjectId;
    private bool _isActive = true;
    private int _displayOrder;
    private DateTime _createdAt = DateTime.Now;
    private DateTime _updatedAt = DateTime.Now;

    public Guid Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    public string DisplayName
    {
        get => _displayName;
        set => SetProperty(ref _displayName, value);
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

    public string YibfNo
    {
        get => _yibfNo;
        set => SetProperty(ref _yibfNo, value);
    }

    public string Belediye
    {
        get => _belediye;
        set => SetProperty(ref _belediye, value);
    }

    public string Muteahhit
    {
        get => _muteahhit;
        set => SetProperty(ref _muteahhit, value);
    }

    public ProjectCatalogKind Kind
    {
        get => _kind;
        set => SetProperty(ref _kind, value);
    }

    public Guid? ParentProjectId
    {
        get => _parentProjectId;
        set => SetProperty(ref _parentProjectId, value);
    }

    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
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

    public ProjectCatalogEntry Clone()
        => new()
        {
            Id = Id,
            DisplayName = DisplayName,
            AdaParsel = AdaParsel,
            YapiSahibi = YapiSahibi,
            YibfNo = YibfNo,
            Belediye = Belediye,
            Muteahhit = Muteahhit,
            Kind = Kind,
            ParentProjectId = ParentProjectId,
            IsActive = IsActive,
            DisplayOrder = DisplayOrder,
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt
        };
}
