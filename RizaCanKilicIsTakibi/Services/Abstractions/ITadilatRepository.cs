using RizaCanKilicIsTakibi.Models;

namespace RizaCanKilicIsTakibi.Services.Abstractions;

public interface ITadilatRepository
{
    Task<IReadOnlyList<TadilatEntry>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TadilatCellState>> GetCellStatesAsync(CancellationToken cancellationToken = default);
    Task SaveManyAsync(IEnumerable<TadilatEntry> entries, IEnumerable<TadilatCellState> cellStates, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
