using Npgsql;
using TaskFlow.Application.Abstractions;
using TaskFlow.Domain.Tasks;
using DomainTaskStatus = TaskFlow.Domain.Tasks.TaskStatus;

namespace TaskFlow.Infrastructure.Persistence;

public sealed class NpgsqlTaskRepository(NpgsqlDataSource dataSource) : ITaskRepository
{
    private const string SelectById = @"
        select id, user_id, title, description, status, due_date_utc, created_at_utc, updated_at_utc
        from tasks where id = @id and user_id = @u limit 1";

    private const string SelectByUser = @"
        select id, user_id, title, description, status, due_date_utc, created_at_utc, updated_at_utc
        from tasks where user_id = @u order by created_at_utc desc";

    private const string Insert = @"
        insert into tasks (id, user_id, title, description, status, due_date_utc, created_at_utc, updated_at_utc)
        values (@id, @u, @t, @d, @s, @due, @c, @up)";

    private const string Update = @"
        update tasks
        set title = @t, description = @d, status = @s, due_date_utc = @due, updated_at_utc = @up
        where id = @id and user_id = @u";

    private const string Delete = @"delete from tasks where id = @id and user_id = @u";

    public async Task<TaskItem?> GetByIdAsync(Guid id, Guid userId, CancellationToken ct)
    {
        await using var cmd = dataSource.CreateCommand(SelectById);
        cmd.Parameters.AddWithValue("id", id);
        cmd.Parameters.AddWithValue("u", userId);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct))
        {
            return null;
        }
        return Read(reader);
    }

    public async Task<IReadOnlyList<TaskItem>> ListByUserAsync(Guid userId, CancellationToken ct)
    {
        await using var cmd = dataSource.CreateCommand(SelectByUser);
        cmd.Parameters.AddWithValue("u", userId);
        var list = new List<TaskItem>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            list.Add(Read(reader));
        }
        return list;
    }

    public async Task AddAsync(TaskItem task, CancellationToken ct)
    {
        await using var cmd = dataSource.CreateCommand(Insert);
        BindAll(cmd, task);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task UpdateAsync(TaskItem task, CancellationToken ct)
    {
        await using var cmd = dataSource.CreateCommand(Update);
        cmd.Parameters.AddWithValue("id", task.Id);
        cmd.Parameters.AddWithValue("u", task.UserId);
        cmd.Parameters.AddWithValue("t", task.Title);
        cmd.Parameters.AddWithValue("d", (object?)task.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("s", (short)task.Status);
        cmd.Parameters.AddWithValue("due", task.DueDateUtc);
        cmd.Parameters.AddWithValue("up", task.UpdatedAtUtc);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<bool> DeleteAsync(Guid id, Guid userId, CancellationToken ct)
    {
        await using var cmd = dataSource.CreateCommand(Delete);
        cmd.Parameters.AddWithValue("id", id);
        cmd.Parameters.AddWithValue("u", userId);
        var rows = await cmd.ExecuteNonQueryAsync(ct);
        return rows > 0;
    }

    private static void BindAll(NpgsqlCommand cmd, TaskItem t)
    {
        cmd.Parameters.AddWithValue("id", t.Id);
        cmd.Parameters.AddWithValue("u", t.UserId);
        cmd.Parameters.AddWithValue("t", t.Title);
        cmd.Parameters.AddWithValue("d", (object?)t.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("s", (short)t.Status);
        cmd.Parameters.AddWithValue("due", t.DueDateUtc);
        cmd.Parameters.AddWithValue("c", t.CreatedAtUtc);
        cmd.Parameters.AddWithValue("up", t.UpdatedAtUtc);
    }

    private static TaskItem Read(NpgsqlDataReader r) => TaskItem.Rehydrate(
        r.GetGuid(0),
        r.GetGuid(1),
        r.GetString(2),
        r.IsDBNull(3) ? null : r.GetString(3),
        (DomainTaskStatus)r.GetInt16(4),
        r.GetFieldValue<DateTimeOffset>(5),
        r.GetFieldValue<DateTimeOffset>(6),
        r.GetFieldValue<DateTimeOffset>(7));
}
