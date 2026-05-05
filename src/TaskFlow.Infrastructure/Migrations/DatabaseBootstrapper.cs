using System.Reflection;
using DbUp;
using DbUp.Engine;
using Microsoft.Extensions.Logging;
using Npgsql;
using TaskFlow.Application.Abstractions;
using TaskFlow.Domain.Common;
using TaskFlow.Domain.Users;

namespace TaskFlow.Infrastructure.Migrations;

public sealed class DatabaseBootstrapper(
    string connectionString,
    IPasswordHasher hasher,
    IClock clock,
    ILogger<DatabaseBootstrapper> logger)
{
    public void EnsureDatabase()
    {
        EnsureDatabaseExists();
        RunMigrations();
        SeedDemoData();
    }

    private void EnsureDatabaseExists()
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        var dbName = builder.Database
            ?? throw new InvalidOperationException("Connection string is missing 'Database'.");
        builder.Database = "postgres";

        using var conn = new NpgsqlConnection(builder.ConnectionString);
        conn.Open();
        using var check = new NpgsqlCommand("select 1 from pg_database where datname = @n", conn);
        check.Parameters.AddWithValue("n", dbName);
        if (check.ExecuteScalar() is null)
        {
            using var create = new NpgsqlCommand($"create database \"{dbName}\"", conn);
            create.ExecuteNonQuery();
            logger.LogInformation("Created database {DbName}", dbName);
        }
    }

    private void RunMigrations()
    {
        var upgrader = DeployChanges.To
            .PostgresqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly())
            .LogToConsole()
            .Build();

        DatabaseUpgradeResult result = upgrader.PerformUpgrade();
        if (!result.Successful)
        {
            throw new InvalidOperationException("Database migration failed.", result.Error);
        }
    }

    private void SeedDemoData()
    {
        const string demoEmail = "demo@taskflow.dev";
        const string demoPassword = "Demo123!";
        var demoUserId = new Guid("11111111-1111-1111-1111-111111111111");

        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();

        using (var checkUser = new NpgsqlCommand("select 1 from users where email = @e", conn))
        {
            checkUser.Parameters.AddWithValue("e", demoEmail);
            if (checkUser.ExecuteScalar() is null)
            {
                var hash = hasher.Hash(demoPassword);
                var user = User.Rehydrate(demoUserId, Email.Create(demoEmail), hash, clock.UtcNow);
                using var insertUser = new NpgsqlCommand(
                    "insert into users (id, email, password_hash, created_at_utc) values (@id, @e, @h, @c)", conn);
                insertUser.Parameters.AddWithValue("id", user.Id);
                insertUser.Parameters.AddWithValue("e", user.Email.Value);
                insertUser.Parameters.AddWithValue("h", user.PasswordHash);
                insertUser.Parameters.AddWithValue("c", user.CreatedAtUtc);
                insertUser.ExecuteNonQuery();
                logger.LogInformation("Seeded demo user {Email}", demoEmail);
            }
        }

        using var checkTasks = new NpgsqlCommand("select count(1) from tasks where user_id = @u", conn);
        checkTasks.Parameters.AddWithValue("u", demoUserId);
        var taskCount = (long)(checkTasks.ExecuteScalar() ?? 0L);
        if (taskCount == 0)
        {
            InsertSeedTask(conn, demoUserId, "Welcome to TaskFlow", "Edit or complete this task.", status: 0, dueInDays: 3);
            InsertSeedTask(conn, demoUserId, "Read the architecture doc", "See docs/ARCHITECTURE.md", status: 1, dueInDays: 1);
            logger.LogInformation("Seeded demo tasks");
        }
    }

    private void InsertSeedTask(NpgsqlConnection conn, Guid userId, string title, string description, short status, int dueInDays)
    {
        var now = clock.UtcNow;
        using var cmd = new NpgsqlCommand(
            @"insert into tasks (id, user_id, title, description, status, due_date_utc, created_at_utc, updated_at_utc)
              values (@id, @u, @t, @d, @s, @due, @c, @up)", conn);
        cmd.Parameters.AddWithValue("id", Guid.NewGuid());
        cmd.Parameters.AddWithValue("u", userId);
        cmd.Parameters.AddWithValue("t", title);
        cmd.Parameters.AddWithValue("d", description);
        cmd.Parameters.AddWithValue("s", status);
        cmd.Parameters.AddWithValue("due", now.AddDays(dueInDays));
        cmd.Parameters.AddWithValue("c", now);
        cmd.Parameters.AddWithValue("up", now);
        cmd.ExecuteNonQuery();
    }
}
