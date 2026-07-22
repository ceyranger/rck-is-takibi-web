using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services.Abstractions;

namespace RizaCanKilicIsTakibi.Services;

public sealed class ProjectCatalogUiState : IProjectCatalogUiState
{
    private readonly object _gate = new();
    private IReadOnlyList<ProjectCatalogEntry> _entries = [];

    public IReadOnlyList<ProjectCatalogEntry> GetActiveEntries()
    {
        lock (_gate)
        {
            return _entries
                .Where(item => item.IsActive)
                .Select(item => item.Clone())
                .ToList();
        }
    }

    public void SetEntries(IEnumerable<ProjectCatalogEntry> entries)
    {
        lock (_gate)
        {
            _entries = entries.Select(item => item.Clone()).ToList();
        }
    }
}
