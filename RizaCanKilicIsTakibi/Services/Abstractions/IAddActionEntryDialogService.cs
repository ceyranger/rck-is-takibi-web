using RizaCanKilicIsTakibi.Models;

namespace RizaCanKilicIsTakibi.Services.Abstractions;

public interface IAddActionEntryDialogService
{
    Task<ActionEntry?> ShowDialogAsync(string district, ActionEntryCategory category, CancellationToken cancellationToken = default);
}
