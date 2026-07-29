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
    private string _projectSummaryText = string.Empty;
    private bool _isIdentityManualEdit;
    private bool _isProjectIdentityIncomplete;

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
        ToggleIdentityManualEditCommand = new RelayCommand(ToggleIdentityManualEdit);
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

    public string ProjectSummaryText
    {
        get => _projectSummaryText;
        private set => SetProperty(ref _projectSummaryText, value);
    }

    public bool HasSelectedProject => SelectedProjectId is not null;

    public bool IsIdentityManualEdit
    {
        get => _isIdentityManualEdit;
        private set
        {
            if (SetProperty(ref _isIdentityManualEdit, value))
            {
                OnPropertyChanged(nameof(ShowIdentityFields));
                OnPropertyChanged(nameof(IdentityEditToggleText));
            }
        }
    }

    public bool IsProjectIdentityIncomplete
    {
        get => _isProjectIdentityIncomplete;
        private set
        {
            if (SetProperty(ref _isProjectIdentityIncomplete, value))
            {
                OnPropertyChanged(nameof(ShowIdentityFields));
            }
        }
    }

    public bool ShowIdentityFields => !HasSelectedProject || IsIdentityManualEdit || IsProjectIdentityIncomplete;

    public string IdentityEditToggleText => IsIdentityManualEdit ? "Projeden kullan" : "Elle düzenle";

    public RelayCommand SaveCommand { get; }
    public RelayCommand CancelCommand { get; }
    public RelayCommand ToggleIdentityManualEditCommand { get; }

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
            ProjectSummaryText = string.Empty;
            IsProjectIdentityIncomplete = false;
            IsIdentityManualEdit = false;
            NotifyProjectUi();
            return;
        }

        var project = CatalogEntries.FirstOrDefault(item => item.Id == projectId);
        if (project is null)
        {
            ProjectSummaryText = string.Empty;
            IsProjectIdentityIncomplete = true;
            IsIdentityManualEdit = true;
            NotifyProjectUi();
            return;
        }

        var temp = new TadilatEntry();
        _catalogService.ApplyProjectSelection(temp, project);
        JobName = temp.JobName;
        ProjectSummaryText = EntryDialogProjectHelper.BuildJobSummary(project, temp.JobName);
        IsProjectIdentityIncomplete = string.IsNullOrWhiteSpace(temp.JobName);
        IsIdentityManualEdit = IsProjectIdentityIncomplete;
        NotifyProjectUi();
    }

    private void ToggleIdentityManualEdit()
    {
        if (!HasSelectedProject)
        {
            return;
        }

        if (IsIdentityManualEdit && IsProjectIdentityIncomplete)
        {
            return;
        }

        IsIdentityManualEdit = !IsIdentityManualEdit;
        if (!IsIdentityManualEdit && SelectedProjectId is Guid projectId)
        {
            var project = CatalogEntries.FirstOrDefault(item => item.Id == projectId);
            if (project is not null)
            {
                var temp = new TadilatEntry();
                _catalogService.ApplyProjectSelection(temp, project);
                JobName = temp.JobName;
                ProjectSummaryText = EntryDialogProjectHelper.BuildJobSummary(project, temp.JobName);
            }
        }
    }

    private void NotifyProjectUi()
    {
        OnPropertyChanged(nameof(HasSelectedProject));
        OnPropertyChanged(nameof(ShowIdentityFields));
        OnPropertyChanged(nameof(IdentityEditToggleText));
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
