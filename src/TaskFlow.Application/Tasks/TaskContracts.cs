using TaskFlow.Domain.Tasks;
using DomainTaskStatus = TaskFlow.Domain.Tasks.TaskStatus;

namespace TaskFlow.Application.Tasks;

public sealed record TaskDto(
    Guid Id,
    string Title,
    string? Description,
    DomainTaskStatus Status,
    DateTimeOffset DueDateUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc)
{
    public static TaskDto From(TaskItem t) =>
        new(t.Id, t.Title, t.Description, t.Status, t.DueDateUtc, t.CreatedAtUtc, t.UpdatedAtUtc);
}

public sealed record CreateTaskCommand(Guid UserId, string Title, string? Description, DateTimeOffset DueDateUtc);

public sealed record UpdateTaskCommand(
    Guid Id,
    Guid UserId,
    string Title,
    string? Description,
    DateTimeOffset DueDateUtc,
    DomainTaskStatus Status);
