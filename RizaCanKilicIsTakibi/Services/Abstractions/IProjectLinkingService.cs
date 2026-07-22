using RizaCanKilicIsTakibi.Models;

namespace RizaCanKilicIsTakibi.Services.Abstractions;

public interface IProjectLinkingService
{
    ProjectLinkDryRunResult DryRun(
        IReadOnlyList<ProjectCatalogEntry> catalog,
        IReadOnlyList<KarotEntry> karot,
        IReadOnlyList<TadilatEntry> tadilat,
        IReadOnlyList<ActionEntry> action,
        IReadOnlyList<MissingProjectEntry> missing,
        IReadOnlyList<TaskItem> tasks,
        IReadOnlyList<YibfIsTakibiEntry> yibfIsTakibi);

    void Apply(
        IReadOnlyList<AutoProjectLinkAction> autoActions,
        IReadOnlyList<UnresolvedLinkResolution> userResolutions,
        IList<KarotEntry> karot,
        IList<TadilatEntry> tadilat,
        IList<ActionEntry> action,
        IList<MissingProjectEntry> missing,
        IList<TaskItem> tasks,
        IList<YibfIsTakibiEntry> yibfIsTakibi,
        IList<ProjectCatalogEntry> catalog);
}
