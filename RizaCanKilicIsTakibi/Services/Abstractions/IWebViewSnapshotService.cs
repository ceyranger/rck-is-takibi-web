using RizaCanKilicIsTakibi.Models;

namespace RizaCanKilicIsTakibi.Services.Abstractions;

public interface IWebViewSnapshotService
{
    const string LatestFileName = "web-view-latest.json";

    Task<WebViewSnapshotExportResult?> TryExportLatestAsync(
        WebViewSnapshotExportRequest request,
        string exportDirectory,
        CancellationToken cancellationToken = default);
}
