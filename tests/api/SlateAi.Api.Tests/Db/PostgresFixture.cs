using Npgsql;
using Testcontainers.PostgreSql;

namespace SlateAi.Api.Tests.Db;

/// <summary>
/// The small set of tests only a real Postgres can prove (CLAUDE.md, "Testing expectations"). Excluded from
/// verify.sh; run with <c>dotnet test --filter Category=Db</c> (needs Docker) and in CI.
/// </summary>
[CollectionDefinition(Name)]
public sealed class DbCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "Db";
}

/// <summary>One Postgres container for the Db collection, started only when a Db test runs.</summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16").Build();

    /// <summary>Creates a fresh, empty database in the container and returns its connection string.</summary>
    public async Task<string> CreateEmptyDatabaseAsync(CancellationToken cancellationToken)
    {
        var name = $"test_{Guid.NewGuid():N}";
        await using (var connection = new NpgsqlConnection(_container.GetConnectionString()))
        {
            await connection.OpenAsync(cancellationToken);
            await using var command = new NpgsqlCommand($"CREATE DATABASE \"{name}\"", connection);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        return new NpgsqlConnectionStringBuilder(_container.GetConnectionString()) { Database = name }.ConnectionString;
    }

    public async ValueTask InitializeAsync() => await _container.StartAsync();

    public ValueTask DisposeAsync() => _container.DisposeAsync();
}
