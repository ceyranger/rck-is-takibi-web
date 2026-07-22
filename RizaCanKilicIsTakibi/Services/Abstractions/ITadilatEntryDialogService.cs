using RizaCanKilicIsTakibi.Models;

namespace RizaCanKilicIsTakibi.Services.Abstractions;

public interface ITadilatEntryDialogService
{
    Task<TadilatEntry?> ShowDialogAsync(string district, TadilatSubTab subTab, CancellationToken cancellationToken = default);
}
