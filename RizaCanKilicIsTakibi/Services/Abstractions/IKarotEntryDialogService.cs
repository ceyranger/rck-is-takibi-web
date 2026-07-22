using RizaCanKilicIsTakibi.Models;

namespace RizaCanKilicIsTakibi.Services.Abstractions;

public interface IKarotEntryDialogService
{
    Task<KarotEntry?> ShowDialogAsync(KarotSubTab subTab, CancellationToken cancellationToken = default);
}
