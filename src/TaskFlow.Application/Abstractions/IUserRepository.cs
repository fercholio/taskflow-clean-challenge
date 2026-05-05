using TaskFlow.Domain.Users;

namespace TaskFlow.Application.Abstractions;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(Email email, CancellationToken ct);
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct);
    Task AddAsync(User user, CancellationToken ct);
}
