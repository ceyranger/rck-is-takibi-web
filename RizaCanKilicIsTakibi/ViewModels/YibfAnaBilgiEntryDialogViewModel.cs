using CommunityToolkit.Mvvm.Input;
using RizaCanKilicIsTakibi.Helpers;
using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services.Abstractions;
using System.Collections.ObjectModel;

namespace RizaCanKilicIsTakibi.ViewModels;

public sealed class YibfAnaBilgiEntryDialogViewModel : ViewModelBase
{
    private readonly IProjectCatalogService _catalogService;
    private string _adaParsel = string.Empty;
    private string _yibfNo = string.Empty;
    private string _idare = string.Empty;
    private string _yapiSahibi = string.Empty;
    private string _muteahhit = string.Empty;
    private Guid? _selectedProjectId;
    private string _validationMessage = string.Empty;
    private string _projectSummaryText = string.Empty;
    private bool _isIdentityManualEdit;
    private bool _isProjectIdentityIncomplete;

    public YibfAnaBilgiEntryDialogViewModel(
        IEnumerable<ProjectCatalogEntry> catalogEntries,
        IProjectCatalogService catalogService,
        YibfAnaBilgiEntryDialogResult? initialValues = null,
        bool isEditMode = false)
    {
        _catalogService = catalogService;
        CatalogEntries = new ObservableCollection<ProjectCatalogEntry>(
            catalogEntries.Where(item => item.IsActive && item.Kind != ProjectCatalogKind.Istinat));

        if (initialValues is not null)
        {
            _adaParsel = initialValues.AdaParsel;
            _yibfNo = initialValues.YibfNo;
            _idare = initialValues.Idare;
            _yapiSahibi = initialValues.YapiSahibi;
            _muteahhit = initialValues.Muteahhit;
            _selectedProjectId = initialValues.ProjectId ?? initialValues.WorkGroupId;
        }

        WindowTitle = isEditMode ? "YİBF Kaydı Düzenle" : "Yeni YİBF Kaydı";
        PrimaryActionText = isEditMode ? "Güncelle" : "Kaydet";
        SaveCommand = new RelayCommand(Save);
        CancelCommand = new RelayCommand(() => RequestClose?.Invoke(this, null));
        ToggleIdentityManualEditCommand = new RelayCommand(ToggleIdentityManualEdit);

        if (_selectedProjectId is not null)
        {
            RefreshSummaryFromSelection(overwriteFields: false);
        }
    }

    public event EventHandler<YibfAnaBilgiEntryDialogResult?>? RequestClose;

    public string WindowTitle { get; }
    public string PrimaryActionText { get; }

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

        RefreshSummaryFromSelection(overwriteFields: true);
    }

    private void RefreshSummaryFromSelection(bool overwriteFields)
    {
        if (SelectedProjectId is not Guid projectId)
        {
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

        if (overwriteFields)
        {
            var temp = new YibfAnaBilgiEntry();
            _catalogService.ApplyProjectSelection(temp, project);
            AdaParsel = temp.AdaParsel;
            YapiSahibi = temp.YapiSahibi;
            YibfNo = temp.YibfNo;
            Idare = temp.Idare;
            Muteahhit = temp.Muteahhit;
        }

        ProjectSummaryText = EntryDialogProjectHelper.BuildOwnerParcelSummary(project);
        IsProjectIdentityIncomplete = EntryDialogProjectHelper.IsOwnerParcelIncomplete(project)
            || string.IsNullOrWhiteSpace(AdaParsel)
            || string.IsNullOrWhiteSpace(YapiSahibi);
        if (overwriteFields)
        {
            IsIdentityManualEdit = IsProjectIdentityIncomplete;
        }

        NotifyProjectUi();
    }

    private void ToggleIdentityManualEdit()
    {
        if (!HasSelectedProject)
        {
            return;
        }

        IsIdentityManualEdit = !IsIdentityManualEdit;
        if (!IsIdentityManualEdit)
        {
            RefreshSummaryFromSelection(overwriteFields: true);
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

        if (SelectedProjectId is Guid projectId)
        {
            var project = CatalogEntries.FirstOrDefault(item => item.Id == projectId);
            if (project is null)
            {
                ValidationMessage = "Ana Bilgi için yalnızca normal proje seçilebilir.";
                return;
            }
        }

        ValidationMessage = string.Empty;
        RequestClose?.Invoke(this, new YibfAnaBilgiEntryDialogResult
        {
            ProjectId = SelectedProjectId,
            WorkGroupId = SelectedProjectId,
            AdaParsel = AdaParsel.Trim(),
            YibfNo = YibfNo.Trim(),
            Idare = Idare.Trim(),
            YapiSahibi = YapiSahibi.Trim(),
            Muteahhit = Muteahhit.Trim()
        });
    }
}
