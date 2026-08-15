using CommunityToolkit.Mvvm.ComponentModel;

namespace RizaCanKilicIsTakibi.Models;

public sealed class YibfIsTakibiEntry : ObservableObject
{
    private Guid _id = Guid.NewGuid();
    private Guid _workGroupId;
    private Guid _workIdentityId;
    private string _workVariantLabel = string.Empty;
    private string _jobName = string.Empty;
    private string _muellifBilgileriGeldiMi = string.Empty;
    private string _denetciAtamalariYapildiMi = string.Empty;
    private string _tumProjelerinDijitaliVarMi = string.Empty;
    private string _evraklarTamMi = string.Empty;
    private string _yibfSozlesmeHazirlandiMi = string.Empty;
    private string _dekontAlindiMi = string.Empty;
    private string _ruhsatBasvurusuYapildiMi = string.Empty;
    private string _ruhsatNushasiAlindiMi = string.Empty;
    private string _isyeriTeslimTutangiHazirlandiMi = string.Empty;
    private string _isgYazisiHazirlandiMi = string.Empty;
    private string _saglikGuvenlikPlaniGeldiMi = string.Empty;
    private string _temelTopraklamaTutanagiHazirlandiMi = string.Empty;
    private int _displayOrder;
    private DateTime _createdAt = DateTime.Now;
    private DateTime _updatedAt = DateTime.Now;
    private string _assignedPersonnelBadge = string.Empty;

    public Guid Id { get => _id; set => SetProperty(ref _id, value); }
    public Guid WorkGroupId { get => _workGroupId; set => SetProperty(ref _workGroupId, value); }
    public Guid WorkIdentityId { get => _workIdentityId; set => SetProperty(ref _workIdentityId, value); }
    public string WorkVariantLabel { get => _workVariantLabel; set => SetProperty(ref _workVariantLabel, value); }
    public string JobName { get => _jobName; set => SetProperty(ref _jobName, value); }
    public string MuellifBilgileriGeldiMi { get => _muellifBilgileriGeldiMi; set => SetProperty(ref _muellifBilgileriGeldiMi, value); }
    public string DenetciAtamalariYapildiMi { get => _denetciAtamalariYapildiMi; set => SetProperty(ref _denetciAtamalariYapildiMi, value); }
    public string TumProjelerinDijitaliVarMi { get => _tumProjelerinDijitaliVarMi; set => SetProperty(ref _tumProjelerinDijitaliVarMi, value); }
    public string EvraklarTamMi { get => _evraklarTamMi; set => SetProperty(ref _evraklarTamMi, value); }
    public string YibfSozlesmeHazirlandiMi { get => _yibfSozlesmeHazirlandiMi; set => SetProperty(ref _yibfSozlesmeHazirlandiMi, value); }
    public string DekontAlindiMi { get => _dekontAlindiMi; set => SetProperty(ref _dekontAlindiMi, value); }
    public string RuhsatBasvurusuYapildiMi { get => _ruhsatBasvurusuYapildiMi; set => SetProperty(ref _ruhsatBasvurusuYapildiMi, value); }
    public string RuhsatNushasiAlindiMi { get => _ruhsatNushasiAlindiMi; set => SetProperty(ref _ruhsatNushasiAlindiMi, value); }
    public string IsyeriTeslimTutangiHazirlandiMi { get => _isyeriTeslimTutangiHazirlandiMi; set => SetProperty(ref _isyeriTeslimTutangiHazirlandiMi, value); }
    public string IsgYazisiHazirlandiMi { get => _isgYazisiHazirlandiMi; set => SetProperty(ref _isgYazisiHazirlandiMi, value); }
    public string SaglikGuvenlikPlaniGeldiMi { get => _saglikGuvenlikPlaniGeldiMi; set => SetProperty(ref _saglikGuvenlikPlaniGeldiMi, value); }
    public string TemelTopraklamaTutanagiHazirlandiMi { get => _temelTopraklamaTutanagiHazirlandiMi; set => SetProperty(ref _temelTopraklamaTutanagiHazirlandiMi, value); }
    public int DisplayOrder { get => _displayOrder; set => SetProperty(ref _displayOrder, value); }
    public DateTime CreatedAt { get => _createdAt; set => SetProperty(ref _createdAt, value); }
    public DateTime UpdatedAt { get => _updatedAt; set => SetProperty(ref _updatedAt, value); }

    /// <summary>UI-only badge; not persisted.</summary>
    public string AssignedPersonnelBadge
    {
        get => _assignedPersonnelBadge;
        set
        {
            if (SetProperty(ref _assignedPersonnelBadge, value ?? string.Empty))
            {
                OnPropertyChanged(nameof(HasAssignedPersonnel));
            }
        }
    }

    public bool HasAssignedPersonnel => !string.IsNullOrWhiteSpace(AssignedPersonnelBadge);
}
