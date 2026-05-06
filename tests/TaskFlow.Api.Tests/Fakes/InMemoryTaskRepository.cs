using System.Collections.Concurrent;
using TaskFlow.Application.Abstractions;
using TaskFlow.Domain.Tasks;

namespace TaskFlow.Api.Tests.Fakes;

public sealed class InMemoryTaskRepository : ITaskRepository
{
    private readonly ConcurrentDictionary<Guid, TaskItem> _store = new();

    public Task AddAsync(TaskItem task, CancellationToken ct)
    {
        _store[task.Id] = task;
        return Task.CompletedTask;
    }

    public Task<bool> DeleteAsync(Guid id, Guid userId, CancellationToken ct)
    {
        if (_store.TryGetValue(id, out var existing) && existing.UserId == userId)
        {
            return Task.FromResult(_store.TryRemove(id, out _));
        }
        return Task.FromResult(false);
    }

    public Task<TaskItem?> GetByIdAsync(Guid id, Guid userId, CancellationToken ct)
        => Task.FromResult(_store.TryGetValue(id, out var t) && t.UserId == userId ? t : null);

    public Task<IReadOnlyList<TaskItem>> ListByUserAsync(Guid userId, CancellationToken ct)
    {
        IReadOnlyList<TaskItem> list = _store.Values.Where(t => t.UserId == userId).ToList();
        return Task.FromResult(list);
    }

    public Task UpdateAsync(TaskItem task, CancellationToken ct)
    {
        _store[task.Id] = task;
        return Task.CompletedTask;
    }
}
