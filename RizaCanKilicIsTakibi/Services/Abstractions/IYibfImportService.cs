using RizaCanKilicIsTakibi.Models;

namespace RizaCanKilicIsTakibi.Services.Abstractions;

public interface IYibfImportService
{
    Task<YibfImportData> ImportAsync(string filePath, CancellationToken cancellationToken = default);
}