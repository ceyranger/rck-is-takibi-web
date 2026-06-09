using RizaCanKilicIsTakibi.Models;

namespace RizaCanKilicIsTakibi.Services.Abstractions;

public interface IContextQueryService
{
    ContextQueryMatch ExtractMatch(string question, IEnumerable<SearchResultItem> corpus);
}
