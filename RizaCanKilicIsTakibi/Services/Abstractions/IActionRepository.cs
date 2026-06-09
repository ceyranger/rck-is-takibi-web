namespace RizaCanKilicIsTakibi.Services.Abstractions;

public interface IActionRepository
{
    Task<IReadOnlyList<Models.ActionEntry>> GetByCategoryAsync(Models.ActionEntryCategory category, CancellationToken cancellationToken = default);
    Task SaveManyAsync(IEnumerable<Models.ActionEntry> entries, CancellationToken cancellationToken = default);
    Task AddAsync(Models.ActionEntry entry, CancellationToken cancellationToken = default);
    Task UpdateAsync(Models.ActionEntry entry, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task ReorderAsync(Models.ActionEntryCategory category, string district, IEnumerable<Guid> orderedIds, CancellationToken cancellationToken = default);
}
