using RizaCanKilicIsTakibi.Models;

namespace RizaCanKilicIsTakibi.Services.Abstractions;

public interface ISessionRecoveryService
{
    bool IsPendingRecoveryAvailable();
    DateTime? GetPendingRecoveryTimestamp();
    void MarkDirtySession();
    void ClearPendingRecovery();
    Task WriteRecoverySnapshotAsync(
        IEnumerable<TaskItem> tasks,
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
        CancellationToken cancellationToken = default);
    Task<BackupRestoreData?> LoadPendingRecoveryAsync(CancellationToken cancellationToken = default);
    void RegisterFlushCallback(Func<Task>? callback);
    void TryFlushBestEffort();
}
