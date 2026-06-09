using RizaCanKilicIsTakibi.Models;

namespace RizaCanKilicIsTakibi.Services.Abstractions;

public interface IKarotRepository
{
    Task<IReadOnlyList<KarotEntry>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<KarotCellState>> GetCellStatesAsync(CancellationToken cancellationToken = default);
    Task SaveManyAsync(IEnumerable<KarotEntry> entries, IEnumerable<KarotCellState> cellStates, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
