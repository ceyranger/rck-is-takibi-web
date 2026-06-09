namespace RizaCanKilicIsTakibi.Models;

public sealed class YibfImportData
{
    public IReadOnlyList<YibfAnaBilgiEntry> AnaBilgiEntries { get; init; } = Array.Empty<YibfAnaBilgiEntry>();
    public IReadOnlyList<YibfAnaBilgiEvent> AnaBilgiEvents { get; init; } = Array.Empty<YibfAnaBilgiEvent>();
    public IReadOnlyList<YibfIsTakibiEntry> IsTakibiEntries { get; init; } = Array.Empty<YibfIsTakibiEntry>();
    public IReadOnlyList<YibfCellState> CellStates { get; init; } = Array.Empty<YibfCellState>();
}