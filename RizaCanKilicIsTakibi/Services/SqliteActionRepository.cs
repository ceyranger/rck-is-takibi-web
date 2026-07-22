using Microsoft.Data.Sqlite;
using RizaCanKilicIsTakibi.Helpers;
using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services.Abstractions;
using System.IO;

namespace RizaCanKilicIsTakibi.Services;

public sealed class SqliteActionRepository : IActionRepository
{
    private readonly string _databasePath;
    private readonly string _connectionString;

    public SqliteActionRepository(string databasePath)
    {
        _databasePath = databasePath;
        _connectionString = SqliteConnectionSettings.BuildConnectionString(_databasePath);
        EnsureDatabase();
    }

    public async Task<IReadOnlyList<ActionEntry>> GetByCategoryAsync(ActionEntryCategory category, CancellationToken cancellationToken = default)
    {
        var result = new List<ActionEntry>();

        await using var connection = await SqliteConnectionSettings.OpenAsync(_connectionString, cancellationToken);

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
SELECT Id, Category, District, OwnerParcelText, WorkText, DisplayOrder, CreatedAt, UpdatedAt, ProjectId
FROM ActionEntries
WHERE Category = $category AND IsDeleted = 0
ORDER BY District, DisplayOrder, UpdatedAt DESC;";
        cmd.Parameters.AddWithValue("$category", (int)category);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new ActionEntry
            {
                Id = Guid.Parse(reader.GetString(0)),
                Category = (ActionEntryCategory)reader.GetInt32(1),
                District = reader.GetString(2),
                OwnerParcelText = reader.GetString(3),
                WorkText = reader.GetString(4),
                DisplayOrder = reader.GetInt32(5),
                CreatedAt = DateTime.Parse(reader.GetString(6)),
                UpdatedAt = DateTime.Parse(reader.GetString(7)),
                ProjectId = SqliteGuidHelper.ParseNullable(reader.IsDBNull(8) ? null : reader.GetString(8))
            });
        }

        return result;
    }

    public async Task AddAsync(ActionEntry entry, CancellationToken cancellationToken = default)
    {
        if (entry.Id == Guid.Empty)
        {
            entry.Id = Guid.NewGuid();
        }

        entry.CreatedAt = DateTime.Now;
        entry.UpdatedAt = DateTime.Now;

        await using var connection = await SqliteConnectionSettings.OpenAsync(_connectionString, cancellationToken);

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
INSERT INTO ActionEntries (Id, Category, District, OwnerParcelText, WorkText, DisplayOrder, CreatedAt, UpdatedAt, ProjectId)
VALUES ($id, $category, $district, $ownerParcelText, $workText, $displayOrder, $createdAt, $updatedAt, $projectId);";
        BindCommonParameters(cmd, entry);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task SaveManyAsync(IEnumerable<ActionEntry> entries, CancellationToken cancellationToken = default)
    {
        var orderedEntries = entries
            .OrderBy(item => item.Category)
            .ThenBy(item => item.District, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.DisplayOrder)
            .ThenBy(item => item.UpdatedAt)
            .ToList();

        await using var connection = await SqliteConnectionSettings.OpenAsync(_connectionString, cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        var currentIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in orderedEntries)
        {
            if (entry.Id == Guid.Empty)
            {
                entry.Id = Guid.NewGuid();
            }

            currentIds.Add(entry.Id.ToString());
        }

        var existingIds = new List<string>();
        await using (var readIds = connection.CreateCommand())
        {
            readIds.Transaction = transaction;
            readIds.CommandText = "SELECT Id FROM ActionEntries;";
            await using var reader = await readIds.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                existingIds.Add(reader.GetString(0));
            }
        }

        foreach (var staleId in existingIds.Where(id => !currentIds.Contains(id)))
        {
            await using var deleteEntry = connection.CreateCommand();
            deleteEntry.Transaction = transaction;
            deleteEntry.CommandText = "UPDATE ActionEntries SET IsDeleted = 1, UpdatedAt = $updatedAt WHERE Id = $id;";
            deleteEntry.Parameters.AddWithValue("$updatedAt", DateTime.Now.ToString("O"));
            deleteEntry.Parameters.AddWithValue("$id", staleId);
            await deleteEntry.ExecuteNonQueryAsync(cancellationToken);
        }

        foreach (var entry in orderedEntries)
        {
            if (entry.CreatedAt == default)
            {
                entry.CreatedAt = DateTime.Now;
            }

            entry.UpdatedAt = DateTime.Now;

            await using var upsertEntry = connection.CreateCommand();
            upsertEntry.Transaction = transaction;
            upsertEntry.CommandText = @"
INSERT INTO ActionEntries (Id, Category, District, OwnerParcelText, WorkText, DisplayOrder, CreatedAt, UpdatedAt, ProjectId)
VALUES ($id, $category, $district, $ownerParcelText, $workText, $displayOrder, $createdAt, $updatedAt, $projectId)
ON CONFLICT(Id) DO UPDATE SET
    Category = excluded.Category,
    District = excluded.District,
    OwnerParcelText = excluded.OwnerParcelText,
    WorkText = excluded.WorkText,
    DisplayOrder = excluded.DisplayOrder,
    UpdatedAt = excluded.UpdatedAt,
    ProjectId = excluded.ProjectId,
    IsDeleted = 0;";
            BindCommonParameters(upsertEntry, entry);
            await upsertEntry.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task UpdateAsync(ActionEntry entry, CancellationToken cancellationToken = default)
    {
        entry.UpdatedAt = DateTime.Now;

        await using var connection = await SqliteConnectionSettings.OpenAsync(_connectionString, cancellationToken);

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
UPDATE ActionEntries
SET Category = $category,
    District = $district,
    OwnerParcelText = $ownerParcelText,
    WorkText = $workText,
    DisplayOrder = $displayOrder,
    UpdatedAt = $updatedAt,
    ProjectId = $projectId
WHERE Id = $id;";
        BindCommonParameters(cmd, entry);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await SqliteConnectionSettings.OpenAsync(_connectionString, cancellationToken);

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "UPDATE ActionEntries SET IsDeleted = 1, UpdatedAt = $updatedAt WHERE Id = $id;";
        cmd.Parameters.AddWithValue("$updatedAt", DateTime.Now.ToString("O"));
        cmd.Parameters.AddWithValue("$id", id.ToString());
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task ReorderAsync(ActionEntryCategory category, string district, IEnumerable<Guid> orderedIds, CancellationToken cancellationToken = default)
    {
        await using var connection = await SqliteConnectionSettings.OpenAsync(_connectionString, cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        var index = 0;
        foreach (var id in orderedIds)
        {
            await using var cmd = connection.CreateCommand();
            cmd.Transaction = transaction;
            cmd.CommandText = @"
UPDATE ActionEntries
SET DisplayOrder = $displayOrder,
    UpdatedAt = $updatedAt
WHERE Id = $id AND Category = $category AND District = $district;";
            cmd.Parameters.AddWithValue("$displayOrder", index++);
            cmd.Parameters.AddWithValue("$updatedAt", DateTime.Now.ToString("O"));
            cmd.Parameters.AddWithValue("$id", id.ToString());
            cmd.Parameters.AddWithValue("$category", (int)category);
            cmd.Parameters.AddWithValue("$district", district);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    private void EnsureDatabase()
    {
        var directory = Path.GetDirectoryName(_databasePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var connection = SqliteConnectionSettings.Open(_connectionString);

        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS ActionEntries (
    Id TEXT PRIMARY KEY,
    Category INTEGER NOT NULL,
    District TEXT NOT NULL,
    OwnerParcelText TEXT NOT NULL,
    WorkText TEXT NOT NULL,
    DisplayOrder INTEGER NOT NULL,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL
);";

        cmd.ExecuteNonQuery();
        SqliteConnectionSettings.EnsureColumnExists(connection, "ActionEntries", "IsDeleted", "INTEGER NOT NULL DEFAULT 0");
        SqliteConnectionSettings.EnsureColumnExists(connection, "ActionEntries", "ProjectId", "TEXT NOT NULL DEFAULT ''");
    }

    private static void BindCommonParameters(SqliteCommand cmd, ActionEntry entry)
    {
        cmd.Parameters.AddWithValue("$id", entry.Id.ToString());
        cmd.Parameters.AddWithValue("$category", (int)entry.Category);
        cmd.Parameters.AddWithValue("$district", entry.District);
        cmd.Parameters.AddWithValue("$ownerParcelText", entry.OwnerParcelText);
        cmd.Parameters.AddWithValue("$workText", entry.WorkText);
        cmd.Parameters.AddWithValue("$displayOrder", entry.DisplayOrder);
        cmd.Parameters.AddWithValue("$createdAt", entry.CreatedAt.ToString("O"));
        cmd.Parameters.AddWithValue("$updatedAt", entry.UpdatedAt.ToString("O"));
        cmd.Parameters.AddWithValue("$projectId", SqliteGuidHelper.ToDb(entry.ProjectId));
    }
}
