namespace TaskFlow.Application.Users;

public sealed record RegisterUserCommand(string Email, string Password);

public sealed record LoginUserCommand(string Email, string Password);

public sealed record AuthResponse(string AccessToken, DateTimeOffset ExpiresAtUtc, Guid UserId, string Email);
