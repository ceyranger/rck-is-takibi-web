namespace RizaCanKilicIsTakibi.Models;

public sealed class QueryInsightSection
{
    public string Title { get; init; } = string.Empty;
    public IReadOnlyList<string> Items { get; init; } = Array.Empty<string>();
    public int SourceCount { get; init; }
}
