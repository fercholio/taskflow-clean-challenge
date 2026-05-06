using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TaskFlow.Application.Tasks;
using TaskFlow.Application.Users;
using Xunit;
using DomainTaskStatus = TaskFlow.Domain.Tasks.TaskStatus;

namespace TaskFlow.Api.Tests;

public class AuthFlowTests : IClassFixture<TaskFlowApiFactory>
{
    private readonly TaskFlowApiFactory _factory;

    public AuthFlowTests(TaskFlowApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Ping_Anonymous_ReturnsOk()
    {
        var client = _factory.CreateClient();

        var resp = await client.GetAsync("/api/v1/ping");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Tasks_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();

        var resp = await client.GetAsync("/api/v1/tasks");

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task FullFlow_Register_Login_Crud_Works()
    {
        var client = _factory.CreateClient();
        var email = $"u{Guid.NewGuid():N}@taskflow.dev";

        var reg = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterUserCommand(email, "Password1!"));
        reg.StatusCode.Should().Be(HttpStatusCode.OK);
        var auth = await reg.Content.ReadFromJsonAsync<AuthResponse>();
        auth!.AccessToken.Should().NotBeNullOrWhiteSpace();

        client.DefaultRequestHeaders.Authorization = new("Bearer", auth.AccessToken);

        var ping = await client.GetAsync("/api/v1/ping/secure");
        ping.StatusCode.Should().Be(HttpStatusCode.OK);

        var create = await client.PostAsJsonAsync("/api/v1/tasks", new
        {
            Title = "Demo task",
            Description = "x",
            DueDateUtc = DateTimeOffset.UtcNow.AddDays(2),
        });
        create.StatusCode.Should().Be(HttpStatusCode.OK);
        var task = await create.Content.ReadFromJsonAsync<TaskDto>();

        var list = await client.GetFromJsonAsync<TaskDto[]>("/api/v1/tasks");
        list!.Should().ContainSingle(t => t.Id == task!.Id);

        var update = await client.PutAsJsonAsync($"/api/v1/tasks/{task!.Id}", new
        {
            Title = "Updated",
            Description = (string?)null,
            DueDateUtc = DateTimeOffset.UtcNow.AddDays(3),
            Status = DomainTaskStatus.InProgress,
        });
        update.StatusCode.Should().Be(HttpStatusCode.OK);

        var del = await client.DeleteAsync($"/api/v1/tasks/{task.Id}");
        del.StatusCode.Should().Be(HttpStatusCode.OK);

        var listAfter = await client.GetFromJsonAsync<TaskDto[]>("/api/v1/tasks");
        listAfter!.Should().BeEmpty();
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var client = _factory.CreateClient();
        var email = $"u{Guid.NewGuid():N}@taskflow.dev";
        await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterUserCommand(email, "Password1!"));

        var resp = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginUserCommand(email, "WrongPass1!"));

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
