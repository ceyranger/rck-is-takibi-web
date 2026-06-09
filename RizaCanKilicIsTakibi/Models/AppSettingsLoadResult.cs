namespace RizaCanKilicIsTakibi.Models;

public sealed class AppSettingsLoadResult
{
    public required AppSettings Settings { get; init; }
    public required AppSettingsLoadStatus Status { get; init; }
    public string? OriginalPath { get; init; }
    public string? CorruptBackupPath { get; init; }
    public string? ErrorMessage { get; init; }
}
