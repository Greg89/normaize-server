using Microsoft.EntityFrameworkCore;
using Npgsql;
using Normaize.DataNormalization.Infrastructure.Data;
using Testcontainers.PostgreSql;
using Xunit;

namespace Normaize.DataNormalization.PostgresTests;

public sealed class PostgresContainerFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;

    public bool IsAvailable { get; private set; }
    public string SkipReason { get; private set; } = "PostgreSQL Testcontainers are not available.";

    public string ConnectionString
    {
        get
        {
            if (_container is null)
            {
                throw new InvalidOperationException("Postgres container has not been started.");
            }

            return _container.GetConnectionString();
        }
    }

    public async Task InitializeAsync()
    {
        try
        {
            _container = new PostgreSqlBuilder()
                .WithImage("postgres:16-alpine")
                .WithDatabase("normaize_tests")
                .WithUsername("postgres")
                .WithPassword("postgres")
                .Build();

            await _container.StartAsync();

            await EnsureExtensionsAsync(ConnectionString);
            await ApplyMigrationsAsync(ConnectionString);

            IsAvailable = true;
            SkipReason = string.Empty;
        }
        catch (Exception ex)
        {
            // Keep this project runnable locally without breaking devs/CI agents that don't have Docker.
            IsAvailable = false;
            SkipReason = $"PostgreSQL Testcontainers could not start. Ensure Docker is running. Details: {ex.Message}";
        }
    }

    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    private static async Task EnsureExtensionsAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        // Needed for migrations that use uuid_generate_v4()
        await using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "CREATE EXTENSION IF NOT EXISTS \"uuid-ossp\";";
            await cmd.ExecuteNonQueryAsync();
        }
    }

    private static async Task ApplyMigrationsAsync(string connectionString)
    {
        var options = new DbContextOptionsBuilder<DataNormalizationDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        await using var context = new DataNormalizationDbContext(options);
        await context.Database.MigrateAsync();
    }
}
