namespace RizaCanKilicIsTakibi.Models;

public sealed class BackupRestoreData
{
    public IReadOnlyList<TaskItem> Tasks { get; init; } = Array.Empty<TaskItem>();
    public IReadOnlyList<QuickTaskTemplate> QuickTaskTemplates { get; init; } = Array.Empty<QuickTaskTemplate>();
    public IReadOnlyList<ActionEntry> ActionEntries { get; init; } = Array.Empty<ActionEntry>();
    public IReadOnlyList<MissingProjectEntry> MissingProjectEntries { get; init; } = Array.Empty<MissingProjectEntry>();
    public IReadOnlyList<MissingProjectCellState> MissingProjectCellStates { get; init; } = Array.Empty<MissingProjectCellState>();
    public IReadOnlyList<KarotEntry> KarotEntries { get; init; } = Array.Empty<KarotEntry>();
    public IReadOnlyList<KarotCellState> KarotCellStates { get; init; } = Array.Empty<KarotCellState>();
    public IReadOnlyList<TadilatEntry> TadilatEntries { get; init; } = Array.Empty<TadilatEntry>();
    public IReadOnlyList<TadilatCellState> TadilatCellStates { get; init; } = Array.Empty<TadilatCellState>();
    public IReadOnlyList<YibfAnaBilgiEntry> YibfAnaBilgiEntries { get; init; } = Array.Empty<YibfAnaBilgiEntry>();
    public IReadOnlyList<YibfAnaBilgiEvent> YibfAnaBilgiEvents { get; init; } = Array.Empty<YibfAnaBilgiEvent>();
    public IReadOnlyList<YibfIsTakibiEntry> YibfIsTakibiEntries { get; init; } = Array.Empty<YibfIsTakibiEntry>();
    public IReadOnlyList<YibfCellState> YibfCellStates { get; init; } = Array.Empty<YibfCellState>();
    public IReadOnlyList<ProjectCatalogEntry> ProjectCatalogEntries { get; init; } = Array.Empty<ProjectCatalogEntry>();
    public IReadOnlyList<Personnel> Personnel { get; init; } = Array.Empty<Personnel>();
    public IReadOnlyList<PersonnelAssignment> PersonnelAssignments { get; init; } = Array.Empty<PersonnelAssignment>();
}
