using RizaCanKilicIsTakibi.Models;

namespace RizaCanKilicIsTakibi.Services.Abstractions;

public interface IProjectCatalogUiState
{
    IReadOnlyList<ProjectCatalogEntry> GetActiveEntries();
    void SetEntries(IEnumerable<ProjectCatalogEntry> entries);
}
