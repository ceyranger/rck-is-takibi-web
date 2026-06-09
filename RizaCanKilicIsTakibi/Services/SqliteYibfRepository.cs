using Microsoft.Data.Sqlite;
using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services.Abstractions;
using System.IO;

namespace RizaCanKilicIsTakibi.Services;

public sealed class SqliteYibfRepository : IYibfRepository
{
    private readonly string _connectionString;

    public SqliteYibfRepository(string databasePath)
    {
        _connectionString = SqliteConnectionSettings.BuildConnectionString(databasePath);
        EnsureDatabase(databasePath);
    }

    public async Task<IReadOnlyList<YibfAnaBilgiEntry>> GetAnaBilgiEntriesAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<YibfAnaBilgiEntry>();

        await using var connection = await SqliteConnectionSettings.OpenAsync(_connectionString, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
SELECT Id, AdaParsel, YibfNo, Idare, YapiSahibi, Muteahhit, DisplayOrder, CreatedAt, UpdatedAt
FROM YibfAnaBilgiEntries
WHERE IsDeleted = 0
ORDER BY DisplayOrder, UpdatedAt DESC;
""";

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new YibfAnaBilgiEntry
            {
                Id = Guid.Parse(reader.GetString(0)),
                AdaParsel = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                YibfNo = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                Idare = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                YapiSahibi = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                Muteahhit = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                DisplayOrder = reader.GetInt32(6),
                CreatedAt = DateTime.Parse(reader.GetString(7)),
                UpdatedAt = DateTime.Parse(reader.GetString(8))
            });
        }

        return results;
    }

    public async Task<IReadOnlyList<YibfAnaBilgiEvent>> GetAnaBilgiEventsAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<YibfAnaBilgiEvent>();

        await using var connection = await SqliteConnectionSettings.OpenAsync(_connectionString, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
SELECT Id, EntryId, EventDate, Description, BackgroundColor, NoteText, DisplayOrder
FROM YibfAnaBilgiEvents
ORDER BY EntryId, DisplayOrder;
""";

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new YibfAnaBilgiEvent
            {
                Id = Guid.Parse(reader.GetString(0)),
                EntryId = Guid.Parse(reader.GetString(1)),
                EventDate = reader.IsDBNull(2) || string.IsNullOrWhiteSpace(reader.GetString(2)) ? null : DateTime.Parse(reader.GetString(2)),
                Description = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                BackgroundColor = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                NoteText = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                DisplayOrder = reader.GetInt32(6)
            });
        }

        return results;
    }

    public async Task<IReadOnlyList<YibfIsTakibiEntry>> GetIsTakibiEntriesAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<YibfIsTakibiEntry>();

        await using var connection = await SqliteConnectionSettings.OpenAsync(_connectionString, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
SELECT Id, JobName, MuellifBilgileriGeldiMi, DenetciAtamalariYapildiMi, TumProjelerinDijitaliVarMi,
       EvraklarTamMi, YibfSozlesmeHazirlandiMi, DekontAlindiMi, RuhsatBasvurusuYapildiMi,
       RuhsatNushasiAlindiMi, IsyeriTeslimTutangiHazirlandiMi, IsgYazisiHazirlandiMi,
       SaglikGuvenlikPlaniGeldiMi, TemelTopraklamaTutanagiHazirlandiMi, DisplayOrder, CreatedAt, UpdatedAt
FROM YibfIsTakibiEntries
WHERE IsDeleted = 0
ORDER BY DisplayOrder, UpdatedAt DESC;
""";

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new YibfIsTakibiEntry
            {
                Id = Guid.Parse(reader.GetString(0)),
                JobName = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                MuellifBilgileriGeldiMi = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                DenetciAtamalariYapildiMi = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                TumProjelerinDijitaliVarMi = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                EvraklarTamMi = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                YibfSozlesmeHazirlandiMi = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                DekontAlindiMi = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                RuhsatBasvurusuYapildiMi = reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
                RuhsatNushasiAlindiMi = reader.IsDBNull(9) ? string.Empty : reader.GetString(9),
                IsyeriTeslimTutangiHazirlandiMi = reader.IsDBNull(10) ? string.Empty : reader.GetString(10),
                IsgYazisiHazirlandiMi = reader.IsDBNull(11) ? string.Empty : reader.GetString(11),
                SaglikGuvenlikPlaniGeldiMi = reader.IsDBNull(12) ? string.Empty : reader.GetString(12),
                TemelTopraklamaTutanagiHazirlandiMi = reader.IsDBNull(13) ? string.Empty : reader.GetString(13),
                DisplayOrder = reader.GetInt32(14),
                CreatedAt = DateTime.Parse(reader.GetString(15)),
                UpdatedAt = DateTime.Parse(reader.GetString(16))
            });
        }

        return results;
    }

    public async Task<IReadOnlyList<YibfCellState>> GetCellStatesAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<YibfCellState>();

        await using var connection = await SqliteConnectionSettings.OpenAsync(_connectionString, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
SELECT EntryId, ColumnKey, BackgroundColor, NoteText
FROM YibfCellStates
ORDER BY EntryId, ColumnKey;
""";

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new YibfCellState
            {
                EntryId = Guid.Parse(reader.GetString(0)),
                ColumnKey = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                BackgroundColor = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                NoteText = reader.IsDBNull(3) ? string.Empty : reader.GetString(3)
            });
        }

        return results;
    }

    public async Task SaveManyAsync(
        IEnumerable<YibfAnaBilgiEntry> anaBilgiEntries,
        IEnumerable<YibfAnaBilgiEvent> anaBilgiEvents,
        IEnumerable<YibfIsTakibiEntry> isTakibiEntries,
        IEnumerable<YibfCellState> cellStates,
        CancellationToken cancellationToken = default)
    {
        var entries = anaBilgiEntries.OrderBy(item => item.DisplayOrder).ThenBy(item => item.UpdatedAt).ToList();
        var eventsList = anaBilgiEvents.OrderBy(item => item.EntryId).ThenBy(item => item.DisplayOrder).ToList();
        var rows = isTakibiEntries.OrderBy(item => item.DisplayOrder).ThenBy(item => item.UpdatedAt).ToList();
        var states = cellStates.Where(item => item.EntryId != Guid.Empty && !string.IsNullOrWhiteSpace(item.ColumnKey))
            .OrderBy(item => item.EntryId).ThenBy(item => item.ColumnKey, StringComparer.OrdinalIgnoreCase).ToList();

        await using var connection = await SqliteConnectionSettings.OpenAsync(_connectionString, cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        var currentAnaBilgiIds = entries
            .Select(item =>
            {
                if (item.Id == Guid.Empty)
                {
                    item.Id = Guid.NewGuid();
                }

                return item.Id.ToString();
            })
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var currentEventIds = eventsList
            .Select(item =>
            {
                if (item.Id == Guid.Empty)
                {
                    item.Id = Guid.NewGuid();
                }

                return item.Id.ToString();
            })
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var currentIsTakibiIds = rows
            .Select(item =>
            {
                if (item.Id == Guid.Empty)
                {
                    item.Id = Guid.NewGuid();
                }

                return item.Id.ToString();
            })
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var currentCellStateKeys = states
            .Select(item => BuildCellStateKey(item.EntryId, item.ColumnKey))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var staleId in await ReadIdListAsync(connection, transaction, "SELECT Id FROM YibfAnaBilgiEntries;", cancellationToken))
        {
            if (currentAnaBilgiIds.Contains(staleId))
            {
                continue;
            }

            // Child kayıtları fiziki silebiliriz (YibfAnaBilgiEvents)
            // Ancak ana kaydı soft delete yapmalıyız
            await DeleteByIdAsync(connection, transaction, "YibfAnaBilgiEvents", "EntryId", staleId, cancellationToken);
            await SoftDeleteByIdAsync(connection, transaction, "YibfAnaBilgiEntries", staleId, cancellationToken);
        }

        foreach (var staleId in await ReadIdListAsync(connection, transaction, "SELECT Id FROM YibfAnaBilgiEvents;", cancellationToken))
        {
            if (!currentEventIds.Contains(staleId))
            {
                await DeleteByIdAsync(connection, transaction, "YibfAnaBilgiEvents", "Id", staleId, cancellationToken);
            }
        }

        foreach (var staleId in await ReadIdListAsync(connection, transaction, "SELECT Id FROM YibfIsTakibiEntries;", cancellationToken))
        {
            if (!currentIsTakibiIds.Contains(staleId))
            {
                await SoftDeleteByIdAsync(connection, transaction, "YibfIsTakibiEntries", staleId, cancellationToken);
            }
        }

        foreach (var staleState in await ReadCellStateKeysAsync(connection, transaction, cancellationToken))
        {
            if (!currentCellStateKeys.Contains(BuildCellStateKey(Guid.Parse(staleState.EntryId), staleState.ColumnKey)))
            {
                await DeleteCellStateAsync(connection, transaction, staleState.EntryId, staleState.ColumnKey, cancellationToken);
            }
        }

        try
        {
            for (var index = 0; index < entries.Count; index++)
            {
                var entry = entries[index];
                if (entry.CreatedAt == default)
                {
                    entry.CreatedAt = DateTime.Now;
                }

                entry.UpdatedAt = DateTime.Now;

                await using var insert = connection.CreateCommand();
                insert.Transaction = transaction;
                insert.CommandText = @"
INSERT INTO YibfAnaBilgiEntries (Id, AdaParsel, YibfNo, Idare, YapiSahibi, Muteahhit, DisplayOrder, CreatedAt, UpdatedAt)
VALUES ($id, $adaParsel, $yibfNo, $idare, $yapiSahibi, $muteahhit, $displayOrder, $createdAt, $updatedAt)
ON CONFLICT(Id) DO UPDATE SET
    AdaParsel = excluded.AdaParsel,
    YibfNo = excluded.YibfNo,
    Idare = excluded.Idare,
    YapiSahibi = excluded.YapiSahibi,
    Muteahhit = excluded.Muteahhit,
    DisplayOrder = excluded.DisplayOrder,
    CreatedAt = excluded.CreatedAt,
    UpdatedAt = excluded.UpdatedAt,
    IsDeleted = 0;
";
                insert.Parameters.AddWithValue("$id", entry.Id.ToString());
                insert.Parameters.AddWithValue("$adaParsel", entry.AdaParsel);
                insert.Parameters.AddWithValue("$yibfNo", entry.YibfNo);
                insert.Parameters.AddWithValue("$idare", entry.Idare);
                insert.Parameters.AddWithValue("$yapiSahibi", entry.YapiSahibi);
                insert.Parameters.AddWithValue("$muteahhit", entry.Muteahhit);
                insert.Parameters.AddWithValue("$displayOrder", index);
                insert.Parameters.AddWithValue("$createdAt", entry.CreatedAt.ToString("O"));
                insert.Parameters.AddWithValue("$updatedAt", entry.UpdatedAt.ToString("O"));
                await insert.ExecuteNonQueryAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("YibfAnaBilgiEntries kaydı başarısız.", ex);
        }

        try
        {
            for (var index = 0; index < eventsList.Count; index++)
            {
                var item = eventsList[index];
                await using var insert = connection.CreateCommand();
                insert.Transaction = transaction;
                insert.CommandText = @"
INSERT INTO YibfAnaBilgiEvents (Id, EntryId, EventDate, Description, BackgroundColor, NoteText, DisplayOrder)
VALUES ($id, $entryId, $eventDate, $description, $backgroundColor, $noteText, $displayOrder)
ON CONFLICT(Id) DO UPDATE SET
    EntryId = excluded.EntryId,
    EventDate = excluded.EventDate,
    Description = excluded.Description,
    BackgroundColor = excluded.BackgroundColor,
    NoteText = excluded.NoteText,
    DisplayOrder = excluded.DisplayOrder;
";
                insert.Parameters.AddWithValue("$id", item.Id.ToString());
                insert.Parameters.AddWithValue("$entryId", item.EntryId.ToString());
                insert.Parameters.AddWithValue("$eventDate", item.EventDate?.ToString("O") ?? string.Empty);
                insert.Parameters.AddWithValue("$description", item.Description);
                insert.Parameters.AddWithValue("$backgroundColor", item.BackgroundColor);
                insert.Parameters.AddWithValue("$noteText", item.NoteText);
                insert.Parameters.AddWithValue("$displayOrder", item.DisplayOrder);
                await insert.ExecuteNonQueryAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("YibfAnaBilgiEvents kaydı başarısız.", ex);
        }

        try
        {
            for (var index = 0; index < rows.Count; index++)
            {
                var entry = rows[index];
                if (entry.CreatedAt == default)
                {
                    entry.CreatedAt = DateTime.Now;
                }

                entry.UpdatedAt = DateTime.Now;

                await using var insert = connection.CreateCommand();
                insert.Transaction = transaction;
                insert.CommandText = @"
INSERT INTO YibfIsTakibiEntries (
    Id, JobName, MuellifBilgileriGeldiMi, DenetciAtamalariYapildiMi, TumProjelerinDijitaliVarMi,
    EvraklarTamMi, YibfSozlesmeHazirlandiMi, DekontAlindiMi, RuhsatBasvurusuYapildiMi,
    RuhsatNushasiAlindiMi, IsyeriTeslimTutangiHazirlandiMi, IsgYazisiHazirlandiMi,
    SaglikGuvenlikPlaniGeldiMi, TemelTopraklamaTutanagiHazirlandiMi, DisplayOrder, CreatedAt, UpdatedAt)
VALUES (
    $id, $jobName, $muellif, $denetci, $tumDijital, $evrak, $sozlesme, $dekont, $ruhsatBasvuru,
    $ruhsatNusha, $isyeriTeslim, $isg, $saglik, $topraklama, $displayOrder, $createdAt, $updatedAt)
ON CONFLICT(Id) DO UPDATE SET
    JobName = excluded.JobName,
    MuellifBilgileriGeldiMi = excluded.MuellifBilgileriGeldiMi,
    DenetciAtamalariYapildiMi = excluded.DenetciAtamalariYapildiMi,
    TumProjelerinDijitaliVarMi = excluded.TumProjelerinDijitaliVarMi,
    EvraklarTamMi = excluded.EvraklarTamMi,
    YibfSozlesmeHazirlandiMi = excluded.YibfSozlesmeHazirlandiMi,
    DekontAlindiMi = excluded.DekontAlindiMi,
    RuhsatBasvurusuYapildiMi = excluded.RuhsatBasvurusuYapildiMi,
    RuhsatNushasiAlindiMi = excluded.RuhsatNushasiAlindiMi,
    IsyeriTeslimTutangiHazirlandiMi = excluded.IsyeriTeslimTutangiHazirlandiMi,
    IsgYazisiHazirlandiMi = excluded.IsgYazisiHazirlandiMi,
    SaglikGuvenlikPlaniGeldiMi = excluded.SaglikGuvenlikPlaniGeldiMi,
    TemelTopraklamaTutanagiHazirlandiMi = excluded.TemelTopraklamaTutanagiHazirlandiMi,
    DisplayOrder = excluded.DisplayOrder,
    CreatedAt = excluded.CreatedAt,
    UpdatedAt = excluded.UpdatedAt,
    IsDeleted = 0;
";
                insert.Parameters.AddWithValue("$id", entry.Id.ToString());
                insert.Parameters.AddWithValue("$jobName", entry.JobName);
                insert.Parameters.AddWithValue("$muellif", entry.MuellifBilgileriGeldiMi);
                insert.Parameters.AddWithValue("$denetci", entry.DenetciAtamalariYapildiMi);
                insert.Parameters.AddWithValue("$tumDijital", entry.TumProjelerinDijitaliVarMi);
                insert.Parameters.AddWithValue("$evrak", entry.EvraklarTamMi);
                insert.Parameters.AddWithValue("$sozlesme", entry.YibfSozlesmeHazirlandiMi);
                insert.Parameters.AddWithValue("$dekont", entry.DekontAlindiMi);
                insert.Parameters.AddWithValue("$ruhsatBasvuru", entry.RuhsatBasvurusuYapildiMi);
                insert.Parameters.AddWithValue("$ruhsatNusha", entry.RuhsatNushasiAlindiMi);
                insert.Parameters.AddWithValue("$isyeriTeslim", entry.IsyeriTeslimTutangiHazirlandiMi);
                insert.Parameters.AddWithValue("$isg", entry.IsgYazisiHazirlandiMi);
                insert.Parameters.AddWithValue("$saglik", entry.SaglikGuvenlikPlaniGeldiMi);
                insert.Parameters.AddWithValue("$topraklama", entry.TemelTopraklamaTutanagiHazirlandiMi);
                insert.Parameters.AddWithValue("$displayOrder", index);
                insert.Parameters.AddWithValue("$createdAt", entry.CreatedAt.ToString("O"));
                insert.Parameters.AddWithValue("$updatedAt", entry.UpdatedAt.ToString("O"));
                await insert.ExecuteNonQueryAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("YibfIsTakibiEntries kaydı başarısız.", ex);
        }

        try
        {
            foreach (var state in states)
            {
                await using var insert = connection.CreateCommand();
                insert.Transaction = transaction;
                insert.CommandText = @"
INSERT INTO YibfCellStates (EntryId, ColumnKey, BackgroundColor, NoteText)
VALUES ($entryId, $columnKey, $backgroundColor, $noteText)
ON CONFLICT(EntryId, ColumnKey) DO UPDATE SET
    BackgroundColor = excluded.BackgroundColor,
    NoteText = excluded.NoteText;
";
                insert.Parameters.AddWithValue("$entryId", state.EntryId.ToString());
                insert.Parameters.AddWithValue("$columnKey", state.ColumnKey);
                insert.Parameters.AddWithValue("$backgroundColor", state.BackgroundColor);
                insert.Parameters.AddWithValue("$noteText", state.NoteText);
                await insert.ExecuteNonQueryAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("YibfCellStates kaydı başarısız.", ex);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    private static async Task<List<string>> ReadIdListAsync(SqliteConnection connection, SqliteTransaction transaction, string sql, CancellationToken cancellationToken)
    {
        var results = new List<string>();
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(reader.GetString(0));
        }

        return results;
    }

    private static async Task<List<(string EntryId, string ColumnKey)>> ReadCellStateKeysAsync(SqliteConnection connection, SqliteTransaction transaction, CancellationToken cancellationToken)
    {
        var results = new List<(string EntryId, string ColumnKey)>();
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT EntryId, ColumnKey FROM YibfCellStates;";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add((reader.GetString(0), reader.GetString(1)));
        }

        return results;
    }

    private static async Task DeleteByIdAsync(SqliteConnection connection, SqliteTransaction transaction, string tableName, string columnName, string id, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"DELETE FROM {tableName} WHERE {columnName} = $id;";
        command.Parameters.AddWithValue("$id", id);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task SoftDeleteByIdAsync(SqliteConnection connection, SqliteTransaction transaction, string tableName, string id, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"UPDATE {tableName} SET IsDeleted = 1, UpdatedAt = $updatedAt WHERE Id = $id;";
        command.Parameters.AddWithValue("$updatedAt", DateTime.Now.ToString("O"));
        command.Parameters.AddWithValue("$id", id);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task DeleteCellStateAsync(SqliteConnection connection, SqliteTransaction transaction, string entryId, string columnKey, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "DELETE FROM YibfCellStates WHERE EntryId = $entryId AND ColumnKey = $columnKey;";
        command.Parameters.AddWithValue("$entryId", entryId);
        command.Parameters.AddWithValue("$columnKey", columnKey);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static string BuildCellStateKey(Guid entryId, string columnKey)
        => $"{entryId:N}|{columnKey.Trim().ToUpperInvariant()}";

    public async Task DeleteIsTakibiAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await SqliteConnectionSettings.OpenAsync(_connectionString, cancellationToken);

        await using var deleteRow = connection.CreateCommand();
        deleteRow.CommandText = "UPDATE YibfIsTakibiEntries SET IsDeleted = 1, UpdatedAt = $updatedAt WHERE Id = $id;";
        deleteRow.Parameters.AddWithValue("$updatedAt", DateTime.Now.ToString("O"));
        deleteRow.Parameters.AddWithValue("$id", id.ToString());
        await deleteRow.ExecuteNonQueryAsync(cancellationToken);
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
CREATE TABLE IF NOT EXISTS YibfAnaBilgiEntries (
    Id TEXT PRIMARY KEY,
    AdaParsel TEXT NOT NULL DEFAULT '',
    YibfNo TEXT NOT NULL DEFAULT '',
    Idare TEXT NOT NULL DEFAULT '',
    YapiSahibi TEXT NOT NULL DEFAULT '',
    Muteahhit TEXT NOT NULL DEFAULT '',
    DisplayOrder INTEGER NOT NULL,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS YibfAnaBilgiEvents (
    Id TEXT PRIMARY KEY,
    EntryId TEXT NOT NULL,
    EventDate TEXT NOT NULL DEFAULT '',
    Description TEXT NOT NULL DEFAULT '',
    BackgroundColor TEXT NOT NULL DEFAULT '',
    NoteText TEXT NOT NULL DEFAULT '',
    DisplayOrder INTEGER NOT NULL
);

CREATE TABLE IF NOT EXISTS YibfIsTakibiEntries (
    Id TEXT PRIMARY KEY,
    JobName TEXT NOT NULL DEFAULT '',
    MuellifBilgileriGeldiMi TEXT NOT NULL DEFAULT '',
    DenetciAtamalariYapildiMi TEXT NOT NULL DEFAULT '',
    TumProjelerinDijitaliVarMi TEXT NOT NULL DEFAULT '',
    EvraklarTamMi TEXT NOT NULL DEFAULT '',
    YibfSozlesmeHazirlandiMi TEXT NOT NULL DEFAULT '',
    DekontAlindiMi TEXT NOT NULL DEFAULT '',
    RuhsatBasvurusuYapildiMi TEXT NOT NULL DEFAULT '',
    RuhsatNushasiAlindiMi TEXT NOT NULL DEFAULT '',
    IsyeriTeslimTutangiHazirlandiMi TEXT NOT NULL DEFAULT '',
    IsgYazisiHazirlandiMi TEXT NOT NULL DEFAULT '',
    SaglikGuvenlikPlaniGeldiMi TEXT NOT NULL DEFAULT '',
    TemelTopraklamaTutanagiHazirlandiMi TEXT NOT NULL DEFAULT '',
    DisplayOrder INTEGER NOT NULL,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS YibfCellStates (
    EntryId TEXT NOT NULL,
    ColumnKey TEXT NOT NULL,
    BackgroundColor TEXT NOT NULL DEFAULT '',
    NoteText TEXT NOT NULL DEFAULT '',
    PRIMARY KEY (EntryId, ColumnKey)
);

DELETE FROM YibfAnaBilgiEvents
WHERE NOT EXISTS (
    SELECT 1
    FROM YibfAnaBilgiEntries
    WHERE YibfAnaBilgiEntries.Id = YibfAnaBilgiEvents.EntryId
);

DELETE FROM YibfCellStates
WHERE NOT EXISTS (
    SELECT 1
    FROM YibfIsTakibiEntries
    WHERE YibfIsTakibiEntries.Id = YibfCellStates.EntryId
);
""";
        command.ExecuteNonQuery();

        SqliteConnectionSettings.EnsureColumnExists(connection, "YibfAnaBilgiEntries", "IsDeleted", "INTEGER NOT NULL DEFAULT 0");
        SqliteConnectionSettings.EnsureColumnExists(connection, "YibfIsTakibiEntries", "IsDeleted", "INTEGER NOT NULL DEFAULT 0");
    }
}


