using RizaCanKilicIsTakibi.Helpers;
using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services.Abstractions;

namespace RizaCanKilicIsTakibi.Services;

public sealed class SearchService : ISearchService
{
    private const int MaxResults = 60;

    public IReadOnlyList<SearchResultItem> SearchAll(IEnumerable<SearchResultItem> items, string query, SearchScope scope = SearchScope.All)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Array.Empty<SearchResultItem>();
        }

        var normalized = SearchTextNormalizer.Normalize(query);

        return items
            .Where(item => MatchesScope(item, scope) && Contains(item.SearchText, normalized))
            .Select(item => new
            {
                Item = item,
                Score = GetScore(item, normalized)
            })
            .OrderBy(x => x.Score)
            .ThenBy(x => x.Item.BoardLabel)
            .ThenBy(x => x.Item.Title)
            .Take(MaxResults)
            .Select(x => x.Item)
            .ToList();
    }

    private static bool Contains(string source, string query)
        => SearchTextNormalizer.Contains(source, query);

    private static bool MatchesScope(SearchResultItem item, SearchScope scope)
        => scope == SearchScope.All || item.TargetTab == MapScope(scope);

    private static MainNavigationTab MapScope(SearchScope scope)
        => scope switch
        {
            SearchScope.GenelIsTakibi => MainNavigationTab.GenelIsTakibi,
            SearchScope.Aksiyon => MainNavigationTab.Aksiyon,
            SearchScope.EksikProje => MainNavigationTab.EksikProje,
            SearchScope.KarotTakibi => MainNavigationTab.KarotTakibi,
            SearchScope.TadilatTakibi => MainNavigationTab.TadilatTakibi,
            SearchScope.YibfAnaBilgi => MainNavigationTab.YibfAnaBilgi,
            SearchScope.YibfIsTakibi => MainNavigationTab.YibfIsTakibi,
            _ => MainNavigationTab.GenelIsTakibi
        };

    private static int GetScore(SearchResultItem item, string query)
    {
        if (Equals(item.Title, query))
        {
            return 0;
        }

        if (StartsWith(item.Title, query))
        {
            return 1;
        }

        if (Contains(item.Title, query))
        {
            return 2;
        }

        if (StartsWith(item.Summary, query))
        {
            return 3;
        }

        if (Contains(item.Summary, query))
        {
            return 4;
        }

        if (StartsWith(item.SearchText, query))
        {
            return 5;
        }

        return 6;
    }

    private static bool StartsWith(string source, string query)
        => SearchTextNormalizer.StartsWith(source, query);

    private static bool Equals(string source, string query)
        => SearchTextNormalizer.EqualsNormalized(source, query);
}
