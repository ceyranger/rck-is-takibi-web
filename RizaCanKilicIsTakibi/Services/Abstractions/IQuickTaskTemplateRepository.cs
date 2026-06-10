using RizaCanKilicIsTakibi.Models;

namespace RizaCanKilicIsTakibi.Services.Abstractions;

public interface IQuickTaskTemplateRepository
{
    IReadOnlyList<QuickTaskTemplate> GetAll();
    Task<IReadOnlyList<QuickTaskTemplate>> GetAllAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(QuickTaskTemplate template, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    void ReplaceAll(IEnumerable<QuickTaskTemplate> templates);
}
