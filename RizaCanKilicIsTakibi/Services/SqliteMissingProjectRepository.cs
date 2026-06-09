using Microsoft.Data.Sqlite;
using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services.Abstractions;
using System.IO;

namespace RizaCanKilicIsTakibi.Services;

public sealed class SqliteMissingProjectRepository : IMissingProjectRepository
{
    private readonly string _connectionString;

    public SqliteMissingProjectRepository(string databasePath)
    {
        _connectionString = SqliteConnectionSettings.BuildConnectionString(databasePath);
        EnsureDatabase(databasePath);
    }

    public async Task<IReadOnlyList<MissingProjectEntry>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<MissingProjectEntry>();

        await using var connection = await SqliteConnectionSettings.OpenAsync(_connectionString, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
SELECT Id, AdaParsel, YapiSahibi, RecordMedium, RecordMediumText, MissingProjectText, Description, DisplayOrder, CreatedAt, UpdatedAt
FROM MissingProjectEntries
WHERE IsDeleted = 0
ORDER BY DisplayOrder, UpdatedAt DESC;
""";

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new MissingProjectEntry
            {
                Id = Guid.Parse(reader.GetString(0)),
                AdaParsel = reader.GetString(1),
                YapiSahibi = reader.GetString(2),
                RecordMedium = (MissingProjectMedium)reader.GetInt32(3),
                RecordMediumText = reader.GetString(4),
                MissingProjectText = reader.GetString(5),
                Description = reader.GetString(6),
                DisplayOrder = reader.GetInt32(7),
                CreatedAt = DateTime.Parse(reader.GetString(8)),
                UpdatedAt = DateTime.Parse(reader.GetString(9))
            });
        }

        return results;
    }

    public async Task<IReadOnlyList<MissingProjectCellState>> GetCellStatesAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<MissingProjectCellState>();

        await using var connection = await SqliteConnectionSettings.OpenAsync(_connectionString, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
SELECT EntryId, ColumnKey, BackgroundColor, NoteText
FROM MissingProjectCellStates
ORDER BY EntryId, ColumnKey;
""";

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new MissingProjectCellState
            {
                EntryId = Guid.Parse(reader.GetString(0)),
                ColumnKey = reader.GetString(1),
                BackgroundColor = reader.GetString(2),
                NoteText = reader.GetString(3)
            });
        }

        return results;
    }

    public async Task AddAsync(MissingProjectEntry entry, CancellationToken cancellationToken = default)
    {
        if (entry.Id == Guid.Empty)
        {
            entry.Id = Guid.NewGuid();
        }

        entry.CreatedAt = DateTime.Now;
        entry.UpdatedAt = DateTime.Now;

        await using var connection = await SqliteConnectionSettings.OpenAsync(_connectionString, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
INSERT INTO MissingProjectEntries (Id, AdaParsel, YapiSahibi, RecordMedium, RecordMediumText, MissingProjectText, Description, DisplayOrder, CreatedAt, UpdatedAt)
VALUES ($id, $adaParsel, $yapiSahibi, $recordMedium, $recordMediumText, $missingProjectText, $description, $displayOrder, $createdAt, $updatedAt);
""";

        BindEntryParameters(command, entry);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpdateAsync(MissingProjectEntry entry, CancellationToken cancellationToken = default)
    {
        entry.UpdatedAt = DateTime.Now;

        await using var connection = await SqliteConnectionSettings.OpenAsync(_connectionString, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
UPDATE MissingProjectEntries
SET AdaParsel = $adaParsel,
    YapiSahibi = $yapiSahibi,
    RecordMedium = $recordMedium,
    RecordMediumText = $recordMediumText,
    MissingProjectText = $missingProjectText,
    Description = $description,
    DisplayOrder = $displayOrder,
    UpdatedAt = $updatedAt
WHERE Id = $id;
""";

        BindEntryParameters(command, entry);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await SqliteConnectionSettings.OpenAsync(_connectionString, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE MissingProjectEntries SET IsDeleted = 1, UpdatedAt = $updatedAt WHERE Id = $id;";
        command.Parameters.AddWithValue("$updatedAt", DateTime.Now.ToString("O"));
        command.Parameters.AddWithValue("$id", id.ToString());
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task SaveManyAsync(IEnumerable<MissingProjectEntry> entries, IEnumerable<MissingProjectCellState> cellStates, CancellationToken cancellationToken = default)
    {
        var orderedEntries = entries
            .OrderBy(item => item.DisplayOrder)
            .ThenBy(item => item.UpdatedAt)
            .ToList();

        var states = cellStates
            .Where(item => item.EntryId != Guid.Empty && !string.IsNullOrWhiteSpace(item.ColumnKey))
            .OrderBy(item => item.EntryId)
            .ThenBy(item => item.ColumnKey, StringComparer.OrdinalIgnoreCase)
            .ToList();

        await using var connection = await SqliteConnectionSettings.OpenAsync(_connectionString, cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        for (var index = 0; index < orderedEntries.Count; index++)
        {
            var entry = orderedEntries[index];
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

            await using var upsertCommand = connection.CreateCommand();
            upsertCommand.Transaction = transaction;
            upsertCommand.CommandText = """
INSERT INTO MissingProjectEntries (Id, AdaParsel, YapiSahibi, RecordMedium, RecordMediumText, MissingProjectText, Description, DisplayOrder, CreatedAt, UpdatedAt)
VALUES ($id, $adaParsel, $yapiSahibi, $recordMedium, $recordMediumText, $missingProjectText, $description, $displayOrder, $createdAt, $updatedAt)
ON CONFLICT(Id) DO UPDATE SET
    AdaParsel = excluded.AdaParsel,
    YapiSahibi = excluded.YapiSahibi,
    RecordMedium = excluded.RecordMedium,
    RecordMediumText = excluded.RecordMediumText,
    MissingProjectText = excluded.MissingProjectText,
    Description = excluded.Description,
    DisplayOrder = excluded.DisplayOrder,
    UpdatedAt = excluded.UpdatedAt,
    IsDeleted = 0;
""";

            BindEntryParameters(upsertCommand, entry);
            await upsertCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        var currentIds = orderedEntries.Select(item => item.Id).ToHashSet();
        var existingIds = new List<Guid>();

        await using (var readCommand = connection.CreateCommand())
        {
            readCommand.Transaction = transaction;
            readCommand.CommandText = "SELECT Id FROM MissingProjectEntries;";

            await using var reader = await readCommand.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                existingIds.Add(Guid.Parse(reader.GetString(0)));
            }
        }

        foreach (var staleId in existingIds.Where(id => !currentIds.Contains(id)))
        {
            await using var deleteCommand = connection.CreateCommand();
            deleteCommand.Transaction = transaction;
            deleteCommand.CommandText = "UPDATE MissingProjectEntries SET IsDeleted = 1, UpdatedAt = $updatedAt WHERE Id = $id;";
            deleteCommand.Parameters.AddWithValue("$updatedAt", DateTime.Now.ToString("O"));
            deleteCommand.Parameters.AddWithValue("$id", staleId.ToString());
            await deleteCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        var currentStateKeys = states
            .Select(state => BuildCellStateKey(state.EntryId, state.ColumnKey))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var existingStates = new List<(string EntryId, string ColumnKey)>();
        await using (var readStates = connection.CreateCommand())
        {
            readStates.Transaction = transaction;
            readStates.CommandText = "SELECT EntryId, ColumnKey FROM MissingProjectCellStates;";
            await using var reader = await readStates.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                existingStates.Add((reader.GetString(0), reader.GetString(1)));
            }
        }

        foreach (var staleState in existingStates.Where(item => !currentStateKeys.Contains(BuildCellStateKey(Guid.Parse(item.EntryId), item.ColumnKey))))
        {
            await using var deleteState = connection.CreateCommand();
            deleteState.Transaction = transaction;
            deleteState.CommandText = "DELETE FROM MissingProjectCellStates WHERE EntryId = $entryId AND ColumnKey = $columnKey;";
            deleteState.Parameters.AddWithValue("$entryId", staleState.EntryId);
            deleteState.Parameters.AddWithValue("$columnKey", staleState.ColumnKey);
            await deleteState.ExecuteNonQueryAsync(cancellationToken);
        }

        foreach (var state in states)
        {
            await using var insertState = connection.CreateCommand();
            insertState.Transaction = transaction;
            insertState.CommandText = """
INSERT INTO MissingProjectCellStates (EntryId, ColumnKey, BackgroundColor, NoteText)
VALUES ($entryId, $columnKey, $backgroundColor, $noteText)
ON CONFLICT(EntryId, ColumnKey) DO UPDATE SET
    BackgroundColor = excluded.BackgroundColor,
    NoteText = excluded.NoteText;
""";
            insertState.Parameters.AddWithValue("$entryId", state.EntryId.ToString());
            insertState.Parameters.AddWithValue("$columnKey", state.ColumnKey);
            insertState.Parameters.AddWithValue("$backgroundColor", state.BackgroundColor ?? string.Empty);
            insertState.Parameters.AddWithValue("$noteText", state.NoteText ?? string.Empty);
            await insertState.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    private static void BindEntryParameters(SqliteCommand command, MissingProjectEntry entry)
    {
        command.Parameters.AddWithValue("$id", entry.Id.ToString());
        command.Parameters.AddWithValue("$adaParsel", entry.AdaParsel);
        command.Parameters.AddWithValue("$yapiSahibi", entry.YapiSahibi);
        command.Parameters.AddWithValue("$recordMedium", (int)entry.RecordMedium);
        command.Parameters.AddWithValue("$recordMediumText", entry.RecordMediumText);
        command.Parameters.AddWithValue("$missingProjectText", entry.MissingProjectText);
        command.Parameters.AddWithValue("$description", entry.Description);
        command.Parameters.AddWithValue("$displayOrder", entry.DisplayOrder);
        command.Parameters.AddWithValue("$createdAt", entry.CreatedAt.ToString("O"));
        command.Parameters.AddWithValue("$updatedAt", entry.UpdatedAt.ToString("O"));
    }

    private static string BuildCellStateKey(Guid entryId, string columnKey)
        => $"{entryId:N}|{columnKey.Trim().ToUpperInvariant()}";

    private void EnsureDatabase(string databasePath)
    {
        var directory = Path.GetDirectoryName(databasePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var connection = SqliteConnectionSettings.Open(_connectionString);

        using var command = connection.CreateCommand();
        command.CommandText = """
CREATE TABLE IF NOT EXISTS MissingProjectEntries (
    Id TEXT PRIMARY KEY,
    AdaParsel TEXT NOT NULL,
    YapiSahibi TEXT NOT NULL,
    RecordMedium INTEGER NOT NULL,
    RecordMediumText TEXT NOT NULL DEFAULT '',
    MissingProjectText TEXT NOT NULL,
    Description TEXT NOT NULL,
    DisplayOrder INTEGER NOT NULL,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS MissingProjectCellStates (
    EntryId TEXT NOT NULL,
    ColumnKey TEXT NOT NULL,
    BackgroundColor TEXT NOT NULL DEFAULT '',
    NoteText TEXT NOT NULL DEFAULT '',
    PRIMARY KEY (EntryId, ColumnKey)
);

DELETE FROM MissingProjectCellStates
WHERE NOT EXISTS (
    SELECT 1
    FROM MissingProjectEntries
    WHERE MissingProjectEntries.Id = MissingProjectCellStates.EntryId
);
""";
        command.ExecuteNonQuery();

        var hasRecordMediumText = false;
        {
            using var migrationCheckCommand = connection.CreateCommand();
            migrationCheckCommand.CommandText = "PRAGMA table_info(MissingProjectEntries);";
            using var reader = migrationCheckCommand.ExecuteReader();

            while (reader.Read())
            {
                if (reader.GetString(1).Equals("RecordMediumText", StringComparison.OrdinalIgnoreCase))
                {
                    hasRecordMediumText = true;
                    break;
                }
            }
        }

        if (!hasRecordMediumText)
        {
            using var alterCommand = connection.CreateCommand();
            alterCommand.CommandText = "ALTER TABLE MissingProjectEntries ADD COLUMN RecordMediumText TEXT NOT NULL DEFAULT '';";
            alterCommand.ExecuteNonQuery();
        }

        using var backfillCommand = connection.CreateCommand();
        backfillCommand.CommandText = """
UPDATE MissingProjectEntries
SET RecordMediumText = CASE RecordMedium
    WHEN 0 THEN 'Dijital'
    WHEN 1 THEN 'Fiziksel'
    WHEN 2 THEN 'Fiziksel + Dijital'
    ELSE ''
END
WHERE TRIM(COALESCE(RecordMediumText, '')) = '';
""";
        backfillCommand.ExecuteNonQuery();

        SqliteConnectionSettings.EnsureColumnExists(connection, "MissingProjectEntries", "IsDeleted", "INTEGER NOT NULL DEFAULT 0");
    }
}
