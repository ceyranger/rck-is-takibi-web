using CommunityToolkit.Mvvm.Input;
using RizaCanKilicIsTakibi.Helpers;
using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services.Abstractions;
using System.Collections.ObjectModel;

namespace RizaCanKilicIsTakibi.ViewModels;

public sealed class ProjectLinkResolveItemViewModel : ViewModelBase
{
    private UnresolvedLinkResolutionKind _resolutionKind = UnresolvedLinkResolutionKind.Skip;
    private Guid? _selectedProjectId;
    private string _catalogSearchQuery = string.Empty;

    public ProjectLinkResolveItemViewModel(
        UnresolvedProjectLinkItem source,
        IReadOnlyList<ProjectCatalogEntry> catalog,
        IProjectCatalogService catalogService)
    {
        Source = source;
        ModuleLabel = source.Module switch
        {
            ProjectLinkSourceModule.Karot => "Karot",
            ProjectLinkSourceModule.Tadilat => "Tadilat",
            ProjectLinkSourceModule.Aksiyon => "Aksiyon",
            ProjectLinkSourceModule.EksikProje => "Eksik Proje",
            ProjectLinkSourceModule.GenelIs => "Genel İş",
            ProjectLinkSourceModule.YibfIsTakibi => "YİBF İş Takibi",
            _ => source.Module.ToString()
        };

        AvailableKinds =
        [
            new ResolutionOption(UnresolvedLinkResolutionKind.Skip, "Atla"),
            new ResolutionOption(UnresolvedLinkResolutionKind.LinkToProject, "Projeye bağla"),
            new ResolutionOption(UnresolvedLinkResolutionKind.CreateCatalogAndLink, "Katalog oluştur ve bağla")
        ];

        if (source.Module == ProjectLinkSourceModule.GenelIs)
        {
            AvailableKinds.Add(new ResolutionOption(UnresolvedLinkResolutionKind.MarkSpecialJob, "Özel iş olarak işaretle"));
        }

        Candidates = new ObservableCollection<ProjectLinkCandidate>(source.Candidates);
        _catalogService = catalogService;
        _catalog = catalog;
        ResolutionKind = Candidates.Count > 0 ? UnresolvedLinkResolutionKind.LinkToProject : UnresolvedLinkResolutionKind.Skip;
        SelectedProjectId = Candidates.FirstOrDefault()?.ProjectId;
        RefreshCatalogMatches();
    }

    private readonly IProjectCatalogService _catalogService;
    private readonly IReadOnlyList<ProjectCatalogEntry> _catalog;

    public UnresolvedProjectLinkItem Source { get; }
    public string ModuleLabel { get; }
    public ObservableCollection<ProjectLinkCandidate> Candidates { get; }
    public ObservableCollection<ResolutionOption> AvailableKinds { get; }
    public ObservableCollection<ProjectCatalogEntry> CatalogMatches { get; } = [];

    public string SummaryText => Source.SummaryText;
    public string AdaParsel => Source.AdaParsel;
    public string YapiSahibi => Source.YapiSahibi;

    public UnresolvedLinkResolutionKind ResolutionKind
    {
        get => _resolutionKind;
        set => SetProperty(ref _resolutionKind, value);
    }

    public Guid? SelectedProjectId
    {
        get => _selectedProjectId;
        set => SetProperty(ref _selectedProjectId, value);
    }

    public string CatalogSearchQuery
    {
        get => _catalogSearchQuery;
        set
        {
            if (SetProperty(ref _catalogSearchQuery, value))
            {
                RefreshCatalogMatches();
            }
        }
    }

    public bool ShowProjectSelectors => ResolutionKind == UnresolvedLinkResolutionKind.LinkToProject;

    public bool HasBestCandidate => Candidates.Count > 0;

    public ProjectLinkCandidate? BestCandidate
        => Candidates
            .OrderByDescending(candidate => candidate.Score)
            .ThenBy(candidate => candidate.Kind == ProjectCatalogKind.Normal ? 0 : 1)
            .ThenBy(candidate => candidate.DisplayName)
            .FirstOrDefault();

    public void ApplyBestCandidate()
    {
        var best = BestCandidate;
        if (best is null)
        {
            return;
        }

        ResolutionKind = UnresolvedLinkResolutionKind.LinkToProject;
        SelectedProjectId = best.ProjectId;
    }

    public UnresolvedLinkResolution BuildResolution(ProjectCatalogEntry? createdEntry = null)
        => ResolutionKind switch
        {
            UnresolvedLinkResolutionKind.LinkToProject => new UnresolvedLinkResolution
            {
                EntryId = Source.EntryId,
                Module = Source.Module,
                Kind = ResolutionKind,
                ProjectId = SelectedProjectId
            },
            UnresolvedLinkResolutionKind.CreateCatalogAndLink when createdEntry is not null => new UnresolvedLinkResolution
            {
                EntryId = Source.EntryId,
                Module = Source.Module,
                Kind = ResolutionKind,
                ProjectId = createdEntry.Id,
                NewCatalogEntry = createdEntry
            },
            UnresolvedLinkResolutionKind.MarkSpecialJob => new UnresolvedLinkResolution
            {
                EntryId = Source.EntryId,
                Module = Source.Module,
                Kind = ResolutionKind
            },
            _ => new UnresolvedLinkResolution
            {
                EntryId = Source.EntryId,
                Module = Source.Module,
                Kind = UnresolvedLinkResolutionKind.Skip
            }
        };

    public ProjectCatalogEntry BuildSuggestedCatalogEntry()
        => new()
        {
            Id = Guid.NewGuid(),
            DisplayName = StringHelpers.FirstNonEmpty(Source.SummaryText, Source.AdaParsel, Source.YapiSahibi, "Yeni Proje"),
            AdaParsel = Source.AdaParsel ?? string.Empty,
            YapiSahibi = Source.YapiSahibi ?? string.Empty,
            Kind = ProjectCatalogKind.Normal,
            IsActive = true,
            DisplayOrder = _catalog.Count,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

    public void RefreshCatalogMatches()
    {
        CatalogMatches.Clear();
        foreach (var item in _catalogService.Search(_catalog.Where(item => item.IsActive), CatalogSearchQuery))
        {
            CatalogMatches.Add(item);
        }
    }

    public sealed record ResolutionOption(UnresolvedLinkResolutionKind Kind, string Label);
}
