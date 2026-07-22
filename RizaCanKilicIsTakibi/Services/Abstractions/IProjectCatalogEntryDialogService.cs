using RizaCanKilicIsTakibi.Models;

namespace RizaCanKilicIsTakibi.Services.Abstractions;

public interface IProjectCatalogEntryDialogService
{
    Task<ProjectCatalogEntry?> ShowDialogAsync(
        ProjectCatalogEntry? existing,
        IReadOnlyList<ProjectCatalogEntry> catalog,
        CancellationToken cancellationToken = default);
}
