namespace RizaCanKilicIsTakibi.Models;

public enum ProjectLinkSourceModule
{
    Karot,
    Tadilat,
    Aksiyon,
    EksikProje,
    GenelIs,
    YibfIsTakibi
}

public sealed class ProjectLinkCandidate
{
    public Guid ProjectId { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public ProjectCatalogKind Kind { get; init; }
    public int Score { get; init; }
    public bool HasAdaMatch { get; init; }
    public bool HasOwnerMatch { get; init; }
    public bool HasOwnerFirstWordsMatch { get; init; }
    public bool HasYibfMatch { get; init; }
}

public sealed class UnresolvedProjectLinkItem
{
    public ProjectLinkSourceModule Module { get; init; }
    public Guid EntryId { get; init; }
    public string SummaryText { get; init; } = string.Empty;
    public string AdaParsel { get; init; } = string.Empty;
    public string YapiSahibi { get; init; } = string.Empty;
    public IReadOnlyList<ProjectLinkCandidate> Candidates { get; init; } = [];
}

public sealed class ProjectLinkDryRunResult
{
    public int AutoLinkCount { get; init; }
    public int SpecialJobCount { get; init; }
    public int SkippedAlreadyLinkedCount { get; init; }
    public IReadOnlyList<UnresolvedProjectLinkItem> Unresolved { get; init; } = [];
    public IReadOnlyList<AutoProjectLinkAction> AutoActions { get; init; } = [];
}

public sealed class AutoProjectLinkAction
{
    public ProjectLinkSourceModule Module { get; init; }
    public Guid EntryId { get; init; }
    public Guid? ProjectId { get; init; }
    public bool MarkSpecialJob { get; init; }
}

public enum UnresolvedLinkResolutionKind
{
    Skip,
    LinkToProject,
    CreateCatalogAndLink,
    MarkSpecialJob
}

public sealed class UnresolvedLinkResolution
{
    public Guid EntryId { get; init; }
    public ProjectLinkSourceModule Module { get; init; }
    public UnresolvedLinkResolutionKind Kind { get; init; }
    public Guid? ProjectId { get; init; }
    public ProjectCatalogEntry? NewCatalogEntry { get; init; }
}
