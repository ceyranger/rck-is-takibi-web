using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services.Abstractions;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Windows.Threading;

namespace RizaCanKilicIsTakibi.Services;

public sealed class BackupService : IBackupService
{
    private const int CurrentBackupSchemaVersion = 2;
    private readonly string _backupRoot;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private DispatcherTimer? _timer;
    private readonly SemaphoreSlim _autoBackupGate = new(1, 1);
    private Func<Task>? _scheduledCallback;

    public BackupService(string backupRoot)
    {
        _backupRoot = backupRoot;
        Directory.CreateDirectory(_backupRoot);
    }

    public async Task<BackupMetadata> CreateBackupAsync(
        IEnumerable<TaskItem> tasks,
        string? backupPath = null,
        IEnumerable<ActionEntry>? actionEntries = null,
        IEnumerable<MissingProjectEntry>? missingProjectEntries = null,
        IEnumerable<MissingProjectCellState>? missingProjectCellStates = null,
        IEnumerable<KarotEntry>? karotEntries = null,
        IEnumerable<KarotCellState>? karotCellStates = null,
        IEnumerable<TadilatEntry>? tadilatEntries = null,
        IEnumerable<YibfAnaBilgiEntry>? yibfAnaBilgiEntries = null,
        IEnumerable<YibfAnaBilgiEvent>? yibfAnaBilgiEvents = null,
        IEnumerable<YibfIsTakibiEntry>? yibfIsTakibiEntries = null,
        IEnumerable<YibfCellState>? yibfCellStates = null,
        IEnumerable<TadilatCellState>? tadilatCellStates = null,
        IEnumerable<QuickTaskTemplate>? quickTaskTemplates = null,
        CancellationToken cancellationToken = default)
    {
        var payload = new BackupEnvelope
        {
            SchemaVersion = CurrentBackupSchemaVersion,
            AppVersion = typeof(BackupService).Assembly.GetName().Version?.ToString() ?? "unknown",
            Tasks = tasks.Select(MapTaskToDto).ToList(),
            QuickTaskTemplates = (quickTaskTemplates ?? Array.Empty<QuickTaskTemplate>()).Select(MapQuickTaskTemplateToDto).ToList(),
            ActionEntries = (actionEntries ?? Array.Empty<ActionEntry>()).Select(MapActionToDto).ToList(),
            MissingProjectEntries = (missingProjectEntries ?? Array.Empty<MissingProjectEntry>()).Select(MapMissingProjectToDto).ToList(),
            MissingProjectCellStates = (missingProjectCellStates ?? Array.Empty<MissingProjectCellState>()).Select(MapMissingProjectCellStateToDto).ToList(),
            KarotEntries = (karotEntries ?? Array.Empty<KarotEntry>()).Select(MapKarotToDto).ToList(),
            KarotCellStates = (karotCellStates ?? Array.Empty<KarotCellState>()).Select(MapKarotCellStateToDto).ToList(),
            TadilatEntries = (tadilatEntries ?? Array.Empty<TadilatEntry>()).Select(MapTadilatToDto).ToList(),
            YibfAnaBilgiEntries = (yibfAnaBilgiEntries ?? Array.Empty<YibfAnaBilgiEntry>()).Select(MapYibfAnaBilgiToDto).ToList(),
            YibfAnaBilgiEvents = (yibfAnaBilgiEvents ?? Array.Empty<YibfAnaBilgiEvent>()).Select(MapYibfAnaBilgiEventToDto).ToList(),
            YibfIsTakibiEntries = (yibfIsTakibiEntries ?? Array.Empty<YibfIsTakibiEntry>()).Select(MapYibfIsTakibiToDto).ToList(),
            YibfCellStates = (yibfCellStates ?? Array.Empty<YibfCellState>()).Select(MapYibfCellStateToDto).ToList(),
            TadilatCellStates = (tadilatCellStates ?? Array.Empty<TadilatCellState>()).Select(MapTadilatCellStateToDto).ToList(),
            CreatedAt = DateTime.Now
        };
        payload.Checksum = ComputeChecksum(payload);

        var finalPath = backupPath;
        if (string.IsNullOrWhiteSpace(finalPath))
        {
            finalPath = Path.Combine(_backupRoot, $"backup_{DateTime.Now:yyyyMMdd_HHmmss_fff}_{Guid.NewGuid():N}.json");
        }
        
        var directory = Path.GetDirectoryName(finalPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var tempPath = Path.Combine(directory ?? _backupRoot, $".{Path.GetFileName(finalPath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            await using (var stream = File.Create(tempPath))
            {
                await JsonSerializer.SerializeAsync(stream, payload, _jsonOptions, cancellationToken);
                await stream.FlushAsync(cancellationToken);
            }

            File.Move(tempPath, finalPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }

        return new BackupMetadata
        {
            CreatedAt = payload.CreatedAt,
            BackupFilePath = finalPath,
            TaskCount = payload.Tasks.Count
        };
    }

    public async Task<BackupRestoreData> RestoreBackupAsync(string backupPath, CancellationToken cancellationToken = default)
    {
        await using var stream = File.OpenRead(backupPath);
        var payload = await JsonSerializer.DeserializeAsync<BackupEnvelope>(stream, _jsonOptions, cancellationToken);

        if (payload is null)
        {
            throw new InvalidDataException("Yedek dosyası okunamadı veya boş.");
        }

        if (payload.SchemaVersion != 0 && payload.SchemaVersion != 1 && payload.SchemaVersion != CurrentBackupSchemaVersion)
        {
            throw new InvalidDataException($"Desteklenmeyen yedek sürümü: {payload.SchemaVersion}");
        }

        if (payload.SchemaVersion is 1 or CurrentBackupSchemaVersion)
        {
            if (string.IsNullOrWhiteSpace(payload.Checksum))
            {
                throw new InvalidDataException("Yedek doğrulama bilgisi eksik.");
            }

            var expectedChecksum = payload.SchemaVersion == 1
                ? ComputeChecksumV1(payload)
                : ComputeChecksum(payload);
            if (!string.Equals(payload.Checksum, expectedChecksum, StringComparison.Ordinal))
            {
                throw new InvalidDataException("Yedek dosyası bozuk veya değiştirilmiş görünüyor.");
            }
        }
        else if (!HasAnyData(payload))
        {
            throw new InvalidDataException("Yedek dosyası geçerli bir uygulama yedeği değil.");
        }

        return new BackupRestoreData
        {
            Tasks = payload.Tasks.Select(MapTaskToModel).ToList(),
            QuickTaskTemplates = payload.QuickTaskTemplates.Select(MapQuickTaskTemplateToModel).ToList(),
            ActionEntries = payload.ActionEntries.Select(MapActionToModel).ToList(),
            MissingProjectEntries = payload.MissingProjectEntries.Select(MapMissingProjectToModel).ToList(),
            MissingProjectCellStates = payload.MissingProjectCellStates.Select(MapMissingProjectCellStateToModel).ToList(),
            KarotEntries = payload.KarotEntries.Select(MapKarotToModel).ToList(),
            KarotCellStates = payload.KarotCellStates.Select(MapKarotCellStateToModel).ToList(),
            TadilatEntries = payload.TadilatEntries.Select(MapTadilatToModel).ToList(),
            YibfAnaBilgiEntries = payload.YibfAnaBilgiEntries.Select(MapYibfAnaBilgiToModel).ToList(),
            YibfAnaBilgiEvents = payload.YibfAnaBilgiEvents.Select(MapYibfAnaBilgiEventToModel).ToList(),
            YibfIsTakibiEntries = payload.YibfIsTakibiEntries.Select(MapYibfIsTakibiToModel).ToList(),
            YibfCellStates = payload.YibfCellStates.Select(MapYibfCellStateToModel).ToList(),
            TadilatCellStates = payload.TadilatCellStates.Select(MapTadilatCellStateToModel).ToList()
        };
    }

    private string ComputeChecksum(BackupEnvelope envelope)
    {
        var payloadBytes = JsonSerializer.SerializeToUtf8Bytes(new
        {
            envelope.SchemaVersion,
            envelope.AppVersion,
            envelope.CreatedAt,
            envelope.Tasks,
            envelope.QuickTaskTemplates,
            envelope.ActionEntries,
            envelope.MissingProjectEntries,
            envelope.MissingProjectCellStates,
            envelope.KarotEntries,
            envelope.KarotCellStates,
            envelope.TadilatEntries,
            envelope.YibfAnaBilgiEntries,
            envelope.YibfAnaBilgiEvents,
            envelope.YibfIsTakibiEntries,
            envelope.YibfCellStates,
            envelope.TadilatCellStates
        }, _jsonOptions);

        return Convert.ToHexString(SHA256.HashData(payloadBytes));
    }

    private string ComputeChecksumV1(BackupEnvelope envelope)
    {
        var payloadBytes = JsonSerializer.SerializeToUtf8Bytes(new
        {
            envelope.SchemaVersion,
            envelope.AppVersion,
            envelope.CreatedAt,
            envelope.Tasks,
            envelope.ActionEntries,
            envelope.MissingProjectEntries,
            envelope.MissingProjectCellStates,
            envelope.KarotEntries,
            envelope.KarotCellStates,
            envelope.TadilatEntries,
            envelope.YibfAnaBilgiEntries,
            envelope.YibfAnaBilgiEvents,
            envelope.YibfIsTakibiEntries,
            envelope.YibfCellStates,
            envelope.TadilatCellStates
        }, _jsonOptions);

        return Convert.ToHexString(SHA256.HashData(payloadBytes));
    }

    private static bool HasAnyData(BackupEnvelope envelope)
        => envelope.Tasks.Count > 0
           || envelope.QuickTaskTemplates.Count > 0
           || envelope.ActionEntries.Count > 0
           || envelope.MissingProjectEntries.Count > 0
           || envelope.MissingProjectCellStates.Count > 0
           || envelope.KarotEntries.Count > 0
           || envelope.KarotCellStates.Count > 0
           || envelope.TadilatEntries.Count > 0
           || envelope.TadilatCellStates.Count > 0
           || envelope.YibfAnaBilgiEntries.Count > 0
           || envelope.YibfAnaBilgiEvents.Count > 0
           || envelope.YibfIsTakibiEntries.Count > 0
           || envelope.YibfCellStates.Count > 0;

    public void ScheduleAutoBackup(TimeSpan interval, Func<Task> callback)
    {
        StopAutoBackup();
        _scheduledCallback = callback;

        _timer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = interval
        };

        _timer.Tick += OnAutoBackupTick;

        _timer.Start();
    }

    private async void OnAutoBackupTick(object? sender, EventArgs e)
        => await RunScheduledCallbackOnceAsync();

    internal async Task RunScheduledCallbackOnceAsync()
    {
        var callback = _scheduledCallback;
        if (callback is null)
        {
            return;
        }

        if (!await _autoBackupGate.WaitAsync(0))
        {
            return;
        }

        try
        {
            await callback();
        }
        catch
        {
            // Suppressed on timer to keep UI responsive.
        }
        finally
        {
            _autoBackupGate.Release();
        }
    }

    public void StopAutoBackup()
    {
        if (_timer is null)
        {
            return;
        }

        _timer.Stop();
        _timer.Tick -= OnAutoBackupTick;
        _timer = null;
        _scheduledCallback = null;
    }

    public Task<int> ClearManagedBackupsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var deletedCount = 0;
        if (!Directory.Exists(_backupRoot))
        {
            return Task.FromResult(deletedCount);
        }

        foreach (var file in Directory.EnumerateFiles(_backupRoot, "backup_*.json", SearchOption.TopDirectoryOnly))
        {
            cancellationToken.ThrowIfCancellationRequested();
            File.Delete(file);
            deletedCount++;
        }

        return Task.FromResult(deletedCount);
    }

    public Task<int> CleanOldBackupsAsync(int keepCount = 30, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var deletedCount = 0;
        if (!Directory.Exists(_backupRoot))
        {
            return Task.FromResult(deletedCount);
        }

        var files = Directory.GetFiles(_backupRoot, "backup_*.json", SearchOption.TopDirectoryOnly)
            .Select(f => new FileInfo(f))
            .OrderByDescending(f => f.CreationTimeUtc)
            .ToList();

        if (files.Count <= keepCount)
        {
            return Task.FromResult(deletedCount);
        }

        foreach (var file in files.Skip(keepCount))
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                file.Delete();
                deletedCount++;
            }
            catch
            {
                // Silinemediyse bir sonraki döngüde denenir.
            }
        }

        return Task.FromResult(deletedCount);
    }

    private static BackupTaskDto MapTaskToDto(TaskItem item)
    {
        return new BackupTaskDto
        {
            Id = item.Id,
            Title = item.Title,
            Description = item.Description,
            DueDate = item.DueDate,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt,
            BoardType = item.BoardType,
            SortOrder = item.SortOrder,
            Notes = item.Notes.Select(note => new BackupTaskNoteDto
            {
                Id = note.Id,
                Text = note.Text,
                CreatedAt = note.CreatedAt
            }).ToList()
        };
    }

    private static TaskItem MapTaskToModel(BackupTaskDto dto)
    {
        var item = new TaskItem
        {
            Id = dto.Id,
            Title = dto.Title,
            Description = dto.Description,
            DueDate = dto.DueDate,
            CreatedAt = dto.CreatedAt,
            UpdatedAt = dto.UpdatedAt,
            BoardType = dto.BoardType,
            SortOrder = dto.SortOrder
        };

        foreach (var note in dto.Notes)
        {
            item.Notes.Add(new TaskNote
            {
                Id = note.Id,
                Text = note.Text,
                CreatedAt = note.CreatedAt
            });
        }

        return item;
    }

    private static BackupActionEntryDto MapActionToDto(ActionEntry item)
    {
        return new BackupActionEntryDto
        {
            Id = item.Id,
            Category = item.Category,
            District = item.District,
            OwnerParcelText = item.OwnerParcelText,
            WorkText = item.WorkText,
            DisplayOrder = item.DisplayOrder,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt
        };
    }

    private static ActionEntry MapActionToModel(BackupActionEntryDto dto)
    {
        return new ActionEntry
        {
            Id = dto.Id,
            Category = dto.Category,
            District = dto.District,
            OwnerParcelText = dto.OwnerParcelText,
            WorkText = dto.WorkText,
            DisplayOrder = dto.DisplayOrder,
            CreatedAt = dto.CreatedAt,
            UpdatedAt = dto.UpdatedAt
        };
    }

    private static BackupMissingProjectEntryDto MapMissingProjectToDto(MissingProjectEntry item)
    {
        return new BackupMissingProjectEntryDto
        {
            Id = item.Id,
            AdaParsel = item.AdaParsel,
            YapiSahibi = item.YapiSahibi,
            RecordMedium = item.RecordMedium,
            RecordMediumText = item.RecordMediumText,
            MissingProjectText = item.MissingProjectText,
            Description = item.Description,
            DisplayOrder = item.DisplayOrder,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt
        };
    }

    private static MissingProjectEntry MapMissingProjectToModel(BackupMissingProjectEntryDto dto)
    {
        return new MissingProjectEntry
        {
            Id = dto.Id,
            AdaParsel = dto.AdaParsel,
            YapiSahibi = dto.YapiSahibi,
            RecordMedium = dto.RecordMedium,
            RecordMediumText = dto.RecordMediumText,
            MissingProjectText = dto.MissingProjectText,
            Description = dto.Description,
            DisplayOrder = dto.DisplayOrder,
            CreatedAt = dto.CreatedAt,
            UpdatedAt = dto.UpdatedAt
        };
    }

    private static BackupMissingProjectCellStateDto MapMissingProjectCellStateToDto(MissingProjectCellState item)
    {
        return new BackupMissingProjectCellStateDto
        {
            EntryId = item.EntryId,
            ColumnKey = item.ColumnKey,
            BackgroundColor = item.BackgroundColor,
            NoteText = item.NoteText
        };
    }

    private static MissingProjectCellState MapMissingProjectCellStateToModel(BackupMissingProjectCellStateDto dto)
    {
        return new MissingProjectCellState
        {
            EntryId = dto.EntryId,
            ColumnKey = dto.ColumnKey,
            BackgroundColor = dto.BackgroundColor,
            NoteText = dto.NoteText
        };
    }

    private static BackupKarotEntryDto MapKarotToDto(KarotEntry item)
    {
        return new BackupKarotEntryDto
        {
            Id = item.Id,
            SampleReceivedDate = item.SampleReceivedDate,
            YibfNo = item.YibfNo,
            AdaParsel = item.AdaParsel,
            YapiSahibi = item.YapiSahibi,
            Muteahhit = item.Muteahhit,
            KatBilgisi = item.KatBilgisi,
            BetonSinifi = item.BetonSinifi,
            TwentyEightDayResult = item.TwentyEightDayResult,
            BetonFirmasi = item.BetonFirmasi,
            Laboratuvar = item.Laboratuvar,
            Aciklama = item.Aciklama,
            Status = item.Status,
            DisplayOrder = item.DisplayOrder,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt
        };
    }

    private static KarotEntry MapKarotToModel(BackupKarotEntryDto dto)
    {
        return new KarotEntry
        {
            Id = dto.Id,
            SampleReceivedDate = dto.SampleReceivedDate,
            YibfNo = dto.YibfNo,
            AdaParsel = dto.AdaParsel,
            YapiSahibi = dto.YapiSahibi,
            Muteahhit = dto.Muteahhit,
            KatBilgisi = dto.KatBilgisi,
            BetonSinifi = dto.BetonSinifi,
            TwentyEightDayResult = dto.TwentyEightDayResult,
            BetonFirmasi = dto.BetonFirmasi,
            Laboratuvar = dto.Laboratuvar,
            Aciklama = dto.Aciklama,
            Status = dto.Status,
            DisplayOrder = dto.DisplayOrder,
            CreatedAt = dto.CreatedAt,
            UpdatedAt = dto.UpdatedAt
        };
    }

    private static BackupKarotCellStateDto MapKarotCellStateToDto(KarotCellState item)
    {
        return new BackupKarotCellStateDto
        {
            EntryId = item.EntryId,
            ColumnKey = item.ColumnKey,
            NoteText = item.NoteText
        };
    }

    private static KarotCellState MapKarotCellStateToModel(BackupKarotCellStateDto dto)
    {
        return new KarotCellState
        {
            EntryId = dto.EntryId,
            ColumnKey = dto.ColumnKey,
            NoteText = dto.NoteText
        };
    }

    private static BackupTadilatEntryDto MapTadilatToDto(TadilatEntry item)
    {
        return new BackupTadilatEntryDto
        {
            Id = item.Id,
            SubTab = item.SubTab,
            District = item.District,
            JobName = item.JobName,
            ProjectType = item.ProjectType,
            DigitalReceived = item.DigitalReceived,
            InspectorApproved = item.InspectorApproved,
            OutputAndReportArrived = item.OutputAndReportArrived,
            OfficialLetterSubmitted = item.OfficialLetterSubmitted,
            ArchivedFromMunicipality = item.ArchivedFromMunicipality,
            Description1 = item.Description1,
            Description2 = item.Description2,
            DisplayOrder = item.DisplayOrder,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt
        };
    }

    private static TadilatEntry MapTadilatToModel(BackupTadilatEntryDto dto)
    {
        return new TadilatEntry
        {
            Id = dto.Id,
            SubTab = dto.SubTab,
            District = dto.District,
            JobName = dto.JobName,
            ProjectType = dto.ProjectType,
            DigitalReceived = dto.DigitalReceived,
            InspectorApproved = dto.InspectorApproved,
            OutputAndReportArrived = dto.OutputAndReportArrived,
            OfficialLetterSubmitted = dto.OfficialLetterSubmitted,
            ArchivedFromMunicipality = dto.ArchivedFromMunicipality,
            Description1 = dto.Description1,
            Description2 = dto.Description2,
            DisplayOrder = dto.DisplayOrder,
            CreatedAt = dto.CreatedAt,
            UpdatedAt = dto.UpdatedAt
        };
    }

    private static BackupTadilatCellStateDto MapTadilatCellStateToDto(TadilatCellState item)
    {
        return new BackupTadilatCellStateDto
        {
            EntryId = item.EntryId,
            ColumnKey = item.ColumnKey,
            BackgroundColor = item.BackgroundColor,
            NoteText = item.NoteText
        };
    }

    private static TadilatCellState MapTadilatCellStateToModel(BackupTadilatCellStateDto dto)
    {
        return new TadilatCellState
        {
            EntryId = dto.EntryId,
            ColumnKey = dto.ColumnKey,
            BackgroundColor = dto.BackgroundColor,
            NoteText = dto.NoteText
        };
    }

    private static BackupYibfAnaBilgiEntryDto MapYibfAnaBilgiToDto(YibfAnaBilgiEntry item)
    {
        return new BackupYibfAnaBilgiEntryDto
        {
            Id = item.Id,
            WorkGroupId = item.WorkGroupId,
            WorkIdentityId = item.WorkIdentityId,
            AdaParsel = item.AdaParsel,
            YibfNo = item.YibfNo,
            Idare = item.Idare,
            YapiSahibi = item.YapiSahibi,
            Muteahhit = item.Muteahhit,
            DisplayOrder = item.DisplayOrder,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt
        };
    }

    private static YibfAnaBilgiEntry MapYibfAnaBilgiToModel(BackupYibfAnaBilgiEntryDto dto)
    {
        return new YibfAnaBilgiEntry
        {
            Id = dto.Id,
            WorkGroupId = dto.WorkGroupId,
            WorkIdentityId = dto.WorkIdentityId,
            AdaParsel = dto.AdaParsel,
            YibfNo = dto.YibfNo,
            Idare = dto.Idare,
            YapiSahibi = dto.YapiSahibi,
            Muteahhit = dto.Muteahhit,
            DisplayOrder = dto.DisplayOrder,
            CreatedAt = dto.CreatedAt,
            UpdatedAt = dto.UpdatedAt
        };
    }

    private static BackupYibfAnaBilgiEventDto MapYibfAnaBilgiEventToDto(YibfAnaBilgiEvent item)
    {
        return new BackupYibfAnaBilgiEventDto
        {
            Id = item.Id,
            EntryId = item.EntryId,
            EventDate = item.EventDate,
            Description = item.Description,
            BackgroundColor = item.BackgroundColor,
            NoteText = item.NoteText,
            DisplayOrder = item.DisplayOrder
        };
    }

    private static YibfAnaBilgiEvent MapYibfAnaBilgiEventToModel(BackupYibfAnaBilgiEventDto dto)
    {
        return new YibfAnaBilgiEvent
        {
            Id = dto.Id,
            EntryId = dto.EntryId,
            EventDate = dto.EventDate,
            Description = dto.Description,
            BackgroundColor = dto.BackgroundColor,
            NoteText = dto.NoteText,
            DisplayOrder = dto.DisplayOrder
        };
    }

    private static BackupYibfIsTakibiEntryDto MapYibfIsTakibiToDto(YibfIsTakibiEntry item)
    {
        return new BackupYibfIsTakibiEntryDto
        {
            Id = item.Id,
            WorkGroupId = item.WorkGroupId,
            WorkIdentityId = item.WorkIdentityId,
            WorkVariantLabel = item.WorkVariantLabel,
            JobName = item.JobName,
            MuellifBilgileriGeldiMi = item.MuellifBilgileriGeldiMi,
            DenetciAtamalariYapildiMi = item.DenetciAtamalariYapildiMi,
            TumProjelerinDijitaliVarMi = item.TumProjelerinDijitaliVarMi,
            EvraklarTamMi = item.EvraklarTamMi,
            YibfSozlesmeHazirlandiMi = item.YibfSozlesmeHazirlandiMi,
            DekontAlindiMi = item.DekontAlindiMi,
            RuhsatBasvurusuYapildiMi = item.RuhsatBasvurusuYapildiMi,
            RuhsatNushasiAlindiMi = item.RuhsatNushasiAlindiMi,
            IsyeriTeslimTutangiHazirlandiMi = item.IsyeriTeslimTutangiHazirlandiMi,
            IsgYazisiHazirlandiMi = item.IsgYazisiHazirlandiMi,
            SaglikGuvenlikPlaniGeldiMi = item.SaglikGuvenlikPlaniGeldiMi,
            TemelTopraklamaTutanagiHazirlandiMi = item.TemelTopraklamaTutanagiHazirlandiMi,
            DisplayOrder = item.DisplayOrder,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt
        };
    }

    private static YibfIsTakibiEntry MapYibfIsTakibiToModel(BackupYibfIsTakibiEntryDto dto)
    {
        return new YibfIsTakibiEntry
        {
            Id = dto.Id,
            WorkGroupId = dto.WorkGroupId,
            WorkIdentityId = dto.WorkIdentityId,
            WorkVariantLabel = dto.WorkVariantLabel,
            JobName = dto.JobName,
            MuellifBilgileriGeldiMi = dto.MuellifBilgileriGeldiMi,
            DenetciAtamalariYapildiMi = dto.DenetciAtamalariYapildiMi,
            TumProjelerinDijitaliVarMi = dto.TumProjelerinDijitaliVarMi,
            EvraklarTamMi = dto.EvraklarTamMi,
            YibfSozlesmeHazirlandiMi = dto.YibfSozlesmeHazirlandiMi,
            DekontAlindiMi = dto.DekontAlindiMi,
            RuhsatBasvurusuYapildiMi = dto.RuhsatBasvurusuYapildiMi,
            RuhsatNushasiAlindiMi = dto.RuhsatNushasiAlindiMi,
            IsyeriTeslimTutangiHazirlandiMi = dto.IsyeriTeslimTutangiHazirlandiMi,
            IsgYazisiHazirlandiMi = dto.IsgYazisiHazirlandiMi,
            SaglikGuvenlikPlaniGeldiMi = dto.SaglikGuvenlikPlaniGeldiMi,
            TemelTopraklamaTutanagiHazirlandiMi = dto.TemelTopraklamaTutanagiHazirlandiMi,
            DisplayOrder = dto.DisplayOrder,
            CreatedAt = dto.CreatedAt,
            UpdatedAt = dto.UpdatedAt
        };
    }

    private static BackupYibfCellStateDto MapYibfCellStateToDto(YibfCellState item)
    {
        return new BackupYibfCellStateDto
        {
            EntryId = item.EntryId,
            ColumnKey = item.ColumnKey,
            BackgroundColor = item.BackgroundColor,
            NoteText = item.NoteText
        };
    }

    private static YibfCellState MapYibfCellStateToModel(BackupYibfCellStateDto dto)
    {
        return new YibfCellState
        {
            EntryId = dto.EntryId,
            ColumnKey = dto.ColumnKey,
            BackgroundColor = dto.BackgroundColor,
            NoteText = dto.NoteText
        };
    }

    private static BackupQuickTaskTemplateDto MapQuickTaskTemplateToDto(QuickTaskTemplate template)
        => new()
        {
            Id = template.Id,
            Title = template.Title,
            SortOrder = template.SortOrder,
            CreatedAt = template.CreatedAt,
            UpdatedAt = template.UpdatedAt,
            IsDeleted = template.IsDeleted
        };

    private static QuickTaskTemplate MapQuickTaskTemplateToModel(BackupQuickTaskTemplateDto dto)
        => new()
        {
            Id = dto.Id,
            Title = dto.Title,
            SortOrder = dto.SortOrder,
            CreatedAt = dto.CreatedAt,
            UpdatedAt = dto.UpdatedAt,
            IsDeleted = dto.IsDeleted
        };

    private sealed class BackupEnvelope
    {
        public int SchemaVersion { get; set; }
        public string AppVersion { get; set; } = string.Empty;
        public string Checksum { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public List<BackupTaskDto> Tasks { get; set; } = new();
        public List<BackupQuickTaskTemplateDto> QuickTaskTemplates { get; set; } = new();
        public List<BackupActionEntryDto> ActionEntries { get; set; } = new();
        public List<BackupMissingProjectEntryDto> MissingProjectEntries { get; set; } = new();
        public List<BackupMissingProjectCellStateDto> MissingProjectCellStates { get; set; } = new();
        public List<BackupKarotEntryDto> KarotEntries { get; set; } = new();
        public List<BackupKarotCellStateDto> KarotCellStates { get; set; } = new();
        public List<BackupTadilatEntryDto> TadilatEntries { get; set; } = new();
        public List<BackupYibfAnaBilgiEntryDto> YibfAnaBilgiEntries { get; set; } = new();
        public List<BackupYibfAnaBilgiEventDto> YibfAnaBilgiEvents { get; set; } = new();
        public List<BackupYibfIsTakibiEntryDto> YibfIsTakibiEntries { get; set; } = new();
        public List<BackupYibfCellStateDto> YibfCellStates { get; set; } = new();
        public List<BackupTadilatCellStateDto> TadilatCellStates { get; set; } = new();
    }

    private sealed class BackupTaskDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime? DueDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public TaskBoardType BoardType { get; set; }
        public int SortOrder { get; set; }
        public List<BackupTaskNoteDto> Notes { get; set; } = new();
    }

    private sealed class BackupQuickTaskTemplateDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
    }

    private sealed class BackupTaskNoteDto
    {
        public Guid Id { get; set; }
        public string Text { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    private sealed class BackupActionEntryDto
    {
        public Guid Id { get; set; }
        public ActionEntryCategory Category { get; set; }
        public string District { get; set; } = string.Empty;
        public string OwnerParcelText { get; set; } = string.Empty;
        public string WorkText { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    private sealed class BackupMissingProjectEntryDto
    {
        public Guid Id { get; set; }
        public string AdaParsel { get; set; } = string.Empty;
        public string YapiSahibi { get; set; } = string.Empty;
        public MissingProjectMedium RecordMedium { get; set; }
        public string RecordMediumText { get; set; } = string.Empty;
        public string MissingProjectText { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    private sealed class BackupMissingProjectCellStateDto
    {
        public Guid EntryId { get; set; }
        public string ColumnKey { get; set; } = string.Empty;
        public string BackgroundColor { get; set; } = string.Empty;
        public string NoteText { get; set; } = string.Empty;
    }

    private sealed class BackupKarotEntryDto
    {
        public Guid Id { get; set; }
        public DateTime? SampleReceivedDate { get; set; }
        public string YibfNo { get; set; } = string.Empty;
        public string AdaParsel { get; set; } = string.Empty;
        public string YapiSahibi { get; set; } = string.Empty;
        public string Muteahhit { get; set; } = string.Empty;
        public string KatBilgisi { get; set; } = string.Empty;
        public string BetonSinifi { get; set; } = string.Empty;
        public string TwentyEightDayResult { get; set; } = string.Empty;
        public string BetonFirmasi { get; set; } = string.Empty;
        public string Laboratuvar { get; set; } = string.Empty;
        public string Aciklama { get; set; } = string.Empty;
        public KarotStatus Status { get; set; }
        public int DisplayOrder { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    private sealed class BackupKarotCellStateDto
    {
        public Guid EntryId { get; set; }
        public string ColumnKey { get; set; } = string.Empty;
        public string NoteText { get; set; } = string.Empty;
    }

    private sealed class BackupTadilatEntryDto
    {
        public Guid Id { get; set; }
        public TadilatSubTab SubTab { get; set; }
        public string District { get; set; } = string.Empty;
        public string JobName { get; set; } = string.Empty;
        public string ProjectType { get; set; } = string.Empty;
        public string DigitalReceived { get; set; } = string.Empty;
        public string InspectorApproved { get; set; } = string.Empty;
        public string OutputAndReportArrived { get; set; } = string.Empty;
        public string OfficialLetterSubmitted { get; set; } = string.Empty;
        public string ArchivedFromMunicipality { get; set; } = string.Empty;
        public string Description1 { get; set; } = string.Empty;
        public string Description2 { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    private sealed class BackupTadilatCellStateDto
    {
        public Guid EntryId { get; set; }
        public string ColumnKey { get; set; } = string.Empty;
        public string BackgroundColor { get; set; } = string.Empty;
        public string NoteText { get; set; } = string.Empty;
    }

    private sealed class BackupYibfAnaBilgiEntryDto
    {
        public Guid Id { get; set; }
        public Guid WorkGroupId { get; set; }
        public Guid WorkIdentityId { get; set; }
        public string AdaParsel { get; set; } = string.Empty;
        public string YibfNo { get; set; } = string.Empty;
        public string Idare { get; set; } = string.Empty;
        public string YapiSahibi { get; set; } = string.Empty;
        public string Muteahhit { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    private sealed class BackupYibfAnaBilgiEventDto
    {
        public Guid Id { get; set; }
        public Guid EntryId { get; set; }
        public DateTime? EventDate { get; set; }
        public string Description { get; set; } = string.Empty;
        public string BackgroundColor { get; set; } = string.Empty;
        public string NoteText { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
    }

    private sealed class BackupYibfIsTakibiEntryDto
    {
        public Guid Id { get; set; }
        public Guid WorkGroupId { get; set; }
        public Guid WorkIdentityId { get; set; }
        public string WorkVariantLabel { get; set; } = string.Empty;
        public string JobName { get; set; } = string.Empty;
        public string MuellifBilgileriGeldiMi { get; set; } = string.Empty;
        public string DenetciAtamalariYapildiMi { get; set; } = string.Empty;
        public string TumProjelerinDijitaliVarMi { get; set; } = string.Empty;
        public string EvraklarTamMi { get; set; } = string.Empty;
        public string YibfSozlesmeHazirlandiMi { get; set; } = string.Empty;
        public string DekontAlindiMi { get; set; } = string.Empty;
        public string RuhsatBasvurusuYapildiMi { get; set; } = string.Empty;
        public string RuhsatNushasiAlindiMi { get; set; } = string.Empty;
        public string IsyeriTeslimTutangiHazirlandiMi { get; set; } = string.Empty;
        public string IsgYazisiHazirlandiMi { get; set; } = string.Empty;
        public string SaglikGuvenlikPlaniGeldiMi { get; set; } = string.Empty;
        public string TemelTopraklamaTutanagiHazirlandiMi { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    private sealed class BackupYibfCellStateDto
    {
        public Guid EntryId { get; set; }
        public string ColumnKey { get; set; } = string.Empty;
        public string BackgroundColor { get; set; } = string.Empty;
        public string NoteText { get; set; } = string.Empty;
    }
}
