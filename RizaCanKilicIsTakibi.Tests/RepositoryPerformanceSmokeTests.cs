using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services;
using System.Diagnostics;
using Xunit.Abstractions;

namespace RizaCanKilicIsTakibi.Tests;

public class RepositoryPerformanceSmokeTests
{
    private readonly ITestOutputHelper _output;

    public RepositoryPerformanceSmokeTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task SaveMany_Handles_5000_Record_Smoke_Test_On_Temp_Databases()
    {
        await MeasureTadilatAsync();
        await MeasureYibfAsync();
        await MeasureKarotAsync();
        await MeasureMissingProjectAsync();
    }

    private async Task MeasureTadilatAsync()
    {
        await ExecuteWithTempDirectoryAsync("tadilat", async databasePath =>
        {
            var repository = new SqliteTadilatRepository(databasePath);
            var entries = Enumerable.Range(0, 5000)
                .Select(index => new TadilatEntry
                {
                    District = $"ILCE-{index % 12:D2}",
                    JobName = $"Tadilat İşi {index}",
                    ProjectType = $"Proje Türü {index % 5}",
                    DigitalReceived = index % 2 == 0 ? "Var" : "Bekleniyor",
                    InspectorApproved = index % 3 == 0 ? "Tamam" : string.Empty,
                    OutputAndReportArrived = index % 4 == 0 ? "Hazır" : string.Empty,
                    OfficialLetterSubmitted = index % 5 == 0 ? "Verildi" : string.Empty,
                    ArchivedFromMunicipality = index % 7 == 0 ? "Arşivde" : string.Empty,
                    Description1 = $"Açıklama-1 {index}",
                    Description2 = $"Açıklama-2 {index}",
                    DisplayOrder = index,
                    SubTab = index % 6 == 0 ? TadilatSubTab.Biten : TadilatSubTab.Aktif
                })
                .ToList();

            var cellStates = entries
                .Where((_, index) => index % 3 == 0)
                .Select(entry => new TadilatCellState
                {
                    EntryId = entry.Id,
                    ColumnKey = TadilatColumnKeys.JobName,
                    BackgroundColor = "#FFF4B400",
                    NoteText = $"Not {entry.JobName}"
                })
                .ToList();

            var initialSave = Stopwatch.StartNew();
            await repository.SaveManyAsync(entries, cellStates);
            initialSave.Stop();

            foreach (var entry in entries.Where((_, index) => index % 10 == 0))
            {
                entry.Description2 = $"{entry.Description2} - güncellendi";
            }

            entries.RemoveRange(entries.Count - 250, 250);
            entries.AddRange(Enumerable.Range(0, 250).Select(index => new TadilatEntry
            {
                District = "ILCE-YENI",
                JobName = $"Yeni Tadilat {index}",
                ProjectType = "Yeni Tür",
                Description1 = "Yeni kayıt",
                Description2 = "Yeni açıklama",
                DisplayOrder = entries.Count + index,
                SubTab = TadilatSubTab.Aktif
            }));

            var secondSave = Stopwatch.StartNew();
            await repository.SaveManyAsync(entries, cellStates.Where(state => entries.Any(entry => entry.Id == state.EntryId)));
            secondSave.Stop();

            var load = Stopwatch.StartNew();
            var loadedEntries = await repository.GetAllAsync();
            var loadedStates = await repository.GetCellStatesAsync();
            load.Stop();

            Assert.Equal(5000, loadedEntries.Count);
            Assert.True(loadedStates.Count > 0);
            _output.WriteLine($"Tadilat | ilk kaydet: {initialSave.ElapsedMilliseconds} ms | ikinci kaydet: {secondSave.ElapsedMilliseconds} ms | yükleme: {load.ElapsedMilliseconds} ms");
        });
    }

    private async Task MeasureYibfAsync()
    {
        await ExecuteWithTempDirectoryAsync("yibf", async databasePath =>
        {
            var repository = new SqliteYibfRepository(databasePath);

            var anaBilgiEntries = Enumerable.Range(0, 2500)
                .Select(index => new YibfAnaBilgiEntry
                {
                    AdaParsel = $"{200 + index}-{index % 30}",
                    YibfNo = $"YIBF-{index:D6}",
                    Idare = $"İdare {index % 10}",
                    YapiSahibi = $"Yapı Sahibi {index}",
                    Muteahhit = $"Müteahhit {index % 300}",
                    DisplayOrder = index
                })
                .ToList();

            var events = anaBilgiEntries
                .Select((entry, index) => new YibfAnaBilgiEvent
                {
                    EntryId = entry.Id,
                    EventDate = DateTime.Today.AddDays(-(index % 45)),
                    Description = $"Olay {index}",
                    BackgroundColor = index % 2 == 0 ? "#FFF4B400" : "#FF4F81BD",
                    NoteText = $"Not {index}",
                    DisplayOrder = index
                })
                .ToList();

            var isTakibiEntries = Enumerable.Range(0, 2500)
                .Select(index => new YibfIsTakibiEntry
                {
                    JobName = $"YİBF İş {index}",
                    MuellifBilgileriGeldiMi = index % 2 == 0 ? "Evet" : string.Empty,
                    DenetciAtamalariYapildiMi = index % 3 == 0 ? "Evet" : string.Empty,
                    TumProjelerinDijitaliVarMi = index % 4 == 0 ? "Evet" : string.Empty,
                    EvraklarTamMi = index % 5 == 0 ? "Evet" : string.Empty,
                    YibfSozlesmeHazirlandiMi = index % 6 == 0 ? "Evet" : string.Empty,
                    DekontAlindiMi = index % 7 == 0 ? "Evet" : string.Empty,
                    RuhsatBasvurusuYapildiMi = index % 8 == 0 ? "Evet" : string.Empty,
                    RuhsatNushasiAlindiMi = index % 9 == 0 ? "Evet" : string.Empty,
                    IsyeriTeslimTutangiHazirlandiMi = index % 10 == 0 ? "Evet" : string.Empty,
                    IsgYazisiHazirlandiMi = index % 11 == 0 ? "Evet" : string.Empty,
                    SaglikGuvenlikPlaniGeldiMi = index % 12 == 0 ? "Evet" : string.Empty,
                    TemelTopraklamaTutanagiHazirlandiMi = index % 13 == 0 ? "Evet" : string.Empty,
                    DisplayOrder = index
                })
                .ToList();

            var cellStates = isTakibiEntries
                .Where((_, index) => index % 3 == 0)
                .Select(entry => new YibfCellState
                {
                    EntryId = entry.Id,
                    ColumnKey = YibfIsTakibiColumnKeys.JobName,
                    BackgroundColor = "#FFF4B400",
                    NoteText = $"Not {entry.JobName}"
                })
                .ToList();

            var initialSave = Stopwatch.StartNew();
            await repository.SaveManyAsync(anaBilgiEntries, events, isTakibiEntries, cellStates);
            initialSave.Stop();

            foreach (var entry in anaBilgiEntries.Where((_, index) => index % 8 == 0))
            {
                entry.Idare = $"{entry.Idare} - güncellendi";
            }

            anaBilgiEntries.RemoveRange(anaBilgiEntries.Count - 125, 125);
            isTakibiEntries.RemoveRange(isTakibiEntries.Count - 125, 125);

            anaBilgiEntries.AddRange(Enumerable.Range(0, 125).Select(index => new YibfAnaBilgiEntry
            {
                AdaParsel = $"YENI-{index}",
                YibfNo = $"YENI-{index:D5}",
                Idare = "Yeni İdare",
                YapiSahibi = $"Yeni Sahip {index}",
                Muteahhit = $"Yeni Müteahhit {index}",
                DisplayOrder = anaBilgiEntries.Count + index
            }));

            isTakibiEntries.AddRange(Enumerable.Range(0, 125).Select(index => new YibfIsTakibiEntry
            {
                JobName = $"Yeni YİBF İş {index}",
                DisplayOrder = isTakibiEntries.Count + index
            }));

            var secondSave = Stopwatch.StartNew();
            await repository.SaveManyAsync(
                anaBilgiEntries,
                events.Where(item => anaBilgiEntries.Any(entry => entry.Id == item.EntryId)),
                isTakibiEntries,
                cellStates.Where(state => isTakibiEntries.Any(entry => entry.Id == state.EntryId)));
            secondSave.Stop();

            var load = Stopwatch.StartNew();
            var loadedAnaBilgi = await repository.GetAnaBilgiEntriesAsync();
            var loadedEvents = await repository.GetAnaBilgiEventsAsync();
            var loadedRows = await repository.GetIsTakibiEntriesAsync();
            var loadedStates = await repository.GetCellStatesAsync();
            load.Stop();

            Assert.Equal(2500, loadedAnaBilgi.Count);
            Assert.Equal(2500, loadedRows.Count);
            Assert.True(loadedEvents.Count > 0);
            Assert.True(loadedStates.Count > 0);
            _output.WriteLine($"YİBF | ilk kaydet: {initialSave.ElapsedMilliseconds} ms | ikinci kaydet: {secondSave.ElapsedMilliseconds} ms | yükleme: {load.ElapsedMilliseconds} ms");
        });
    }

    private async Task MeasureKarotAsync()
    {
        await ExecuteWithTempDirectoryAsync("karot", async databasePath =>
        {
            var repository = new SqliteKarotRepository(databasePath);
            var entries = Enumerable.Range(0, 5000)
                .Select(index => new KarotEntry
                {
                    SampleReceivedDate = DateTime.Today.AddDays(-(index % 30)),
                    YibfNo = $"K-{index:D6}",
                    AdaParsel = $"{index + 10}-{index % 15}",
                    YapiSahibi = $"Sahip {index}",
                    Muteahhit = $"Müteahhit {index % 400}",
                    KatBilgisi = $"{index % 10}. Kat",
                    BetonSinifi = "C30",
                    TwentyEightDayResult = index % 4 == 0 ? "Olumlu" : string.Empty,
                    BetonFirmasi = $"Firma {index % 20}",
                    Laboratuvar = $"Lab {index % 15}",
                    Aciklama = $"Karot açıklama {index}",
                    Status = (KarotStatus)(index % 4),
                    DisplayOrder = index
                })
                .ToList();

            var cellStates = entries
                .Where((_, index) => index % 3 == 0)
                .Select(entry => new KarotCellState
                {
                    EntryId = entry.Id,
                    ColumnKey = KarotColumnKeys.Aciklama,
                    NoteText = $"Not {entry.AdaParsel}"
                })
                .ToList();

            var initialSave = Stopwatch.StartNew();
            await repository.SaveManyAsync(entries, cellStates);
            initialSave.Stop();

            foreach (var entry in entries.Where((_, index) => index % 12 == 0))
            {
                entry.Aciklama = $"{entry.Aciklama} - güncellendi";
            }

            entries.RemoveRange(entries.Count - 300, 300);
            entries.AddRange(Enumerable.Range(0, 300).Select(index => new KarotEntry
            {
                YibfNo = $"KY-{index:D5}",
                AdaParsel = $"Y-{index}",
                YapiSahibi = $"Yeni Sahip {index}",
                Muteahhit = $"Yeni Müteahhit {index}",
                KatBilgisi = "Zemin",
                BetonSinifi = "C35",
                Status = KarotStatus.KarotAlinacak,
                DisplayOrder = entries.Count + index
            }));

            var secondSave = Stopwatch.StartNew();
            await repository.SaveManyAsync(entries, cellStates.Where(state => entries.Any(entry => entry.Id == state.EntryId)));
            secondSave.Stop();

            var load = Stopwatch.StartNew();
            var loadedEntries = await repository.GetAllAsync();
            var loadedStates = await repository.GetCellStatesAsync();
            load.Stop();

            Assert.Equal(5000, loadedEntries.Count);
            Assert.True(loadedStates.Count > 0);
            _output.WriteLine($"Karot | ilk kaydet: {initialSave.ElapsedMilliseconds} ms | ikinci kaydet: {secondSave.ElapsedMilliseconds} ms | yükleme: {load.ElapsedMilliseconds} ms");
        });
    }

    private async Task MeasureMissingProjectAsync()
    {
        await ExecuteWithTempDirectoryAsync("eksikproje", async databasePath =>
        {
            var repository = new SqliteMissingProjectRepository(databasePath);
            var entries = Enumerable.Range(0, 5000)
                .Select(index => new MissingProjectEntry
                {
                    AdaParsel = $"{300 + index}-{index % 25}",
                    YapiSahibi = $"Sahip {index}",
                    RecordMedium = (MissingProjectMedium)(index % 3),
                    RecordMediumText = (index % 3) switch
                    {
                        0 => "Dijital",
                        1 => "Fiziksel",
                        _ => "Fiziksel + Dijital"
                    },
                    MissingProjectText = $"Eksik Proje {index % 15}",
                    Description = $"Açıklama {index}",
                    DisplayOrder = index
                })
                .ToList();

            var cellStates = entries
                .Where((_, index) => index % 4 == 0)
                .Select(entry => new MissingProjectCellState
                {
                    EntryId = entry.Id,
                    ColumnKey = MissingProjectColumnKeys.MissingProjectText,
                    BackgroundColor = "#FFF4B400",
                    NoteText = $"Not {entry.AdaParsel}"
                })
                .ToList();

            var initialSave = Stopwatch.StartNew();
            await repository.SaveManyAsync(entries, cellStates);
            initialSave.Stop();

            foreach (var entry in entries.Where((_, index) => index % 9 == 0))
            {
                entry.Description = $"{entry.Description} - güncellendi";
            }

            entries.RemoveRange(entries.Count - 300, 300);
            entries.AddRange(Enumerable.Range(0, 300).Select(index => new MissingProjectEntry
            {
                AdaParsel = $"MP-{index}",
                YapiSahibi = $"Yeni Sahip {index}",
                RecordMedium = MissingProjectMedium.Dijital,
                RecordMediumText = "Dijital",
                MissingProjectText = "Yeni Eksik Proje",
                Description = "Yeni açıklama",
                DisplayOrder = entries.Count + index
            }));

            var secondSave = Stopwatch.StartNew();
            await repository.SaveManyAsync(entries, cellStates.Where(state => entries.Any(entry => entry.Id == state.EntryId)));
            secondSave.Stop();

            var load = Stopwatch.StartNew();
            var loadedEntries = await repository.GetAllAsync();
            var loadedStates = await repository.GetCellStatesAsync();
            load.Stop();

            Assert.Equal(5000, loadedEntries.Count);
            Assert.True(loadedStates.Count > 0);
            _output.WriteLine($"Eksik Proje | ilk kaydet: {initialSave.ElapsedMilliseconds} ms | ikinci kaydet: {secondSave.ElapsedMilliseconds} ms | yükleme: {load.ElapsedMilliseconds} ms");
        });
    }

    private static async Task ExecuteWithTempDirectoryAsync(string prefix, Func<string, Task> action)
    {
        var root = Path.Combine(Path.GetTempPath(), "RizaCanKilicIsTakibiPerf", $"{prefix}_{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        var databasePath = Path.Combine(root, "perf.db");

        try
        {
            await action(databasePath);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                for (var attempt = 0; attempt < 5; attempt++)
                {
                    try
                    {
                        Directory.Delete(root, true);
                        break;
                    }
                    catch (IOException) when (attempt < 4)
                    {
                        await Task.Delay(100);
                    }
                    catch (UnauthorizedAccessException) when (attempt < 4)
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
