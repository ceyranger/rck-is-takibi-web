using CommunityToolkit.Mvvm.Input;
using RizaCanKilicIsTakibi.Helpers;
using RizaCanKilicIsTakibi.Models;
using System.Collections.ObjectModel;

namespace RizaCanKilicIsTakibi.ViewModels;

public sealed class ProjectCatalogEntryDialogViewModel : ViewModelBase
{
    private string _displayName = string.Empty;
    private string _adaParsel = string.Empty;
    private string _yapiSahibi = string.Empty;
    private string _yibfNo = string.Empty;
    private string _belediye = string.Empty;
    private string _muteahhit = string.Empty;
    private ProjectCatalogKindChoice _selectedKindChoice;
    private Guid? _parentProjectId;
    private bool _isActive = true;
    private string _validationMessage = string.Empty;

    public ProjectCatalogEntryDialogViewModel(
        ProjectCatalogEntry? existing,
        IReadOnlyList<ProjectCatalogEntry> catalog)
    {
        IsEditMode = existing is not null;
        WindowTitle = IsEditMode ? "Proje Kataloğu Düzenle" : "Proje Kataloğu Ekle";
        AvailableKinds = new ObservableCollection<ProjectCatalogKindChoice>(ProjectCatalogKindLabels.AllChoices);
        ParentProjects = new ObservableCollection<ProjectCatalogEntry>(
            catalog
                .Where(item => item.IsActive && item.Kind == ProjectCatalogKind.Normal)
                .OrderBy(item => item.DisplayName)
                .ThenBy(item => item.AdaParsel));

        var initialKind = existing?.Kind ?? ProjectCatalogKind.Normal;
        _selectedKindChoice = AvailableKinds.First(item => item.Kind == initialKind);

        if (existing is not null)
        {
            _displayName = NormalizeIstinatDisplayName(existing.DisplayName, existing.Kind);
            _adaParsel = existing.AdaParsel;
            _yapiSahibi = existing.YapiSahibi;
            _yibfNo = existing.YibfNo;
            _belediye = existing.Belediye;
            _muteahhit = existing.Muteahhit;
            _parentProjectId = existing.ParentProjectId;
            _isActive = existing.IsActive;
            EntryId = existing.Id;
            CreatedAt = existing.CreatedAt;
            DisplayOrder = existing.DisplayOrder;
        }
        else
        {
            EntryId = Guid.NewGuid();
            CreatedAt = DateTime.Now;
            DisplayOrder = catalog.Count == 0 ? 0 : catalog.Max(item => item.DisplayOrder) + 1;
        }

        SaveCommand = new RelayCommand(Save);
        CancelCommand = new RelayCommand(() => RequestClose?.Invoke(this, false));
    }

    public event EventHandler<bool>? RequestClose;

    public bool IsEditMode { get; }
    public string WindowTitle { get; }
    public Guid EntryId { get; }
    public DateTime CreatedAt { get; }
    public int DisplayOrder { get; }
    public ObservableCollection<ProjectCatalogKindChoice> AvailableKinds { get; }
    public ObservableCollection<ProjectCatalogEntry> ParentProjects { get; }

    public string DisplayName
    {
        get => _displayName;
        set => SetProperty(ref _displayName, value);
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

    public string Belediye
    {
        get => _belediye;
        set => SetProperty(ref _belediye, value);
    }

    public string Muteahhit
    {
        get => _muteahhit;
        set => SetProperty(ref _muteahhit, value);
    }

    public ProjectCatalogKindChoice SelectedKindChoice
    {
        get => _selectedKindChoice;
        set
        {
            var next = value ?? AvailableKinds.First(item => item.Kind == ProjectCatalogKind.Normal);
            if (!SetProperty(ref _selectedKindChoice, next))
            {
                return;
            }

            OnPropertyChanged(nameof(Kind));
            OnKindChanged(next.Kind);
        }
    }

    public ProjectCatalogKind Kind => SelectedKindChoice.Kind;

    public Guid? ParentProjectId
    {
        get => _parentProjectId;
        set
        {
            if (SetProperty(ref _parentProjectId, value))
            {
                OnPropertyChanged(nameof(ParentSelectionHint));
                if (!string.IsNullOrWhiteSpace(ValidationMessage)
                    && value is Guid selected
                    && selected != Guid.Empty)
                {
                    ValidationMessage = string.Empty;
                }

                CopyBlankIdentityFromParent();
            }
        }
    }

    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
    }

    public bool RequiresParent => Kind == ProjectCatalogKind.Istinat;

    public bool HasParentProjects => ParentProjects.Count > 0;

    public string ParentSelectionHint
    {
        get
        {
            if (!RequiresParent)
            {
                return string.Empty;
            }

            if (!HasParentProjects)
            {
                return "Seçilebilecek Normal üst proje yok. Önce Normal bir proje ekleyin veya Proje Takibi'den kataloğu doldurun.";
            }

            if (ParentProjectId is null || ParentProjectId == Guid.Empty)
            {
                return "Üst proje seçmek için kutuya tıklayıp listeden seçin.";
            }

            return string.Empty;
        }
    }

    public string ValidationMessage
    {
        get => _validationMessage;
        private set => SetProperty(ref _validationMessage, value);
    }

    public RelayCommand SaveCommand { get; }
    public RelayCommand CancelCommand { get; }

    public ProjectCatalogEntry BuildEntry()
        => new()
        {
            Id = EntryId,
            DisplayName = DisplayName.Trim(),
            AdaParsel = AdaParsel.Trim(),
            YapiSahibi = YapiSahibi.Trim(),
            YibfNo = YibfNo.Trim(),
            Belediye = Belediye.Trim(),
            Muteahhit = Muteahhit.Trim(),
            Kind = Kind,
            ParentProjectId = Kind == ProjectCatalogKind.Istinat ? ParentProjectId : null,
            IsActive = IsActive,
            DisplayOrder = DisplayOrder,
            CreatedAt = CreatedAt,
            UpdatedAt = DateTime.Now
        };

    private void OnKindChanged(ProjectCatalogKind kind)
    {
        if (kind != ProjectCatalogKind.Istinat)
        {
            ParentProjectId = null;
        }
        else if (string.IsNullOrWhiteSpace(DisplayName)
                 || string.Equals(DisplayName.Trim(), "Istinat", StringComparison.OrdinalIgnoreCase))
        {
            DisplayName = "İstinat";
        }

        OnPropertyChanged(nameof(RequiresParent));
        OnPropertyChanged(nameof(ParentSelectionHint));
        ValidationMessage = string.Empty;
        CopyBlankIdentityFromParent();
    }

    private void CopyBlankIdentityFromParent()
    {
        if (Kind != ProjectCatalogKind.Istinat
            || ParentProjectId is not Guid parentId
            || parentId == Guid.Empty)
        {
            return;
        }

        var parent = ParentProjects.FirstOrDefault(item => item.Id == parentId);
        if (parent is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(AdaParsel))
        {
            AdaParsel = parent.AdaParsel?.Trim() ?? string.Empty;
        }

        if (string.IsNullOrWhiteSpace(YapiSahibi))
        {
            YapiSahibi = parent.YapiSahibi?.Trim() ?? string.Empty;
        }

        if (string.IsNullOrWhiteSpace(YibfNo))
        {
            YibfNo = parent.YibfNo?.Trim() ?? string.Empty;
        }

        if (string.IsNullOrWhiteSpace(Belediye))
        {
            Belediye = parent.Belediye?.Trim() ?? string.Empty;
        }

        if (string.IsNullOrWhiteSpace(Muteahhit))
        {
            var muteahhit = parent.Muteahhit?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(muteahhit))
            {
                muteahhit = parent.YapiSahibi?.Trim() ?? string.Empty;
            }

            Muteahhit = muteahhit;
        }
    }

    private void Save()
    {
        if (string.IsNullOrWhiteSpace(DisplayName))
        {
            ValidationMessage = "Görünen ad zorunludur.";
            return;
        }

        if (Kind == ProjectCatalogKind.Istinat && (ParentProjectId is null || ParentProjectId == Guid.Empty))
        {
            ValidationMessage = HasParentProjects
                ? "İstinat projeleri için üst proje seçilmelidir."
                : "İstinat için önce Normal bir üst proje eklenmelidir.";
            OnPropertyChanged(nameof(ParentSelectionHint));
            return;
        }

        ValidationMessage = string.Empty;
        RequestClose?.Invoke(this, true);
    }

    private static string NormalizeIstinatDisplayName(string displayName, ProjectCatalogKind kind)
    {
        if (kind == ProjectCatalogKind.Istinat
            && (string.IsNullOrWhiteSpace(displayName)
                || string.Equals(displayName.Trim(), "Istinat", StringComparison.OrdinalIgnoreCase)))
        {
            return "İstinat";
        }

        return displayName;
    }
}
