using FluentAssertions;
using TaskFlow.Domain.Common;
using TaskFlow.Domain.Tasks;
using Xunit;
using DomainTaskStatus = TaskFlow.Domain.Tasks.TaskStatus;

namespace TaskFlow.Domain.Tests.Tasks;

public class TaskItemTests
{
    private static readonly IClock Clock = new FixedClock(new DateTimeOffset(2025, 11, 5, 12, 0, 0, TimeSpan.Zero));
    private static readonly Guid UserId = Guid.NewGuid();

    private static TaskItem NewTask() =>
        TaskItem.Create(UserId, "Write report", "Q4 numbers", Clock.UtcNow.AddDays(2), Clock);

    [Fact]
    public void Create_WhenValid_DefaultsToPending()
    {
        var task = NewTask();

        task.Id.Should().NotBeEmpty();
        task.UserId.Should().Be(UserId);
        task.Status.Should().Be(DomainTaskStatus.Pending);
        task.CreatedAtUtc.Should().Be(Clock.UtcNow);
        task.UpdatedAtUtc.Should().Be(Clock.UtcNow);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WhenTitleEmpty_Throws(string title)
    {
        var act = () => TaskItem.Create(UserId, title, null, Clock.UtcNow.AddDays(1), Clock);

        act.Should().Throw<DomainException>().WithMessage("*Title*");
    }

    [Fact]
    public void Create_WhenTitleTooLong_Throws()
    {
        var title = new string('x', TaskItem.MaxTitleLength + 1);

        var act = () => TaskItem.Create(UserId, title, null, Clock.UtcNow.AddDays(1), Clock);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_WhenDueDateInPast_Throws()
    {
        var act = () => TaskItem.Create(UserId, "x", null, Clock.UtcNow.AddDays(-1), Clock);

        act.Should().Throw<DomainException>().WithMessage("*future*");
    }

    [Fact]
    public void Create_WhenUserIdEmpty_Throws()
    {
        var act = () => TaskItem.Create(Guid.Empty, "x", null, Clock.UtcNow.AddDays(1), Clock);

        act.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData(DomainTaskStatus.Pending, DomainTaskStatus.InProgress, true)]
    [InlineData(DomainTaskStatus.InProgress, DomainTaskStatus.Done, true)]
    [InlineData(DomainTaskStatus.Pending, DomainTaskStatus.Done, true)]
    [InlineData(DomainTaskStatus.Pending, DomainTaskStatus.Cancelled, true)]
    [InlineData(DomainTaskStatus.InProgress, DomainTaskStatus.Cancelled, true)]
    [InlineData(DomainTaskStatus.Done, DomainTaskStatus.InProgress, false)]
    [InlineData(DomainTaskStatus.Done, DomainTaskStatus.Cancelled, false)]
    [InlineData(DomainTaskStatus.Cancelled, DomainTaskStatus.InProgress, false)]
    public void ChangeStatus_RespectsAllowedTransitions(DomainTaskStatus from, DomainTaskStatus to, bool allowed)
    {
        var task = TaskItem.Rehydrate(Guid.NewGuid(), UserId, "x", null, from, Clock.UtcNow.AddDays(1), Clock.UtcNow, Clock.UtcNow);

        var act = () => task.ChangeStatus(to, Clock);

        if (allowed)
        {
            act.Should().NotThrow();
            task.Status.Should().Be(to);
        }
        else
        {
            act.Should().Throw<DomainException>();
        }
    }

    [Fact]
    public void UpdateDetails_WhenValid_UpdatesFieldsAndTimestamp()
    {
        var task = NewTask();
        var laterClock = new FixedClock(Clock.UtcNow.AddHours(1));

        task.UpdateDetails("New title", "  new desc  ", Clock.UtcNow.AddDays(3), laterClock);

        task.Title.Should().Be("New title");
        task.Description.Should().Be("new desc");
        task.UpdatedAtUtc.Should().Be(laterClock.UtcNow);
    }
}
