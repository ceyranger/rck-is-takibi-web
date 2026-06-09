using CommunityToolkit.Mvvm.Input;
using RizaCanKilicIsTakibi.Helpers;
using RizaCanKilicIsTakibi.Models;

namespace RizaCanKilicIsTakibi.ViewModels;

public sealed class YibfAnaBilgiEntryDialogViewModel : ViewModelBase
{
    private string _adaParsel = string.Empty;
    private string _yibfNo = string.Empty;
    private string _idare = string.Empty;
    private string _yapiSahibi = string.Empty;
    private string _muteahhit = string.Empty;
    private string _validationMessage = string.Empty;

    public YibfAnaBilgiEntryDialogViewModel(
        YibfAnaBilgiEntryDialogResult? initialValues = null,
        bool isEditMode = false)
    {
        if (initialValues is not null)
        {
            _adaParsel = initialValues.AdaParsel;
            _yibfNo = initialValues.YibfNo;
            _idare = initialValues.Idare;
            _yapiSahibi = initialValues.YapiSahibi;
            _muteahhit = initialValues.Muteahhit;
        }

        WindowTitle = isEditMode ? "YİBF Kaydı Düzenle" : "Yeni YİBF Kaydı";
        PrimaryActionText = isEditMode ? "Güncelle" : "Kaydet";
        SaveCommand = new RelayCommand(Save);
        CancelCommand = new RelayCommand(() => RequestClose?.Invoke(this, null));
    }

    public event EventHandler<YibfAnaBilgiEntryDialogResult?>? RequestClose;

    public string WindowTitle { get; }
    public string PrimaryActionText { get; }

    public string AdaParsel
    {
        get => _adaParsel;
        set => SetProperty(ref _adaParsel, value);
    }

    public string YibfNo
    {
        get => _yibfNo;
        set => SetProperty(ref _yibfNo, value);
    }

    public string Idare
    {
        get => _idare;
        set => SetProperty(ref _idare, value);
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

    public string ValidationMessage
    {
        get => _validationMessage;
        private set => SetProperty(ref _validationMessage, value);
    }

    public RelayCommand SaveCommand { get; }
    public RelayCommand CancelCommand { get; }

    private void Save()
    {
        if (string.IsNullOrWhiteSpace(AdaParsel))
        {
            ValidationMessage = "Ada Parsel alanı zorunludur.";
            return;
        }

        if (string.IsNullOrWhiteSpace(YapiSahibi))
        {
            ValidationMessage = "Yapı Sahibi alanı zorunludur.";
            return;
        }

        ValidationMessage = string.Empty;
        RequestClose?.Invoke(this, new YibfAnaBilgiEntryDialogResult
        {
            AdaParsel = AdaParsel.Trim(),
            YibfNo = YibfNo.Trim(),
            Idare = Idare.Trim(),
            YapiSahibi = YapiSahibi.Trim(),
            Muteahhit = Muteahhit.Trim()
        });
    }
}
