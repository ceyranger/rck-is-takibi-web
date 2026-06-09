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
    public required string Summary { get; init; }
    public required string SearchText { get; init; }
    public string RawSearchText { get; init; } = string.Empty;
    public string MatchOriginLabel { get; init; } = string.Empty;

    public string AutomationId => $"SearchResult_{Kind}_{ItemId:N}";

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
