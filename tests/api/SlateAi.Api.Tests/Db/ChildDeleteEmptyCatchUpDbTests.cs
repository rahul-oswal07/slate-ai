using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using SlateAi.Api.Data.Entities;
using SlateAi.Api.Features.Attendance;
using SlateAi.Api.Features.CatchUps;
using SlateAi.Api.Features.Lessons;

namespace SlateAi.Api.Tests.Db;

[Trait("Category", "Db")]
[Collection(DbCollection.Name)]
public sealed class ChildDeleteEmptyCatchUpDbTests(PostgresFixture postgres)
{
    [Fact]
    public async Task Deleting_the_only_child_of_a_catch_up_removes_the_catch_up_and_its_audio()
    {
        // FR-15 (T-19): a catch-up made for one child is about that child; once they're deleted it must not stay on
        // Approve for nobody, with its audio. A catch-up shared with classmates stays.
        await using var api = await DemoClassApi.StartAsync(postgres);
        var today = SchoolClock.Today(TimeProvider.System);
        var ravi = api.ChildIds[3];    // roll 4, Telugu
        var saiTeja = api.ChildIds[9]; // roll 10, Telugu: shares Ravi's catch-up
        var rohan = api.ChildIds[23];  // roll 24, the only English family: a catch-up of his own
        (await api.MarkAsync(Guid.NewGuid(), today, ravi, saiTeja, rohan)).EnsureSuccessStatusCode();
        (await api.Client.PutAsJsonAsync($"/api/classes/{api.ClassId}/lessons/{today:yyyy-MM-dd}",
            new SaveLessonRequest(Guid.NewGuid(), ["M3.SUB.NOBORROW"]), DemoClassApi.Ct)).EnsureSuccessStatusCode();
        (await api.Client.PostAsJsonAsync($"/api/classes/{api.ClassId}/catchups/generate",
            new GenerateCatchUpsRequest(Guid.NewGuid()), DemoClassApi.Ct)).EnsureSuccessStatusCode();
        for (var i = 0; i < 50 && !await api.QueryAsync(db => db.CatchUpPackages.AllAsync(p => p.Status != PackageStatus.Generating, DemoClassApi.Ct)); i++)
        {
            await Task.Delay(100, DemoClassApi.Ct);
        }

        Task<Guid> PackageOf(Guid child) => api.QueryAsync(db =>
            db.CatchUpAssignments.Where(a => a.ChildId == child).Select(a => a.PackageId).SingleAsync(DemoClassApi.Ct));
        var rohans = await PackageOf(rohan);
        var shared = await PackageOf(ravi);
        Assert.Equal(shared, await PackageOf(saiTeja));
        await api.QueryAsync(db =>
        {
            db.AudioClips.Add(Clip(rohans));
            db.AudioClips.Add(Clip(shared));
            return db.SaveChangesAsync(DemoClassApi.Ct);
        });

        var deleteRohan = $"/api/classes/{api.ClassId}/children/{rohan}?clientRequestId={Guid.NewGuid()}";
        var deleteRavi = $"/api/classes/{api.ClassId}/children/{ravi}?clientRequestId={Guid.NewGuid()}";
        Assert.Equal(HttpStatusCode.NoContent, (await api.Client.DeleteAsync(deleteRohan, DemoClassApi.Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await api.Client.DeleteAsync(deleteRavi, DemoClassApi.Ct)).StatusCode);

        Assert.False(await api.QueryAsync(db => db.CatchUpPackages.AnyAsync(p => p.Id == rohans, DemoClassApi.Ct)));
        Assert.False(await api.QueryAsync(db => db.AudioClips.AnyAsync(c => c.PackageId == rohans, DemoClassApi.Ct)));
        Assert.True(await api.QueryAsync(db => db.CatchUpPackages.AnyAsync(p => p.Id == shared, DemoClassApi.Ct)));
        Assert.True(await api.QueryAsync(db => db.AudioClips.AnyAsync(c => c.PackageId == shared, DemoClassApi.Ct)));
    }

    private static AudioClip Clip(Guid packageId) => new()
    {
        Id = Guid.NewGuid(), Kind = "script", Language = "te", PackageId = packageId,
        ContentType = "audio/ogg", Bytes = [1, 2, 3], Seconds = 1, CreatedAt = DateTimeOffset.UtcNow,
    };
}
