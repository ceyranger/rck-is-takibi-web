using Microsoft.Data.Sqlite;
using RizaCanKilicIsTakibi.Helpers;
using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services.Abstractions;
using System.IO;

namespace RizaCanKilicIsTakibi.Services;

public sealed class SqliteProjectCatalogRepository : IProjectCatalogRepository
{
    private readonly string _connectionString;
    private readonly string _databasePath;

    public SqliteProjectCatalogRepository(string databasePath)
    {
        _databasePath = databasePath;
        _connectionString = SqliteConnectionSettings.BuildConnectionString(databasePath);
        EnsureDatabase(databasePath);
    }

    public async Task<IReadOnlyList<ProjectCatalogEntry>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<ProjectCatalogEntry>();
        await using var connection = await SqliteConnectionSettings.OpenAsync(_connectionString, cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
SELECT Id, DisplayName, AdaParsel, YapiSahibi, YibfNo, Belediye, Muteahhit, Kind, ParentProjectId, IsActive, DisplayOrder, CreatedAt, UpdatedAt
FROM ProjectCatalogEntries
WHERE IsDeleted = 0
ORDER BY DisplayOrder, UpdatedAt DESC;
""";

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new ProjectCatalogEntry
            {
                Id = Guid.Parse(reader.GetString(0)),
                DisplayName = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                AdaParsel = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                YapiSahibi = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                YibfNo = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                Belediye = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                Muteahhit = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                Kind = (ProjectCatalogKind)reader.GetInt32(7),
                ParentProjectId = SqliteGuidHelper.ParseNullable(reader.IsDBNull(8) ? null : reader.GetString(8)),
                IsActive = reader.GetInt32(9) != 0,
                DisplayOrder = reader.GetInt32(10),
                CreatedAt = DateTime.Parse(reader.GetString(11)),
                UpdatedAt = DateTime.Parse(reader.GetString(12))
            });
        }

        return results;
    }

    public async Task SaveManyAsync(IEnumerable<ProjectCatalogEntry> entries, CancellationToken cancellationToken = default)
    {
        var ordered = entries.OrderBy(item => item.DisplayOrder).ThenBy(item => item.UpdatedAt).ToList();
        await using var connection = await SqliteConnectionSettings.OpenAsync(_connectionString, cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        for (var index = 0; index < ordered.Count; index++)
        {
            var entry = ordered[index];
            if (entry.Id == Guid.Empty)
            {
                entry.Id = Guid.NewGuid();
            }

            entry.DisplayOrder = index;
            if (entry.CreatedAt == default)
            {
                entry.CreatedAt = DateTime.Now;
            }

            entry.UpdatedAt = DateTime.Now;

            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
INSERT INTO ProjectCatalogEntries
    (Id, DisplayName, AdaParsel, YapiSahibi, YibfNo, Belediye, Muteahhit, Kind, ParentProjectId, IsActive, DisplayOrder, CreatedAt, UpdatedAt, IsDeleted)
VALUES
    ($id, $displayName, $adaParsel, $yapiSahibi, $yibfNo, $belediye, $muteahhit, $kind, $parentProjectId, $isActive, $displayOrder, $createdAt, $updatedAt, 0)
ON CONFLICT(Id) DO UPDATE SET
    DisplayName = excluded.DisplayName,
    AdaParsel = excluded.AdaParsel,
    YapiSahibi = excluded.YapiSahibi,
    YibfNo = excluded.YibfNo,
    Belediye = excluded.Belediye,
    Muteahhit = excluded.Muteahhit,
    Kind = excluded.Kind,
    ParentProjectId = excluded.ParentProjectId,
    IsActive = excluded.IsActive,
    DisplayOrder = excluded.DisplayOrder,
    UpdatedAt = excluded.UpdatedAt,
    IsDeleted = 0;
""";
            command.Parameters.AddWithValue("$id", entry.Id.ToString("D"));
            command.Parameters.AddWithValue("$displayName", entry.DisplayName ?? string.Empty);
            command.Parameters.AddWithValue("$adaParsel", entry.AdaParsel ?? string.Empty);
            command.Parameters.AddWithValue("$yapiSahibi", entry.YapiSahibi ?? string.Empty);
            command.Parameters.AddWithValue("$yibfNo", entry.YibfNo ?? string.Empty);
            command.Parameters.AddWithValue("$belediye", entry.Belediye ?? string.Empty);
            command.Parameters.AddWithValue("$muteahhit", entry.Muteahhit ?? string.Empty);
            command.Parameters.AddWithValue("$kind", (int)entry.Kind);
            command.Parameters.AddWithValue("$parentProjectId", SqliteGuidHelper.ToDb(entry.ParentProjectId));
            command.Parameters.AddWithValue("$isActive", entry.IsActive ? 1 : 0);
            command.Parameters.AddWithValue("$displayOrder", entry.DisplayOrder);
            command.Parameters.AddWithValue("$createdAt", entry.CreatedAt.ToString("O"));
            command.Parameters.AddWithValue("$updatedAt", entry.UpdatedAt.ToString("O"));
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        var keepIds = ordered.Select(item => item.Id.ToString("D")).ToHashSet(StringComparer.OrdinalIgnoreCase);
        await using (var listCommand = connection.CreateCommand())
        {
            listCommand.Transaction = transaction;
            listCommand.CommandText = "SELECT Id FROM ProjectCatalogEntries WHERE IsDeleted = 0;";
            await using var reader = await listCommand.ExecuteReaderAsync(cancellationToken);
            var stale = new List<string>();
            while (await reader.ReadAsync(cancellationToken))
            {
                var id = reader.GetString(0);
                if (!keepIds.Contains(id))
                {
                    stale.Add(id);
                }
            }

            await reader.DisposeAsync();
            foreach (var id in stale)
            {
                await using var deleteCommand = connection.CreateCommand();
                deleteCommand.Transaction = transaction;
                deleteCommand.CommandText = "UPDATE ProjectCatalogEntries SET IsDeleted = 1, UpdatedAt = $updatedAt WHERE Id = $id;";
                deleteCommand.Parameters.AddWithValue("$updatedAt", DateTime.Now.ToString("O"));
                deleteCommand.Parameters.AddWithValue("$id", id);
                await deleteCommand.ExecuteNonQueryAsync(cancellationToken);
            }
        }

        await transaction.CommitAsync(cancellationToken);
    }

    private void EnsureDatabase(string databasePath)
    {
        var directory = Path.GetDirectoryName(databasePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var connection = SqliteConnectionSettings.Open(_connectionString);
        if (NeedsProjectCatalogMigrationBackup(connection))
        {
            CreateProjectCatalogMigrationBackup(databasePath);
        }

        using (var command = connection.CreateCommand())
        {
            command.CommandText = """
CREATE TABLE IF NOT EXISTS ProjectCatalogEntries (
    Id TEXT PRIMARY KEY,
    DisplayName TEXT NOT NULL DEFAULT '',
    AdaParsel TEXT NOT NULL DEFAULT '',
    YapiSahibi TEXT NOT NULL DEFAULT '',
    YibfNo TEXT NOT NULL DEFAULT '',
    Kind INTEGER NOT NULL DEFAULT 0,
    ParentProjectId TEXT NOT NULL DEFAULT '',
    IsActive INTEGER NOT NULL DEFAULT 1,
    DisplayOrder INTEGER NOT NULL,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL,
    IsDeleted INTEGER NOT NULL DEFAULT 0
);
""";
            command.ExecuteNonQuery();
        }

        SqliteConnectionSettings.EnsureColumnExists(connection, "ProjectCatalogEntries", "Belediye", "TEXT NOT NULL DEFAULT ''");
        SqliteConnectionSettings.EnsureColumnExists(connection, "ProjectCatalogEntries", "Muteahhit", "TEXT NOT NULL DEFAULT ''");

        EnsureProjectIdColumn(connection, "KarotEntries");
        EnsureProjectIdColumn(connection, "TadilatEntries");
        EnsureProjectIdColumn(connection, "ActionEntries");
        EnsureProjectIdColumn(connection, "MissingProjectEntries");
        EnsureProjectIdColumn(connection, "Tasks");
        SqliteConnectionSettings.EnsureColumnExists(connection, "Tasks", "IsSpecialJob", "INTEGER NOT NULL DEFAULT 0");
    }

    private static void EnsureProjectIdColumn(SqliteConnection connection, string tableName)
    {
        if (!TableExists(connection, tableName))
        {
            return;
        }

        SqliteConnectionSettings.EnsureColumnExists(connection, tableName, "ProjectId", "TEXT NOT NULL DEFAULT ''");
    }

    private static bool NeedsProjectCatalogMigrationBackup(SqliteConnection connection)
    {
        if (!TableExists(connection, "ProjectCatalogEntries"))
        {
            return TableExists(connection, "KarotEntries")
                || TableExists(connection, "TadilatEntries")
                || TableExists(connection, "ActionEntries")
                || TableExists(connection, "Tasks");
        }

        foreach (var table in new[] { "KarotEntries", "TadilatEntries", "ActionEntries", "MissingProjectEntries", "Tasks" })
        {
            if (TableExists(connection, table) && !ColumnExists(connection, table, "ProjectId"))
            {
                return true;
            }
        }

        return TableExists(connection, "Tasks") && !ColumnExists(connection, "Tasks", "IsSpecialJob");
    }

    private static bool TableExists(SqliteConnection connection, string tableName)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = $tableName LIMIT 1;";
        command.Parameters.AddWithValue("$tableName", tableName);
        return command.ExecuteScalar() is not null;
    }

    private static bool ColumnExists(SqliteConnection connection, string tableName, string columnName)
    {
        using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info({tableName});";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static void CreateProjectCatalogMigrationBackup(string databasePath)
    {
        if (!File.Exists(databasePath))
        {
            return;
        }

        var dataDirectory = Path.GetDirectoryName(databasePath);
        if (string.IsNullOrWhiteSpace(dataDirectory))
        {
            return;
        }

        var parent = Directory.GetParent(dataDirectory);
        var backupRoot = string.Equals(Path.GetFileName(dataDirectory), "Data", StringComparison.OrdinalIgnoreCase) && parent is not null
            ? Path.Combine(parent.FullName, "Backup")
            : Path.Combine(dataDirectory, "Backup");
        var backupDirectory = Path.Combine(backupRoot, $"schema-migration-project-catalog-{DateTime.Now:yyyyMMdd-HHmmss}");
        Directory.CreateDirectory(backupDirectory);

        foreach (var sourcePath in new[] { databasePath, databasePath + "-wal", databasePath + "-shm", Path.Combine(dataDirectory, "last-save.json") })
        {
            if (!File.Exists(sourcePath))
            {
                continue;
            }

            File.Copy(sourcePath, Path.Combine(backupDirectory, Path.GetFileName(sourcePath)), overwrite: true);
        }
    }
}
