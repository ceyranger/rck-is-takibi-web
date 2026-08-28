namespace RizaCanKilicIsTakibi.Models;

public sealed class AppSettings
{
    public bool AutoBackupEnabled { get; set; } = true;
    public int AutoBackupMinutes { get; set; } = 15;
    public bool SeedSampleDataOnEmpty { get; set; } = false;
    public bool WebViewExportEnabled { get; set; }
    public string WebViewExportDirectory { get; set; } = string.Empty;
    public bool WebViewGitHubPublishEnabled { get; set; } = true;
    public string WebViewGitHubRepository { get; set; } = "ceyranger/rck-is-takibi-web";
}
