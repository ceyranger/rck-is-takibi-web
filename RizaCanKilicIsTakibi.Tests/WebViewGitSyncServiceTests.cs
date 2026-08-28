using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services;

namespace RizaCanKilicIsTakibi.Tests;

public sealed class WebViewGitSyncServiceTests
{
    [Fact]
    public void ValidateRepoRoot_Returns_Error_When_Path_Is_Empty()
    {
        var validation = WebViewRepoPaths.ValidateRepoRoot("   ");
        Assert.False(validation.IsValid);
        Assert.Contains("tanımlı değil", validation.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateRepoRoot_Returns_Error_When_Directory_Missing()
    {
        var missing = Path.Combine(Path.GetTempPath(), "RizaCanKilicIsTakibiTests", Guid.NewGuid().ToString("N"));
        var validation = WebViewRepoPaths.ValidateRepoRoot(missing);
        Assert.False(validation.IsValid);
        Assert.Contains("bulunamadı", validation.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateRepoRoot_Returns_Error_When_Git_Folder_Missing()
    {
        var root = Path.Combine(Path.GetTempPath(), "RizaCanKilicIsTakibiTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var validation = WebViewRepoPaths.ValidateRepoRoot(root);
            Assert.False(validation.IsValid);
            Assert.Contains(".git", validation.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void ValidateRepoRoot_Succeeds_For_Real_Repo()
    {
        var repoRoot = FindRepoRoot();
        var validation = WebViewRepoPaths.ValidateRepoRoot(repoRoot);
        Assert.True(validation.IsValid);
        Assert.Equal(Path.GetFullPath(repoRoot), Path.GetFullPath(validation.RepoRoot!));
    }

    [Fact]
    public void GetExportDirectory_Uses_Repo_Web_Export_Path()
    {
        var repoRoot = @"C:\Example\RCK İŞ TAKİBİV1";
        var exportDirectory = WebViewRepoPaths.GetExportDirectory(repoRoot);
        Assert.Equal(Path.Combine(repoRoot, "web", "export"), exportDirectory);
    }

    [Fact]
    public void GetExportFilePath_Uses_Latest_File_Name()
    {
        var repoRoot = @"C:\Example\RCK İŞ TAKİBİV1";
        var exportFile = WebViewRepoPaths.GetExportFilePath(repoRoot);
        Assert.EndsWith("web-view-latest.json", exportFile, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(Path.Combine(repoRoot, "web", "export", "web-view-latest.json"), exportFile);
    }

    [Fact]
    public void NormalizeRepoRoot_Uses_Default_When_Empty()
    {
        var normalized = WebViewRepoPaths.NormalizeRepoRoot("  ");
        Assert.Equal(AppSettings.DefaultWebViewRepoRoot, normalized);
    }

    [Fact]
    public async Task TrySyncAsync_Returns_Error_When_Repo_Invalid()
    {
        var service = new WebViewGitSyncService();
        var result = await service.TrySyncAsync("   ", "missing.json");
        Assert.False(result.Success);
        Assert.Contains("tanımlı değil", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TrySyncAsync_Returns_Error_When_Json_Missing()
    {
        var repoRoot = FindRepoRoot();
        var service = new WebViewGitSyncService();
        var result = await service.TrySyncAsync(repoRoot, Path.Combine(repoRoot, "missing.json"));
        Assert.False(result.Success);
        Assert.Contains("JSON", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static string FindRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (Directory.Exists(Path.Combine(current.FullName, ".git")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Test repo root not found.");
    }
}
