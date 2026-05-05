using TaskFlow.Domain.Tasks;

namespace TaskFlow.Application.Abstractions;

public interface ITaskRepository
{
    Task<TaskItem?> GetByIdAsync(Guid id, Guid userId, CancellationToken ct);
    Task<IReadOnlyList<TaskItem>> ListByUserAsync(Guid userId, CancellationToken ct);
    Task AddAsync(TaskItem task, CancellationToken ct);
    Task UpdateAsync(TaskItem task, CancellationToken ct);
    Task<bool> DeleteAsync(Guid id, Guid userId, CancellationToken ct);
}
