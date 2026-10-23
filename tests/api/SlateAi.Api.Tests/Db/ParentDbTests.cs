using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using SlateAi.Api.Features.Attendance;
using SlateAi.Api.Features.CatchUps;
using SlateAi.Api.Features.EveningMessages;
using SlateAi.Api.Features.Ledger;
using SlateAi.Api.Features.Lessons;
using SlateAi.Api.Features.Parent;

namespace SlateAi.Api.Tests.Db;

[Trait("Category", "Db")]
[Collection(DbCollection.Name)]
public sealed class ParentDbTests(PostgresFixture postgres)
{
    [Fact]
    public async Task Family_opens_answers_once_and_a_stop_blocks_every_send()
    {
        // On a real database: the token lookup's joins, the first answer winning a race (advisory lock + primary key),
        // the ledger reading the answer, FR-19 (after a stop the evening send refuses the child) and FR-21 (one reason
        // per child per day, the latest tap wins, a replay after it changes nothing).
        await using var api = await DemoClassApi.StartAsync(postgres);
        var today = SchoolClock.Today(TimeProvider.System);
        var ravi = api.ChildIds[3]; // roll 4, Telugu, consent yes
        var saiTeja = api.ChildIds[9]; // roll 10, Telugu, consent yes
        (await api.MarkAsync(Guid.NewGuid(), today, ravi, saiTeja)).EnsureSuccessStatusCode();
        (await api.Client.PutAsJsonAsync($"/api/classes/{api.ClassId}/lessons/{today:yyyy-MM-dd}",
            new SaveLessonRequest(Guid.NewGuid(), ["M3.SUB.NOBORROW"]), DemoClassApi.Ct)).EnsureSuccessStatusCode();
        (await api.Client.PostAsJsonAsync($"/api/classes/{api.ClassId}/catchups/generate",
            new GenerateCatchUpsRequest(Guid.NewGuid()), DemoClassApi.Ct)).EnsureSuccessStatusCode();
        await WaitAsync(api, () => api.QueryAsync(db => db.CatchUpPackages.AllAsync(p => p.Status != PackageStatus.Generating, DemoClassApi.Ct)));

        await api.QueryAsync(db => db.CatchUpPackages.ExecuteUpdateAsync(
            s => s.SetProperty(p => p.Status, PackageStatus.Ready).SetProperty(p => p.ScriptVersion, 1), DemoClassApi.Ct));
        var packageId = await api.QueryAsync(db => db.CatchUpAssignments.Where(a => a.ChildId == ravi).Select(a => a.PackageId).SingleAsync(DemoClassApi.Ct));
        (await api.Client.PostAsJsonAsync($"/api/packages/{packageId}/approve", new ApprovePackageRequest(Guid.NewGuid(), 1), DemoClassApi.Ct)).EnsureSuccessStatusCode();
        (await api.Client.PostAsJsonAsync($"/api/packages/{packageId}/send", new SendPackageRequest(Guid.NewGuid()), DemoClassApi.Ct)).EnsureSuccessStatusCode();
        var token = await api.QueryAsync(db => db.CatchUpAssignments.Where(a => a.ChildId == ravi).Select(a => a.Token).SingleAsync(DemoClassApi.Ct));
        var url = $"/api/p/{token}";

        var page = (await api.Client.GetFromJsonAsync<ParentCatchUpResponse>(url, DemoClassApi.Ct))!;
        Assert.Equal("Ravi", page.ChildName);
        Assert.Equal("te", page.Language);
        Assert.False(page.Answered);
        Assert.Equal(3, page.Check!.Options.Count);

        var answers = await Task.WhenAll(Enumerable.Range(0, 3).Select(choice =>
            api.Client.PostAsJsonAsync($"{url}/answer", new AnswerRequest(choice, Guid.NewGuid()), DemoClassApi.Ct)));
        Assert.All(answers, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));
        var stored = Assert.Single(await api.QueryAsync(db => db.CheckResponses.ToListAsync(DemoClassApi.Ct)));
        Assert.True((await api.Client.GetFromJsonAsync<ParentCatchUpResponse>(url, DemoClassApi.Ct))!.Answered);

        var ledger = (await api.Client.GetFromJsonAsync<LedgerResponse>($"/api/classes/{api.ClassId}/ledger", DemoClassApi.Ct))!;
        Assert.Equal(stored.Outcome, Assert.Single(ledger.Children.Single(c => c.ChildId == ravi).MissedLessons).Status);

        (await api.Client.PostAsJsonAsync($"{url}/stop", new StopRequest(Guid.NewGuid()), DemoClassApi.Ct)).EnsureSuccessStatusCode();
        Assert.True((await api.Client.GetFromJsonAsync<ParentCatchUpResponse>(url, DemoClassApi.Ct))!.Stopped);
        Assert.Equal(HttpStatusCode.Conflict,
            (await api.Client.PostAsJsonAsync($"{url}/answer", new AnswerRequest(0, Guid.NewGuid()), DemoClassApi.Ct)).StatusCode);

        var template = $"/api/classes/{api.ClassId}/message-template";
        var draft = (await api.Client.GetFromJsonAsync<MessageTemplateResponse>(template, DemoClassApi.Ct))!;
        (await api.Client.PostAsJsonAsync($"{template}/approve", new ApproveTemplateRequest(Guid.NewGuid(), draft.Version), DemoClassApi.Ct)).EnsureSuccessStatusCode();
        var evening = await api.Client.PostAsJsonAsync(
            $"/api/classes/{api.ClassId}/evening-messages/send", new SendEveningRequest(Guid.NewGuid()), DemoClassApi.Ct);
        var day = (await evening.Content.ReadFromJsonAsync<EveningDayResponse>(DemoClassApi.Ct))!;
        Assert.Equal(EveningOutcome.Stopped, day.Children.Single(c => c.ChildId == ravi).Outcome);
        Assert.False(await api.QueryAsync(db => db.EveningMessages.AnyAsync(m => m.ChildId == ravi, DemoClassApi.Ct)));

        var eveningToken = await api.QueryAsync(db => db.EveningMessages.Where(m => m.ChildId == saiTeja).Select(m => m.Token).SingleAsync(DemoClassApi.Ct));
        var unwell = new ReasonRequest(ReasonCategory.Unwell, Guid.NewGuid());
        (await api.Client.PostAsJsonAsync($"/api/p/{eveningToken}/reason", unwell, DemoClassApi.Ct)).EnsureSuccessStatusCode();
        (await api.Client.PostAsJsonAsync($"/api/p/{eveningToken}/reason", new ReasonRequest(ReasonCategory.Festival, Guid.NewGuid()), DemoClassApi.Ct)).EnsureSuccessStatusCode();
        (await api.Client.PostAsJsonAsync($"/api/p/{eveningToken}/reason", unwell, DemoClassApi.Ct)).EnsureSuccessStatusCode();
        var reason = Assert.Single(await api.QueryAsync(db => db.AbsenceReasons.ToListAsync(DemoClassApi.Ct)));
        Assert.Equal((saiTeja, today, ReasonCategory.Festival), (reason.ChildId, reason.Date, reason.Category));
        Assert.Equal(ReasonCategory.Festival, (await api.Client.GetFromJsonAsync<ParentEveningResponse>($"/api/p/{eveningToken}", DemoClassApi.Ct))!.Reason);

        (await api.Client.PostAsJsonAsync($"{url}/restart", new StopRequest(Guid.NewGuid()), DemoClassApi.Ct)).EnsureSuccessStatusCode();
        Assert.Null(await api.QueryAsync(db => db.Children.Where(c => c.Id == ravi).Select(c => c.ParentStoppedAt).SingleAsync(DemoClassApi.Ct)));
    }

    private static async Task WaitAsync(DemoClassApi api, Func<Task<bool>> done)
    {
        for (var i = 0; i < 50 && !await done(); i++)
        {
            await Task.Delay(100, DemoClassApi.Ct);
        }
    }
}
