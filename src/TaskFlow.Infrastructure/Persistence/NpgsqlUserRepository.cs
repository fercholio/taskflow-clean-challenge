using Npgsql;
using TaskFlow.Application.Abstractions;
using TaskFlow.Domain.Users;

namespace TaskFlow.Infrastructure.Persistence;

public sealed class NpgsqlUserRepository(NpgsqlDataSource dataSource) : IUserRepository
{
    private const string SelectByEmail = @"
        select id, email, password_hash, created_at_utc
        from users where email = @e limit 1";

    private const string SelectById = @"
        select id, email, password_hash, created_at_utc
        from users where id = @id limit 1";

    private const string Insert = @"
        insert into users (id, email, password_hash, created_at_utc)
        values (@id, @e, @h, @c)";

    public async Task<User?> GetByEmailAsync(Email email, CancellationToken ct)
    {
        await using var cmd = dataSource.CreateCommand(SelectByEmail);
        cmd.Parameters.AddWithValue("e", email.Value);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        return await ReadOneAsync(reader, ct);
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        await using var cmd = dataSource.CreateCommand(SelectById);
        cmd.Parameters.AddWithValue("id", id);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        return await ReadOneAsync(reader, ct);
    }

    public async Task AddAsync(User user, CancellationToken ct)
    {
        await using var cmd = dataSource.CreateCommand(Insert);
        cmd.Parameters.AddWithValue("id", user.Id);
        cmd.Parameters.AddWithValue("e", user.Email.Value);
        cmd.Parameters.AddWithValue("h", user.PasswordHash);
        cmd.Parameters.AddWithValue("c", user.CreatedAtUtc);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static async Task<User?> ReadOneAsync(NpgsqlDataReader reader, CancellationToken ct)
    {
        if (!await reader.ReadAsync(ct))
        {
            return null;
        }

        return User.Rehydrate(
            reader.GetGuid(0),
            Email.Create(reader.GetString(1)),
            reader.GetString(2),
            reader.GetFieldValue<DateTimeOffset>(3));
    }
}
