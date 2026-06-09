using RizaCanKilicIsTakibi.Models;

namespace RizaCanKilicIsTakibi.Services.Abstractions;

public interface IYibfAnaBilgiEntryDialogService
{
    Task<YibfAnaBilgiEntryDialogResult?> ShowDialogAsync(
        YibfAnaBilgiEntryDialogResult? initialValues = null,
        bool isEditMode = false,
        CancellationToken cancellationToken = default);
}
