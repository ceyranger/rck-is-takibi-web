using RizaCanKilicIsTakibi.Models;

namespace RizaCanKilicIsTakibi.Services.Abstractions;

public interface IBackupService
{
    Task<BackupMetadata> CreateBackupAsync(
        IEnumerable<TaskItem> tasks,
        string? backupPath = null,
        IEnumerable<ActionEntry>? actionEntries = null,
        IEnumerable<MissingProjectEntry>? missingProjectEntries = null,
        IEnumerable<MissingProjectCellState>? missingProjectCellStates = null,
        IEnumerable<KarotEntry>? karotEntries = null,
        IEnumerable<KarotCellState>? karotCellStates = null,
        IEnumerable<TadilatEntry>? tadilatEntries = null,
        IEnumerable<YibfAnaBilgiEntry>? yibfAnaBilgiEntries = null,
        IEnumerable<YibfAnaBilgiEvent>? yibfAnaBilgiEvents = null,
        IEnumerable<YibfIsTakibiEntry>? yibfIsTakibiEntries = null,
        IEnumerable<YibfCellState>? yibfCellStates = null,
        IEnumerable<TadilatCellState>? tadilatCellStates = null,
        IEnumerable<QuickTaskTemplate>? quickTaskTemplates = null,
        IEnumerable<ProjectCatalogEntry>? projectCatalogEntries = null,
        IEnumerable<Personnel>? personnel = null,
        IEnumerable<PersonnelAssignment>? personnelAssignments = null,
        CancellationToken cancellationToken = default);

    Task<BackupRestoreData> RestoreBackupAsync(string backupPath, CancellationToken cancellationToken = default);

    void ScheduleAutoBackup(TimeSpan interval, Func<Task> callback);
    void StopAutoBackup();
    Task<int> ClearManagedBackupsAsync(CancellationToken cancellationToken = default);
    Task<int> CleanOldBackupsAsync(int keepCount = 30, CancellationToken cancellationToken = default);
}
