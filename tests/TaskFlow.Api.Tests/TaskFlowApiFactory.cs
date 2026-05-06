using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskFlow.Api.Tests.Fakes;
using TaskFlow.Application.Abstractions;

namespace TaskFlow.Api.Tests;

public sealed class TaskFlowApiFactory : WebApplicationFactory<Program>
{
    public InMemoryUserRepository Users { get; } = new();
    public InMemoryTaskRepository Tasks { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, cfg) =>
        {
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] = "Host=localhost;Database=taskflow_test;Username=t;Password=t",
                ["Jwt:Issuer"] = "taskflow",
                ["Jwt:Audience"] = "taskflow-clients",
                ["Jwt:SigningKey"] = "TestSigningKey_AtLeast32Characters_xxxxxxxxx",
                ["Jwt:ExpiryMinutes"] = "30",
            });
        });

        builder.ConfigureServices(services =>
        {
            Replace(services, ServiceDescriptor.Scoped<IUserRepository>(_ => Users));
            Replace(services, ServiceDescriptor.Scoped<ITaskRepository>(_ => Tasks));
        });
    }

    private static void Replace(IServiceCollection services, ServiceDescriptor descriptor)
    {
        for (var i = services.Count - 1; i >= 0; i--)
        {
            if (services[i].ServiceType == descriptor.ServiceType)
            {
                services.RemoveAt(i);
            }
        }
        services.Add(descriptor);
    }
}
