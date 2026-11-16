using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using SlateAi.Api.Data.Entities;
using SlateAi.Api.Features.Attendance;
using SlateAi.Api.Features.CatchUps;
using SlateAi.Api.Features.Delivery;
using SlateAi.Api.Features.Lessons;
using SlateAi.Api.Features.TodaysMessage;

namespace SlateAi.Api.Tests.Db;

/// <summary>
/// T-104 (a, b): the outbox query on real Postgres. A row carrying today's message reads its lessons (an array column)
/// and comes back as <see cref="DeliveryKinds.Todays"/>; a catch-up reads its package's targets; both get the values of
/// Monika's templates; a plain evening row is unchanged.
/// </summary>
[Trait("Category", "Db")]
[Collection(DbCollection.Name)]
public sealed class DeliveryStoreDbTests(PostgresFixture postgres)
{
    private const string Nalikali = "M3.BRIDGE.COMPARE"; // Nalikali Part 1 p 4, the demo story's lesson

    [Fact]
    public async Task Pending_deliveries_carry_the_template_values_for_todays_message_and_the_catch_up()
    {
        await using var api = await DemoClassApi.StartAsync(postgres);
        var today = SchoolClock.Today(TimeProvider.System);
        var ananya = api.ChildIds[0];     // roll 1, Kannada: catch-up
        var ravi = api.ChildIds[3];       // roll 4, Telugu: plain evening message
        var manjunath = api.ChildIds[14]; // roll 15, Kannada: today's message
        (await api.Client.PutAsJsonAsync(
            $"/api/classes/{api.ClassId}/lessons/{today:yyyy-MM-dd}",
            new SaveLessonRequest(Guid.NewGuid(), [Nalikali]), DemoClassApi.Ct)).EnsureSuccessStatusCode();
        (await api.Client.GetAsync($"/api/classes/{api.ClassId}/message-template", DemoClassApi.Ct)).EnsureSuccessStatusCode();

        var now = DateTimeOffset.UtcNow;
        await api.QueryAsync(async db =>
        {
            var lessonId = await db.Lessons.Where(l => l.ClassId == api.ClassId && l.Date == today).Select(l => l.Id).SingleAsync(DemoClassApi.Ct);
            var templates = await db.MessageTemplates.Where(t => t.ClassId == api.ClassId).ToDictionaryAsync(t => t.Language, t => t.Id, DemoClassApi.Ct);
            var todaysId = Guid.NewGuid();
            db.TodaysMessages.Add(new TodaysMessage
            {
                Id = todaysId, ClassId = api.ClassId, Date = today.AddDays(-1), CompetencyIds = [Nalikali],
                Status = TodaysMessageStatus.Approved, Version = 1, ApprovedVersion = 1, Sections = "{}",
                CreatedAt = now, UpdatedAt = now,
            });
            foreach (var (child, language, todays) in new[] { (manjunath, "kn", (Guid?)todaysId), (ravi, "te", null) })
            {
                var id = Guid.NewGuid();
                db.EveningMessages.Add(new EveningMessage
                {
                    Id = id, ClassId = api.ClassId, ChildId = child, Date = today, TemplateId = templates[language],
                    LessonId = lessonId, TodaysMessageId = todays, Language = language,
                    SlotValues = """{"lesson":"ఎక్కువ – తక్కువ – సమానం","page":"నలికలి గణితం భాగం 1, పేజీ 4"}""",
                    Token = $"tok-{language}", SentAt = now, ExpiresAt = now.AddDays(14),
                });
                db.ParentDeliveries.Add(DeliveryStore.Queue(api.ClassId, child, now, eveningMessageId: id));
            }

            var packageId = Guid.NewGuid();
            db.CatchUpPackages.Add(new CatchUpPackage
            {
                Id = packageId, ClassId = api.ClassId, WeekOf = today, TargetCompetencyIds = [Nalikali], TargetKey = Nalikali,
                Language = "kn", Status = PackageStatus.Sent, Activity = "{}", CheckItem = "{}", CreatedAt = now, UpdatedAt = now,
            });
            var assignmentId = Guid.NewGuid();
            db.CatchUpAssignments.Add(new CatchUpAssignment
            {
                Id = assignmentId, ClassId = api.ClassId, ChildId = ananya, PackageId = packageId, WeekOf = today,
                MissedCompetencyIds = [Nalikali], CompressionRationale = "{}", Token = "tok-catchup",
                ExpiresAt = now.AddDays(14), CreatedAt = now,
            });
            db.ParentDeliveries.Add(DeliveryStore.Queue(api.ClassId, ananya, now, assignmentId: assignmentId));
            return await db.SaveChangesAsync(DemoClassApi.Ct);
        });

        var pending = await api.ScopedAsync<IDeliveryStore, IReadOnlyList<PendingDelivery>>(
            s => s.GetPendingAsync(10, DemoClassApi.Ct));

        var byChild = pending.ToDictionary(p => p.Message.ChildId, p => p.Message);
        Assert.Equal(3, byChild.Count);
        Assert.Equal((DeliveryKinds.Todays, "tok-kn"), (byChild[manjunath].Kind, byChild[manjunath].Token));
        Assert.Equal(["Manjunath", "ಹೆಚ್ಚು – ಕಡಿಮೆ – ಸಮ", "4"], byChild[manjunath].Parameters);
        Assert.Equal((DeliveryKinds.CatchUp, "tok-catchup"), (byChild[ananya].Kind, byChild[ananya].Token));
        Assert.Equal(["Ananya", "ಹೆಚ್ಚು – ಕಡಿಮೆ – ಸಮ", "ಪುಟ 4"], byChild[ananya].Parameters);
        Assert.Equal(DeliveryKinds.Evening, byChild[ravi].Kind);
        Assert.Equal(["Ravi", "ఎక్కువ – తక్కువ – సమానం", "నలికలి గణితం భాగం 1, పేజీ 4"], byChild[ravi].Parameters);
    }

    [Fact]
    public async Task Each_pending_row_carries_its_own_saved_number_and_a_seeded_or_cleared_one_gives_none_T140()
    {
        await using var api = await DemoClassApi.StartAsync(postgres);
        var today = SchoolClock.Today(TimeProvider.System);
        var ravi = api.ChildIds[3];       // both numbers saved
        var manjunath = api.ChildIds[14]; // only the seed's number 1 (never dialled) and number 2 saved then cleared
        (await api.Client.PutAsJsonAsync(
            $"/api/classes/{api.ClassId}/lessons/{today:yyyy-MM-dd}",
            new SaveLessonRequest(Guid.NewGuid(), [Nalikali]), DemoClassApi.Ct)).EnsureSuccessStatusCode();
        (await api.Client.GetAsync($"/api/classes/{api.ClassId}/message-template", DemoClassApi.Ct)).EnsureSuccessStatusCode();
        foreach (var (child, slot, phone) in new[] { (ravi, 1, "98765 43210"), (ravi, 2, "98450 12345"), (manjunath, 2, "98123 45678"), (manjunath, 2, "") })
        {
            (await api.Client.PutAsJsonAsync(
                $"/api/admin/children/{child}/phone",
                new SlateAi.Api.Features.Admin.SetParentPhoneRequest(Guid.NewGuid(), phone, slot), DemoClassApi.Ct)).EnsureSuccessStatusCode();
        }

        var now = DateTimeOffset.UtcNow;
        await api.QueryAsync(async db =>
        {
            var lessonId = await db.Lessons.Where(l => l.ClassId == api.ClassId && l.Date == today).Select(l => l.Id).SingleAsync(DemoClassApi.Ct);
            var template = await db.MessageTemplates.Where(t => t.ClassId == api.ClassId && t.Language == "kn").Select(t => t.Id).SingleAsync(DemoClassApi.Ct);
            foreach (var child in new[] { ravi, manjunath })
            {
                var id = Guid.NewGuid();
                db.EveningMessages.Add(new EveningMessage
                {
                    Id = id, ClassId = api.ClassId, ChildId = child, Date = today, TemplateId = template, LessonId = lessonId,
                    Language = "kn", SlotValues = """{"lesson":"ಹೆಚ್ಚು – ಕಡಿಮೆ – ಸಮ","page":"ಪುಟ 4"}""",
                    Token = $"tok2-{(child == ravi ? "r" : "m")}", SentAt = now, ExpiresAt = now.AddDays(14),
                });
                db.ParentDeliveries.AddRange(DeliveryStore.QueueAll(api.ClassId, child, now, eveningMessageId: id));
            }

            return await db.SaveChangesAsync(DemoClassApi.Ct);
        });

        var pending = await api.ScopedAsync<IDeliveryStore, IReadOnlyList<PendingDelivery>>(
            s => s.GetPendingAsync(10, DemoClassApi.Ct));

        var phones = pending.ToDictionary(p => (p.Message.ChildId, p.Message.Recipient), p => p.Message.Phone);
        Assert.Equal(4, phones.Count);
        Assert.Equal("+919876543210", phones[(ravi, 1)]);
        Assert.Equal("+919845012345", phones[(ravi, 2)]);
        Assert.Null(phones[(manjunath, 1)]);
        Assert.Null(phones[(manjunath, 2)]);
    }
}
