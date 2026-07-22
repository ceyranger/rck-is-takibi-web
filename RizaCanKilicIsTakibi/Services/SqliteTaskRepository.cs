using Microsoft.Data.Sqlite;
using RizaCanKilicIsTakibi.Helpers;
using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services.Abstractions;
using System.IO;

namespace RizaCanKilicIsTakibi.Services;

public sealed class SqliteTaskRepository : ITaskRepository
{
    private readonly string _databasePath;
    private readonly string _connectionString;

    public SqliteTaskRepository(string databasePath)
    {
        _databasePath = databasePath;
        _connectionString = SqliteConnectionSettings.BuildConnectionString(_databasePath);
        EnsureDatabase();
    }

    internal string DatabasePath => _databasePath;

    public async Task<IReadOnlyList<TaskItem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var result = new List<TaskItem>();
        var map = new Dictionary<Guid, TaskItem>();

        await using var connection = await SqliteConnectionSettings.OpenAsync(_connectionString, cancellationToken);

        await using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = @"
SELECT Id, Title, Description, DueDate, CreatedAt, UpdatedAt, BoardType, SortOrder, ProjectId, IsSpecialJob
FROM Tasks
WHERE IsDeleted = 0
ORDER BY BoardType, SortOrder, UpdatedAt DESC;";

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var item = new TaskItem
                {
                    Id = Guid.Parse(reader.GetString(0)),
                    Title = reader.GetString(1),
                    Description = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                    DueDate = reader.IsDBNull(3) ? null : DateTime.Parse(reader.GetString(3)),
                    CreatedAt = DateTime.Parse(reader.GetString(4)),
                    UpdatedAt = DateTime.Parse(reader.GetString(5)),
                    BoardType = (TaskBoardType)reader.GetInt32(6),
                    SortOrder = reader.GetInt32(7),
                    ProjectId = SqliteGuidHelper.ParseNullable(reader.IsDBNull(8) ? null : reader.GetString(8)),
                    IsSpecialJob = !reader.IsDBNull(9) && reader.GetInt32(9) != 0
                };

                result.Add(item);
                map[item.Id] = item;
            }
        }

        await using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = @"
SELECT Id, TaskId, Text, CreatedAt
FROM TaskNotes
ORDER BY CreatedAt;";

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var taskId = Guid.Parse(reader.GetString(1));
                if (!map.TryGetValue(taskId, out var task))
                {
                    continue;
                }

                task.Notes.Add(new TaskNote
                {
                    Id = Guid.Parse(reader.GetString(0)),
                    Text = reader.GetString(2),
                    CreatedAt = DateTime.Parse(reader.GetString(3))
                });
            }
        }

        return result;
    }

    public async Task SaveAsync(TaskItem item, CancellationToken cancellationToken = default)
    {
        await using var connection = await SqliteConnectionSettings.OpenAsync(_connectionString, cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        await UpsertTaskAsync(connection, transaction, item, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task SaveManyAsync(IEnumerable<TaskItem> items, CancellationToken cancellationToken = default)
    {
        await using var connection = await SqliteConnectionSettings.OpenAsync(_connectionString, cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        foreach (var item in items)
        {
            await UpsertTaskAsync(connection, transaction, item, cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await SqliteConnectionSettings.OpenAsync(_connectionString, cancellationToken);

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "UPDATE Tasks SET IsDeleted = 1, UpdatedAt = $updatedAt WHERE Id = $id;";
        cmd.Parameters.AddWithValue("$updatedAt", DateTime.Now.ToString("O"));
        cmd.Parameters.AddWithValue("$id", id.ToString());
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task ReorderAsync(TaskBoardType boardType, IEnumerable<Guid> orderedIds, CancellationToken cancellationToken = default)
    {
        await using var connection = await SqliteConnectionSettings.OpenAsync(_connectionString, cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        var index = 0;
        foreach (var id in orderedIds)
        {
            await using var cmd = connection.CreateCommand();
            cmd.Transaction = transaction;
            cmd.CommandText = @"
UPDATE Tasks
SET SortOrder = $sortOrder, UpdatedAt = $updatedAt
WHERE Id = $id AND BoardType = $boardType;";
            cmd.Parameters.AddWithValue("$sortOrder", index++);
            cmd.Parameters.AddWithValue("$updatedAt", DateTime.Now.ToString("O"));
            cmd.Parameters.AddWithValue("$id", id.ToString());
            cmd.Parameters.AddWithValue("$boardType", (int)boardType);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task MoveBoardAsync(Guid id, TaskBoardType boardType, int newSortOrder, CancellationToken cancellationToken = default)
    {
        await using var connection = await SqliteConnectionSettings.OpenAsync(_connectionString, cancellationToken);

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
UPDATE Tasks
SET BoardType = $boardType, SortOrder = $sortOrder, UpdatedAt = $updatedAt
WHERE Id = $id;";
        cmd.Parameters.AddWithValue("$boardType", (int)boardType);
        cmd.Parameters.AddWithValue("$sortOrder", newSortOrder);
        cmd.Parameters.AddWithValue("$updatedAt", DateTime.Now.ToString("O"));
        cmd.Parameters.AddWithValue("$id", id.ToString());
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private void EnsureDatabase()
    {
        var directory = Path.GetDirectoryName(_databasePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var connection = SqliteConnectionSettings.Open(_connectionString);

        using (var createCommand = connection.CreateCommand())
        {
            createCommand.CommandText = @"
CREATE TABLE IF NOT EXISTS Tasks (
    Id TEXT PRIMARY KEY,
    Title TEXT NOT NULL,
    Description TEXT NULL,
    DueDate TEXT NULL,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL,
    BoardType INTEGER NOT NULL,
    SortOrder INTEGER NOT NULL
);

CREATE TABLE IF NOT EXISTS TaskNotes (
    Id TEXT PRIMARY KEY,
    TaskId TEXT NOT NULL,
    Text TEXT NOT NULL,
    CreatedAt TEXT NOT NULL,
    FOREIGN KEY(TaskId) REFERENCES Tasks(Id) ON DELETE CASCADE
);";

            createCommand.ExecuteNonQuery();
        }

        EnsureTasksSchema(connection);
        SqliteConnectionSettings.EnsureColumnExists(connection, "Tasks", "IsDeleted", "INTEGER NOT NULL DEFAULT 0");
        SqliteConnectionSettings.EnsureColumnExists(connection, "Tasks", "ProjectId", "TEXT NOT NULL DEFAULT ''");
        SqliteConnectionSettings.EnsureColumnExists(connection, "Tasks", "IsSpecialJob", "INTEGER NOT NULL DEFAULT 0");
    }

    private static void EnsureTasksSchema(SqliteConnection connection)
    {
        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        using (var command = connection.CreateCommand())
        {
            command.CommandText = "PRAGMA table_info(Tasks);";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                columns.Add(reader.GetString(1));
            }
        }

        if (!columns.Contains("Status") && !columns.Contains("Priority") && !columns.Contains("IsStarred"))
        {
            return;
        }

        using var migrationCommand = connection.CreateCommand();
        migrationCommand.CommandText = @"
PRAGMA foreign_keys=OFF;
BEGIN TRANSACTION;

CREATE TABLE IF NOT EXISTS Tasks_New (
    Id TEXT PRIMARY KEY,
    Title TEXT NOT NULL,
    Description TEXT NULL,
    DueDate TEXT NULL,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL,
    BoardType INTEGER NOT NULL,
    SortOrder INTEGER NOT NULL
);

INSERT INTO Tasks_New (Id, Title, Description, DueDate, CreatedAt, UpdatedAt, BoardType, SortOrder)
SELECT Id, Title, Description, DueDate, CreatedAt, UpdatedAt, BoardType, SortOrder
FROM Tasks;

DROP TABLE Tasks;
ALTER TABLE Tasks_New RENAME TO Tasks;

COMMIT;
PRAGMA foreign_keys=ON;";

        migrationCommand.ExecuteNonQuery();
    }

    private static async Task UpsertTaskAsync(SqliteConnection connection, SqliteTransaction transaction, TaskItem item, CancellationToken cancellationToken)
    {
        var createdAt = item.CreatedAt == default ? DateTime.Now : item.CreatedAt;
        var updatedAt = DateTime.Now;

        await using (var taskCmd = connection.CreateCommand())
        {
            taskCmd.Transaction = transaction;
            taskCmd.CommandText = @"
INSERT INTO Tasks (Id, Title, Description, DueDate, CreatedAt, UpdatedAt, BoardType, SortOrder, ProjectId, IsSpecialJob)
VALUES ($id, $title, $description, $dueDate, $createdAt, $updatedAt, $boardType, $sortOrder, $projectId, $isSpecialJob)
ON CONFLICT(Id) DO UPDATE SET
    Title = excluded.Title,
    Description = excluded.Description,
    DueDate = excluded.DueDate,
    UpdatedAt = excluded.UpdatedAt,
    BoardType = excluded.BoardType,
    SortOrder = excluded.SortOrder,
    ProjectId = excluded.ProjectId,
    IsSpecialJob = excluded.IsSpecialJob,
    IsDeleted = 0;";

            taskCmd.Parameters.AddWithValue("$id", item.Id.ToString());
            taskCmd.Parameters.AddWithValue("$title", item.Title);
            taskCmd.Parameters.AddWithValue("$description", item.Description);
            taskCmd.Parameters.AddWithValue("$dueDate", item.DueDate?.ToString("O") ?? (object)DBNull.Value);
            taskCmd.Parameters.AddWithValue("$createdAt", createdAt.ToString("O"));
            taskCmd.Parameters.AddWithValue("$updatedAt", updatedAt.ToString("O"));
            taskCmd.Parameters.AddWithValue("$boardType", (int)item.BoardType);
            taskCmd.Parameters.AddWithValue("$sortOrder", item.SortOrder);
            taskCmd.Parameters.AddWithValue("$projectId", SqliteGuidHelper.ToDb(item.ProjectId));
            taskCmd.Parameters.AddWithValue("$isSpecialJob", item.IsSpecialJob ? 1 : 0);
            await taskCmd.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var deleteNotesCmd = connection.CreateCommand())
        {
            deleteNotesCmd.Transaction = transaction;
            deleteNotesCmd.CommandText = "DELETE FROM TaskNotes WHERE TaskId = $taskId;";
            deleteNotesCmd.Parameters.AddWithValue("$taskId", item.Id.ToString());
            await deleteNotesCmd.ExecuteNonQueryAsync(cancellationToken);
        }

        foreach (var note in item.Notes)
        {
            await using var noteCmd = connection.CreateCommand();
            noteCmd.Transaction = transaction;
            noteCmd.CommandText = @"
INSERT INTO TaskNotes (Id, TaskId, Text, CreatedAt)
VALUES ($id, $taskId, $text, $createdAt);";
            noteCmd.Parameters.AddWithValue("$id", note.Id.ToString());
            noteCmd.Parameters.AddWithValue("$taskId", item.Id.ToString());
            noteCmd.Parameters.AddWithValue("$text", note.Text);
            noteCmd.Parameters.AddWithValue("$createdAt", note.CreatedAt.ToString("O"));
            await noteCmd.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}
