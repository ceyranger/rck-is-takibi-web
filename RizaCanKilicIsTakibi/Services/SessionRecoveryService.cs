using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services.Abstractions;
using System.IO;

namespace RizaCanKilicIsTakibi.Services;

public sealed class SessionRecoveryService : ISessionRecoveryService
{
    private readonly string _pendingRecoveryPath;
    private readonly string _sessionDirtyFlagPath;
    private readonly IBackupService _backupService;
    private readonly object _sync = new();
    private Func<Task>? _flushCallback;

    public SessionRecoveryService(PathService pathService, IBackupService backupService)
        : this(pathService.PendingRecoveryPath, pathService.SessionDirtyFlagPath, backupService)
    {
    }

    public SessionRecoveryService(string pendingRecoveryPath, string sessionDirtyFlagPath, IBackupService backupService)
    {
        _pendingRecoveryPath = pendingRecoveryPath;
        _sessionDirtyFlagPath = sessionDirtyFlagPath;
        _backupService = backupService;
    }

    public bool IsPendingRecoveryAvailable()
        => File.Exists(_pendingRecoveryPath);

    public DateTime? GetPendingRecoveryTimestamp()
    {
        try
        {
            if (!File.Exists(_pendingRecoveryPath))
            {
                return null;
            }

            return File.GetLastWriteTime(_pendingRecoveryPath);
        }
        catch
        {
            return null;
        }
    }

    public void MarkDirtySession()
    {
        try
        {
            var directory = Path.GetDirectoryName(_sessionDirtyFlagPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(_sessionDirtyFlagPath, DateTime.Now.ToString("O"));
        }
        catch
        {
        }
    }

    public void ClearPendingRecovery()
    {
        TryDelete(_sessionDirtyFlagPath);
        TryDelete(_pendingRecoveryPath);
    }

    public async Task WriteRecoverySnapshotAsync(
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
        IEnumerable<Personnel>? personnel = null,
        IEnumerable<PersonnelAssignment>? personnelAssignments = null,
        CancellationToken cancellationToken = default)
    {
        MarkDirtySession();
        await _backupService.CreateBackupAsync(
            tasks,
            backupPath: _pendingRecoveryPath,
            actionEntries: actionEntries,
            missingProjectEntries: missingProjectEntries,
            missingProjectCellStates: missingProjectCellStates,
            karotEntries: karotEntries,
            karotCellStates: karotCellStates,
            tadilatEntries: tadilatEntries,
            yibfAnaBilgiEntries: yibfAnaBilgiEntries,
            yibfAnaBilgiEvents: yibfAnaBilgiEvents,
            yibfIsTakibiEntries: yibfIsTakibiEntries,
            yibfCellStates: yibfCellStates,
            tadilatCellStates: tadilatCellStates,
            quickTaskTemplates: quickTaskTemplates,
            projectCatalogEntries: projectCatalogEntries,
            personnel: personnel,
            personnelAssignments: personnelAssignments,
            cancellationToken: cancellationToken);
    }

    public async Task<BackupRestoreData?> LoadPendingRecoveryAsync(CancellationToken cancellationToken = default)
    {
        if (!IsPendingRecoveryAvailable())
        {
            return null;
        }

        try
        {
            return await _backupService.RestoreBackupAsync(_pendingRecoveryPath, cancellationToken);
        }
        catch
        {
            return null;
        }
    }

    public void RegisterFlushCallback(Func<Task>? callback)
    {
        lock (_sync)
        {
            _flushCallback = callback;
        }
    }

    public void TryFlushBestEffort()
    {
        Func<Task>? callback;
        lock (_sync)
        {
            callback = _flushCallback;
        }

        if (callback is null)
        {
            return;
        }

        try
        {
            callback().GetAwaiter().GetResult();
        }
        catch
        {
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
        }
    }
}
