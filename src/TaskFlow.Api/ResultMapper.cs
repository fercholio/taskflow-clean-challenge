using Microsoft.AspNetCore.Mvc;
using TaskFlow.Application.Common;

namespace TaskFlow.Api;

internal static class ResultMapper
{
    public static IActionResult ToActionResult<T>(this Result<T> result)
    {
        if (result.IsSuccess)
        {
            return new OkObjectResult(result.Value);
        }

        var error = result.Error!;
        return error.Type switch
        {
            ErrorType.Validation => new BadRequestObjectResult(Problem(error, StatusCodes.Status400BadRequest)),
            ErrorType.NotFound => new NotFoundObjectResult(Problem(error, StatusCodes.Status404NotFound)),
            ErrorType.Conflict => new ConflictObjectResult(Problem(error, StatusCodes.Status409Conflict)),
            ErrorType.Unauthorized => new UnauthorizedObjectResult(Problem(error, StatusCodes.Status401Unauthorized)),
            ErrorType.Domain => new UnprocessableEntityObjectResult(Problem(error, StatusCodes.Status422UnprocessableEntity)),
            _ => new ObjectResult(Problem(error, StatusCodes.Status500InternalServerError)) { StatusCode = 500 },
        };
    }

    private static ProblemDetails Problem(Error error, int status) => new()
    {
        Status = status,
        Title = error.Code,
        Detail = error.Message,
    };
}
