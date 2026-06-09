using RizaCanKilicIsTakibi.Models;

namespace RizaCanKilicIsTakibi.Services.Abstractions;

public interface IKarotStatusDialogService
{
    Task<KarotStatus?> ShowDialogAsync(KarotStatus currentStatus, CancellationToken cancellationToken = default);
}
