using RizaCanKilicIsTakibi.Models;

namespace RizaCanKilicIsTakibi.Services.Abstractions;

public interface IContextInsightBuilder
{
    QueryInsightResult Build(
        ContextQueryMatch match,
        IReadOnlyList<SearchResultItem> corpus,
        IEnumerable<TaskItem> tasks,
        IEnumerable<ActionEntry> actionEntries,
        IEnumerable<MissingProjectEntry> missingProjectEntries,
        IEnumerable<KarotEntry> karotEntries,
        IEnumerable<TadilatEntry> aktifTadilatEntries,
        IEnumerable<TadilatCellState> tadilatCellStates,
        IEnumerable<YibfAnaBilgiEntry> yibfAnaBilgiEntries,
        IEnumerable<YibfAnaBilgiEvent> yibfAnaBilgiEvents,
        IEnumerable<YibfIsTakibiEntry> yibfIsTakibiEntries,
        IEnumerable<YibfCellState> yibfCellStates);
}
