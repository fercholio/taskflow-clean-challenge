using TaskFlow.Domain.Common;

namespace TaskFlow.Domain.Users;

public sealed class User
{
    public Guid Id { get; }
    public Email Email { get; }
    public string PasswordHash { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }

    private User(Guid id, Email email, string passwordHash, DateTimeOffset createdAtUtc)
    {
        Id = id;
        Email = email;
        PasswordHash = passwordHash;
        CreatedAtUtc = createdAtUtc;
    }

    public static User Register(Email email, string passwordHash, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(email);
        ArgumentNullException.ThrowIfNull(clock);

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new DomainException("Password hash is required.");
        }

        return new User(Guid.NewGuid(), email, passwordHash, clock.UtcNow);
    }

    public static User Rehydrate(Guid id, Email email, string passwordHash, DateTimeOffset createdAtUtc)
        => new(id, email, passwordHash, createdAtUtc);
}
