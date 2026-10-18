using Npgsql;
using Testcontainers.PostgreSql;

[assembly: AssemblyFixture(typeof(SlateAi.Api.Tests.Infrastructure.PostgresFixture))]

namespace SlateAi.Api.Tests.Infrastructure;

/// <summary>One PostgreSQL container shared by every test in the assembly.</summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16").Build();

    public string ConnectionString => _container.GetConnectionString();

    /// <summary>Creates a fresh, empty database in the shared container and returns its connection string.</summary>
    public async Task<string> CreateEmptyDatabaseAsync(CancellationToken cancellationToken)
    {
        var name = $"test_{Guid.NewGuid():N}";
        await using (var connection = new NpgsqlConnection(ConnectionString))
        {
            await connection.OpenAsync(cancellationToken);
            await using var command = new NpgsqlCommand($"CREATE DATABASE \"{name}\"", connection);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        return new NpgsqlConnectionStringBuilder(ConnectionString) { Database = name }.ConnectionString;
    }

    public async ValueTask InitializeAsync() => await _container.StartAsync();

    public ValueTask DisposeAsync() => _container.DisposeAsync();
}
