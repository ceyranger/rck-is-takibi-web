using RizaCanKilicIsTakibi.Models;

namespace RizaCanKilicIsTakibi.Services.Abstractions;

public interface ITaskRepository
{
    Task<IReadOnlyList<TaskItem>> GetAllAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(TaskItem item, CancellationToken cancellationToken = default);
    Task SaveManyAsync(IEnumerable<TaskItem> items, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task ReorderAsync(TaskBoardType boardType, IEnumerable<Guid> orderedIds, CancellationToken cancellationToken = default);
    Task MoveBoardAsync(Guid id, TaskBoardType boardType, int newSortOrder, CancellationToken cancellationToken = default);
}
