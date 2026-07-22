using RizaCanKilicIsTakibi.Models;

namespace RizaCanKilicIsTakibi.Services.Abstractions;

public interface IProjectLinkResolveDialogService
{
    Task<IReadOnlyList<UnresolvedLinkResolution>?> ShowDialogAsync(
        IReadOnlyList<UnresolvedProjectLinkItem> unresolved,
        IReadOnlyList<ProjectCatalogEntry> catalog,
        CancellationToken cancellationToken = default);
}
