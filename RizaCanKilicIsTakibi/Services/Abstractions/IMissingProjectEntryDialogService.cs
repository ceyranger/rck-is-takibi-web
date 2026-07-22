using RizaCanKilicIsTakibi.Models;

namespace RizaCanKilicIsTakibi.Services.Abstractions;

public interface IMissingProjectEntryDialogService
{
    Task<MissingProjectEntry?> ShowDialogAsync(CancellationToken cancellationToken = default);
}
