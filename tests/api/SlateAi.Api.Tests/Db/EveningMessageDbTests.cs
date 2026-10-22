using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using SlateAi.Api.Features.Attendance;
using SlateAi.Api.Features.EveningMessages;
using SlateAi.Api.Features.Lessons;

namespace SlateAi.Api.Tests.Db;

[Trait("Category", "Db")]
[Collection(DbCollection.Name)]
public sealed class EveningMessageDbTests(PostgresFixture postgres)
{
    [Fact]
    public async Task Approved_template_and_confirmed_lesson_send_once_per_family_from_the_seed()
    {
        // Guardrail 3 on a real database: the migration, the seeded consent, the approval rule, and one message
        // per child per day even when two sends race (advisory lock + unique index). Demo:Enabled allows any hour.
        await using var api = await DemoClassApi.StartAsync(postgres);
        var today = SchoolClock.Today(TimeProvider.System);
        var ravi = api.ChildIds[3];   // roll 4, Telugu, consent yes
        var noForm = api.ChildIds[11]; // roll 12, consent no
        var send = $"/api/classes/{api.ClassId}/evening-messages/send";
        var template = $"/api/classes/{api.ClassId}/message-template";

        (await api.MarkAsync(Guid.NewGuid(), today, ravi, noForm)).EnsureSuccessStatusCode();
        (await api.Client.PutAsJsonAsync(
            $"/api/classes/{api.ClassId}/lessons/{today:yyyy-MM-dd}",
            new SaveLessonRequest(Guid.NewGuid(), ["M3.SUB.BORROW"]), DemoClassApi.Ct)).EnsureSuccessStatusCode();

        var unapproved = await api.Client.PostAsJsonAsync(send, new SendEveningRequest(Guid.NewGuid()), DemoClassApi.Ct);
        Assert.Equal(HttpStatusCode.Conflict, unapproved.StatusCode);

        var draft = (await api.Client.GetFromJsonAsync<MessageTemplateResponse>(template, DemoClassApi.Ct))!;
        (await api.Client.PostAsJsonAsync(
            $"{template}/approve", new ApproveTemplateRequest(Guid.NewGuid(), draft.Version), DemoClassApi.Ct)).EnsureSuccessStatusCode();

        var sends = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ =>
            api.Client.PostAsJsonAsync(send, new SendEveningRequest(Guid.NewGuid()), DemoClassApi.Ct)));
        Assert.All(sends, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));
        var day = (await sends[0].Content.ReadFromJsonAsync<EveningDayResponse>(DemoClassApi.Ct))!;
        Assert.Equal(EveningOutcome.Sent, day.Children.Single(c => c.ChildId == ravi).Outcome);
        Assert.Equal(EveningOutcome.NoConsent, day.Children.Single(c => c.ChildId == noForm).Outcome);

        var rows = await api.QueryAsync(db => db.EveningMessages.ToListAsync(DemoClassApi.Ct));
        var message = Assert.Single(rows);
        Assert.Equal(ravi, message.ChildId);
        Assert.Equal("te", message.Language);

        var parent = (await api.Client.GetFromJsonAsync<ParentEveningResponse>($"/api/p/{message.Token}", DemoClassApi.Ct))!;
        Assert.Contains("పది తీసుకుని తీసివేత", parent.Text);
        Assert.Contains("Ravi", parent.Text);
    }
}
