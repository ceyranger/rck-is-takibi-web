using CommunityToolkit.Mvvm.ComponentModel;

namespace RizaCanKilicIsTakibi.Models;

public sealed class KarotEntry : ObservableObject
{
    private Guid _id = Guid.NewGuid();
    private DateTime? _sampleReceivedDate;
    private string _yibfNo = string.Empty;
    private string _adaParsel = string.Empty;
    private string _yapiSahibi = string.Empty;
    private string _muteahhit = string.Empty;
    private string _katBilgisi = string.Empty;
    private string _betonSinifi = string.Empty;
    private string _twentyEightDayResult = string.Empty;
    private string _betonFirmasi = string.Empty;
    private string _laboratuvar = string.Empty;
    private string _aciklama = string.Empty;
    private KarotStatus _status = KarotStatus.KarotAlinacak;
    private int _displayOrder;
    private DateTime _createdAt = DateTime.Now;
    private DateTime _updatedAt = DateTime.Now;

    public Guid Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    public DateTime? SampleReceivedDate
    {
        get => _sampleReceivedDate;
        set => SetProperty(ref _sampleReceivedDate, value);
    }

    public string YibfNo
    {
        get => _yibfNo;
        set => SetProperty(ref _yibfNo, value);
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

    public string Muteahhit
    {
        get => _muteahhit;
        set => SetProperty(ref _muteahhit, value);
    }

    public string KatBilgisi
    {
        get => _katBilgisi;
        set => SetProperty(ref _katBilgisi, value);
    }

    public string BetonSinifi
    {
        get => _betonSinifi;
        set => SetProperty(ref _betonSinifi, value);
    }

    public string TwentyEightDayResult
    {
        get => _twentyEightDayResult;
        set => SetProperty(ref _twentyEightDayResult, value);
    }

    public string BetonFirmasi
    {
        get => _betonFirmasi;
        set => SetProperty(ref _betonFirmasi, value);
    }

    public string Laboratuvar
    {
        get => _laboratuvar;
        set => SetProperty(ref _laboratuvar, value);
    }

    public string Aciklama
    {
        get => _aciklama;
        set => SetProperty(ref _aciklama, value);
    }

    public KarotStatus Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
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
