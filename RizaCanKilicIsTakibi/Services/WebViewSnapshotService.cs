using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services.Abstractions;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace RizaCanKilicIsTakibi.Services;

public sealed class WebViewSnapshotService : IWebViewSnapshotService
{
    private readonly IBackupService _backupService;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };
    private readonly JsonSerializerOptions _jsonOptions = JsonOptions;

    public WebViewSnapshotService(IBackupService backupService)
    {
        _backupService = backupService;
    }

    public async Task<WebViewSnapshotExportResult?> TryExportLatestAsync(
        WebViewSnapshotExportRequest request,
        string exportDirectory,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(exportDirectory))
        {
            return null;
        }

        Directory.CreateDirectory(exportDirectory);

        var tempBackupPath = Path.Combine(
            exportDirectory,
            $".web-view-backup-{Guid.NewGuid():N}.tmp");

        try
        {
            await _backupService.CreateBackupAsync(
                request.Tasks,
                tempBackupPath,
                request.ActionEntries,
                request.MissingProjectEntries,
                request.MissingProjectCellStates,
                request.KarotEntries,
                request.KarotCellStates,
                request.TadilatEntries,
                request.YibfAnaBilgiEntries,
                request.YibfAnaBilgiEvents,
                request.YibfIsTakibiEntries,
                request.YibfCellStates,
                request.TadilatCellStates,
                request.QuickTaskTemplates,
                request.ProjectCatalogEntries,
                request.Personnel,
                request.PersonnelAssignments,
                cancellationToken);

            await using var backupStream = File.OpenRead(tempBackupPath);
            using var backupDocument = await JsonDocument.ParseAsync(backupStream, cancellationToken: cancellationToken);

            var exportedAt = DateTime.Now;
            var envelope = new WebViewSnapshotEnvelope
            {
                ExportedAt = exportedAt,
                AppVersion = typeof(WebViewSnapshotService).Assembly.GetName().Version?.ToString() ?? "unknown",
                Data = backupDocument.RootElement.Clone(),
                Derived = request.Derived
            };
            envelope.Checksum = ComputeChecksum(envelope);

            var finalPath = Path.Combine(exportDirectory, IWebViewSnapshotService.LatestFileName);
            var tempFinalPath = Path.Combine(exportDirectory, $".{IWebViewSnapshotService.LatestFileName}.{Guid.NewGuid():N}.tmp");

            try
            {
                await using (var stream = File.Create(tempFinalPath))
                {
                    await JsonSerializer.SerializeAsync(stream, envelope, _jsonOptions, cancellationToken);
                    await stream.FlushAsync(cancellationToken);
                }

                File.Move(tempFinalPath, finalPath, overwrite: true);
            }
            finally
            {
                if (File.Exists(tempFinalPath))
                {
                    File.Delete(tempFinalPath);
                }
            }

            var fileInfo = new FileInfo(finalPath);
            return new WebViewSnapshotExportResult
            {
                FilePath = finalPath,
                ExportedAt = exportedAt,
                FileSizeBytes = fileInfo.Length
            };
        }
        finally
        {
            if (File.Exists(tempBackupPath))
            {
                File.Delete(tempBackupPath);
            }
        }
    }

    internal static string ComputeChecksum(WebViewSnapshotEnvelope envelope)
    {
        var payload = new WebViewSnapshotChecksumPayload
        {
            Kind = envelope.Kind,
            SchemaVersion = envelope.SchemaVersion,
            ExportedAt = envelope.ExportedAt,
            AppVersion = envelope.AppVersion,
            Data = envelope.Data,
            Derived = envelope.Derived
        };

        var json = JsonSerializer.Serialize(payload, JsonOptions);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(hash);
    }

    private sealed class WebViewSnapshotChecksumPayload
    {
        public string Kind { get; set; } = string.Empty;
        public int SchemaVersion { get; set; }
        public DateTime ExportedAt { get; set; }
        public string AppVersion { get; set; } = string.Empty;
        public JsonElement Data { get; set; }
        public WebViewSnapshotDerived Derived { get; set; } = new();
    }
}
