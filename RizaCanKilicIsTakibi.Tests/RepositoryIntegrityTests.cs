using Microsoft.Data.Sqlite;
using RizaCanKilicIsTakibi.Services;

namespace RizaCanKilicIsTakibi.Tests;

public class RepositoryIntegrityTests
{
    [Fact]
    public async Task MissingProjectRepository_Cleans_Orphan_CellStates_On_Startup()
    {
        var root = CreateTempRoot();
        var databasePath = Path.Combine(root, "missing-orphan.db");

        try
        {
            _ = new SqliteMissingProjectRepository(databasePath);
            await InsertOrphanRowAsync(
                databasePath,
                "INSERT INTO MissingProjectCellStates (EntryId, ColumnKey, BackgroundColor, NoteText) VALUES ($entryId, 'MissingProjectText', '', 'orphan');");

            var repository = new SqliteMissingProjectRepository(databasePath);
            var states = await repository.GetCellStatesAsync();

            Assert.Empty(states);
        }
        finally
        {
            await DeleteDirectoryWithRetriesAsync(root);
        }
    }

    [Fact]
    public async Task TadilatRepository_Cleans_Orphan_CellStates_On_Startup()
    {
        var root = CreateTempRoot();
        var databasePath = Path.Combine(root, "tadilat-orphan.db");

        try
        {
            _ = new SqliteTadilatRepository(databasePath);
            await InsertOrphanRowAsync(
                databasePath,
                "INSERT INTO TadilatCellStates (EntryId, ColumnKey, BackgroundColor, NoteText) VALUES ($entryId, 'ProjectType', '', 'orphan');");

            var repository = new SqliteTadilatRepository(databasePath);
            var states = await repository.GetCellStatesAsync();

            Assert.Empty(states);
        }
        finally
        {
            await DeleteDirectoryWithRetriesAsync(root);
        }
    }

    [Fact]
    public async Task YibfRepository_Cleans_Orphan_Events_And_CellStates_On_Startup()
    {
        var root = CreateTempRoot();
        var databasePath = Path.Combine(root, "yibf-orphan.db");

        try
        {
            _ = new SqliteYibfRepository(databasePath);
            var orphanEntryId = Guid.NewGuid();

            await using (var connection = new SqliteConnection($"Data Source={databasePath}"))
            {
                await connection.OpenAsync();

                await using var command = connection.CreateCommand();
                command.CommandText = """
INSERT INTO YibfAnaBilgiEvents (Id, EntryId, EventDate, Description, BackgroundColor, NoteText, DisplayOrder)
VALUES ($eventId, $entryId, '', 'orphan event', '', '', 0);
INSERT INTO YibfCellStates (EntryId, ColumnKey, BackgroundColor, NoteText)
VALUES ($entryId, 'JobName', '', 'orphan state');
""";
                command.Parameters.AddWithValue("$eventId", Guid.NewGuid().ToString());
                command.Parameters.AddWithValue("$entryId", orphanEntryId.ToString());
                await command.ExecuteNonQueryAsync();
            }

            var repository = new SqliteYibfRepository(databasePath);
            var events = await repository.GetAnaBilgiEventsAsync();
            var states = await repository.GetCellStatesAsync();

            Assert.Empty(events);
            Assert.Empty(states);
        }
        finally
        {
            await DeleteDirectoryWithRetriesAsync(root);
        }
    }

    private static async Task InsertOrphanRowAsync(string databasePath, string sql)
    {
        await using var connection = new SqliteConnection($"Data Source={databasePath}");
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$entryId", Guid.NewGuid().ToString());
        await command.ExecuteNonQueryAsync();
    }

    private static string CreateTempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "RizaCanKilicIsTakibiTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    private static async Task DeleteDirectoryWithRetriesAsync(string root)
    {
        if (!Directory.Exists(root))
        {
            return;
        }

        for (var attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                Directory.Delete(root, true);
                break;
            }
            catch (IOException) when (attempt < 2)
            {
                await Task.Delay(100);
            }
            catch (UnauthorizedAccessException) when (attempt < 2)
            {
                await Task.Delay(100);
            }
            catch (IOException)
            {
                break;
            }
            catch (UnauthorizedAccessException)
            {
                break;
            }
        }
    }
}
