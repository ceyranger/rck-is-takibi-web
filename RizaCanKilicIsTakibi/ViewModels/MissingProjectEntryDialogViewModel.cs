using CommunityToolkit.Mvvm.Input;
using RizaCanKilicIsTakibi.Helpers;
using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services.Abstractions;
using System.Collections.ObjectModel;

namespace RizaCanKilicIsTakibi.ViewModels;

public sealed class MissingProjectEntryDialogViewModel : ViewModelBase
{
    private readonly IProjectCatalogService _catalogService;
    private string _adaParsel = string.Empty;
    private string _yapiSahibi = string.Empty;
    private string _missingProjectText = string.Empty;
    private string _description = string.Empty;
    private MissingProjectMedium _recordMedium = MissingProjectMedium.Fiziki;
    private string _recordMediumText = MissingProjectMediumLabelProvider.GetLabel(MissingProjectMedium.Fiziki);
    private Guid? _selectedProjectId;
    private string _validationMessage = string.Empty;

    public MissingProjectEntryDialogViewModel(
        IEnumerable<ProjectCatalogEntry> catalogEntries,
        IProjectCatalogService catalogService)
    {
        _catalogService = catalogService;
        CatalogEntries = new ObservableCollection<ProjectCatalogEntry>(catalogEntries);
        MediumOptionItems = Enum
            .GetValues<MissingProjectMedium>()
            .Select(medium => new MissingProjectMediumOption(medium, MissingProjectMediumLabelProvider.GetLabel(medium)))
            .ToList();

        SaveCommand = new RelayCommand(Save);
        CancelCommand = new RelayCommand(() => RequestClose?.Invoke(this, null));
    }

    public event EventHandler<MissingProjectEntry?>? RequestClose;

    public ObservableCollection<ProjectCatalogEntry> CatalogEntries { get; }

    public IReadOnlyList<MissingProjectMediumOption> MediumOptionItems { get; }

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

    public MissingProjectMedium RecordMedium
    {
        get => _recordMedium;
        set
        {
            if (SetProperty(ref _recordMedium, value))
            {
                RecordMediumText = MissingProjectMediumLabelProvider.GetLabel(value);
            }
        }
    }

    public string RecordMediumText
    {
        get => _recordMediumText;
        set => SetProperty(ref _recordMediumText, value);
    }

    public string ValidationMessage
    {
        get => _validationMessage;
        private set => SetProperty(ref _validationMessage, value);
    }

    public RelayCommand SaveCommand { get; }
    public RelayCommand CancelCommand { get; }

    public MissingProjectEntry BuildEntry()
    {
        var entry = new MissingProjectEntry
        {
            AdaParsel = AdaParsel.Trim(),
            YapiSahibi = YapiSahibi.Trim(),
            MissingProjectText = MissingProjectText.Trim(),
            Description = Description.Trim(),
            RecordMedium = RecordMedium,
            RecordMediumText = RecordMediumText.Trim(),
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

        var temp = new MissingProjectEntry
        {
            AdaParsel = AdaParsel,
            YapiSahibi = YapiSahibi
        };
        _catalogService.ApplyProjectSelection(temp, project);
        AdaParsel = temp.AdaParsel;
        YapiSahibi = temp.YapiSahibi;
    }

    private void Save()
    {
        if (string.IsNullOrWhiteSpace(AdaParsel)
            && string.IsNullOrWhiteSpace(YapiSahibi)
            && SelectedProjectId is null)
        {
            ValidationMessage = "Ada/Parsel, Yapı Sahibi veya proje seçiminden en az biri gereklidir.";
            return;
        }

        ValidationMessage = string.Empty;
        RequestClose?.Invoke(this, BuildEntry());
    }
}
