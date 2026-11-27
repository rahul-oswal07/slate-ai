using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using SlateAi.Api.Curriculum;
using SlateAi.Api.Data;
using SlateAi.Api.Data.Entities;
using SlateAi.Api.Features.Auth;
using SlateAi.Api.Data.Seed;
using SlateAi.Api.Features.Demo;
using SlateAi.Api.Features.LessonContents;
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
            // The demo class 3A (Shobha, Savitha, Monika) and Monika's own 3B (T-115).
            Assert.Equal(2, await db.Classes.CountAsync(ct));
            Assert.Equal(4, await db.ClassTeachers.CountAsync(ct));
            Assert.Equal(36 + 6, await db.Children.CountAsync(ct));
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
    public async Task The_school_date_switch_starts_off_and_keeps_what_the_admin_page_saves()
    {
        // T-134: a new table (AppSettings); a missing row is off, and a replayed request changes nothing.
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new ApiFactory(await postgres.CreateEmptyDatabaseAsync(ct));
        var at = new DateTimeOffset(2026, 11, 16, 5, 0, 0, TimeSpan.Zero);
        var request = Guid.NewGuid();

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var store = scope.ServiceProvider.GetRequiredService<ISchoolDateSwitchStore>();
            Assert.Equal(new SchoolDateSetting(false, null), await store.GetAsync(ct));
            Assert.True((await store.SetAsync(request, true, at, ct)).On);
            Assert.True((await store.SetAsync(request, false, at.AddMinutes(1), ct)).On);
        }

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var store = scope.ServiceProvider.GetRequiredService<ISchoolDateSwitchStore>();
            Assert.Equal(new SchoolDateSetting(true, at), await store.GetAsync(ct));
            Assert.Equal(new SchoolDateSetting(false, at.AddMinutes(2)), await store.SetAsync(Guid.NewGuid(), false, at.AddMinutes(2), ct));
        }
    }

    [Fact]
    public async Task Lesson_content_loads_at_startup_follows_the_file_and_never_overwrites_an_admin_edit()
    {
        // T-141, D-123: a new table (LessonContents) filled from data/lesson-content at startup; text[] examples.
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new ApiFactory(await postgres.CreateEmptyDatabaseAsync(ct));
        var at = new DateTimeOffset(2026, 11, 16, 5, 0, 0, TimeSpan.Zero);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var store = scope.ServiceProvider.GetRequiredService<ILessonContentStore>();
            var all = await store.GetAllAsync(BuiltInCurriculum.Id, ct);
            Assert.Equal(61, all.Count);
            Assert.Equal(LessonContentSources.Written, all["M3.BRIDGE.COMPARE"].Source);
            Assert.NotEmpty(all["M3.ADD.CARRY"].Examples);

            // An admin edit, replayed: one write.
            var request = Guid.NewGuid();
            var edit = new LessonContentText("Hers.", ["2 + 3 = 5", "4 + 4 = 8"]);
            Assert.Equal(LessonContentSources.Edited, (await store.SetAsync(request, BuiltInCurriculum.Id, "M3.ADD.CARRY", edit, at, ct))!.Source);
            Assert.Equal("Hers.", (await store.SetAsync(request, BuiltInCurriculum.Id, "M3.ADD.CARRY", null, at, ct))!.Description);
        }

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            // The file changed for two lessons: the unedited one follows it, the edited one is kept.
            var store = scope.ServiceProvider.GetRequiredService<ILessonContentStore>();
            var changed = new LessonContentText("Changed in the file.", ["1 + 1 = 2"]);
            var result = await store.SyncAsync(
                [new("M3.ADD.CARRY", changed, LessonContentSources.Textbook), new("M3.SUB.BORROW", changed, LessonContentSources.Textbook)],
                at.AddDays(1), ct);
            Assert.Equal(new LessonContentSyncResult(0, 1, 1), result);

            await scope.ServiceProvider.GetRequiredService<LessonContentLoader>().LoadAsync(ct);
            var two = await store.GetForAsync(BuiltInCurriculum.Id, ["M3.SUB.BORROW", "M3.ADD.CARRY", "M3.NOPE"], ct);
            Assert.Equal(2, two.Count);
            Assert.Equal(["2 + 3 = 5", "4 + 4 = 8"], two["M3.ADD.CARRY"].Examples);
            Assert.Equal(LessonContentSources.Textbook, two["M3.SUB.BORROW"].Source);
            Assert.NotEqual("Changed in the file.", two["M3.SUB.BORROW"].Description); // the startup load put the file back
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
        var demoClass = (await DemoSeedFile.LoadAsync("data/seed/demo-class.json", ct)).Class.Id;
        Assert.Equal(36, await db.Children.CountAsync(c => c.ClassId == demoClass, ct));
        Assert.Equal(6, await db.Children.CountAsync(c => c.ClassId != demoClass, ct));
        var byLanguage = await db.Children.Where(c => c.ClassId == demoClass).GroupBy(c => c.HomeLanguage).ToDictionaryAsync(g => g.Key, g => g.Count(), ct);
        // Rohan (24) and Pooja (20) are the Hindi families, Sanjay (27) and Shreya (28) the English ones (T-18, T-131).
        Assert.Equal(
            (23, 6, 3, 2, 2), (byLanguage["kn"], byLanguage["te"], byLanguage["ta"], byLanguage["hi"], byLanguage["en"]));
    }

    [Fact]
    public async Task Co_teachers_are_seeded_with_their_own_login_and_reach_their_classes_but_a_stranger_does_not()
    {
        // Monika and Savitha share Shobha's class 3A (many teachers per class); Class.TeacherId stays Shobha. Monika is
        // also the main teacher of 3B (T-115), so 3B is her default and she sees two classes.
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new ApiFactory(
            await postgres.CreateEmptyDatabaseAsync(ct),
            settings: new Dictionary<string, string?> { ["Demo:TeacherPin"] = "2468", ["Demo:CoTeacherPin"] = "7755" });
        await using var scope = factory.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<DemoSeeder>().SeedAsync(ct);
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthStore>();

        var seed = await DemoSeedFile.LoadAsync("data/seed/demo-class.json", ct);
        var classA = seed.Class.Id;
        var shobha = await db.Classes.Where(c => c.Id == classA).Select(c => c.TeacherId).SingleAsync(ct);
        var coTeachers = await db.Teachers.Where(t => t.Id != shobha).OrderBy(t => t.DisplayName).ToListAsync(ct);

        Assert.Equal(["Monika", "Savitha"], coTeachers.Select(t => t.DisplayName));
        Assert.Equal(["9123456788", "9123456789"], coTeachers.Select(t => t.Phone));
        Assert.All(coTeachers, t => Assert.NotNull(t.PinHash));
        var monika = coTeachers.Single(t => t.DisplayName == "Monika").Id;
        var classB = await db.Classes.Where(c => c.TeacherId == monika).Select(c => c.Id).SingleAsync(ct);
        foreach (var teacher in coTeachers.Append(await db.Teachers.SingleAsync(t => t.Id == shobha, ct)))
        {
            var me = (await auth.GetMeAsync(teacher.Id, ct))!;
            if (teacher.Id == monika)
            {
                Assert.Equal(classB, me.ClassId);
                Assert.Equal([(classB, "3B", "Monika"), (classA, "3A", "Shobha")], me.Classes!.Select(c => (c.Id, c.Name, c.ClassTeacherName)));
                Assert.Equal("Monika", me.ClassTeacherName);
            }
            else
            {
                Assert.Equal(classA, me.ClassId);
                Assert.Equal([(classA, "3A")], me.Classes!.Select(c => (c.Id, c.Name)));
                // T-135: co-teachers too see that it is Shobha's class.
                Assert.Equal("Shobha", me.ClassTeacherName);
            }
        }

        // A database seeded before the rename (T-72) still says "Lakshmi"; the next start brings the seed's name. A class
        // seeded before names (T-115) gets the file's name once; a name set since is kept.
        var savitha = coTeachers.Single(t => t.DisplayName == "Savitha").Id;
        await db.Teachers.Where(t => t.Id == savitha).ExecuteUpdateAsync(s => s.SetProperty(t => t.DisplayName, "Lakshmi"), ct);
        await db.Classes.Where(c => c.Id == classA).ExecuteUpdateAsync(s => s.SetProperty(c => c.Name, ""), ct);
        await db.Classes.Where(c => c.Id == classB).ExecuteUpdateAsync(s => s.SetProperty(c => c.Name, "3 B"), ct);
        await using (var restart = factory.Services.CreateAsyncScope())
        {
            await restart.ServiceProvider.GetRequiredService<DemoSeeder>().SeedAsync(ct);
        }

        Assert.Equal("Savitha", await db.Teachers.AsNoTracking().Where(t => t.Id == savitha).Select(t => t.DisplayName).SingleAsync(ct));
        Assert.Equal("3A", await db.Classes.AsNoTracking().Where(c => c.Id == classA).Select(c => c.Name).SingleAsync(ct));
        Assert.Equal("3 B", await db.Classes.AsNoTracking().Where(c => c.Id == classB).Select(c => c.Name).SingleAsync(ct));

        var stranger = new Teacher { Id = Guid.NewGuid(), DisplayName = "Stranger" };
        db.Teachers.Add(stranger);
        await db.SaveChangesAsync(ct);
        var nobody = (await auth.GetMeAsync(stranger.Id, ct))!;
        Assert.Null(nobody.ClassId);
        Assert.Empty(nobody.Classes!);
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
        // T-115: an existing class has no name until the seed (for the demo class) or someone gives it one.
        Assert.Equal("", await db.Classes.Select(c => c.Name).SingleAsync(ct));
    }
}
