using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services;
using System.Text.Json;

namespace RizaCanKilicIsTakibi.Tests;

public class BackupServiceTests
{
    [Fact]
    public async Task CreateBackup_And_RestoreBackup_RoundTrips_Data()
    {
        var root = Path.Combine(Path.GetTempPath(), "RizaCanKilicIsTakibiTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var service = new BackupService(root);
            var tasks = new List<TaskItem>
            {
                new()
                {
                    Title = "Deneme görev",
                    Description = "Açıklama",
                    BoardType = TaskBoardType.Acil,
                    Notes = { new TaskNote { Text = "not1" } }
                }
            };
            var karotEntries = new List<KarotEntry>
            {
                new()
                {
                    AdaParsel = "430-11",
                    YapiSahibi = "HİF OTEL",
                    Aciklama = "Karot süreci takip ediliyor.",
                    Status = KarotStatus.KarotAlindiSonucBekleniyor
                }
            };
            var tadilatEntries = new List<TadilatEntry>
            {
                new()
                {
                    District = "GERZE",
                    SubTab = TadilatSubTab.Aktif,
                    JobName = "738-8 MUSTAFA KIRMIZI",
                    ProjectType = "MİMARİ",
                    DigitalReceived = "EVET"
                }
            };
            var tadilatCellStates = new List<TadilatCellState>
            {
                new()
                {
                    EntryId = tadilatEntries[0].Id,
                    ColumnKey = TadilatColumnKeys.JobName,
                    BackgroundColor = "#FFF4C4C4",
                    NoteText = "Excel comment örneği"
                }
            };
            var yibfAnaBilgiEntries = new List<YibfAnaBilgiEntry>
            {
                new()
                {
                    AdaParsel = "235-1",
                    YibfNo = "1855397",
                    Idare = "İL ÖZEL İDARESİ",
                    YapiSahibi = "ORSA ENERJİ",
                    Muteahhit = "SEKVAN"
                }
            };
            var yibfAnaBilgiEvents = new List<YibfAnaBilgiEvent>
            {
                new()
                {
                    EntryId = yibfAnaBilgiEntries[0].Id,
                    EventDate = new DateTime(2021, 12, 9),
                    Description = "RUHSAT AŞAMASINDA EKSİK EVRAKLAR GELDİ",
                    BackgroundColor = "#FFFFFF00",
                    NoteText = "Ana bilgi notu",
                    DisplayOrder = 0
                }
            };
            var yibfIsTakibiEntries = new List<YibfIsTakibiEntry>
            {
                new()
                {
                    JobName = "235-1 ORSA ENERJİ",
                    MuellifBilgileriGeldiMi = "EVET"
                }
            };
            var yibfCellStates = new List<YibfCellState>
            {
                new()
                {
                    EntryId = yibfIsTakibiEntries[0].Id,
                    ColumnKey = YibfIsTakibiColumnKeys.TumProjelerinDijitaliVarMi,
                    BackgroundColor = "#FFFF0000",
                    NoteText = "Dijital eksik"
                }
            };

            var backup = await service.CreateBackupAsync(
                tasks,
                karotEntries: karotEntries,
                tadilatEntries: tadilatEntries,
                yibfAnaBilgiEntries: yibfAnaBilgiEntries,
                yibfAnaBilgiEvents: yibfAnaBilgiEvents,
                yibfIsTakibiEntries: yibfIsTakibiEntries,
                yibfCellStates: yibfCellStates,
                tadilatCellStates: tadilatCellStates);
            var restored = await service.RestoreBackupAsync(backup.BackupFilePath);

            Assert.Single(restored.Tasks);
            Assert.Equal("Deneme görev", restored.Tasks[0].Title);
            Assert.Single(restored.Tasks[0].Notes);
            Assert.Empty(restored.ActionEntries);
            Assert.Single(restored.KarotEntries);
            Assert.Equal("430-11", restored.KarotEntries[0].AdaParsel);
            Assert.Equal(KarotStatus.KarotAlindiSonucBekleniyor, restored.KarotEntries[0].Status);
            Assert.Single(restored.TadilatEntries);
            Assert.Equal("GERZE", restored.TadilatEntries[0].District);
            Assert.Single(restored.TadilatCellStates);
            Assert.Equal("Excel comment örneği", restored.TadilatCellStates[0].NoteText);
            Assert.Single(restored.YibfAnaBilgiEntries);
            Assert.Single(restored.YibfAnaBilgiEvents);
            Assert.Single(restored.YibfIsTakibiEntries);
            Assert.Single(restored.YibfCellStates);
            Assert.Equal("235-1", restored.YibfAnaBilgiEntries[0].AdaParsel);
            Assert.Equal("Ana bilgi notu", restored.YibfAnaBilgiEvents[0].NoteText);
            Assert.Equal("Dijital eksik", restored.YibfCellStates[0].NoteText);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, true);
            }
        }
    }

    [Fact]
    public async Task RunScheduledCallbackOnceAsync_Does_Not_Run_Overlap_Callbacks()
    {
        var root = Path.Combine(Path.GetTempPath(), "RizaCanKilicIsTakibiTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var service = new BackupService(root);
            var started = 0;
            var invocationCount = 0;
            var maxConcurrent = 0;

            service.ScheduleAutoBackup(TimeSpan.FromMinutes(1), async () =>
            {
                Interlocked.Increment(ref invocationCount);
                var current = Interlocked.Increment(ref started);
                InterlockedExtensions.Max(ref maxConcurrent, current);
                await Task.Delay(75);
                Interlocked.Decrement(ref started);
            });

            await Task.WhenAll(
                service.RunScheduledCallbackOnceAsync(),
                service.RunScheduledCallbackOnceAsync());

            Assert.Equal(1, invocationCount);
            Assert.Equal(1, maxConcurrent);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, true);
            }
        }
    }

    [Fact]
    public async Task CreateBackupAsync_Writes_Atomically_Without_Leaving_Temp_File()
    {
        var root = Path.Combine(Path.GetTempPath(), "RizaCanKilicIsTakibiTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var service = new BackupService(root);
            var targetPath = Path.Combine(root, "manual-backup.json");

            await service.CreateBackupAsync(
                [new TaskItem { Title = "Test", Description = "A", BoardType = TaskBoardType.Genel }],
                targetPath);

            Assert.True(File.Exists(targetPath));
            Assert.Empty(Directory.EnumerateFiles(root, "*.tmp", SearchOption.TopDirectoryOnly));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, true);
            }
        }
    }

    [Fact]
    public async Task ClearManagedBackupsAsync_Deletes_Only_Managed_Backup_Files()
    {
        var root = Path.Combine(Path.GetTempPath(), "RizaCanKilicIsTakibiTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var service = new BackupService(root);
            var managedPath = Path.Combine(root, "backup_20260322_120000.json");
            var customJsonPath = Path.Combine(root, "manual-export.json");
            var textPath = Path.Combine(root, "backup_notes.txt");

            await File.WriteAllTextAsync(managedPath, "{}");
            await File.WriteAllTextAsync(customJsonPath, "{}");
            await File.WriteAllTextAsync(textPath, "notes");

            var deletedCount = await service.ClearManagedBackupsAsync();

            Assert.Equal(1, deletedCount);
            Assert.False(File.Exists(managedPath));
            Assert.True(File.Exists(customJsonPath));
            Assert.True(File.Exists(textPath));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, true);
            }
        }
    }

    [Fact]
    public async Task RestoreBackupAsync_Allows_Legacy_Backup_Without_Schema_Or_Checksum()
    {
        var root = Path.Combine(Path.GetTempPath(), "RizaCanKilicIsTakibiTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var service = new BackupService(root);
            var path = Path.Combine(root, "legacy-backup.json");
            var taskId = Guid.NewGuid();

            await File.WriteAllTextAsync(path, $$"""
            {
              "createdAt": "2026-03-22T12:00:00+03:00",
              "tasks": [
                {
                  "id": "{{taskId}}",
                  "title": "Legacy görev",
                  "description": "Açıklama",
                  "dueDate": null,
                  "createdAt": "2026-03-22T12:00:00+03:00",
                  "updatedAt": "2026-03-22T12:00:00+03:00",
                  "boardType": 0,
                  "sortOrder": 0,
                  "notes": []
                }
              ]
            }
            """);

            var restored = await service.RestoreBackupAsync(path);

            Assert.Single(restored.Tasks);
            Assert.Equal("Legacy görev", restored.Tasks[0].Title);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, true);
            }
        }
    }

    [Fact]
    public async Task RestoreBackupAsync_Rejects_Empty_Json_Backup()
    {
        var root = Path.Combine(Path.GetTempPath(), "RizaCanKilicIsTakibiTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var service = new BackupService(root);
            var path = Path.Combine(root, "empty-backup.json");

            await File.WriteAllTextAsync(path, "{}");

            await Assert.ThrowsAsync<InvalidDataException>(() => service.RestoreBackupAsync(path));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, true);
            }
        }
    }

    [Fact]
    public async Task CreateBackupAsync_Generates_Unique_File_Names_When_No_Path_Is_Provided()
    {
        var root = Path.Combine(Path.GetTempPath(), "RizaCanKilicIsTakibiTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var service = new BackupService(root);

            var first = await service.CreateBackupAsync([new TaskItem { Title = "Bir", BoardType = TaskBoardType.Genel }]);
            var second = await service.CreateBackupAsync([new TaskItem { Title = "Iki", BoardType = TaskBoardType.Genel }]);

            Assert.NotEqual(first.BackupFilePath, second.BackupFilePath);
            Assert.True(File.Exists(first.BackupFilePath));
            Assert.True(File.Exists(second.BackupFilePath));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, true);
            }
        }
    }

    [Fact]
    public async Task RestoreBackupAsync_Rejects_Unsupported_SchemaVersion()
    {
        var root = Path.Combine(Path.GetTempPath(), "RizaCanKilicIsTakibiTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var service = new BackupService(root);
            var backup = await service.CreateBackupAsync(
                [new TaskItem { Title = "Test", Description = "A", BoardType = TaskBoardType.Genel }]);

            var json = await File.ReadAllTextAsync(backup.BackupFilePath);
            using var document = JsonDocument.Parse(json);
            var mutated = new Dictionary<string, object?>();
            foreach (var property in document.RootElement.EnumerateObject())
            {
                mutated[property.Name] = property.Name == "schemaVersion"
                    ? 999
                    : JsonSerializer.Deserialize<object?>(property.Value.GetRawText());
            }

            await File.WriteAllTextAsync(backup.BackupFilePath, JsonSerializer.Serialize(mutated, new JsonSerializerOptions(JsonSerializerDefaults.Web)));

            await Assert.ThrowsAsync<InvalidDataException>(() => service.RestoreBackupAsync(backup.BackupFilePath));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, true);
            }
        }
    }

    [Fact]
    public async Task RestoreBackupAsync_Rejects_Backup_With_Invalid_Checksum()
    {
        var root = Path.Combine(Path.GetTempPath(), "RizaCanKilicIsTakibiTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var service = new BackupService(root);
            var backup = await service.CreateBackupAsync(
                [new TaskItem { Title = "Test", Description = "A", BoardType = TaskBoardType.Genel }]);

            var json = await File.ReadAllTextAsync(backup.BackupFilePath);
            using var document = JsonDocument.Parse(json);
            var mutated = new Dictionary<string, object?>();
            foreach (var property in document.RootElement.EnumerateObject())
            {
                mutated[property.Name] = property.Name == "checksum"
                    ? "DEADBEEF"
                    : JsonSerializer.Deserialize<object?>(property.Value.GetRawText());
            }

            await File.WriteAllTextAsync(backup.BackupFilePath, JsonSerializer.Serialize(mutated, new JsonSerializerOptions(JsonSerializerDefaults.Web)));

            await Assert.ThrowsAsync<InvalidDataException>(() => service.RestoreBackupAsync(backup.BackupFilePath));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, true);
            }
        }
    }

    [Fact]
    public async Task ClearManagedBackupsAsync_Returns_Zero_For_Empty_Directory()
    {
        var root = Path.Combine(Path.GetTempPath(), "RizaCanKilicIsTakibiTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var service = new BackupService(root);

            var deletedCount = await service.ClearManagedBackupsAsync();

            Assert.Equal(0, deletedCount);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, true);
            }
        }
    }

    [Fact]
    public async Task ClearManagedBackupsAsync_Deletes_All_Managed_Backups_And_Nothing_Else()
    {
        var root = Path.Combine(Path.GetTempPath(), "RizaCanKilicIsTakibiTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var service = new BackupService(root);

            // Uygulamanın ürettiği yönetilen backup'lar
            var managed1 = Path.Combine(root, "backup_20260101_100000.json");
            var managed2 = Path.Combine(root, "backup_20260201_120000.json");
            var managed3 = Path.Combine(root, "backup_20260301_083000.json");

            // Kullanıcının veya başka araçların eklediği dosyalar
            var userJson  = Path.Combine(root, "export.json");
            var notesFile = Path.Combine(root, "backup_notes.txt");
            var otherFile = Path.Combine(root, "readme.md");

            foreach (var f in new[] { managed1, managed2, managed3, userJson, notesFile, otherFile })
            {
                await File.WriteAllTextAsync(f, "{}");
            }

            var deletedCount = await service.ClearManagedBackupsAsync();

            Assert.Equal(3, deletedCount);
            Assert.False(File.Exists(managed1));
            Assert.False(File.Exists(managed2));
            Assert.False(File.Exists(managed3));
            Assert.True(File.Exists(userJson));
            Assert.True(File.Exists(notesFile));
            Assert.True(File.Exists(otherFile));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, true);
            }
        }
    }

    private static class InterlockedExtensions
    {
        public static void Max(ref int target, int candidate)
        {
            int current;
            do
            {
                current = target;
                if (candidate <= current)
                {
                    return;
                }
            }
            while (Interlocked.CompareExchange(ref target, candidate, current) != current);
        }
    }

}


