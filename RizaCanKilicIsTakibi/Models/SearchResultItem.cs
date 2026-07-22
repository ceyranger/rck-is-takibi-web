namespace RizaCanKilicIsTakibi.Models;

public sealed class SearchResultItem
{
    public required SearchResultKind Kind { get; init; }
    public required MainNavigationTab TargetTab { get; init; }
    public required Guid ItemId { get; init; }
    public Guid? ParentItemId { get; init; }
    public TaskBoardType? BoardType { get; init; }
    public required string BoardLabel { get; init; }
    public required string Title { get; init; }
    public string Subtitle { get; init; } = string.Empty;
    public string MatchReason { get; init; } = string.Empty;
    public required string Summary { get; init; }
    public required string SearchText { get; init; }
    public string RawSearchText { get; init; } = string.Empty;
    public string MatchOriginLabel { get; init; } = string.Empty;

    public string AutomationId => $"SearchResult_{Kind}_{ItemId:N}";

    public string MatchReasonDisplay => string.IsNullOrWhiteSpace(MatchReason) ? BoardLabel : MatchReason;

    public string SubtitleLine
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(Subtitle))
            {
                return Subtitle;
            }

            return Kind switch
            {
                SearchResultKind.ActionEntry => Summary,
                SearchResultKind.MissingProjectEntry => Title,
                SearchResultKind.KarotEntry => Summary,
                SearchResultKind.TadilatEntry => BoardLabel,
                SearchResultKind.YibfAnaBilgiEntry => Summary,
                SearchResultKind.YibfAnaBilgiEvent => Summary,
                SearchResultKind.YibfIsTakibiEntry => Summary,
                SearchResultKind.GeneralTask => BoardLabel,
                _ => string.Empty
            };
        }
    }

    public string MatchSnippet
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Summary))
            {
                return string.Empty;
            }

            if (string.Equals(Summary, SubtitleLine, StringComparison.CurrentCultureIgnoreCase))
            {
                return string.IsNullOrWhiteSpace(MatchOriginLabel) ? string.Empty : MatchOriginLabel;
            }

            return Summary;
        }
    }

    public string KindLabel => Kind switch
    {
        SearchResultKind.GeneralTask => "Görev",
        SearchResultKind.ActionEntry => "Aksiyon",
        SearchResultKind.MissingProjectEntry => "Eksik",
        SearchResultKind.KarotEntry => "Karot",
        SearchResultKind.TadilatEntry => "Tadilat",
        SearchResultKind.YibfAnaBilgiEntry => "YİBF Kayıt",
        SearchResultKind.YibfAnaBilgiEvent => "Olay",
        SearchResultKind.YibfIsTakibiEntry => "İş Takibi",
        _ => "Kayıt"
    };
}
