using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using SlateAi.Api.Features.Attendance;
using SlateAi.Api.Features.CatchUps;
using SlateAi.Api.Features.Lessons;

namespace SlateAi.Api.Tests.Db;

[Trait("Category", "Db")]
[Collection(DbCollection.Name)]
public sealed class CatchUpDbTests(PostgresFixture postgres)
{
    [Fact]
    public async Task Generate_stores_one_catch_up_per_targets_and_language_and_a_rerun_adds_nothing()
    {
        // Real Postgres: the migration, text[] and jsonb round trips, and the unique keys under two generates at once.
        // AI is the fake provider here (A2 takes the first candidate; A3's fake words fail validation → "failed").
        await using var api = await DemoClassApi.StartAsync(postgres);
        var today = SchoolClock.Today(TimeProvider.System);
        var ravi = api.ChildIds[3]; // roll 4, Telugu
        (await api.MarkAsync(Guid.NewGuid(), today, ravi)).EnsureSuccessStatusCode();
        (await api.Client.PutAsJsonAsync(
            $"/api/classes/{api.ClassId}/lessons/{today:yyyy-MM-dd}",
            new SaveLessonRequest(Guid.NewGuid(), ["M3.SUB.NOBORROW"]), DemoClassApi.Ct)).EnsureSuccessStatusCode();
        var generate = $"/api/classes/{api.ClassId}/catchups/generate";

        var responses = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ =>
            api.Client.PostAsJsonAsync(generate, new GenerateCatchUpsRequest(Guid.NewGuid()), DemoClassApi.Ct)));
        Assert.All(responses, r => Assert.Equal(HttpStatusCode.Accepted, r.StatusCode));
        (await api.Client.PostAsJsonAsync(generate, new GenerateCatchUpsRequest(Guid.NewGuid()), DemoClassApi.Ct)).EnsureSuccessStatusCode();

        var packages = await api.QueryAsync(db => db.CatchUpPackages.ToListAsync(DemoClassApi.Ct));
        var package = Assert.Single(packages);
        Assert.Equal("te", package.Language);
        Assert.Equal(["M3.SUB.NOBORROW"], package.TargetCompetencyIds);
        var assignment = Assert.Single(await api.QueryAsync(db => db.CatchUpAssignments.ToListAsync(DemoClassApi.Ct)));
        Assert.Equal(ravi, assignment.ChildId);

        CatchUpWeekResponse week;
        var tries = 0;
        do
        {
            await Task.Delay(100, DemoClassApi.Ct);
            week = (await api.Client.GetFromJsonAsync<CatchUpWeekResponse>($"/api/classes/{api.ClassId}/packages", DemoClassApi.Ct))!;
        }
        while (week.Packages[0].Status == PackageStatus.Generating && ++tries < 50);

        var view = Assert.Single(week.Packages);
        Assert.NotEqual(PackageStatus.Generating, view.Status);
        Assert.Equal(view.Check.Answer, view.Check.Options[view.Check.CorrectIndex]);
        Assert.Equal(["M3.SUB.NOBORROW"], Assert.Single(view.Children).Rationale.Targets.Select(t => t.Id));
    }
}
