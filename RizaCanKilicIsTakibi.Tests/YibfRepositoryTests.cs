using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services;

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
}



