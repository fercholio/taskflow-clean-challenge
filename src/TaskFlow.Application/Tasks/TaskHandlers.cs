using FluentValidation;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Common;
using TaskFlow.Domain.Common;
using TaskFlow.Domain.Tasks;

namespace TaskFlow.Application.Tasks;

public sealed class UpdateTaskValidator : AbstractValidator<UpdateTaskCommand>
{
    public UpdateTaskValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(TaskItem.MaxTitleLength);
        RuleFor(x => x.Description).MaximumLength(TaskItem.MaxDescriptionLength);
    }
}

public sealed class UpdateTaskHandler(ITaskRepository tasks, IClock clock, UpdateTaskValidator validator)
{
    public async Task<Result<TaskDto>> HandleAsync(UpdateTaskCommand cmd, CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(cmd, ct);
        if (!validation.IsValid)
        {
            return Error.Validation(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)));
        }

        var task = await tasks.GetByIdAsync(cmd.Id, cmd.UserId, ct);
        if (task is null)
        {
            return Error.NotFound("Task not found.");
        }

        try
        {
            task.UpdateDetails(cmd.Title, cmd.Description, cmd.DueDateUtc, clock);
            task.ChangeStatus(cmd.Status, clock);
        }
        catch (DomainException ex)
        {
            return Error.Domain(ex.Message);
        }

        await tasks.UpdateAsync(task, ct);
        return TaskDto.From(task);
    }
}

public sealed class DeleteTaskHandler(ITaskRepository tasks)
{
    public async Task<Result<bool>> HandleAsync(Guid id, Guid userId, CancellationToken ct)
    {
        var deleted = await tasks.DeleteAsync(id, userId, ct);
        return deleted ? true : Error.NotFound("Task not found.");
    }
}

public sealed class GetTaskByIdHandler(ITaskRepository tasks)
{
    public async Task<Result<TaskDto>> HandleAsync(Guid id, Guid userId, CancellationToken ct)
    {
        var task = await tasks.GetByIdAsync(id, userId, ct);
        return task is null ? Error.NotFound("Task not found.") : TaskDto.From(task);
    }
}

public sealed class ListTasksHandler(ITaskRepository tasks)
{
    public async Task<Result<IReadOnlyList<TaskDto>>> HandleAsync(Guid userId, CancellationToken ct)
    {
        var items = await tasks.ListByUserAsync(userId, ct);
        IReadOnlyList<TaskDto> dtos = items.Select(TaskDto.From).ToList();
        return Result<IReadOnlyList<TaskDto>>.Success(dtos);
    }
}
