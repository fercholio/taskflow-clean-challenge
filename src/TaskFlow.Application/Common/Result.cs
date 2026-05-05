namespace TaskFlow.Application.Common;

public enum ErrorType
{
    Validation,
    NotFound,
    Conflict,
    Unauthorized,
    Domain,
    Unexpected,
}

public sealed record Error(ErrorType Type, string Code, string Message)
{
    public static Error Validation(string message) => new(ErrorType.Validation, "validation", message);
    public static Error NotFound(string message) => new(ErrorType.NotFound, "not_found", message);
    public static Error Conflict(string message) => new(ErrorType.Conflict, "conflict", message);
    public static Error Unauthorized(string message) => new(ErrorType.Unauthorized, "unauthorized", message);
    public static Error Domain(string message) => new(ErrorType.Domain, "domain", message);
}

public readonly record struct Result<T>
{
    public T? Value { get; }
    public Error? Error { get; }
    public bool IsSuccess => Error is null;

    private Result(T? value, Error? error)
    {
        Value = value;
        Error = error;
    }

    public static Result<T> Success(T value) => new(value, null);
    public static Result<T> Failure(Error error) => new(default, error);

    public static implicit operator Result<T>(T value) => Success(value);
    public static implicit operator Result<T>(Error error) => Failure(error);
}
