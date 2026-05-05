using TaskFlow.Domain.Common;

namespace TaskFlow.Domain.Tasks;

public sealed class TaskItem
{
    public const int MaxTitleLength = 200;
    public const int MaxDescriptionLength = 2000;

    public Guid Id { get; }
    public Guid UserId { get; }
    public string Title { get; private set; }
    public string? Description { get; private set; }
    public TaskStatus Status { get; private set; }
    public DateTimeOffset DueDateUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private TaskItem(
        Guid id,
        Guid userId,
        string title,
        string? description,
        TaskStatus status,
        DateTimeOffset dueDateUtc,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        Id = id;
        UserId = userId;
        Title = title;
        Description = description;
        Status = status;
        DueDateUtc = dueDateUtc;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public static TaskItem Create(
        Guid userId,
        string title,
        string? description,
        DateTimeOffset dueDateUtc,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);

        if (userId == Guid.Empty)
        {
            throw new DomainException("UserId is required.");
        }

        ValidateTitle(title);
        ValidateDescription(description);
        ValidateDueDate(dueDateUtc, clock);

        var now = clock.UtcNow;
        return new TaskItem(
            Guid.NewGuid(),
            userId,
            title.Trim(),
            string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            TaskStatus.Pending,
            dueDateUtc,
            now,
            now);
    }

    public static TaskItem Rehydrate(
        Guid id,
        Guid userId,
        string title,
        string? description,
        TaskStatus status,
        DateTimeOffset dueDateUtc,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
        => new(id, userId, title, description, status, dueDateUtc, createdAtUtc, updatedAtUtc);

    public void UpdateDetails(string title, string? description, DateTimeOffset dueDateUtc, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        ValidateTitle(title);
        ValidateDescription(description);
        ValidateDueDate(dueDateUtc, clock);

        Title = title.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        DueDateUtc = dueDateUtc;
        UpdatedAtUtc = clock.UtcNow;
    }

    public void ChangeStatus(TaskStatus next, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);

        if (Status == next)
        {
            return;
        }

        var allowed = (Status, next) switch
        {
            (TaskStatus.Pending, TaskStatus.InProgress) => true,
            (TaskStatus.InProgress, TaskStatus.Done) => true,
            (TaskStatus.Pending, TaskStatus.Done) => true,
            (_, TaskStatus.Cancelled) when Status != TaskStatus.Done => true,
            _ => false,
        };

        if (!allowed)
        {
            throw new DomainException($"Cannot transition task from {Status} to {next}.");
        }

        Status = next;
        UpdatedAtUtc = clock.UtcNow;
    }

    private static void ValidateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException("Title is required.");
        }

        if (title.Length > MaxTitleLength)
        {
            throw new DomainException($"Title cannot exceed {MaxTitleLength} characters.");
        }
    }

    private static void ValidateDescription(string? description)
    {
        if (description is not null && description.Length > MaxDescriptionLength)
        {
            throw new DomainException($"Description cannot exceed {MaxDescriptionLength} characters.");
        }
    }

    private static void ValidateDueDate(DateTimeOffset dueDateUtc, IClock clock)
    {
        if (dueDateUtc < clock.UtcNow.AddMinutes(-1))
        {
            throw new DomainException("DueDate must be in the future.");
        }
    }
}
