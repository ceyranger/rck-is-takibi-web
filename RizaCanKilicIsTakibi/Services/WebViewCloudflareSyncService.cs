using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;

namespace RizaCanKilicIsTakibi.Services;

public sealed class WebViewCloudflareSyncService
{
    private static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromMinutes(2)
    };

    public static bool IsConfigured(string? apiUrl, string? apiKey)
        => !string.IsNullOrWhiteSpace(apiUrl) && !string.IsNullOrWhiteSpace(apiKey);

    public async Task<WebViewCloudflareSyncResult> TryUploadAsync(
        string apiUrl,
        string apiKey,
        string jsonFilePath,
        CancellationToken cancellationToken = default)
    {
        var normalizedUrl = apiUrl.Trim();
        var normalizedKey = apiKey.Trim();
        if (!IsConfigured(normalizedUrl, normalizedKey))
        {
            return WebViewCloudflareSyncResult.Failed("Cloudflare API URL veya anahtar tanımlı değil.");
        }

        if (string.IsNullOrWhiteSpace(jsonFilePath) || !File.Exists(jsonFilePath))
        {
            return WebViewCloudflareSyncResult.Failed("Web JSON dosyası bulunamadı.");
        }

        var jsonBytes = await File.ReadAllBytesAsync(jsonFilePath, cancellationToken);
        using var content = new ByteArrayContent(jsonBytes);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        using var request = new HttpRequestMessage(HttpMethod.Put, normalizedUrl);
        request.Headers.Add("X-API-Key", normalizedKey);
        request.Content = content;

        using var response = await Http.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var message = TryReadErrorMessage(body) ?? $"Cloudflare yükleme hatası ({(int)response.StatusCode}).";
            return WebViewCloudflareSyncResult.Failed(message);
        }

        return WebViewCloudflareSyncResult.Succeeded(jsonBytes.LongLength);
    }

    private static string? TryReadErrorMessage(string body)
    {
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("error", out var error))
            {
                return error.GetString();
            }
        }
        catch
        {
            /* ignore */
        }

        return null;
    }
}

public sealed record WebViewCloudflareSyncResult(bool Success, string Message, long Bytes)
{
    public static WebViewCloudflareSyncResult Succeeded(long bytes, string message = "Cloudflare güncellendi.")
        => new(true, message, bytes);

    public static WebViewCloudflareSyncResult Failed(string message)
        => new(false, message, 0);
}
