using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using TaskFlow.Application.Abstractions;
using TaskFlow.Domain.Common;
using TaskFlow.Infrastructure.Migrations;
using TaskFlow.Infrastructure.Persistence;
using TaskFlow.Infrastructure.Security;

namespace TaskFlow.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var connectionString = config.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("ConnectionStrings:Postgres is missing.");

        var dataSource = new NpgsqlDataSourceBuilder(connectionString).Build();
        services.AddSingleton(dataSource);

        services.Configure<JwtOptions>(config.GetSection(JwtOptions.SectionName));

        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IPasswordHasher, IdentityPasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();

        services.AddScoped<IUserRepository, NpgsqlUserRepository>();
        services.AddScoped<ITaskRepository, NpgsqlTaskRepository>();

        services.AddSingleton<DatabaseBootstrapper>(sp => new DatabaseBootstrapper(
            connectionString,
            sp.GetRequiredService<IPasswordHasher>(),
            sp.GetRequiredService<IClock>(),
            sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<DatabaseBootstrapper>>()));

        return services;
    }
}
