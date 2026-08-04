using RizaCanKilicIsTakibi.Models;

namespace RizaCanKilicIsTakibi.Services.Abstractions;

public interface IProjectCatalogService
{
    Task<IReadOnlyList<ProjectCatalogEntry>> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(IEnumerable<ProjectCatalogEntry> entries, CancellationToken cancellationToken = default);
    IReadOnlyList<ProjectCatalogEntry> Search(IEnumerable<ProjectCatalogEntry> source, string? query);
    IReadOnlyList<ProjectCatalogEntry> BuildSeedFromAnaBilgi(IEnumerable<YibfAnaBilgiEntry> anaBilgi);
    ProjectCatalogFanOutResult BuildFanOut(ProjectCatalogEntry entry);
    void ApplyProjectSelection(KarotEntry entry, ProjectCatalogEntry project, IEnumerable<ProjectCatalogEntry>? catalog = null);
    void ApplyProjectSelection(TadilatEntry entry, ProjectCatalogEntry project, IEnumerable<ProjectCatalogEntry>? catalog = null);
    void ApplyProjectSelection(ActionEntry entry, ProjectCatalogEntry project, IEnumerable<ProjectCatalogEntry>? catalog = null);
    void ApplyProjectSelection(MissingProjectEntry entry, ProjectCatalogEntry project, IEnumerable<ProjectCatalogEntry>? catalog = null);
    void ApplyProjectSelection(TaskItem entry, ProjectCatalogEntry project);
    void ApplyProjectSelection(YibfIsTakibiEntry entry, ProjectCatalogEntry project, IEnumerable<ProjectCatalogEntry>? catalog = null);
    void ApplyProjectSelection(YibfAnaBilgiEntry entry, ProjectCatalogEntry project);
    ProjectCatalogSyncResult PreviewLinkedIdentityOverwrite(
        ProjectCatalogEntry project,
        IReadOnlyList<KarotEntry> karot,
        IReadOnlyList<MissingProjectEntry> missing,
        IReadOnlyList<ActionEntry> action,
        IReadOnlyList<TadilatEntry> tadilat,
        IReadOnlyList<YibfIsTakibiEntry> yibfIsTakibi);
    ProjectCatalogSyncResult OverwriteLinkedIdentityFields(
        ProjectCatalogEntry project,
        IReadOnlyList<KarotEntry> karot,
        IReadOnlyList<MissingProjectEntry> missing,
        IReadOnlyList<ActionEntry> action,
        IReadOnlyList<TadilatEntry> tadilat,
        IReadOnlyList<YibfIsTakibiEntry> yibfIsTakibi);
}
