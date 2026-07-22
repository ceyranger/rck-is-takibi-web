using RizaCanKilicIsTakibi.Models;

namespace RizaCanKilicIsTakibi.Services.Abstractions;

public interface IYibfIsTakibiEntryDialogService
{
    Task<YibfIsTakibiEntry?> ShowDialogAsync(CancellationToken cancellationToken = default);
}
