using System.Diagnostics;
using System.IO;

namespace RizaCanKilicIsTakibi.Services;

public sealed class WebViewGitSyncService
{
    private readonly WebViewGitHubPublishService _apiFallback = new();

    public async Task<WebViewGitSyncResult> TrySyncAsync(
        string repoRoot,
        string jsonFilePath,
        CancellationToken cancellationToken = default)
    {
        var validation = WebViewRepoPaths.ValidateRepoRoot(repoRoot);
        if (!validation.IsValid)
        {
            return WebViewGitSyncResult.Failed(validation.ErrorMessage ?? "Git repo doğrulanamadı.");
        }

        if (string.IsNullOrWhiteSpace(jsonFilePath) || !File.Exists(jsonFilePath))
        {
            return WebViewGitSyncResult.Failed("Web JSON dosyası bulunamadı.");
        }

        var gitResult = await TryGitSyncAsync(validation.RepoRoot!, jsonFilePath, cancellationToken);
        if (gitResult.Success)
        {
            return gitResult;
        }

        var apiResult = await _apiFallback.TryPublishAsync(jsonFilePath, cancellationToken: cancellationToken);
        return apiResult switch
        {
            { Success: true } => WebViewGitSyncResult.Succeeded(apiResult.Bytes, "Site güncellendi (API yedek)."),
            { Success: false } => WebViewGitSyncResult.Failed(
                $"{gitResult.Message} API yedek: {apiResult.Message}"),
            _ => WebViewGitSyncResult.Failed(gitResult.Message)
        };
    }

    private static async Task<WebViewGitSyncResult> TryGitSyncAsync(
        string repoRoot,
        string jsonFilePath,
        CancellationToken cancellationToken)
    {
        var branchResult = await RunGitAsync(repoRoot, "rev-parse --abbrev-ref HEAD", cancellationToken);
        if (branchResult.ExitCode != 0 || string.IsNullOrWhiteSpace(branchResult.Output))
        {
            return WebViewGitSyncResult.Failed("Git branch okunamadı.");
        }

        var branch = branchResult.Output.Trim();
        var pullResult = await RunGitAsync(repoRoot, $"pull --rebase origin {branch}", cancellationToken);
        if (pullResult.ExitCode != 0 && !IsBenignPullFailure(pullResult.Error))
        {
            return WebViewGitSyncResult.Failed($"Git pull başarısız: {TrimGitMessage(pullResult.Error)}");
        }

        var addResult = await RunGitAsync(
            repoRoot,
            $"add -- \"{WebViewRepoPaths.ExportRelativeFile.Replace('\\', '/')}\"",
            cancellationToken);
        if (addResult.ExitCode != 0)
        {
            return WebViewGitSyncResult.Failed($"Git add başarısız: {TrimGitMessage(addResult.Error)}");
        }

        var diffResult = await RunGitAsync(repoRoot, "diff --cached --quiet", cancellationToken);
        if (diffResult.ExitCode == 0)
        {
            var bytes = new FileInfo(jsonFilePath).Length;
            return WebViewGitSyncResult.Succeeded(bytes, "Site verisi zaten güncel.");
        }

        var message = $"Web görüntüleme verisi {DateTime.Now:yyyy-MM-dd HH:mm}";
        var commitResult = await RunGitAsync(
            repoRoot,
            $"commit -m \"{EscapeGitMessage(message)}\"",
            cancellationToken);
        if (commitResult.ExitCode != 0)
        {
            return WebViewGitSyncResult.Failed($"Git commit başarısız: {TrimGitMessage(commitResult.Error)}");
        }

        var pushResult = await RunGitAsync(repoRoot, $"push origin {branch}", cancellationToken);
        if (pushResult.ExitCode != 0)
        {
            return WebViewGitSyncResult.Failed($"Git push başarısız: {TrimGitMessage(pushResult.Error)}");
        }

        return WebViewGitSyncResult.Succeeded(new FileInfo(jsonFilePath).Length, "Site güncellendi.");
    }

    private static bool IsBenignPullFailure(string error)
    {
        var sample = error.Trim();
        return sample.Contains("Already up to date", StringComparison.OrdinalIgnoreCase)
               || sample.Contains("up to date", StringComparison.OrdinalIgnoreCase);
    }

    private static string EscapeGitMessage(string message)
        => message.Replace("\"", "\\\"", StringComparison.Ordinal);

    private static string TrimGitMessage(string? message)
    {
        var sample = (message ?? string.Empty).Trim();
        return sample.Length > 180 ? sample[..180] + "…" : sample;
    }

    private static async Task<GitCommandResult> RunGitAsync(
        string repoRoot,
        string arguments,
        CancellationToken cancellationToken)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = arguments,
            WorkingDirectory = repoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi);
        if (process is null)
        {
            return new GitCommandResult(-1, string.Empty, "git komutu başlatılamadı.");
        }

        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        var stdout = (await stdoutTask).Trim();
        var stderr = (await stderrTask).Trim();
        var combinedError = string.IsNullOrWhiteSpace(stderr) ? stdout : stderr;
        return new GitCommandResult(process.ExitCode, stdout, combinedError);
    }

    private sealed record GitCommandResult(int ExitCode, string Output, string Error);
}

public sealed record WebViewGitSyncResult(bool Success, string Message, long Bytes)
{
    public static WebViewGitSyncResult Succeeded(long bytes, string message = "Site güncellendi.")
        => new(true, message, bytes);

    public static WebViewGitSyncResult Failed(string message)
        => new(false, message, 0);
}
