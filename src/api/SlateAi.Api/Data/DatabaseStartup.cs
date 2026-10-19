using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SlateAi.Api.Data.Seed;

namespace SlateAi.Api.Data;

public static class DatabaseStartup
{
    // Arbitrary app-wide key for pg_advisory_lock; serialises migrations across instances.
    private const long MigrateLockKey = 0x5EED_0002;

    public static async Task InitialiseDatabaseAsync(this WebApplication app, CancellationToken cancellationToken)
    {
        var options = app.Services.GetRequiredService<IOptions<DatabaseOptions>>().Value;
        if (!options.MigrateOnStartup)
        {
            return;
        }

        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Two instances can start at once (e.g. a slot swap). Without the lock both apply the same migration
        // and the loser fails with "relation already exists". Session lock on one open connection, because
        // migrations manage their own transactions.
        await db.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            await db.Database.ExecuteSqlAsync($"SELECT pg_advisory_lock({MigrateLockKey})", cancellationToken);
            await db.Database.MigrateAsync(cancellationToken);
        }
        finally
        {
            await db.Database.ExecuteSqlAsync($"SELECT pg_advisory_unlock({MigrateLockKey})", CancellationToken.None);
            await db.Database.CloseConnectionAsync();
        }

        if (options.SeedOnStartup)
        {
            await scope.ServiceProvider.GetRequiredService<DemoSeeder>().SeedAsync(cancellationToken);
        }
    }
}
