using RizaCanKilicIsTakibi.Models;

namespace RizaCanKilicIsTakibi.Services.Abstractions;

public interface IMissingProjectRepository
{
    Task<IReadOnlyList<MissingProjectEntry>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MissingProjectCellState>> GetCellStatesAsync(CancellationToken cancellationToken = default);
    Task AddAsync(MissingProjectEntry entry, CancellationToken cancellationToken = default);
    Task UpdateAsync(MissingProjectEntry entry, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task SaveManyAsync(IEnumerable<MissingProjectEntry> entries, IEnumerable<MissingProjectCellState> cellStates, CancellationToken cancellationToken = default);
}
