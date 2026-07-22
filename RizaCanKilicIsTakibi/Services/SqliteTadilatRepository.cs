using Microsoft.Data.Sqlite;
using RizaCanKilicIsTakibi.Helpers;
using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services.Abstractions;
using System.IO;

namespace RizaCanKilicIsTakibi.Services;

public sealed class SqliteTadilatRepository : ITadilatRepository
{
    private readonly string _connectionString;

    public SqliteTadilatRepository(string databasePath)
    {
        _connectionString = SqliteConnectionSettings.BuildConnectionString(databasePath);
        EnsureDatabase(databasePath);
    }

    public async Task<IReadOnlyList<TadilatEntry>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<TadilatEntry>();

        await using var connection = await SqliteConnectionSettings.OpenAsync(_connectionString, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
SELECT Id, SubTab, District, JobName, ProjectType, DigitalReceived, InspectorApproved,
       OutputAndReportArrived, OfficialLetterSubmitted, ArchivedFromMunicipality,
       Description1, Description2, DisplayOrder, CreatedAt, UpdatedAt, ProjectId
FROM TadilatEntries
WHERE IsDeleted = 0
ORDER BY SubTab, District, DisplayOrder, UpdatedAt DESC;
""";

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new TadilatEntry
            {
                Id = Guid.Parse(reader.GetString(0)),
                SubTab = (TadilatSubTab)reader.GetInt32(1),
                District = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                JobName = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                ProjectType = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                DigitalReceived = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                InspectorApproved = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                OutputAndReportArrived = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                OfficialLetterSubmitted = reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
                ArchivedFromMunicipality = reader.IsDBNull(9) ? string.Empty : reader.GetString(9),
                Description1 = reader.IsDBNull(10) ? string.Empty : reader.GetString(10),
                Description2 = reader.IsDBNull(11) ? string.Empty : reader.GetString(11),
                DisplayOrder = reader.GetInt32(12),
                CreatedAt = DateTime.Parse(reader.GetString(13)),
                UpdatedAt = DateTime.Parse(reader.GetString(14)),
                ProjectId = SqliteGuidHelper.ParseNullable(reader.IsDBNull(15) ? null : reader.GetString(15))
            });
        }

        return results;
    }

    public async Task<IReadOnlyList<TadilatCellState>> GetCellStatesAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<TadilatCellState>();

        await using var connection = await SqliteConnectionSettings.OpenAsync(_connectionString, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
SELECT EntryId, ColumnKey, BackgroundColor, NoteText
FROM TadilatCellStates
ORDER BY EntryId, ColumnKey;
""";

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new TadilatCellState
            {
                EntryId = Guid.Parse(reader.GetString(0)),
                ColumnKey = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                BackgroundColor = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                NoteText = reader.IsDBNull(3) ? string.Empty : reader.GetString(3)
            });
        }

        return results;
    }

    public async Task SaveManyAsync(IEnumerable<TadilatEntry> entries, IEnumerable<TadilatCellState> cellStates, CancellationToken cancellationToken = default)
    {
        var orderedEntries = entries
            .OrderBy(item => item.SubTab)
            .ThenBy(item => item.District, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.DisplayOrder)
            .ThenBy(item => item.UpdatedAt)
            .ToList();

        var states = cellStates
            .Where(item => item.EntryId != Guid.Empty && !string.IsNullOrWhiteSpace(item.ColumnKey))
            .OrderBy(item => item.EntryId)
            .ThenBy(item => item.ColumnKey, StringComparer.OrdinalIgnoreCase)
            .ToList();

        await using var connection = await SqliteConnectionSettings.OpenAsync(_connectionString, cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        var currentEntryIds = orderedEntries
            .Select(item => item.Id == Guid.Empty ? Guid.NewGuid() : item.Id)
            .ToList();

        var currentEntryIdSet = currentEntryIds
            .Select(item => item.ToString())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < orderedEntries.Count; index++)
        {
            orderedEntries[index].Id = currentEntryIds[index];
        }

        var existingEntryIds = new List<string>();
        await using (var readEntryIds = connection.CreateCommand())
        {
            readEntryIds.Transaction = transaction;
            readEntryIds.CommandText = "SELECT Id FROM TadilatEntries;";
            await using var reader = await readEntryIds.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                existingEntryIds.Add(reader.GetString(0));
            }
        }

        foreach (var staleId in existingEntryIds.Where(id => !currentEntryIdSet.Contains(id)))
        {
            await using var deleteStates = connection.CreateCommand();
            deleteStates.Transaction = transaction;
            deleteStates.CommandText = "DELETE FROM TadilatCellStates WHERE EntryId = $entryId;";
            deleteStates.Parameters.AddWithValue("$entryId", staleId);
            await deleteStates.ExecuteNonQueryAsync(cancellationToken);

            await using var deleteEntry = connection.CreateCommand();
            deleteEntry.Transaction = transaction;
            deleteEntry.CommandText = "UPDATE TadilatEntries SET IsDeleted = 1, UpdatedAt = $updatedAt WHERE Id = $id;";
            deleteEntry.Parameters.AddWithValue("$updatedAt", DateTime.Now.ToString("O"));
            deleteEntry.Parameters.AddWithValue("$id", staleId);
            await deleteEntry.ExecuteNonQueryAsync(cancellationToken);
        }

        for (var index = 0; index < orderedEntries.Count; index++)
        {
            var entry = orderedEntries[index];
            if (entry.CreatedAt == default)
            {
                entry.CreatedAt = DateTime.Now;
            }

            entry.UpdatedAt = DateTime.Now;

            await using var insertEntry = connection.CreateCommand();
            insertEntry.Transaction = transaction;
            insertEntry.CommandText = """
INSERT INTO TadilatEntries (
    Id, SubTab, District, JobName, ProjectType, DigitalReceived, InspectorApproved,
    OutputAndReportArrived, OfficialLetterSubmitted, ArchivedFromMunicipality,
    Description1, Description2, DisplayOrder, CreatedAt, UpdatedAt, ProjectId)
VALUES (
    $id, $subTab, $district, $jobName, $projectType, $digitalReceived, $inspectorApproved,
    $outputAndReportArrived, $officialLetterSubmitted, $archivedFromMunicipality,
    $description1, $description2, $displayOrder, $createdAt, $updatedAt, $projectId)
ON CONFLICT(Id) DO UPDATE SET
    SubTab = excluded.SubTab,
    District = excluded.District,
    JobName = excluded.JobName,
    ProjectType = excluded.ProjectType,
    DigitalReceived = excluded.DigitalReceived,
    InspectorApproved = excluded.InspectorApproved,
    OutputAndReportArrived = excluded.OutputAndReportArrived,
    OfficialLetterSubmitted = excluded.OfficialLetterSubmitted,
    ArchivedFromMunicipality = excluded.ArchivedFromMunicipality,
    Description1 = excluded.Description1,
    Description2 = excluded.Description2,
    DisplayOrder = excluded.DisplayOrder,
    CreatedAt = excluded.CreatedAt,
    UpdatedAt = excluded.UpdatedAt,
    ProjectId = excluded.ProjectId,
    IsDeleted = 0;
""";

            insertEntry.Parameters.AddWithValue("$id", entry.Id.ToString());
            insertEntry.Parameters.AddWithValue("$subTab", (int)entry.SubTab);
            insertEntry.Parameters.AddWithValue("$district", entry.District);
            insertEntry.Parameters.AddWithValue("$jobName", entry.JobName);
            insertEntry.Parameters.AddWithValue("$projectType", entry.ProjectType);
            insertEntry.Parameters.AddWithValue("$digitalReceived", entry.DigitalReceived);
            insertEntry.Parameters.AddWithValue("$inspectorApproved", entry.InspectorApproved);
            insertEntry.Parameters.AddWithValue("$outputAndReportArrived", entry.OutputAndReportArrived);
            insertEntry.Parameters.AddWithValue("$officialLetterSubmitted", entry.OfficialLetterSubmitted);
            insertEntry.Parameters.AddWithValue("$archivedFromMunicipality", entry.ArchivedFromMunicipality);
            insertEntry.Parameters.AddWithValue("$description1", entry.Description1);
            insertEntry.Parameters.AddWithValue("$description2", entry.Description2);
            insertEntry.Parameters.AddWithValue("$displayOrder", index);
            insertEntry.Parameters.AddWithValue("$createdAt", entry.CreatedAt.ToString("O"));
            insertEntry.Parameters.AddWithValue("$updatedAt", entry.UpdatedAt.ToString("O"));
            insertEntry.Parameters.AddWithValue("$projectId", SqliteGuidHelper.ToDb(entry.ProjectId));
            await insertEntry.ExecuteNonQueryAsync(cancellationToken);
        }

        var currentStateKeys = states
            .Select(item => BuildCellStateKey(item.EntryId, item.ColumnKey))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var existingStates = new List<(string EntryId, string ColumnKey)>();
        await using (var readStates = connection.CreateCommand())
        {
            readStates.Transaction = transaction;
            readStates.CommandText = "SELECT EntryId, ColumnKey FROM TadilatCellStates;";
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
            deleteState.CommandText = "DELETE FROM TadilatCellStates WHERE EntryId = $entryId AND ColumnKey = $columnKey;";
            deleteState.Parameters.AddWithValue("$entryId", staleState.EntryId);
            deleteState.Parameters.AddWithValue("$columnKey", staleState.ColumnKey);
            await deleteState.ExecuteNonQueryAsync(cancellationToken);
        }

        foreach (var state in states)
        {
            await using var insertState = connection.CreateCommand();
            insertState.Transaction = transaction;
            insertState.CommandText = """
INSERT INTO TadilatCellStates (EntryId, ColumnKey, BackgroundColor, NoteText)
VALUES ($entryId, $columnKey, $backgroundColor, $noteText)
ON CONFLICT(EntryId, ColumnKey) DO UPDATE SET
    BackgroundColor = excluded.BackgroundColor,
    NoteText = excluded.NoteText;
""";

            insertState.Parameters.AddWithValue("$entryId", state.EntryId.ToString());
            insertState.Parameters.AddWithValue("$columnKey", state.ColumnKey);
            insertState.Parameters.AddWithValue("$backgroundColor", state.BackgroundColor);
            insertState.Parameters.AddWithValue("$noteText", state.NoteText);
            await insertState.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    private static string BuildCellStateKey(Guid entryId, string columnKey)
        => $"{entryId:N}|{columnKey.Trim().ToUpperInvariant()}";

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await SqliteConnectionSettings.OpenAsync(_connectionString, cancellationToken);

        await using var deleteEntry = connection.CreateCommand();
        deleteEntry.CommandText = "UPDATE TadilatEntries SET IsDeleted = 1, UpdatedAt = $updatedAt WHERE Id = $id;";
        deleteEntry.Parameters.AddWithValue("$updatedAt", DateTime.Now.ToString("O"));
        deleteEntry.Parameters.AddWithValue("$id", id.ToString());
        await deleteEntry.ExecuteNonQueryAsync(cancellationToken);
    }

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
CREATE TABLE IF NOT EXISTS TadilatEntries (
    Id TEXT PRIMARY KEY,
    SubTab INTEGER NOT NULL,
    District TEXT NOT NULL DEFAULT '',
    JobName TEXT NOT NULL DEFAULT '',
    ProjectType TEXT NOT NULL DEFAULT '',
    DigitalReceived TEXT NOT NULL DEFAULT '',
    InspectorApproved TEXT NOT NULL DEFAULT '',
    OutputAndReportArrived TEXT NOT NULL DEFAULT '',
    OfficialLetterSubmitted TEXT NOT NULL DEFAULT '',
    ArchivedFromMunicipality TEXT NOT NULL DEFAULT '',
    Description1 TEXT NOT NULL DEFAULT '',
    Description2 TEXT NOT NULL DEFAULT '',
    DisplayOrder INTEGER NOT NULL,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS TadilatCellStates (
    EntryId TEXT NOT NULL,
    ColumnKey TEXT NOT NULL,
    BackgroundColor TEXT NOT NULL DEFAULT '',
    NoteText TEXT NOT NULL DEFAULT '',
    PRIMARY KEY (EntryId, ColumnKey)
);

DELETE FROM TadilatCellStates
WHERE NOT EXISTS (
    SELECT 1
    FROM TadilatEntries
    WHERE TadilatEntries.Id = TadilatCellStates.EntryId
);
""";
        command.ExecuteNonQuery();
        
        SqliteConnectionSettings.EnsureColumnExists(connection, "TadilatEntries", "IsDeleted", "INTEGER NOT NULL DEFAULT 0");
        SqliteConnectionSettings.EnsureColumnExists(connection, "TadilatEntries", "ProjectId", "TEXT NOT NULL DEFAULT ''");
    }
}
