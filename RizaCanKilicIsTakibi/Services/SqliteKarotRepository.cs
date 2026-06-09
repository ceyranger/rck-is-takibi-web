using Microsoft.Data.Sqlite;
using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services.Abstractions;
using System.IO;

namespace RizaCanKilicIsTakibi.Services;

public sealed class SqliteKarotRepository : IKarotRepository
{
    private readonly string _connectionString;

    public SqliteKarotRepository(string databasePath)
    {
        _connectionString = SqliteConnectionSettings.BuildConnectionString(databasePath);
        EnsureDatabase(databasePath);
    }

    public async Task<IReadOnlyList<KarotEntry>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<KarotEntry>();

        await using var connection = await SqliteConnectionSettings.OpenAsync(_connectionString, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
SELECT Id, SampleReceivedDate, YibfNo, AdaParsel, YapiSahibi, Muteahhit, KatBilgisi, BetonSinifi, TwentyEightDayResult,
       BetonFirmasi, Laboratuvar, Aciklama3, Status, DisplayOrder, CreatedAt, UpdatedAt
FROM KarotEntries
WHERE IsDeleted = 0
ORDER BY DisplayOrder, UpdatedAt DESC;
""";

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new KarotEntry
            {
                Id = Guid.Parse(reader.GetString(0)),
                SampleReceivedDate = reader.IsDBNull(1) ? null : DateTime.Parse(reader.GetString(1)),
                YibfNo = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                AdaParsel = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                YapiSahibi = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                Muteahhit = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                KatBilgisi = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                BetonSinifi = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                TwentyEightDayResult = reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
                BetonFirmasi = reader.IsDBNull(9) ? string.Empty : reader.GetString(9),
                Laboratuvar = reader.IsDBNull(10) ? string.Empty : reader.GetString(10),
                Aciklama = reader.IsDBNull(11) ? string.Empty : reader.GetString(11),
                Status = (KarotStatus)reader.GetInt32(12),
                DisplayOrder = reader.GetInt32(13),
                CreatedAt = DateTime.Parse(reader.GetString(14)),
                UpdatedAt = DateTime.Parse(reader.GetString(15))
            });
        }

        return results;
    }

    public async Task<IReadOnlyList<KarotCellState>> GetCellStatesAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<KarotCellState>();

        await using var connection = await SqliteConnectionSettings.OpenAsync(_connectionString, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
SELECT EntryId, ColumnKey, NoteText
FROM KarotCellStates
ORDER BY EntryId, ColumnKey;
""";

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new KarotCellState
            {
                EntryId = Guid.Parse(reader.GetString(0)),
                ColumnKey = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                NoteText = reader.IsDBNull(2) ? string.Empty : reader.GetString(2)
            });
        }

        return results;
    }

    public async Task SaveManyAsync(IEnumerable<KarotEntry> entries, IEnumerable<KarotCellState> cellStates, CancellationToken cancellationToken = default)
    {
        var orderedEntries = entries
            .OrderBy(item => item.DisplayOrder)
            .ThenBy(item => item.UpdatedAt)
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

            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
INSERT INTO KarotEntries (Id, SampleReceivedDate, YibfNo, AdaParsel, YapiSahibi, Muteahhit, KatBilgisi, BetonSinifi, TwentyEightDayResult,
                          BetonFirmasi, Laboratuvar, Aciklama3, Status, DisplayOrder, CreatedAt, UpdatedAt)
VALUES ($id, $sampleReceivedDate, $yibfNo, $adaParsel, $yapiSahibi, $muteahhit, $katBilgisi, $betonSinifi, $twentyEightDayResult,
        $betonFirmasi, $laboratuvar, $aciklama, $status, $displayOrder, $createdAt, $updatedAt)
ON CONFLICT(Id) DO UPDATE SET
    SampleReceivedDate = excluded.SampleReceivedDate,
    YibfNo = excluded.YibfNo,
    AdaParsel = excluded.AdaParsel,
    YapiSahibi = excluded.YapiSahibi,
    Muteahhit = excluded.Muteahhit,
    KatBilgisi = excluded.KatBilgisi,
    BetonSinifi = excluded.BetonSinifi,
    TwentyEightDayResult = excluded.TwentyEightDayResult,
    BetonFirmasi = excluded.BetonFirmasi,
    Laboratuvar = excluded.Laboratuvar,
    Aciklama3 = excluded.Aciklama3,
    Status = excluded.Status,
    DisplayOrder = excluded.DisplayOrder,
    UpdatedAt = excluded.UpdatedAt,
    IsDeleted = 0;
""";

            BindParameters(command, entry);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        var currentIds = orderedEntries.Select(item => item.Id).ToHashSet();
        var existingIds = new List<Guid>();

        await using (var readCommand = connection.CreateCommand())
        {
            readCommand.Transaction = transaction;
            readCommand.CommandText = "SELECT Id FROM KarotEntries;";
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
            deleteCommand.CommandText = "UPDATE KarotEntries SET IsDeleted = 1, UpdatedAt = $updatedAt WHERE Id = $id;";
            deleteCommand.Parameters.AddWithValue("$updatedAt", DateTime.Now.ToString("O"));
            deleteCommand.Parameters.AddWithValue("$id", staleId.ToString());
            await deleteCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        var normalizedStates = (cellStates ?? Array.Empty<KarotCellState>())
            .Where(state => state.EntryId != Guid.Empty
                            && currentIds.Contains(state.EntryId)
                            && !string.IsNullOrWhiteSpace(state.ColumnKey)
                            && !string.IsNullOrWhiteSpace(state.NoteText))
            .GroupBy(state => new { state.EntryId, ColumnKey = state.ColumnKey.Trim() })
            .Select(group => new KarotCellState
            {
                EntryId = group.Key.EntryId,
                ColumnKey = group.Key.ColumnKey,
                NoteText = group.Last().NoteText.Trim()
            })
            .ToList();

        var currentStateKeys = normalizedStates
            .Select(state => BuildCellStateKey(state.EntryId, state.ColumnKey))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var existingStates = new List<(string EntryId, string ColumnKey)>();
        await using (var readStates = connection.CreateCommand())
        {
            readStates.Transaction = transaction;
            readStates.CommandText = "SELECT EntryId, ColumnKey FROM KarotCellStates;";
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
            deleteState.CommandText = "DELETE FROM KarotCellStates WHERE EntryId = $entryId AND ColumnKey = $columnKey;";
            deleteState.Parameters.AddWithValue("$entryId", staleState.EntryId);
            deleteState.Parameters.AddWithValue("$columnKey", staleState.ColumnKey);
            await deleteState.ExecuteNonQueryAsync(cancellationToken);
        }

        foreach (var state in normalizedStates)
        {
            await using var stateCommand = connection.CreateCommand();
            stateCommand.Transaction = transaction;
            stateCommand.CommandText = """
INSERT INTO KarotCellStates (EntryId, ColumnKey, NoteText)
VALUES ($entryId, $columnKey, $noteText)
ON CONFLICT(EntryId, ColumnKey) DO UPDATE SET
    NoteText = excluded.NoteText;
""";
            stateCommand.Parameters.AddWithValue("$entryId", state.EntryId.ToString());
            stateCommand.Parameters.AddWithValue("$columnKey", state.ColumnKey);
            stateCommand.Parameters.AddWithValue("$noteText", state.NoteText);
            await stateCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await SqliteConnectionSettings.OpenAsync(_connectionString, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE KarotEntries SET IsDeleted = 1, UpdatedAt = $updatedAt WHERE Id = $id;";
        command.Parameters.AddWithValue("$updatedAt", DateTime.Now.ToString("O"));
        command.Parameters.AddWithValue("$id", id.ToString());
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void BindParameters(SqliteCommand command, KarotEntry entry)
    {
        command.Parameters.AddWithValue("$id", entry.Id.ToString());
        command.Parameters.AddWithValue("$sampleReceivedDate", entry.SampleReceivedDate?.ToString("O") ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("$yibfNo", entry.YibfNo);
        command.Parameters.AddWithValue("$adaParsel", entry.AdaParsel);
        command.Parameters.AddWithValue("$yapiSahibi", entry.YapiSahibi);
        command.Parameters.AddWithValue("$muteahhit", entry.Muteahhit);
        command.Parameters.AddWithValue("$katBilgisi", entry.KatBilgisi);
        command.Parameters.AddWithValue("$betonSinifi", entry.BetonSinifi);
        command.Parameters.AddWithValue("$twentyEightDayResult", entry.TwentyEightDayResult);
        command.Parameters.AddWithValue("$betonFirmasi", entry.BetonFirmasi);
        command.Parameters.AddWithValue("$laboratuvar", entry.Laboratuvar);
        command.Parameters.AddWithValue("$aciklama", entry.Aciklama);
        command.Parameters.AddWithValue("$status", (int)entry.Status);
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
CREATE TABLE IF NOT EXISTS KarotEntries (
    Id TEXT PRIMARY KEY,
    SampleReceivedDate TEXT NULL,
    YibfNo TEXT NOT NULL DEFAULT '',
    AdaParsel TEXT NOT NULL DEFAULT '',
    YapiSahibi TEXT NOT NULL DEFAULT '',
    Muteahhit TEXT NOT NULL DEFAULT '',
    KatBilgisi TEXT NOT NULL DEFAULT '',
    BetonSinifi TEXT NOT NULL DEFAULT '',
    TwentyEightDayResult TEXT NOT NULL DEFAULT '',
    BetonFirmasi TEXT NOT NULL DEFAULT '',
    Laboratuvar TEXT NOT NULL DEFAULT '',
    Aciklama1 TEXT NOT NULL DEFAULT '',
    Aciklama2 TEXT NOT NULL DEFAULT '',
    Aciklama3 TEXT NOT NULL DEFAULT '',
    Status INTEGER NOT NULL,
    DisplayOrder INTEGER NOT NULL,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS KarotCellStates (
    EntryId TEXT NOT NULL,
    ColumnKey TEXT NOT NULL,
    NoteText TEXT NOT NULL DEFAULT '',
    PRIMARY KEY (EntryId, ColumnKey),
    FOREIGN KEY (EntryId) REFERENCES KarotEntries(Id) ON DELETE CASCADE
);
""";
        command.ExecuteNonQuery();
        SqliteConnectionSettings.EnsureColumnExists(connection, "KarotEntries", "IsDeleted", "INTEGER NOT NULL DEFAULT 0");
    }
}
