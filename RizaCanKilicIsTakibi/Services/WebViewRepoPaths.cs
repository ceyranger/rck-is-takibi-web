using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services.Abstractions;
using System.IO;

namespace RizaCanKilicIsTakibi.Services;

public static class WebViewRepoPaths
{
    public const string ExportRelativeFile = "web/export/web-view-latest.json";

    public static string NormalizeRepoRoot(string? repoRoot)
    {
        var normalized = repoRoot?.Trim() ?? string.Empty;
        return string.IsNullOrWhiteSpace(normalized)
            ? AppSettings.DefaultWebViewRepoRoot
            : normalized;
    }

    public static string GetExportDirectory(string repoRoot)
        => Path.Combine(NormalizeRepoRoot(repoRoot), "web", "export");

    public static string GetExportFilePath(string repoRoot)
        => Path.Combine(GetExportDirectory(repoRoot), IWebViewSnapshotService.LatestFileName);

    public static WebViewRepoValidation ValidateRepoRoot(string? repoRoot)
    {
        var normalized = repoRoot?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return WebViewRepoValidation.Invalid("Git repo klasörü tanımlı değil.");
        }

        if (!Directory.Exists(normalized))
        {
            return WebViewRepoValidation.Invalid("Git repo klasörü bulunamadı.");
        }

        if (!Directory.Exists(Path.Combine(normalized, ".git")))
        {
            return WebViewRepoValidation.Invalid("Seçilen klasör git repo değil (.git yok).");
        }

        return WebViewRepoValidation.Valid(normalized);
    }
}

public sealed record WebViewRepoValidation(bool IsValid, string? RepoRoot, string? ErrorMessage)
{
    public static WebViewRepoValidation Valid(string repoRoot)
        => new(true, repoRoot, null);

    public static WebViewRepoValidation Invalid(string errorMessage)
        => new(false, null, errorMessage);
}
