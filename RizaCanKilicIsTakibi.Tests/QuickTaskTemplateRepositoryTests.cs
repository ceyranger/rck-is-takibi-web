using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services;

namespace RizaCanKilicIsTakibi.Tests;

public class QuickTaskTemplateRepositoryTests
{
    [Fact]
    public void Constructor_Creates_Table_And_Seeds_Default_Templates()
    {
        var root = CreateTempRoot();
        var databasePath = Path.Combine(root, "quick-templates.db");

        try
        {
            var repository = new SqliteQuickTaskTemplateRepository(databasePath);

            var templates = repository.GetAll();

            Assert.Contains(templates, item => item.Title == "Eksik evrak istenecek");
            Assert.Contains(templates, item => item.Title == "YİBF takibi yapılacak");
            Assert.Equal(Enumerable.Range(0, templates.Count), templates.Select(item => item.SortOrder));
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
                SortOrder = repository.GetAll().Count,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            await repository.SaveAsync(template);

            var reloaded = new SqliteQuickTaskTemplateRepository(databasePath);
            Assert.Contains(reloaded.GetAll(), item => item.Id == template.Id && item.Title == "Yeni hızlı iş");

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
    public async Task Deleted_Defaults_Are_Not_Seeded_Again()
    {
        var root = CreateTempRoot();
        var databasePath = Path.Combine(root, "quick-template-delete-defaults.db");

        try
        {
            var repository = new SqliteQuickTaskTemplateRepository(databasePath);
            foreach (var template in repository.GetAll())
            {
                await repository.DeleteAsync(template.Id);
            }

            var reloaded = new SqliteQuickTaskTemplateRepository(databasePath);

            Assert.Empty(reloaded.GetAll());
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
