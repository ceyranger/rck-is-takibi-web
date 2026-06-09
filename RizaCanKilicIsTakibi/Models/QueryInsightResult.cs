namespace RizaCanKilicIsTakibi.Models;

public sealed class QueryInsightResult
{
    public string MatchedKey { get; init; } = string.Empty;
    public string SummaryText { get; init; } = string.Empty;
    public string ExplanationText { get; init; } = string.Empty;
    public string AnswerText { get; init; } = string.Empty;
    public IReadOnlyList<QueryInsightSection> Sections { get; init; } = Array.Empty<QueryInsightSection>();
    public IReadOnlyList<SearchResultItem> Sources { get; init; } = Array.Empty<SearchResultItem>();
}
