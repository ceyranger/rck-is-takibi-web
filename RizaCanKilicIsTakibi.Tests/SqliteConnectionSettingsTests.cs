using Microsoft.Data.Sqlite;
using RizaCanKilicIsTakibi.Services;

namespace RizaCanKilicIsTakibi.Tests;

public class SqliteConnectionSettingsTests
{
    [Fact]
    public async Task OpenAsync_Applies_Recommended_Pragma_Settings()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), "RizaCanKilicIsTakibiTests", $"{Guid.NewGuid():N}.db");
        Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);

        var connectionString = SqliteConnectionSettings.BuildConnectionString(databasePath);
        await using (var connection = await SqliteConnectionSettings.OpenAsync(connectionString))
        {
            Assert.Equal(1L, await ExecuteIntPragmaAsync(connection, "foreign_keys"));
            Assert.Equal(5000L, await ExecuteIntPragmaAsync(connection, "busy_timeout"));
            Assert.Equal("wal", await ExecuteStringPragmaAsync(connection, "journal_mode"));
            Assert.Equal(1L, await ExecuteIntPragmaAsync(connection, "synchronous"));
        }
    }

    private static async Task<long> ExecuteIntPragmaAsync(SqliteConnection connection, string pragmaName)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA {pragmaName};";
        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt64(result);
    }

    private static async Task<string> ExecuteStringPragmaAsync(SqliteConnection connection, string pragmaName)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA {pragmaName};";
        var result = await command.ExecuteScalarAsync();
        return Convert.ToString(result) ?? string.Empty;
    }
}
