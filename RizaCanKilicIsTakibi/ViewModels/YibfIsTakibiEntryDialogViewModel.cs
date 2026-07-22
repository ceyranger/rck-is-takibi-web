using CommunityToolkit.Mvvm.Input;
using RizaCanKilicIsTakibi.Helpers;
using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services.Abstractions;
using System.Collections.ObjectModel;

namespace RizaCanKilicIsTakibi.ViewModels;

public sealed class YibfIsTakibiEntryDialogViewModel : ViewModelBase
{
    private readonly IProjectCatalogService _catalogService;
    private string _jobName = string.Empty;
    private string _workVariantLabel = string.Empty;
    private string _belediye = string.Empty;
    private string _muteahhit = string.Empty;
    private Guid? _selectedProjectId;
    private string _validationMessage = string.Empty;

    public YibfIsTakibiEntryDialogViewModel(
        IEnumerable<ProjectCatalogEntry> catalogEntries,
        IProjectCatalogService catalogService)
    {
        _catalogService = catalogService;
        CatalogEntries = new ObservableCollection<ProjectCatalogEntry>(catalogEntries);

        SaveCommand = new RelayCommand(Save);
        CancelCommand = new RelayCommand(() => RequestClose?.Invoke(this, null));
    }

    public event EventHandler<YibfIsTakibiEntry?>? RequestClose;

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

    public string WorkVariantLabel
    {
        get => _workVariantLabel;
        set => SetProperty(ref _workVariantLabel, value);
    }

    public string Belediye
    {
        get => _belediye;
        private set => SetProperty(ref _belediye, value);
    }

    public string Muteahhit
    {
        get => _muteahhit;
        private set => SetProperty(ref _muteahhit, value);
    }

    public string ValidationMessage
    {
        get => _validationMessage;
        private set => SetProperty(ref _validationMessage, value);
    }

    public RelayCommand SaveCommand { get; }
    public RelayCommand CancelCommand { get; }

    public YibfIsTakibiEntry BuildEntry()
    {
        var entry = new YibfIsTakibiEntry
        {
            Id = Guid.NewGuid(),
            JobName = JobName.Trim(),
            WorkVariantLabel = WorkVariantLabel.Trim(),
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
                entry.WorkGroupId = entry.Id;
                entry.WorkIdentityId = entry.Id;
            }
        }
        else
        {
            entry.WorkGroupId = entry.Id;
            entry.WorkIdentityId = entry.Id;
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

        var temp = new YibfIsTakibiEntry { JobName = JobName };
        _catalogService.ApplyProjectSelection(temp, project);
        JobName = temp.JobName;

        var sourceProject = project;
        if (project.Kind == ProjectCatalogKind.Istinat
            && project.ParentProjectId is Guid parentId
            && parentId != Guid.Empty)
        {
            sourceProject = CatalogEntries.FirstOrDefault(item => item.Id == parentId) ?? project;
        }

        Belediye = sourceProject.Belediye?.Trim() ?? string.Empty;
        Muteahhit = sourceProject.Muteahhit?.Trim() ?? string.Empty;
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
