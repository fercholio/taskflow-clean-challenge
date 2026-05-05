using Microsoft.AspNetCore.Identity;
using TaskFlow.Application.Abstractions;
using TaskFlow.Domain.Users;

namespace TaskFlow.Infrastructure.Security;

public sealed class IdentityPasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<User> _inner = new();
    private static readonly User DummyUser = User.Rehydrate(Guid.Empty, Email.Create("dummy@taskflow.dev"), "x", DateTimeOffset.UnixEpoch);

    public string Hash(string password) => _inner.HashPassword(DummyUser, password);

    public bool Verify(string password, string hash)
    {
        var result = _inner.VerifyHashedPassword(DummyUser, hash, password);
        return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }
}
