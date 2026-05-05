using Microsoft.Extensions.DependencyInjection;
using TaskFlow.Application.Tasks;
using TaskFlow.Application.Users;

namespace TaskFlow.Application;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<RegisterUserValidator>();
        services.AddScoped<LoginUserValidator>();
        services.AddScoped<CreateTaskValidator>();
        services.AddScoped<UpdateTaskValidator>();

        services.AddScoped<RegisterUserHandler>();
        services.AddScoped<LoginUserHandler>();
        services.AddScoped<CreateTaskHandler>();
        services.AddScoped<UpdateTaskHandler>();
        services.AddScoped<DeleteTaskHandler>();
        services.AddScoped<GetTaskByIdHandler>();
        services.AddScoped<ListTasksHandler>();

        return services;
    }
}
