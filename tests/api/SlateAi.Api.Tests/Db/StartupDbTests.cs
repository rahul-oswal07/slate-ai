using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SlateAi.Api.Data;
using SlateAi.Api.Data.Seed;
using SlateAi.Api.Tests.Infrastructure;

namespace SlateAi.Api.Tests.Db;

[Trait("Category", "Db")]
[Collection(DbCollection.Name)]
public sealed class StartupDbTests(PostgresFixture postgres)
{
    [Fact]
    public async Task Concurrent_startups_on_an_empty_database_all_migrate_and_seed()
    {
        // Two app instances starting together (e.g. a slot swap) must not race on migrations or the seed.
        var ct = TestContext.Current.CancellationToken;
        var connectionString = await postgres.CreateEmptyDatabaseAsync(ct);
        var factories = Enumerable.Range(0, 4).Select(_ => new ApiFactory(connectionString)).ToList();

        try
        {
            // Touching Services builds the host, which runs InitialiseDatabaseAsync.
            await Task.WhenAll(factories.Select(f => Task.Run(() => f.Services, ct)));

            await using var scope = factories[0].Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.Empty(await db.Database.GetPendingMigrationsAsync(ct));
            Assert.Equal(1, await db.Teachers.CountAsync(ct));
            Assert.Equal(36, await db.Children.CountAsync(ct));
        }
        finally
        {
            foreach (var factory in factories)
            {
                await factory.DisposeAsync();
            }
        }
    }

    [Fact]
    public async Task Seeding_again_adds_nothing()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new ApiFactory(await postgres.CreateEmptyDatabaseAsync(ct));
        await using var scope = factory.Services.CreateAsyncScope();

        await scope.ServiceProvider.GetRequiredService<DemoSeeder>().SeedAsync(ct);

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(36, await db.Children.CountAsync(ct));
        var byLanguage = await db.Children.GroupBy(c => c.HomeLanguage).ToDictionaryAsync(g => g.Key, g => g.Count(), ct);
        // Rohan (roll 24) is the demo's Hindi-speaking family (T-18).
        Assert.Equal((26, 6, 3, 1), (byLanguage["kn"], byLanguage["te"], byLanguage["ta"], byLanguage["hi"]));
    }
}
