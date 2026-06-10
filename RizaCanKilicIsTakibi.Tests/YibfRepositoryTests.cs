using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services;
using Microsoft.Data.Sqlite;

namespace RizaCanKilicIsTakibi.Tests;

public class YibfRepositoryTests
{
    [Fact]
    public async Task SaveMany_Allows_Null_EventDate()
    {
        var root = Path.Combine(Path.GetTempPath(), "RizaCanKilicIsTakibiTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var databasePath = Path.Combine(root, "yibf.db");

        try
        {
            var repository = new SqliteYibfRepository(databasePath);
            var entry = new YibfAnaBilgiEntry
            {
                AdaParsel = "347-1",
                YibfNo = "2076147",
                Idare = "BOYABAT BELEDİYESİ",
                YapiSahibi = "UĞUR DEMİR",
                Muteahhit = "ADEM IŞIK",
                DisplayOrder = 0
            };
            var anaBilgiEvent = new YibfAnaBilgiEvent
            {
                EntryId = entry.Id,
                EventDate = null,
                Description = "Tarihsiz test olayı",
                BackgroundColor = "#FFFFFF00",
                NoteText = "SQLite null date testi",
                DisplayOrder = 0
            };

            await repository.SaveManyAsync(
                new[] { entry },
                new[] { anaBilgiEvent },
                Array.Empty<YibfIsTakibiEntry>(),
                Array.Empty<YibfCellState>());

            var events = await repository.GetAnaBilgiEventsAsync();

            Assert.Single(events);
            Assert.Null(events[0].EventDate);
            Assert.Equal("Tarihsiz test olayı", events[0].Description);
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

    [Fact]
    public async Task SaveMany_Persists_WorkIdentity_For_Exact_And_Variant_IsTakibi_Rows()
    {
        var root = Path.Combine(Path.GetTempPath(), "RizaCanKilicIsTakibiTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var databasePath = Path.Combine(root, "yibf.db");

        try
        {
            var repository = new SqliteYibfRepository(databasePath);
            var anaBilgi = new YibfAnaBilgiEntry
            {
                Id = Guid.NewGuid(),
                AdaParsel = "725-4",
                YapiSahibi = "CEMALETTİN ERSOY",
                DisplayOrder = 0
            };
            var exact = new YibfIsTakibiEntry
            {
                Id = Guid.NewGuid(),
                JobName = "725-4 CEMALETTİN ERSOY",
                DisplayOrder = 0
            };
            var variant = new YibfIsTakibiEntry
            {
                Id = Guid.NewGuid(),
                JobName = "725-4 CEMALETTİN ERSOY A BLOK",
                DisplayOrder = 1
            };

            await repository.SaveManyAsync([anaBilgi], [], [exact, variant], []);

            var anaBilgiRows = await repository.GetAnaBilgiEntriesAsync();
            var isTakibiRows = await repository.GetIsTakibiEntriesAsync();

            var persistedAnaBilgi = Assert.Single(anaBilgiRows);
            var persistedExact = Assert.Single(isTakibiRows, entry => entry.Id == exact.Id);
            var persistedVariant = Assert.Single(isTakibiRows, entry => entry.Id == variant.Id);

            Assert.Equal(persistedAnaBilgi.WorkGroupId, persistedExact.WorkGroupId);
            Assert.Equal(persistedAnaBilgi.WorkIdentityId, persistedExact.WorkIdentityId);
            Assert.Equal(persistedAnaBilgi.WorkGroupId, persistedVariant.WorkGroupId);
            Assert.Equal(variant.Id, persistedVariant.WorkIdentityId);
            Assert.Equal("A BLOK", persistedVariant.WorkVariantLabel);
        }
        finally
        {
            DeleteDirectoryBestEffort(root);
        }
    }

    [Fact]
    public async Task Constructor_Migrates_Old_Yibf_Schema_And_Creates_PreMigration_Backup()
    {
        var root = Path.Combine(Path.GetTempPath(), "RizaCanKilicIsTakibiTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var databasePath = Path.Combine(root, "yibf.db");
        var anaBilgiId = Guid.NewGuid();
        var isTakibiId = Guid.NewGuid();

        try
        {
            await using (var connection = new SqliteConnection($"Data Source={databasePath}"))
            {
                await connection.OpenAsync();
                await using var command = connection.CreateCommand();
                command.CommandText = """
CREATE TABLE YibfAnaBilgiEntries (
    Id TEXT PRIMARY KEY,
    AdaParsel TEXT NOT NULL DEFAULT '',
    YibfNo TEXT NOT NULL DEFAULT '',
    Idare TEXT NOT NULL DEFAULT '',
    YapiSahibi TEXT NOT NULL DEFAULT '',
    Muteahhit TEXT NOT NULL DEFAULT '',
    DisplayOrder INTEGER NOT NULL,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL,
    IsDeleted INTEGER NOT NULL DEFAULT 0
);

CREATE TABLE YibfAnaBilgiEvents (
    Id TEXT PRIMARY KEY,
    EntryId TEXT NOT NULL,
    EventDate TEXT NOT NULL DEFAULT '',
    Description TEXT NOT NULL DEFAULT '',
    BackgroundColor TEXT NOT NULL DEFAULT '',
    NoteText TEXT NOT NULL DEFAULT '',
    DisplayOrder INTEGER NOT NULL
);

CREATE TABLE YibfIsTakibiEntries (
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
    UpdatedAt TEXT NOT NULL,
    IsDeleted INTEGER NOT NULL DEFAULT 0
);

CREATE TABLE YibfCellStates (
    EntryId TEXT NOT NULL,
    ColumnKey TEXT NOT NULL,
    BackgroundColor TEXT NOT NULL DEFAULT '',
    NoteText TEXT NOT NULL DEFAULT '',
    PRIMARY KEY (EntryId, ColumnKey)
);

INSERT INTO YibfAnaBilgiEntries (Id, AdaParsel, YapiSahibi, DisplayOrder, CreatedAt, UpdatedAt)
VALUES ($anaBilgiId, '725-4', 'CEMALETTİN ERSOY', 0, $now, $now);

INSERT INTO YibfIsTakibiEntries (Id, JobName, DisplayOrder, CreatedAt, UpdatedAt)
VALUES ($isTakibiId, '725-4 CEMALETTİN ERSOY', 0, $now, $now);
""";
                command.Parameters.AddWithValue("$anaBilgiId", anaBilgiId.ToString());
                command.Parameters.AddWithValue("$isTakibiId", isTakibiId.ToString());
                command.Parameters.AddWithValue("$now", DateTime.Now.ToString("O"));
                await command.ExecuteNonQueryAsync();
            }

            var repository = new SqliteYibfRepository(databasePath);
            var anaBilgiRows = await repository.GetAnaBilgiEntriesAsync();
            var isTakibiRows = await repository.GetIsTakibiEntriesAsync();

            Assert.Equal(anaBilgiId, Assert.Single(anaBilgiRows).WorkGroupId);
            Assert.Equal(isTakibiId, Assert.Single(isTakibiRows).WorkGroupId);
            Assert.True(Directory.Exists(Path.Combine(root, "Backup")));
            Assert.Contains(Directory.EnumerateDirectories(Path.Combine(root, "Backup")), path => Path.GetFileName(path).StartsWith("schema-migration-yibf-work-id-", StringComparison.Ordinal));
        }
        finally
        {
            DeleteDirectoryBestEffort(root);
        }
    }

    private static void DeleteDirectoryBestEffort(string path)
    {
        if (!Directory.Exists(path))
        {
            return;
        }

        for (var attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                Directory.Delete(path, true);
                break;
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
                break;
            }
            catch (UnauthorizedAccessException)
            {
                break;
            }
        }
    }
}



