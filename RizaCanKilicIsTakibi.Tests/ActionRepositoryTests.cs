using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services;

namespace RizaCanKilicIsTakibi.Tests;

public class ActionRepositoryTests
{
    [Fact]
    public async Task SaveManyAsync_Replaces_Stored_Set_Atomically()
    {
        var root = Path.Combine(Path.GetTempPath(), "RizaCanKilicIsTakibiTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var databasePath = Path.Combine(root, "action.db");

        try
        {
            var repository = new SqliteActionRepository(databasePath);
            var stale = new ActionEntry
            {
                Id = Guid.NewGuid(),
                Category = ActionEntryCategory.Aksiyon,
                District = "MERKEZ",
                OwnerParcelText = "Eski kayıt",
                WorkText = "Silinecek",
                DisplayOrder = 0,
                CreatedAt = DateTime.Now.AddDays(-2),
                UpdatedAt = DateTime.Now.AddDays(-2)
            };
            var kept = new ActionEntry
            {
                Id = Guid.NewGuid(),
                Category = ActionEntryCategory.Aksiyon,
                District = "MERKEZ",
                OwnerParcelText = "Guncel kayıt",
                WorkText = "Yeni iş",
                DisplayOrder = 0,
                CreatedAt = DateTime.Now.AddDays(-1),
                UpdatedAt = DateTime.Now.AddDays(-1)
            };
            var added = new ActionEntry
            {
                Category = ActionEntryCategory.AksiyonaEklenecekler,
                District = "AYANCIK",
                OwnerParcelText = "Yeni giriş",
                WorkText = "Eklenecek iş",
                DisplayOrder = 0,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            await repository.SaveManyAsync(new[] { stale, kept });

            kept.WorkText = "Güncellenmiş iş";
            kept.DisplayOrder = 1;

            await repository.SaveManyAsync(new[] { kept, added });

            var aksiyon = await repository.GetByCategoryAsync(ActionEntryCategory.Aksiyon);
            var eklenecekler = await repository.GetByCategoryAsync(ActionEntryCategory.AksiyonaEklenecekler);

            Assert.Single(aksiyon);
            Assert.Single(eklenecekler);
            Assert.Equal(kept.Id, aksiyon[0].Id);
            Assert.Equal("Güncellenmiş iş", aksiyon[0].WorkText);
            Assert.Equal("Yeni giriş", eklenecekler[0].OwnerParcelText);
        }
        finally
        {
            if (Directory.Exists(root))
            {
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
    }
}
