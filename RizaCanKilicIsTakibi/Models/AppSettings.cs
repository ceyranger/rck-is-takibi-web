namespace RizaCanKilicIsTakibi.Models;

public sealed class AppSettings
{
    public const string DefaultWebViewRepoRoot = @"C:\Users\rizac\Masaüstü\RCK İŞ TAKİBİV1";

    public bool AutoBackupEnabled { get; set; } = true;
    public int AutoBackupMinutes { get; set; } = 15;
    public bool SeedSampleDataOnEmpty { get; set; } = false;
    public bool WebViewExportEnabled { get; set; } = true;
    public string WebViewRepoRoot { get; set; } = DefaultWebViewRepoRoot;
    public bool WebViewGitSyncEnabled { get; set; } = true;
}
