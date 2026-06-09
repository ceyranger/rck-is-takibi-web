using RizaCanKilicIsTakibi.Models;

namespace RizaCanKilicIsTakibi.Services.Abstractions;

public interface IAppSettingsService
{
    AppSettingsLoadResult Load();
    Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default);
}
