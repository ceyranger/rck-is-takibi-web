using RizaCanKilicIsTakibi.Services;

namespace RizaCanKilicIsTakibi.Tests;

public sealed class WebViewCloudflareSyncServiceTests
{
    [Theory]
    [InlineData(null, "key")]
    [InlineData("https://example.com/api/data", "")]
    [InlineData("   ", "key")]
    [InlineData("https://example.com/api/data", "   ")]
    public void IsConfigured_Returns_False_When_Url_Or_Key_Missing(string? apiUrl, string? apiKey)
    {
        Assert.False(WebViewCloudflareSyncService.IsConfigured(apiUrl, apiKey));
    }

    [Fact]
    public void IsConfigured_Returns_True_When_Url_And_Key_Present()
    {
        Assert.True(WebViewCloudflareSyncService.IsConfigured(
            "https://example.com/api/data",
            "secret-key"));
    }

    [Fact]
    public async Task TryUploadAsync_Returns_Error_When_Json_Missing()
    {
        var service = new WebViewCloudflareSyncService();
        var result = await service.TryUploadAsync(
            "https://example.com/api/data",
            "secret-key",
            Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "missing.json"));

        Assert.False(result.Success);
        Assert.Contains("JSON", result.Message, StringComparison.OrdinalIgnoreCase);
    }
}
