using System.Net.Http.Headers;
using System.Net.Http.Json;
using TaskFlow.Application.Users;
using Xunit;
using Xunit.Abstractions;

namespace TaskFlow.Api.Tests;

public class DiagnoseTest(TaskFlowApiFactory factory, ITestOutputHelper output) : IClassFixture<TaskFlowApiFactory>
{
    [Fact]
    public async Task Diagnose()
    {
        var client = factory.CreateClient();
        var email = $"u{Guid.NewGuid():N}@taskflow.dev";
        var reg = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterUserCommand(email, "Password1!"));
        var auth = await reg.Content.ReadFromJsonAsync<AuthResponse>();
        output.WriteLine($"token: {auth!.AccessToken}");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        var ping = await client.GetAsync("/api/v1/ping/secure");
        output.WriteLine($"status: {ping.StatusCode}");
        foreach (var h in ping.Headers) output.WriteLine($"H: {h.Key}={string.Join(',', h.Value)}");
        output.WriteLine(await ping.Content.ReadAsStringAsync());
    }
}
