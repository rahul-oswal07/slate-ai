using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using SlateAi.Api.Data;
using SlateAi.Api.Data.Entities;
using SlateAi.Api.Features.Auth;
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
            Assert.Equal(3, await db.Teachers.CountAsync(ct));
            Assert.Equal(3, await db.ClassTeachers.CountAsync(ct));
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

    [Fact]
    public async Task Co_teachers_are_seeded_with_their_own_login_and_reach_the_class_but_a_stranger_does_not()
    {
        // Monika and Savitha share Shobha's class (many teachers per class); Class.TeacherId stays Shobha.
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new ApiFactory(
            await postgres.CreateEmptyDatabaseAsync(ct),
            settings: new Dictionary<string, string?> { ["Demo:TeacherPin"] = "2468", ["Demo:CoTeacherPin"] = "7755" });
        await using var scope = factory.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<DemoSeeder>().SeedAsync(ct);
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthStore>();

        var classId = await db.Classes.Select(c => c.Id).SingleAsync(ct);
        var shobha = await db.Classes.Select(c => c.TeacherId).SingleAsync(ct);
        var coTeachers = await db.Teachers.Where(t => t.Id != shobha).OrderBy(t => t.DisplayName).ToListAsync(ct);

        Assert.Equal(["Monika", "Savitha"], coTeachers.Select(t => t.DisplayName));
        Assert.Equal(["9123456788", "9123456789"], coTeachers.Select(t => t.Phone));
        Assert.All(coTeachers, t => Assert.NotNull(t.PinHash));
        foreach (var teacher in coTeachers.Append(await db.Teachers.SingleAsync(t => t.Id == shobha, ct)))
        {
            Assert.Equal(classId, (await auth.GetMeAsync(teacher.Id, ct))!.ClassId);
        }

        // A database seeded before the rename (T-72) still says "Lakshmi"; the next start brings the seed's name.
        var savitha = coTeachers.Single(t => t.DisplayName == "Savitha").Id;
        await db.Teachers.Where(t => t.Id == savitha).ExecuteUpdateAsync(s => s.SetProperty(t => t.DisplayName, "Lakshmi"), ct);
        await using (var restart = factory.Services.CreateAsyncScope())
        {
            await restart.ServiceProvider.GetRequiredService<DemoSeeder>().SeedAsync(ct);
        }

        Assert.Equal("Savitha", await db.Teachers.AsNoTracking().Where(t => t.Id == savitha).Select(t => t.DisplayName).SingleAsync(ct));

        var stranger = new Teacher { Id = Guid.NewGuid(), DisplayName = "Stranger" };
        db.Teachers.Add(stranger);
        await db.SaveChangesAsync(ct);
        Assert.Null((await auth.GetMeAsync(stranger.Id, ct))!.ClassId);
    }

    [Fact]
    public async Task Migration_gives_an_existing_class_its_main_teacher_as_a_member()
    {
        // Staging and prod already have a class: its teacher must still reach it after ClassTeachers arrives.
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new ApiFactory(await postgres.CreateEmptyDatabaseAsync(ct), initialiseDatabase: false);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var migrator = db.GetService<IMigrator>();

        await migrator.MigrateAsync("20261023082251_PacingDecisions", ct);
        var teacherId = Guid.NewGuid();
        var classId = Guid.NewGuid();
        await db.Database.ExecuteSqlAsync(
            $"""INSERT INTO "Teachers" ("Id", "DisplayName", "FailedPinAttempts") VALUES ({teacherId}, 'Old', 0)""", ct);
        await db.Database.ExecuteSqlAsync(
            $"""INSERT INTO "Classes" ("Id", "TeacherId", "Grade", "Medium") VALUES ({classId}, {teacherId}, 3, 'kn')""", ct);

        await migrator.MigrateAsync(null, ct);

        var member = await db.ClassTeachers.SingleAsync(ct);
        Assert.Equal((classId, teacherId), (member.ClassId, member.TeacherId));
    }
}
