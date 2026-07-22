using RizaCanKilicIsTakibi.Models;

namespace RizaCanKilicIsTakibi.Services.Abstractions;

public interface IProjectCatalogService
{
    Task<IReadOnlyList<ProjectCatalogEntry>> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(IEnumerable<ProjectCatalogEntry> entries, CancellationToken cancellationToken = default);
    IReadOnlyList<ProjectCatalogEntry> Search(IEnumerable<ProjectCatalogEntry> source, string? query);
    IReadOnlyList<ProjectCatalogEntry> BuildSeedFromAnaBilgi(IEnumerable<YibfAnaBilgiEntry> anaBilgi);
    ProjectCatalogFanOutResult BuildFanOut(ProjectCatalogEntry entry);
    void ApplyProjectSelection(KarotEntry entry, ProjectCatalogEntry project);
    void ApplyProjectSelection(TadilatEntry entry, ProjectCatalogEntry project);
    void ApplyProjectSelection(ActionEntry entry, ProjectCatalogEntry project);
    void ApplyProjectSelection(MissingProjectEntry entry, ProjectCatalogEntry project);
    void ApplyProjectSelection(TaskItem entry, ProjectCatalogEntry project);
    void ApplyProjectSelection(YibfIsTakibiEntry entry, ProjectCatalogEntry project);
    void ApplyProjectSelection(YibfAnaBilgiEntry entry, ProjectCatalogEntry project);
}
