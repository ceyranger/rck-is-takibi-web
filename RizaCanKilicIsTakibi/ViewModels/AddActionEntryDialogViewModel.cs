using CommunityToolkit.Mvvm.Input;
using RizaCanKilicIsTakibi.Helpers;
using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services.Abstractions;
using System.Collections.ObjectModel;

namespace RizaCanKilicIsTakibi.ViewModels;

public sealed class AddActionEntryDialogViewModel : ViewModelBase
{
    private readonly IProjectCatalogService _catalogService;
    private string _ownerParcelText = string.Empty;
    private string _workText = string.Empty;
    private Guid? _selectedProjectId;
    private string _validationMessage = string.Empty;
    private string _projectSummaryText = string.Empty;
    private bool _isIdentityManualEdit;
    private bool _isProjectIdentityIncomplete;
    private string _district = string.Empty;

    public AddActionEntryDialogViewModel(
        string district,
        ActionEntryCategory category,
        IEnumerable<ProjectCatalogEntry> catalogEntries,
        IProjectCatalogService catalogService)
        : this(
            new AddActionEntryDialogRequest
            {
                District = district,
                Category = category
            },
            catalogEntries,
            catalogService)
    {
    }

    public AddActionEntryDialogViewModel(
        AddActionEntryDialogRequest request,
        IEnumerable<ProjectCatalogEntry> catalogEntries,
        IProjectCatalogService catalogService)
    {
        _district = request.District;
        Category = request.Category;
        _catalogService = catalogService;
        CatalogEntries = new ObservableCollection<ProjectCatalogEntry>(catalogEntries);
        DistrictOptions = new ObservableCollection<string>(request.DistrictOptions);
        OwnerParcelText = request.OwnerParcelText;
        WorkText = request.WorkText;
        SelectedProjectId = request.ProjectId;

        SaveCommand = new RelayCommand(Save);
        CancelCommand = new RelayCommand(() => RequestClose?.Invoke(this, false));
        ToggleIdentityManualEditCommand = new RelayCommand(ToggleIdentityManualEdit);
    }

    public event EventHandler<bool>? RequestClose;

    public string District
    {
        get => _district;
        set => SetProperty(ref _district, value);
    }

    public ActionEntryCategory Category { get; }

    public ObservableCollection<ProjectCatalogEntry> CatalogEntries { get; }
    public ObservableCollection<string> DistrictOptions { get; }
    public bool RequiresDistrictSelection => DistrictOptions.Count > 0;
    public bool HasFixedDistrict => !RequiresDistrictSelection;

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

    public string OwnerParcelText
    {
        get => _ownerParcelText;
        set => SetProperty(ref _ownerParcelText, value);
    }

    public string WorkText
    {
        get => _workText;
        set => SetProperty(ref _workText, value);
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

    public ActionEntry BuildEntry(int displayOrder)
    {
        var entry = new ActionEntry
        {
            Id = Guid.NewGuid(),
            Category = Category,
            District = District,
            OwnerParcelText = OwnerParcelText.Trim(),
            WorkText = WorkText.Trim(),
            DisplayOrder = displayOrder,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        if (SelectedProjectId is Guid projectId)
        {
            var project = CatalogEntries.FirstOrDefault(item => item.Id == projectId);
            if (project is not null)
            {
                _catalogService.ApplyProjectSelection(entry, project, CatalogEntries);
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

        var temp = new ActionEntry();
        _catalogService.ApplyProjectSelection(temp, project, CatalogEntries);
        OwnerParcelText = temp.OwnerParcelText;
        ProjectSummaryText = EntryDialogProjectHelper.BuildOwnerParcelSummary(project);
        IsProjectIdentityIncomplete = string.IsNullOrWhiteSpace(temp.OwnerParcelText);
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
                var temp = new ActionEntry();
                _catalogService.ApplyProjectSelection(temp, project, CatalogEntries);
                OwnerParcelText = temp.OwnerParcelText;
                ProjectSummaryText = EntryDialogProjectHelper.BuildOwnerParcelSummary(project);
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
        if (string.IsNullOrWhiteSpace(District))
        {
            ValidationMessage = "İlçe seçimi zorunludur.";
            return;
        }

        if (string.IsNullOrWhiteSpace(OwnerParcelText) || string.IsNullOrWhiteSpace(WorkText))
        {
            ValidationMessage = "Ada/Parsel/Yapı Sahibi ve Yapılacak İş alanları zorunludur.";
            return;
        }

        ValidationMessage = string.Empty;
        RequestClose?.Invoke(this, true);
    }
}
