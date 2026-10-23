using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using SlateAi.Api.Data.Seed;
using SlateAi.Api.Features.Attendance;
using SlateAi.Api.Features.CatchUps;
using SlateAi.Api.Features.EveningMessages;
using SlateAi.Api.Features.Lessons;
using SlateAi.Api.Features.Parent;

namespace SlateAi.Api.Tests.Db;

[Trait("Category", "Db")]
[Collection(DbCollection.Name)]
public sealed class ChildDeleteDbTests(PostgresFixture postgres)
{
    [Fact]
    public async Task Deleting_a_child_removes_every_row_about_them_and_the_seed_does_not_bring_them_back()
    {
        // FR-15 on a real database (CLAUDE.md lists child deletion for this suite): the cascades, the other children
        // untouched, and the seed adding children only with a new class.
        await using var api = await DemoClassApi.StartAsync(postgres);
        var today = SchoolClock.Today(TimeProvider.System);
        var ravi = api.ChildIds[3];    // roll 4, Telugu, consent yes
        var saiTeja = api.ChildIds[9]; // roll 10, Telugu, consent yes
        (await api.MarkAsync(Guid.NewGuid(), today, ravi, saiTeja)).EnsureSuccessStatusCode();
        (await api.Client.PutAsJsonAsync($"/api/classes/{api.ClassId}/lessons/{today:yyyy-MM-dd}",
            new SaveLessonRequest(Guid.NewGuid(), ["M3.SUB.NOBORROW"]), DemoClassApi.Ct)).EnsureSuccessStatusCode();

        // An evening message and a reason.
        var template = $"/api/classes/{api.ClassId}/message-template";
        var draft = (await api.Client.GetFromJsonAsync<MessageTemplateResponse>(template, DemoClassApi.Ct))!;
        (await api.Client.PostAsJsonAsync($"{template}/approve", new ApproveTemplateRequest(Guid.NewGuid(), draft.Version), DemoClassApi.Ct)).EnsureSuccessStatusCode();
        (await api.Client.PostAsJsonAsync($"/api/classes/{api.ClassId}/evening-messages/send", new SendEveningRequest(Guid.NewGuid()), DemoClassApi.Ct)).EnsureSuccessStatusCode();
        var evening = await api.QueryAsync(db => db.EveningMessages.Where(m => m.ChildId == ravi).Select(m => m.Token).SingleAsync(DemoClassApi.Ct));
        (await api.Client.PostAsJsonAsync($"/api/p/{evening}/reason", new ReasonRequest(ReasonCategory.Unwell, Guid.NewGuid()), DemoClassApi.Ct)).EnsureSuccessStatusCode();

        // A catch-up link and an answer.
        (await api.Client.PostAsJsonAsync($"/api/classes/{api.ClassId}/catchups/generate", new GenerateCatchUpsRequest(Guid.NewGuid()), DemoClassApi.Ct)).EnsureSuccessStatusCode();
        for (var i = 0; i < 50 && !await api.QueryAsync(db => db.CatchUpPackages.AllAsync(p => p.Status != PackageStatus.Generating, DemoClassApi.Ct)); i++)
        {
            await Task.Delay(100, DemoClassApi.Ct);
        }

        await api.QueryAsync(db => db.CatchUpPackages.ExecuteUpdateAsync(
            s => s.SetProperty(p => p.Status, PackageStatus.Ready).SetProperty(p => p.ScriptVersion, 1), DemoClassApi.Ct));
        var packageId = await api.QueryAsync(db => db.CatchUpAssignments.Where(a => a.ChildId == ravi).Select(a => a.PackageId).SingleAsync(DemoClassApi.Ct));
        (await api.Client.PostAsJsonAsync($"/api/packages/{packageId}/approve", new ApprovePackageRequest(Guid.NewGuid(), 1), DemoClassApi.Ct)).EnsureSuccessStatusCode();
        (await api.Client.PostAsJsonAsync($"/api/packages/{packageId}/send", new SendPackageRequest(Guid.NewGuid()), DemoClassApi.Ct)).EnsureSuccessStatusCode();
        var token = await api.QueryAsync(db => db.CatchUpAssignments.Where(a => a.ChildId == ravi).Select(a => a.Token).SingleAsync(DemoClassApi.Ct));
        (await api.Client.PostAsJsonAsync($"/api/p/{token}/answer", new AnswerRequest(0, Guid.NewGuid()), DemoClassApi.Ct)).EnsureSuccessStatusCode();

        Task<int[]> RowsFor(Guid child) => api.QueryAsync(async db => new[]
        {
            await db.Children.CountAsync(c => c.Id == child, DemoClassApi.Ct),
            await db.Attendance.CountAsync(a => a.ChildId == child, DemoClassApi.Ct),
            await db.EveningMessages.CountAsync(m => m.ChildId == child, DemoClassApi.Ct),
            await db.CatchUpAssignments.CountAsync(a => a.ChildId == child, DemoClassApi.Ct),
            await db.CheckResponses.CountAsync(r => db.CatchUpAssignments.Any(a => a.Id == r.AssignmentId && a.ChildId == child), DemoClassApi.Ct),
            await db.AbsenceReasons.CountAsync(r => r.ChildId == child, DemoClassApi.Ct),
        });
        Assert.All(await RowsFor(ravi), n => Assert.True(n > 0));
        var answersBefore = await api.QueryAsync(db => db.CheckResponses.CountAsync(DemoClassApi.Ct));

        var url = $"/api/classes/{api.ClassId}/children/{ravi}?clientRequestId={Guid.NewGuid()}";
        Assert.Equal(HttpStatusCode.NoContent, (await api.Client.DeleteAsync(url, DemoClassApi.Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await api.Client.DeleteAsync(url, DemoClassApi.Ct)).StatusCode);

        Assert.All(await RowsFor(ravi), n => Assert.Equal(0, n));
        Assert.Equal(answersBefore - 1, await api.QueryAsync(db => db.CheckResponses.CountAsync(DemoClassApi.Ct)));
        Assert.Equal(35, await api.QueryAsync(db => db.Children.CountAsync(DemoClassApi.Ct)));
        Assert.Equal(1, await api.QueryAsync(db => db.EveningMessages.CountAsync(m => m.ChildId == saiTeja, DemoClassApi.Ct)));
        Assert.Equal(HttpStatusCode.NotFound, (await api.Client.GetAsync($"/api/p/{token}", DemoClassApi.Ct)).StatusCode);

        // The next start (or deploy) runs the seed again: the deleted child stays deleted.
        await api.ScopedAsync<DemoSeeder>(seeder => seeder.SeedAsync(DemoClassApi.Ct));
        Assert.Equal(0, await api.QueryAsync(db => db.Children.CountAsync(c => c.Id == ravi, DemoClassApi.Ct)));
        Assert.Equal(35, await api.QueryAsync(db => db.Children.CountAsync(DemoClassApi.Ct)));
    }
}
