using RizaCanKilicIsTakibi.Models;

namespace RizaCanKilicIsTakibi.Services.Abstractions;

public interface IYibfAnaBilgiEventDialogService
{
    Task<YibfAnaBilgiEventDialogResult?> ShowDialogAsync(
        DateTime? eventDate,
        string description,
        string backgroundColor,
        string noteText,
        string approvalStatus = "",
        CancellationToken cancellationToken = default);
}
