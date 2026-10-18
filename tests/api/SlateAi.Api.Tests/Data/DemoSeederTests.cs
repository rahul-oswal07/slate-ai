using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SlateAi.Api.Data;
using SlateAi.Api.Data.Seed;
using SlateAi.Api.Tests.Infrastructure;

namespace SlateAi.Api.Tests.Data;

[Trait("Category", "Integration")]
public sealed class DemoSeederTests(PostgresFixture postgres)
{
    [Fact]
    public async Task Startup_seeds_one_teacher_one_class_and_36_children()
    {
        await using var factory = new ApiFactory(postgres.ConnectionString);
        var ct = TestContext.Current.CancellationToken;

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.Equal(1, await db.Teachers.CountAsync(ct));
        Assert.Equal(1, await db.Classes.CountAsync(ct));
        Assert.Equal(36, await db.Children.CountAsync(ct));

        var byLanguage = await db.Children
            .GroupBy(c => c.HomeLanguage)
            .ToDictionaryAsync(g => g.Key, g => g.Count(), ct);
        Assert.Equal(27, byLanguage["kn"]);
        Assert.Equal(6, byLanguage["te"]);
        Assert.Equal(3, byLanguage["ta"]);
    }

    [Fact]
    public async Task Seeding_again_adds_nothing()
    {
        await using var factory = new ApiFactory(postgres.ConnectionString);
        var ct = TestContext.Current.CancellationToken;

        await using var scope = factory.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<DemoSeeder>().SeedAsync(ct);
        await scope.ServiceProvider.GetRequiredService<DemoSeeder>().SeedAsync(ct);

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(1, await db.Teachers.CountAsync(ct));
        Assert.Equal(1, await db.Classes.CountAsync(ct));
        Assert.Equal(36, await db.Children.CountAsync(ct));
    }

    [Fact]
    public async Task Concurrent_seeding_into_an_empty_database_does_not_conflict()
    {
        // Two app instances starting together (e.g. a deployment slot swap) must not race on the seed.
        var ct = TestContext.Current.CancellationToken;
        var connectionString = await postgres.CreateEmptyDatabaseAsync(ct);
        await using var factory = new ApiFactory(connectionString, initialiseDatabase: false);

        await using (var migrateScope = factory.Services.CreateAsyncScope())
        {
            await migrateScope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync(ct);
        }

        await Task.WhenAll(Enumerable.Range(0, 4).Select(async _ =>
        {
            await using var scope = factory.Services.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<DemoSeeder>().SeedAsync(ct);
        }));

        await using var checkScope = factory.Services.CreateAsyncScope();
        var db = checkScope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(1, await db.Teachers.CountAsync(ct));
        Assert.Equal(36, await db.Children.CountAsync(ct));
    }
}
