using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace TaskFlow.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (UnauthorizedAccessException ex)
        {
            await Write(context, HttpStatusCode.Unauthorized, "unauthorized", ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception");
            await Write(context, HttpStatusCode.InternalServerError, "unexpected", "An unexpected error occurred.");
        }
    }

    private static Task Write(HttpContext ctx, HttpStatusCode status, string title, string detail)
    {
        if (ctx.Response.HasStarted)
        {
            return Task.CompletedTask;
        }

        ctx.Response.StatusCode = (int)status;
        ctx.Response.ContentType = "application/problem+json";
        var problem = new ProblemDetails { Status = (int)status, Title = title, Detail = detail };
        return ctx.Response.WriteAsync(JsonSerializer.Serialize(problem));
    }
}
