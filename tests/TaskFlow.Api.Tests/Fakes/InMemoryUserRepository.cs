using System.Collections.Concurrent;
using TaskFlow.Application.Abstractions;
using TaskFlow.Domain.Users;

namespace TaskFlow.Api.Tests.Fakes;

public sealed class InMemoryUserRepository : IUserRepository
{
    private readonly ConcurrentDictionary<Guid, User> _byId = new();

    public Task AddAsync(User user, CancellationToken ct)
    {
        _byId[user.Id] = user;
        return Task.CompletedTask;
    }

    public Task<User?> GetByEmailAsync(Email email, CancellationToken ct)
        => Task.FromResult(_byId.Values.FirstOrDefault(u => u.Email.Value == email.Value));

    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct)
        => Task.FromResult(_byId.TryGetValue(id, out var u) ? u : null);
}
