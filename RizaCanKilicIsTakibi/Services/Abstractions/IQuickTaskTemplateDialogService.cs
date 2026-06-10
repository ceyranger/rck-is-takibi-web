namespace RizaCanKilicIsTakibi.Services.Abstractions;

public interface IQuickTaskTemplateDialogService
{
    Task<IReadOnlyList<string>?> ShowDialogAsync(CancellationToken cancellationToken = default);
}
