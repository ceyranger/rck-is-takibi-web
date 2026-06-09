namespace RizaCanKilicIsTakibi.Models;

public sealed class AppSettings
{
    public bool AutoBackupEnabled { get; set; } = true;
    public int AutoBackupMinutes { get; set; } = 15;
    public bool SeedSampleDataOnEmpty { get; set; } = false;
}
