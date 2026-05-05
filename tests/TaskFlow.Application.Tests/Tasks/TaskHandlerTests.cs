using FluentAssertions;
using Moq;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Common;
using TaskFlow.Application.Tasks;
using TaskFlow.Domain.Common;
using TaskFlow.Domain.Tasks;
using Xunit;
using DomainTaskStatus = TaskFlow.Domain.Tasks.TaskStatus;

namespace TaskFlow.Application.Tests.Tasks;

public class TaskHandlerTests
{
    private static readonly IClock Clock = new FixedClock(new DateTimeOffset(2025, 11, 5, 12, 0, 0, TimeSpan.Zero));
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public async Task Create_WhenValid_PersistsAndReturnsDto()
    {
        var repo = new Mock<ITaskRepository>();
        var handler = new CreateTaskHandler(repo.Object, Clock, new CreateTaskValidator());

        var result = await handler.HandleAsync(
            new CreateTaskCommand(UserId, "Write report", "Q4", Clock.UtcNow.AddDays(2)), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Title.Should().Be("Write report");
        repo.Verify(r => r.AddAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_WhenTitleEmpty_ReturnsValidation()
    {
        var handler = new CreateTaskHandler(Mock.Of<ITaskRepository>(), Clock, new CreateTaskValidator());

        var result = await handler.HandleAsync(new CreateTaskCommand(UserId, "", null, Clock.UtcNow.AddDays(1)), CancellationToken.None);

        result.Error!.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task Update_WhenTaskMissing_ReturnsNotFound()
    {
        var repo = new Mock<ITaskRepository>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TaskItem?)null);
        var handler = new UpdateTaskHandler(repo.Object, Clock, new UpdateTaskValidator());

        var result = await handler.HandleAsync(
            new UpdateTaskCommand(Guid.NewGuid(), UserId, "x", null, Clock.UtcNow.AddDays(1), DomainTaskStatus.Pending),
            CancellationToken.None);

        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Update_WhenInvalidTransition_ReturnsDomain()
    {
        var existing = TaskItem.Rehydrate(Guid.NewGuid(), UserId, "x", null, DomainTaskStatus.Done, Clock.UtcNow.AddDays(1), Clock.UtcNow, Clock.UtcNow);
        var repo = new Mock<ITaskRepository>();
        repo.Setup(r => r.GetByIdAsync(existing.Id, UserId, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        var handler = new UpdateTaskHandler(repo.Object, Clock, new UpdateTaskValidator());

        var result = await handler.HandleAsync(
            new UpdateTaskCommand(existing.Id, UserId, "x", null, Clock.UtcNow.AddDays(1), DomainTaskStatus.InProgress),
            CancellationToken.None);

        result.Error!.Type.Should().Be(ErrorType.Domain);
    }

    [Fact]
    public async Task Delete_WhenMissing_ReturnsNotFound()
    {
        var repo = new Mock<ITaskRepository>();
        repo.Setup(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var handler = new DeleteTaskHandler(repo.Object);

        var result = await handler.HandleAsync(Guid.NewGuid(), UserId, CancellationToken.None);

        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task List_ReturnsMappedDtos()
    {
        var t = TaskItem.Create(UserId, "x", null, Clock.UtcNow.AddDays(1), Clock);
        var repo = new Mock<ITaskRepository>();
        repo.Setup(r => r.ListByUserAsync(UserId, It.IsAny<CancellationToken>())).ReturnsAsync(new[] { t });
        var handler = new ListTasksHandler(repo.Object);

        var result = await handler.HandleAsync(UserId, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().HaveCount(1);
    }
}
