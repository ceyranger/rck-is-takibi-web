using RizaCanKilicIsTakibi.Models;

namespace RizaCanKilicIsTakibi.Services.Abstractions;

public interface ITadilatImportService
{
    Task<TadilatImportData> ImportAsync(string filePath, CancellationToken cancellationToken = default);
}
