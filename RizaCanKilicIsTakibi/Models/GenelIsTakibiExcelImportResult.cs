namespace RizaCanKilicIsTakibi.Models;

public sealed class GenelIsTakibiExcelImportResult
{
    public IReadOnlyList<TaskItem> Tasks { get; init; } = [];
    public IReadOnlyList<ActionEntry> ActionEntries { get; init; } = [];
    public IReadOnlyList<MissingProjectEntry> MissingProjectEntries { get; init; } = [];

    public int UrgentTaskCount => Tasks.Count(item => item.BoardType == TaskBoardType.Acil);
    public int GeneralTaskCount => Tasks.Count(item => item.BoardType == TaskBoardType.Genel);
    public int ActionEntryCount => ActionEntries.Count(item => item.Category == ActionEntryCategory.Aksiyon);
    public int ActionToAddEntryCount => ActionEntries.Count(item => item.Category == ActionEntryCategory.AksiyonaEklenecekler);
    public int MissingProjectEntryCount => MissingProjectEntries.Count;
    public int TotalCount => Tasks.Count + ActionEntries.Count + MissingProjectEntries.Count;
}
