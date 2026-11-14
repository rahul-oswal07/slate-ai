using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using SlateAi.Api.Data.Entities;
using SlateAi.Api.Features.Attendance;
using SlateAi.Api.Features.EveningMessages;
using SlateAi.Api.Features.Lessons;
using SlateAi.Api.Features.TodaysMessage;

namespace SlateAi.Api.Tests.Db;

[Trait("Category", "Db")]
[Collection(DbCollection.Name)]
public sealed class TodaysMessageDbTests(PostgresFixture postgres)
{
    [Fact]
    public async Task Only_an_approved_current_version_goes_with_the_evening_message_and_removed_sections_never_reach_the_family()
    {
        // Guardrail 3 for today's message (D-91, T-88) on a real database: the migration, the version-bound approval, an edit
        // clearing it, the evening send attaching only the approved version, the parent query, and read only once sent.
        await using var api = await DemoClassApi.StartAsync(postgres);
        var today = SchoolClock.Today(TimeProvider.System);
        var ravi = api.ChildIds[3];       // roll 4, Telugu, consent yes
        var manjunath = api.ChildIds[14]; // roll 15, Kannada, consent yes
        (await api.MarkAsync(Guid.NewGuid(), today, ravi, manjunath)).EnsureSuccessStatusCode();
        await ConfirmLessonAsync(api, today);
        await ApproveTemplateAsync(api);
        var clips = await ClipsAsync(api, "kn", "te");

        var id = await api.ScopedAsync<ITodaysMessageStore, Guid?>(s => s.StartAsync(api.ClassId, today, ["M3.BRIDGE.COMPARE"], DateTimeOffset.UtcNow, DemoClassApi.Ct));
        await api.ScopedAsync<ITodaysMessageStore>(s => s.SetReadyAsync(id!.Value, Sections(clips), "valid", DateTimeOffset.UtcNow, DemoClassApi.Ct));

        // Approve version 1, then remove the activity: version 2, approval gone, so nothing would attach.
        Assert.Equal(TodaysChange.Applied, await Approve(api, today, 1));
        Assert.Equal(TodaysChange.Conflict, await Approve(api, today, 1));
        Assert.Equal(TodaysChange.Applied, await api.ScopedAsync<ITodaysMessageStore, TodaysChange>(
            s => s.SetRemovedAsync(api.ClassId, today, Guid.NewGuid(), TodaysSectionKind.Activity, true, DateTimeOffset.UtcNow, DemoClassApi.Ct)));
        Assert.Null(await api.ScopedAsync<IEveningStore, (Guid, IReadOnlySet<string>)?>(e => e.GetApprovedTodaysMessageAsync(api.ClassId, today, DemoClassApi.Ct)));
        Assert.Equal(TodaysChange.Conflict, await Approve(api, today, 1));
        Assert.Equal(TodaysChange.Applied, await Approve(api, today, 2));

        (await api.Client.PostAsJsonAsync(
            $"/api/classes/{api.ClassId}/evening-messages/send", new SendEveningRequest(Guid.NewGuid()), DemoClassApi.Ct)).EnsureSuccessStatusCode();

        var messages = await api.QueryAsync(db => db.EveningMessages.ToListAsync(DemoClassApi.Ct));
        Assert.Equal(2, messages.Count);
        Assert.All(messages, m => Assert.Equal(id, m.TodaysMessageId));

        // The Kannada family gets the Kannada concept and its clip, the picture by code, and no activity.
        var page = (await api.Client.GetFromJsonAsync<ParentEveningResponse>(
            $"/api/p/{messages.Single(m => m.ChildId == manjunath).Token}", DemoClassApi.Ct))!;
        Assert.Equal("ಮೊದಲು ಹತ್ತರ ಕಟ್ಟುಗಳನ್ನು ನೋಡಿ.", page.TodaysMessage!.Concept);
        Assert.Null(page.TodaysMessage.Steps);
        Assert.Equal((48, 33), (page.TodaysMessage.Picture!.A, page.TodaysMessage.Picture.B));
        var raw = await api.Client.GetStringAsync($"/api/p/{messages.Single(m => m.ChildId == manjunath).Token}", DemoClassApi.Ct);
        Assert.DoesNotContain("\"answer\"", raw); // scored on the server only (T-90)

        // The child's taps (T-90): wrong first, then right; stored on the message; a third is refused.
        var picture = $"/api/p/{messages.Single(m => m.ChildId == manjunath).Token}/picture";
        var first = await (await api.Client.PostAsJsonAsync(picture, new SlateAi.Api.Features.Parent.PictureAnswerRequest(Guid.NewGuid(), "<"), DemoClassApi.Ct))
            .Content.ReadFromJsonAsync<SlateAi.Api.Features.Parent.PictureAnswerResponse>(DemoClassApi.Ct);
        var second = await (await api.Client.PostAsJsonAsync(picture, new SlateAi.Api.Features.Parent.PictureAnswerRequest(Guid.NewGuid(), ">"), DemoClassApi.Ct))
            .Content.ReadFromJsonAsync<SlateAi.Api.Features.Parent.PictureAnswerResponse>(DemoClassApi.Ct);
        var third = await api.Client.PostAsJsonAsync(picture, new SlateAi.Api.Features.Parent.PictureAnswerRequest(Guid.NewGuid(), "="), DemoClassApi.Ct);
        Assert.Equal(new SlateAi.Api.Features.Parent.PictureAnswerResponse(false, 1, false), first);
        Assert.Equal(new SlateAi.Api.Features.Parent.PictureAnswerResponse(true, 2, true), second);
        Assert.Equal(System.Net.HttpStatusCode.Conflict, third.StatusCode);
        var stored = await api.QueryAsync(db => db.EveningMessages.SingleAsync(m => m.ChildId == manjunath, DemoClassApi.Ct));
        Assert.Equal(("<", ">", TodaysLevel.GettingThere), (stored.PictureFirst, stored.PictureSecond, TodaysLevel.For(stored.PictureFirst, stored.PictureSecond, ">")));

        // Sent: read only.
        Assert.Equal(TodaysChange.Conflict, await api.ScopedAsync<ITodaysMessageStore, TodaysChange>(
            s => s.SetRemovedAsync(api.ClassId, today, Guid.NewGuid(), TodaysSectionKind.Concept, true, DateTimeOffset.UtcNow, DemoClassApi.Ct)));
        Assert.Null(await api.ScopedAsync<ITodaysMessageStore, Guid?>(
            s => s.StartAsync(api.ClassId, today, ["M3.BRIDGE.COMPARE"], DateTimeOffset.UtcNow, DemoClassApi.Ct)));
    }

    [Fact]
    public async Task A_message_planned_with_a_draft_that_lost_its_approval_goes_plain()
    {
        // The send re-checks under today's message's lock: an edit between the plan and the insert means the plain message.
        await using var api = await DemoClassApi.StartAsync(postgres);
        var today = SchoolClock.Today(TimeProvider.System);
        var manjunath = api.ChildIds[14];
        var lessonId = await ConfirmLessonAsync(api, today);
        await ApproveTemplateAsync(api);
        var templateId = await api.QueryAsync(db => db.MessageTemplates.Where(t => t.Language == "kn").Select(t => t.Id).SingleAsync(DemoClassApi.Ct));
        var id = await api.ScopedAsync<ITodaysMessageStore, Guid?>(s => s.StartAsync(api.ClassId, today, ["M3.BRIDGE.COMPARE"], DateTimeOffset.UtcNow, DemoClassApi.Ct));
        var sections = Sections(await ClipsAsync(api, "kn"));
        await api.ScopedAsync<ITodaysMessageStore>(s => s.SetReadyAsync(id!.Value, sections, "valid", DateTimeOffset.UtcNow, DemoClassApi.Ct));

        var now = DateTimeOffset.UtcNow;
        await api.ScopedAsync<IEveningStore>(e => e.AddMessagesAsync(api.ClassId, today,
            [new NewEveningMessage(manjunath, templateId, lessonId, "kn", new SlotValues("ಹೆಚ್ಚು – ಕಡಿಮೆ – ಸಮ", "ಪುಟ 4"), "tok-not-approved", now, now.AddDays(3), TodaysMessageId: id)],
            DemoClassApi.Ct));

        Assert.Null(await api.QueryAsync(db => db.EveningMessages.Select(m => m.TodaysMessageId).SingleAsync(DemoClassApi.Ct)));
    }

    private static Task<TodaysChange> Approve(DemoClassApi api, DateOnly today, int version) =>
        api.ScopedAsync<ITodaysMessageStore, TodaysChange>(s => s.ApproveAsync(
            api.ClassId, today, Guid.NewGuid(), version, api.TeacherId, DateTimeOffset.UtcNow, DemoClassApi.Ct));

    private static async Task<Guid> ConfirmLessonAsync(DemoClassApi api, DateOnly today)
    {
        // Straight into the table: the PUT would also start a real draft in the background.
        var lesson = new Lesson
        {
            Id = Guid.NewGuid(), ClassId = api.ClassId, Date = today, Source = LessonValues.Picked,
            CompetencyIds = ["M3.BRIDGE.COMPARE"], Status = LessonValues.Confirmed,
        };
        await api.QueryAsync(db =>
        {
            db.Lessons.Add(lesson);
            return db.SaveChangesAsync(DemoClassApi.Ct);
        });
        return lesson.Id;
    }

    private static async Task ApproveTemplateAsync(DemoClassApi api)
    {
        var template = $"/api/classes/{api.ClassId}/message-template";
        var draft = (await api.Client.GetFromJsonAsync<MessageTemplateResponse>(template, DemoClassApi.Ct))!;
        if (draft.Status != "approved")
        {
            (await api.Client.PostAsJsonAsync(
                $"{template}/approve", new ApproveTemplateRequest(Guid.NewGuid(), draft.Version), DemoClassApi.Ct)).EnsureSuccessStatusCode();
        }
    }

    private static async Task<Dictionary<string, Guid>> ClipsAsync(DemoClassApi api, params string[] languages)
    {
        var clips = languages.ToDictionary(l => l, _ => Guid.NewGuid());
        await api.QueryAsync(db =>
        {
            db.AudioClips.AddRange(clips.Select(c => new AudioClip
            {
                Id = c.Value, Kind = "lesson", Language = c.Key, TextHash = Guid.NewGuid().ToString("N"), ContentType = "audio/ogg",
                Bytes = [1, 2, 3], Seconds = 4, CreatedAt = DateTimeOffset.UtcNow, Transcript = "concept",
            }));
            return db.SaveChangesAsync(DemoClassApi.Ct);
        });
        return clips;
    }

    private static TodaysSections Sections(Dictionary<string, Guid> clips) => new(
        new TodaysConcept(
            clips.Keys.ToDictionary(l => l, l => l == "kn" ? "ಮೊದಲು ಹತ್ತರ ಕಟ್ಟುಗಳನ್ನು ನೋಡಿ." : "ముందు పదుల కట్టలు చూడండి."),
            clips),
        new TodaysActivity(clips.Keys.ToDictionary(l => l, l => (IReadOnlyList<string>)[l == "kn" ? "ಕಡ್ಡಿಗಳ ಕಟ್ಟು ಮಾಡಿ." : "పుల్లల కట్ట చేయండి."])),
        new TodaysPicture(TodaysPicture.Compare, 48, 33, ">"));
}
