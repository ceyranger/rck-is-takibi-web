using Microsoft.Data.Sqlite;
using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services.Abstractions;
using System.IO;

namespace RizaCanKilicIsTakibi.Services;

public sealed class SqliteQuickTaskTemplateRepository : IQuickTaskTemplateRepository
{
    private static readonly string[] DefaultTemplateTitles =
    [
        "Eksik evrak istenecek",
        "YİBF takibi yapılacak",
        "Ruhsat başvurusu kontrol edilecek",
        "Tadilat dosyası kontrol edilecek",
        "Karot sonucu takip edilecek",
        "İlgili kişi aranacak"
    ];
    private const string DefaultGroupName = "Genel";
    private const string RemovedSeedTemplatesMaintenanceKey = "RemovedSeedTemplates_20260611";

    private readonly string _databasePath;
    private readonly string _connectionString;

    public SqliteQuickTaskTemplateRepository(string databasePath)
    {
        _databasePath = databasePath;
        _connectionString = SqliteConnectionSettings.BuildConnectionString(_databasePath);
        EnsureDatabase();
    }

    public IReadOnlyList<QuickTaskTemplate> GetAll()
    {
        using var connection = SqliteConnectionSettings.Open(_connectionString);
        return GetAll(connection);
    }

    public Task<IReadOnlyList<QuickTaskTemplate>> GetAllAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(GetAll());

    public Task SaveAsync(QuickTaskTemplate template, CancellationToken cancellationToken = default)
    {
        using var connection = SqliteConnectionSettings.Open(_connectionString);
        using var transaction = connection.BeginTransaction();
        UpsertTemplate(connection, transaction, template);
        transaction.Commit();
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = SqliteConnectionSettings.Open(_connectionString);
        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
UPDATE QuickTaskTemplates
SET IsDeleted = 1, UpdatedAt = $updatedAt
WHERE Id = $id;";
        cmd.Parameters.AddWithValue("$updatedAt", DateTime.Now.ToString("O"));
        cmd.Parameters.AddWithValue("$id", id.ToString());
        cmd.ExecuteNonQuery();
        return Task.CompletedTask;
    }

    public void ReplaceAll(IEnumerable<QuickTaskTemplate> templates)
    {
        using var connection = SqliteConnectionSettings.Open(_connectionString);
        using var transaction = connection.BeginTransaction();

        using (var deleteCommand = connection.CreateCommand())
        {
            deleteCommand.Transaction = transaction;
            deleteCommand.CommandText = "DELETE FROM QuickTaskTemplates;";
            deleteCommand.ExecuteNonQuery();
        }

        foreach (var template in templates)
        {
            UpsertTemplate(connection, transaction, template);
        }

        transaction.Commit();
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
CREATE TABLE IF NOT EXISTS QuickTaskTemplates (
    Id TEXT PRIMARY KEY,
    GroupName TEXT NOT NULL DEFAULT '',
    Title TEXT NOT NULL,
    SortOrder INTEGER NOT NULL,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL,
    IsDeleted INTEGER NOT NULL DEFAULT 0
);

CREATE TABLE IF NOT EXISTS QuickTaskTemplateMaintenance (
    Key TEXT PRIMARY KEY,
    AppliedAt TEXT NOT NULL
);";
            createCommand.ExecuteNonQuery();
        }

        SqliteConnectionSettings.EnsureColumnExists(connection, "QuickTaskTemplates", "GroupName", "TEXT NOT NULL DEFAULT ''");
        AssignDefaultGroupToUngroupedTemplates(connection);
        RemoveSeedTemplatesOnce(connection);
    }

    private static bool TableExists(SqliteConnection connection, string tableName)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = $name;";
        command.Parameters.AddWithValue("$name", tableName);
        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }

    private static void AssignDefaultGroupToUngroupedTemplates(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = @"
UPDATE QuickTaskTemplates
SET GroupName = $groupName
WHERE IsDeleted = 0 AND TRIM(COALESCE(GroupName, '')) = '';";
        command.Parameters.AddWithValue("$groupName", DefaultGroupName);
        command.ExecuteNonQuery();
    }

    private static void RemoveSeedTemplatesOnce(SqliteConnection connection)
    {
        using (var checkCommand = connection.CreateCommand())
        {
            checkCommand.CommandText = "SELECT COUNT(*) FROM QuickTaskTemplateMaintenance WHERE Key = $key;";
            checkCommand.Parameters.AddWithValue("$key", RemovedSeedTemplatesMaintenanceKey);
            if (Convert.ToInt32(checkCommand.ExecuteScalar()) > 0)
            {
                return;
            }
        }

        using var transaction = connection.BeginTransaction();
        foreach (var title in DefaultTemplateTitles)
        {
            using var deleteCommand = connection.CreateCommand();
            deleteCommand.Transaction = transaction;
            deleteCommand.CommandText = @"
UPDATE QuickTaskTemplates
SET IsDeleted = 1, UpdatedAt = $updatedAt
WHERE IsDeleted = 0 AND Title = $title;";
            deleteCommand.Parameters.AddWithValue("$updatedAt", DateTime.Now.ToString("O"));
            deleteCommand.Parameters.AddWithValue("$title", title);
            deleteCommand.ExecuteNonQuery();
        }

        using var markCommand = connection.CreateCommand();
        markCommand.Transaction = transaction;
        markCommand.CommandText = @"
INSERT INTO QuickTaskTemplateMaintenance (Key, AppliedAt)
VALUES ($key, $appliedAt);";
        markCommand.Parameters.AddWithValue("$key", RemovedSeedTemplatesMaintenanceKey);
        markCommand.Parameters.AddWithValue("$appliedAt", DateTime.Now.ToString("O"));
        markCommand.ExecuteNonQuery();
        transaction.Commit();
    }

    private static IReadOnlyList<QuickTaskTemplate> GetAll(SqliteConnection connection)
    {
        var result = new List<QuickTaskTemplate>();

        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT Id, GroupName, Title, SortOrder, CreatedAt, UpdatedAt, IsDeleted
FROM QuickTaskTemplates
WHERE IsDeleted = 0
ORDER BY GroupName COLLATE NOCASE, SortOrder, UpdatedAt DESC;";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new QuickTaskTemplate
            {
                Id = Guid.Parse(reader.GetString(0)),
                GroupName = reader.GetString(1),
                Title = reader.GetString(2),
                SortOrder = reader.GetInt32(3),
                CreatedAt = DateTime.Parse(reader.GetString(4)),
                UpdatedAt = DateTime.Parse(reader.GetString(5)),
                IsDeleted = reader.GetInt32(6) != 0
            });
        }

        return result;
    }

    private static void UpsertTemplate(SqliteConnection connection, SqliteTransaction transaction, QuickTaskTemplate template)
    {
        var createdAt = template.CreatedAt == default ? DateTime.Now : template.CreatedAt;
        var updatedAt = template.UpdatedAt == default ? DateTime.Now : template.UpdatedAt;

        using var cmd = connection.CreateCommand();
        cmd.Transaction = transaction;
        cmd.CommandText = @"
INSERT INTO QuickTaskTemplates (Id, GroupName, Title, SortOrder, CreatedAt, UpdatedAt, IsDeleted)
VALUES ($id, $groupName, $title, $sortOrder, $createdAt, $updatedAt, $isDeleted)
ON CONFLICT(Id) DO UPDATE SET
    GroupName = excluded.GroupName,
    Title = excluded.Title,
    SortOrder = excluded.SortOrder,
    UpdatedAt = excluded.UpdatedAt,
    IsDeleted = excluded.IsDeleted;";
        cmd.Parameters.AddWithValue("$id", template.Id.ToString());
        cmd.Parameters.AddWithValue("$groupName", string.IsNullOrWhiteSpace(template.GroupName) ? DefaultGroupName : template.GroupName.Trim());
        cmd.Parameters.AddWithValue("$title", template.Title.Trim());
        cmd.Parameters.AddWithValue("$sortOrder", template.SortOrder);
        cmd.Parameters.AddWithValue("$createdAt", createdAt.ToString("O"));
        cmd.Parameters.AddWithValue("$updatedAt", updatedAt.ToString("O"));
        cmd.Parameters.AddWithValue("$isDeleted", template.IsDeleted ? 1 : 0);
        cmd.ExecuteNonQuery();
    }
}
