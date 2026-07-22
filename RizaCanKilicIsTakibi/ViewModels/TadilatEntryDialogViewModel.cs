using CommunityToolkit.Mvvm.Input;
using RizaCanKilicIsTakibi.Helpers;
using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services.Abstractions;
using System.Collections.ObjectModel;

namespace RizaCanKilicIsTakibi.ViewModels;

public sealed class TadilatEntryDialogViewModel : ViewModelBase
{
    private readonly string _district;
    private readonly TadilatSubTab _subTab;
    private readonly IProjectCatalogService _catalogService;
    private string _jobName = string.Empty;
    private string _projectType = string.Empty;
    private string _description1 = string.Empty;
    private Guid? _selectedProjectId;
    private string _validationMessage = string.Empty;

    public TadilatEntryDialogViewModel(
        string district,
        TadilatSubTab subTab,
        IEnumerable<ProjectCatalogEntry> catalogEntries,
        IProjectCatalogService catalogService)
    {
        _district = district;
        _subTab = subTab;
        _catalogService = catalogService;
        CatalogEntries = new ObservableCollection<ProjectCatalogEntry>(catalogEntries);

        SaveCommand = new RelayCommand(Save);
        CancelCommand = new RelayCommand(() => RequestClose?.Invoke(this, null));
    }

    public event EventHandler<TadilatEntry?>? RequestClose;

    public string District => _district;

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

    public string Description1
    {
        get => _description1;
        set => SetProperty(ref _description1, value);
    }

    public string ValidationMessage
    {
        get => _validationMessage;
        private set => SetProperty(ref _validationMessage, value);
    }

    public RelayCommand SaveCommand { get; }
    public RelayCommand CancelCommand { get; }

    public TadilatEntry BuildEntry()
    {
        var entry = new TadilatEntry
        {
            Id = Guid.NewGuid(),
            SubTab = _subTab,
            District = _district,
            JobName = JobName.Trim(),
            ProjectType = ProjectType.Trim(),
            Description1 = Description1.Trim(),
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

        var temp = new TadilatEntry { JobName = JobName };
        _catalogService.ApplyProjectSelection(temp, project);
        JobName = temp.JobName;
    }

    private void Save()
    {
        if (string.IsNullOrWhiteSpace(JobName) && SelectedProjectId is null)
        {
            ValidationMessage = "İşin ismi veya proje seçiminden en az biri gereklidir.";
            return;
        }

        ValidationMessage = string.Empty;
        RequestClose?.Invoke(this, BuildEntry());
    }
}
