using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace RizaCanKilicIsTakibi.Services;

public sealed class WebViewGitHubPublishService
{
    public const string DefaultRepo = "ceyranger/rck-is-takibi-web";
    public const string DefaultRepoPath = "web/export/web-view-latest.json";

    private static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromMinutes(2)
    };

    public async Task<WebViewGitHubPublishResult?> TryPublishAsync(
        string jsonFilePath,
        string repository = DefaultRepo,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(jsonFilePath) || !File.Exists(jsonFilePath))
        {
            return null;
        }

        var token = await GitHubCliTokenProvider.TryGetTokenAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(token))
        {
            return WebViewGitHubPublishResult.Failed("GitHub oturumu bulunamadı.");
        }

        var parts = repository.Split('/', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 2)
        {
            return WebViewGitHubPublishResult.Failed("GitHub repo adı geçersiz.");
        }

        var owner = parts[0];
        var repo = parts[1];
        var jsonBytes = await File.ReadAllBytesAsync(jsonFilePath, cancellationToken);
        var base64 = Convert.ToBase64String(jsonBytes);
        var existingSha = await TryGetExistingShaAsync(owner, repo, DefaultRepoPath, token, cancellationToken);

        var payload = new Dictionary<string, object?>
        {
            ["message"] = $"Web görüntüleme verisi {DateTime.Now:yyyy-MM-dd HH:mm}",
            ["content"] = base64,
            ["branch"] = "master"
        };
        if (!string.IsNullOrWhiteSpace(existingSha))
        {
            payload["sha"] = existingSha;
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Put,
            $"https://api.github.com/repos/{owner}/{repo}/contents/{DefaultRepoPath}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.UserAgent.ParseAdd("RizaCanKilicIsTakibi");
        request.Headers.Accept.ParseAdd("application/vnd.github+json");
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var response = await Http.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var message = TryReadGitHubError(body) ?? $"GitHub yükleme hatası ({(int)response.StatusCode}).";
            return WebViewGitHubPublishResult.Failed(message);
        }

        return WebViewGitHubPublishResult.Succeeded(jsonBytes.LongLength);
    }

    private static async Task<string?> TryGetExistingShaAsync(
        string owner,
        string repo,
        string path,
        string token,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"https://api.github.com/repos/{owner}/{repo}/contents/{path}?ref=master");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.UserAgent.ParseAdd("RizaCanKilicIsTakibi");
        request.Headers.Accept.ParseAdd("application/vnd.github+json");

        using var response = await Http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        return doc.RootElement.TryGetProperty("sha", out var sha)
            ? sha.GetString()
            : null;
    }

    private static string? TryReadGitHubError(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("message", out var message))
            {
                return message.GetString();
            }
        }
        catch
        {
            /* ignore */
        }

        return null;
    }
}

public sealed record WebViewGitHubPublishResult(bool Success, string Message, long Bytes)
{
    public static WebViewGitHubPublishResult Succeeded(long bytes)
        => new(true, "Site verisi güncellendi.", bytes);

    public static WebViewGitHubPublishResult Failed(string message)
        => new(false, message, 0);
}

internal static class GitHubCliTokenProvider
{
    public static async Task<string?> TryGetTokenAsync(CancellationToken cancellationToken)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = "credential fill",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process is null)
            {
                return null;
            }

            await process.StandardInput.WriteLineAsync("protocol=https");
            await process.StandardInput.WriteLineAsync("host=github.com");
            await process.StandardInput.WriteLineAsync();
            await process.StandardInput.FlushAsync();
            process.StandardInput.Close();

            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);
            if (process.ExitCode != 0)
            {
                return null;
            }

            foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (line.StartsWith("password=", StringComparison.OrdinalIgnoreCase))
                {
                    return line["password=".Length..];
                }
            }
        }
        catch
        {
            /* ignore */
        }

        return null;
    }
}
