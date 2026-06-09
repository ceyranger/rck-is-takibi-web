using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services.Abstractions;
using System.IO;
using System.Text.Json;

namespace RizaCanKilicIsTakibi.Services;

public sealed class LastSaveMetadataService : ILastSaveMetadataService
{
    private readonly string _metadataPath;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public LastSaveMetadataService(string metadataPath)
    {
        _metadataPath = metadataPath;

        var directory = Path.GetDirectoryName(_metadataPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    public DateTime? LoadLastSuccessfulSaveAt()
    {
        if (!File.Exists(_metadataPath))
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(_metadataPath);
            var metadata = JsonSerializer.Deserialize<LastSaveMetadata>(json, _jsonOptions);
            if (metadata is null || metadata.LastSuccessfulSaveAt == default)
            {
                return null;
            }

            return metadata.LastSuccessfulSaveAt;
        }
        catch
        {
            return null;
        }
    }

    public async Task SaveLastSuccessfulSaveAtAsync(DateTime timestamp, CancellationToken cancellationToken = default)
    {
        var metadata = new LastSaveMetadata
        {
            LastSuccessfulSaveAt = timestamp
        };

        var json = JsonSerializer.Serialize(metadata, _jsonOptions);
        var directory = Path.GetDirectoryName(_metadataPath) ?? Directory.GetCurrentDirectory();
        Directory.CreateDirectory(directory);

        var tempPath = Path.Combine(directory, $".{Path.GetFileName(_metadataPath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            await File.WriteAllTextAsync(tempPath, json, cancellationToken);

            if (File.Exists(_metadataPath))
            {
                File.Replace(tempPath, _metadataPath, destinationBackupFileName: null, ignoreMetadataErrors: true);
            }
            else
            {
                File.Move(tempPath, _metadataPath);
            }
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }
}
