using CommunityToolkit.Mvvm.Input;
using RizaCanKilicIsTakibi.Helpers;
using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services.Abstractions;
using System.Collections.ObjectModel;

namespace RizaCanKilicIsTakibi.ViewModels;

public sealed class KarotEntryDialogViewModel : ViewModelBase
{
    private readonly KarotSubTab _subTab;
    private readonly IProjectCatalogService _catalogService;
    private DateTime? _sampleReceivedDate;
    private string _adaParsel = string.Empty;
    private string _yapiSahibi = string.Empty;
    private string _yibfNo = string.Empty;
    private string _muteahhit = string.Empty;
    private string _katBilgisi = string.Empty;
    private string _betonSinifi = string.Empty;
    private string _twentyEightDayResult = string.Empty;
    private string _betonFirmasi = string.Empty;
    private string _laboratuvar = string.Empty;
    private string _aciklama = string.Empty;
    private Guid? _selectedProjectId;
    private string _validationMessage = string.Empty;

    public KarotEntryDialogViewModel(
        KarotSubTab subTab,
        IEnumerable<ProjectCatalogEntry> catalogEntries,
        IProjectCatalogService catalogService)
    {
        _subTab = subTab;
        _catalogService = catalogService;
        CatalogEntries = new ObservableCollection<ProjectCatalogEntry>(catalogEntries);

        SaveCommand = new RelayCommand(Save);
        CancelCommand = new RelayCommand(() => RequestClose?.Invoke(this, null));
    }

    public event EventHandler<KarotEntry?>? RequestClose;

    public ObservableCollection<ProjectCatalogEntry> CatalogEntries { get; }

    public Guid? SelectedProjectId
    {
        get => _selectedProjectId;
        set
        {
            if (!SetProperty(ref _selectedProjectId, value))
            {
                return;
            }

            ApplySelectedProject();
        }
    }

    public DateTime? SampleReceivedDate
    {
        get => _sampleReceivedDate;
        set => SetProperty(ref _sampleReceivedDate, value);
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

    public string ValidationMessage
    {
        get => _validationMessage;
        private set => SetProperty(ref _validationMessage, value);
    }

    public RelayCommand SaveCommand { get; }
    public RelayCommand CancelCommand { get; }

    public KarotEntry BuildEntry()
    {
        var entry = new KarotEntry
        {
            SampleReceivedDate = SampleReceivedDate,
            AdaParsel = AdaParsel.Trim(),
            YapiSahibi = YapiSahibi.Trim(),
            YibfNo = YibfNo.Trim(),
            Muteahhit = Muteahhit.Trim(),
            KatBilgisi = KatBilgisi.Trim(),
            BetonSinifi = BetonSinifi.Trim(),
            TwentyEightDayResult = TwentyEightDayResult.Trim(),
            BetonFirmasi = BetonFirmasi.Trim(),
            Laboratuvar = Laboratuvar.Trim(),
            Aciklama = Aciklama.Trim(),
            Status = _subTab == KarotSubTab.Yapilan ? KarotStatus.KarotAlindiOlumlu : KarotStatus.KarotAlinacak,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        if (SelectedProjectId is Guid projectId)
        {
            var project = CatalogEntries.FirstOrDefault(item => item.Id == projectId);
            if (project is not null)
            {
                _catalogService.ApplyProjectSelection(entry, project);
            }
            else
            {
                entry.ProjectId = projectId;
            }
        }

        return entry;
    }

    private void ApplySelectedProject()
    {
        if (SelectedProjectId is not Guid projectId)
        {
            return;
        }

        var project = CatalogEntries.FirstOrDefault(item => item.Id == projectId);
        if (project is null)
        {
            return;
        }

        var temp = new KarotEntry
        {
            AdaParsel = AdaParsel,
            YapiSahibi = YapiSahibi,
            YibfNo = YibfNo
        };
        _catalogService.ApplyProjectSelection(temp, project);
        AdaParsel = temp.AdaParsel;
        YapiSahibi = temp.YapiSahibi;
        YibfNo = temp.YibfNo;
    }

    private void Save()
    {
        if (string.IsNullOrWhiteSpace(AdaParsel)
            && string.IsNullOrWhiteSpace(YapiSahibi)
            && string.IsNullOrWhiteSpace(YibfNo)
            && SelectedProjectId is null)
        {
            ValidationMessage = "Ada/Parsel, Yapı Sahibi, YİBF No veya proje seçiminden en az biri gereklidir.";
            return;
        }

        ValidationMessage = string.Empty;
        RequestClose?.Invoke(this, BuildEntry());
    }
}
