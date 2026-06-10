using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services;
using Microsoft.Data.Sqlite;

namespace RizaCanKilicIsTakibi.Tests;

public class QuickTaskTemplateRepositoryTests
{
    [Fact]
    public void Constructor_Creates_Table_Without_Default_Templates()
    {
        var root = CreateTempRoot();
        var databasePath = Path.Combine(root, "quick-templates.db");

        try
        {
            var repository = new SqliteQuickTaskTemplateRepository(databasePath);

            var templates = repository.GetAll();

            Assert.Empty(templates);
        }
        finally
        {
            DeleteDirectoryWithRetries(root);
        }
    }

    [Fact]
    public async Task Save_And_Delete_Persist_Across_Reload()
    {
        var root = CreateTempRoot();
        var databasePath = Path.Combine(root, "quick-template-save.db");

        try
        {
            var repository = new SqliteQuickTaskTemplateRepository(databasePath);
            var template = new QuickTaskTemplate
            {
                Title = "Yeni hızlı iş",
                GroupName = "Aybaşı İşlemleri",
                SortOrder = repository.GetAll().Count,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            await repository.SaveAsync(template);

            var reloaded = new SqliteQuickTaskTemplateRepository(databasePath);
            Assert.Contains(reloaded.GetAll(), item => item.Id == template.Id && item.GroupName == "Aybaşı İşlemleri" && item.Title == "Yeni hızlı iş");

            await reloaded.DeleteAsync(template.Id);

            var afterDelete = new SqliteQuickTaskTemplateRepository(databasePath);
            Assert.DoesNotContain(afterDelete.GetAll(), item => item.Id == template.Id);
        }
        finally
        {
            DeleteDirectoryWithRetries(root);
        }
    }

    [Fact]
    public void Existing_Seed_Defaults_Are_Removed_Once()
    {
        var root = CreateTempRoot();
        var databasePath = Path.Combine(root, "quick-template-delete-defaults.db");

        try
        {
            var connectionString = SqliteConnectionSettings.BuildConnectionString(databasePath);
            using (var connection = SqliteConnectionSettings.Open(connectionString))
            {
                using var command = connection.CreateCommand();
                command.CommandText = @"
CREATE TABLE QuickTaskTemplates (
    Id TEXT PRIMARY KEY,
    Title TEXT NOT NULL,
    SortOrder INTEGER NOT NULL,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL,
    IsDeleted INTEGER NOT NULL DEFAULT 0
);

INSERT INTO QuickTaskTemplates (Id, Title, SortOrder, CreatedAt, UpdatedAt, IsDeleted)
VALUES ($defaultId, 'Eksik evrak istenecek', 0, $now, $now, 0),
       ($userId, 'Kullanıcı işi', 1, $now, $now, 0);";
                command.Parameters.AddWithValue("$defaultId", Guid.NewGuid().ToString());
                command.Parameters.AddWithValue("$userId", Guid.NewGuid().ToString());
                command.Parameters.AddWithValue("$now", DateTime.Now.ToString("O"));
                command.ExecuteNonQuery();
            }

            var repository = new SqliteQuickTaskTemplateRepository(databasePath);
            var templates = repository.GetAll();

            Assert.DoesNotContain(templates, item => item.Title == "Eksik evrak istenecek");
            Assert.Contains(templates, item => item.GroupName == "Genel" && item.Title == "Kullanıcı işi");
        }
        finally
        {
            DeleteDirectoryWithRetries(root);
        }
    }

    private static string CreateTempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "RizaCanKilicIsTakibiTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    private static void DeleteDirectoryWithRetries(string root)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                if (Directory.Exists(root))
                {
                    Directory.Delete(root, true);
                }

                return;
            }
            catch (IOException) when (attempt < 2)
            {
                Thread.Sleep(100);
            }
            catch (UnauthorizedAccessException) when (attempt < 2)
            {
                Thread.Sleep(100);
            }
            catch (IOException)
            {
                return;
            }
            catch (UnauthorizedAccessException)
            {
                return;
            }
        }
    }
}
