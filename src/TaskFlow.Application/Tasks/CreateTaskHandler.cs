using FluentValidation;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Common;
using TaskFlow.Domain.Common;
using TaskFlow.Domain.Tasks;

namespace TaskFlow.Application.Tasks;

public sealed class CreateTaskValidator : AbstractValidator<CreateTaskCommand>
{
    public CreateTaskValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(TaskItem.MaxTitleLength);
        RuleFor(x => x.Description).MaximumLength(TaskItem.MaxDescriptionLength);
    }
}

public sealed class CreateTaskHandler(ITaskRepository tasks, IClock clock, CreateTaskValidator validator)
{
    public async Task<Result<TaskDto>> HandleAsync(CreateTaskCommand cmd, CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(cmd, ct);
        if (!validation.IsValid)
        {
            return Error.Validation(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)));
        }

        TaskItem task;
        try
        {
            task = TaskItem.Create(cmd.UserId, cmd.Title, cmd.Description, cmd.DueDateUtc, clock);
        }
        catch (DomainException ex)
        {
            return Error.Domain(ex.Message);
        }

        await tasks.AddAsync(task, ct);
        return TaskDto.From(task);
    }
}
