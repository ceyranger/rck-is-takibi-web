namespace RizaCanKilicIsTakibi.Models;

public sealed class BackupMetadata
{
    public DateTime CreatedAt { get; set; }
    public string BackupFilePath { get; set; } = string.Empty;
    public int TaskCount { get; set; }
}
