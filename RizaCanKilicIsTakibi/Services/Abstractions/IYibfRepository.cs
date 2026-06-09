using RizaCanKilicIsTakibi.Models;

namespace RizaCanKilicIsTakibi.Services.Abstractions;

public interface IYibfRepository
{
    Task<IReadOnlyList<YibfAnaBilgiEntry>> GetAnaBilgiEntriesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<YibfAnaBilgiEvent>> GetAnaBilgiEventsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<YibfIsTakibiEntry>> GetIsTakibiEntriesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<YibfCellState>> GetCellStatesAsync(CancellationToken cancellationToken = default);
    Task SaveManyAsync(
        IEnumerable<YibfAnaBilgiEntry> anaBilgiEntries,
        IEnumerable<YibfAnaBilgiEvent> anaBilgiEvents,
        IEnumerable<YibfIsTakibiEntry> isTakibiEntries,
        IEnumerable<YibfCellState> cellStates,
        CancellationToken cancellationToken = default);
    Task DeleteIsTakibiAsync(Guid id, CancellationToken cancellationToken = default);
}