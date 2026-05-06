using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Api.Auth;
using TaskFlow.Application.Tasks;
using DomainTaskStatus = TaskFlow.Domain.Tasks.TaskStatus;

namespace TaskFlow.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/tasks")]
public sealed class TasksController(
    CreateTaskHandler create,
    UpdateTaskHandler update,
    DeleteTaskHandler delete,
    GetTaskByIdHandler get,
    ListTasksHandler list) : ControllerBase
{
    public sealed record CreateTaskRequest(string Title, string? Description, DateTimeOffset DueDateUtc);
    public sealed record UpdateTaskRequest(string Title, string? Description, DateTimeOffset DueDateUtc, DomainTaskStatus Status);

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
        => (await list.HandleAsync(User.GetUserId(), ct)).ToActionResult();

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
        => (await get.HandleAsync(id, User.GetUserId(), ct)).ToActionResult();

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTaskRequest body, CancellationToken ct)
    {
        var cmd = new CreateTaskCommand(User.GetUserId(), body.Title, body.Description, body.DueDateUtc);
        return (await create.HandleAsync(cmd, ct)).ToActionResult();
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTaskRequest body, CancellationToken ct)
    {
        var cmd = new UpdateTaskCommand(id, User.GetUserId(), body.Title, body.Description, body.DueDateUtc, body.Status);
        return (await update.HandleAsync(cmd, ct)).ToActionResult();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        => (await delete.HandleAsync(id, User.GetUserId(), ct)).ToActionResult();
}
