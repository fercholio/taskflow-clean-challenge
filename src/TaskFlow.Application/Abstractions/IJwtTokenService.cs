using TaskFlow.Domain.Users;

namespace TaskFlow.Application.Abstractions;

public interface IJwtTokenService
{
    JwtTokenResult CreateToken(User user);
}

public sealed record JwtTokenResult(string AccessToken, DateTimeOffset ExpiresAtUtc);
