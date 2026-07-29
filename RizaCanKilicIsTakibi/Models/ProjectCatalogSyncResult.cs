namespace RizaCanKilicIsTakibi.Models;

public sealed class ProjectCatalogSyncResult
{
    public int KarotCount { get; init; }
    public int MissingProjectCount { get; init; }
    public int ActionCount { get; init; }
    public int TadilatCount { get; init; }
    public int YibfIsTakibiCount { get; init; }

    public int TotalCount
        => KarotCount + MissingProjectCount + ActionCount + TadilatCount + YibfIsTakibiCount;
}
