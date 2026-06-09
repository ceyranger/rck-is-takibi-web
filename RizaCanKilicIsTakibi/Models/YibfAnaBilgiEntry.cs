using CommunityToolkit.Mvvm.ComponentModel;

namespace RizaCanKilicIsTakibi.Models;

public sealed class YibfAnaBilgiEntry : ObservableObject
{
    private Guid _id = Guid.NewGuid();
    private string _adaParsel = string.Empty;
    private string _yibfNo = string.Empty;
    private string _idare = string.Empty;
    private string _yapiSahibi = string.Empty;
    private string _muteahhit = string.Empty;
    private int _displayOrder;
    private DateTime _createdAt = DateTime.Now;
    private DateTime _updatedAt = DateTime.Now;

    public Guid Id { get => _id; set => SetProperty(ref _id, value); }
    public string AdaParsel { get => _adaParsel; set => SetProperty(ref _adaParsel, value); }
    public string YibfNo { get => _yibfNo; set => SetProperty(ref _yibfNo, value); }
    public string Idare { get => _idare; set => SetProperty(ref _idare, value); }
    public string YapiSahibi { get => _yapiSahibi; set => SetProperty(ref _yapiSahibi, value); }
    public string Muteahhit { get => _muteahhit; set => SetProperty(ref _muteahhit, value); }
    public int DisplayOrder { get => _displayOrder; set => SetProperty(ref _displayOrder, value); }
    public DateTime CreatedAt { get => _createdAt; set => SetProperty(ref _createdAt, value); }
    public DateTime UpdatedAt { get => _updatedAt; set => SetProperty(ref _updatedAt, value); }
}