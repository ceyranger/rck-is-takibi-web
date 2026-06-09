using RizaCanKilicIsTakibi.Models;

namespace RizaCanKilicIsTakibi.Services.Abstractions;

public interface ISearchService
{
    IReadOnlyList<SearchResultItem> SearchAll(IEnumerable<SearchResultItem> items, string query, SearchScope scope = SearchScope.All);
}
