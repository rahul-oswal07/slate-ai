using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using SlateAi.Api.Data.Entities;
using SlateAi.Api.Features.Attendance;
using SlateAi.Api.Features.CatchUps;
using SlateAi.Api.Features.Demo;
using SlateAi.Api.Features.Lessons;
using SlateAi.Api.Features.Parent;
using SlateAi.Api.Features.EveningMessages;

namespace SlateAi.Api.Tests.Db;

[Trait("Category", "Db")]
[Collection(DbCollection.Name)]
public sealed class DemoResetDbTests(PostgresFixture postgres)
{
    [Fact]
    public async Task Reset_today_clears_only_the_day_with_its_cascades_and_puts_the_sats_register_back()
    {
        // T-143 on a real database (Rahul agreed, 17 Nov): the day's deletes, tonight's messages taking their deliveries
        // and answers with them, yesterday left alone, the simulated register written back, and a replay changing nothing.
        await using var api = await DemoClassApi.StartAsync(postgres);
        var today = SchoolClock.Today(TimeProvider.System);
        var yesterday = today.AddDays(-1);
        var ravi = api.ChildIds[3]; // roll 4, Telugu, consent yes
        var lessons = $"/api/classes/{api.ClassId}/lessons";

        (await api.MarkAsync(Guid.NewGuid(), yesterday, ravi)).EnsureSuccessStatusCode();
        (await api.Client.PutAsJsonAsync($"{lessons}/{yesterday:yyyy-MM-dd}",
            new SaveLessonRequest(Guid.NewGuid(), ["M3.SUB.MAKE"]), DemoClassApi.Ct)).EnsureSuccessStatusCode();
        (await api.MarkAsync(Guid.NewGuid(), today, ravi)).EnsureSuccessStatusCode();
        (await api.Client.PutAsJsonAsync($"{lessons}/{today:yyyy-MM-dd}",
            new SaveLessonRequest(Guid.NewGuid(), ["M3.SUB.NOBORROW"]), DemoClassApi.Ct)).EnsureSuccessStatusCode();
        var template = $"/api/classes/{api.ClassId}/message-template";
        var draft = (await api.Client.GetFromJsonAsync<MessageTemplateResponse>(template, DemoClassApi.Ct))!;
        (await api.Client.PostAsJsonAsync($"{template}/approve", new ApproveTemplateRequest(Guid.NewGuid(), draft.Version), DemoClassApi.Ct)).EnsureSuccessStatusCode();
        (await api.Client.PostAsJsonAsync($"/api/classes/{api.ClassId}/evening-messages/send", new SendEveningRequest(Guid.NewGuid()), DemoClassApi.Ct)).EnsureSuccessStatusCode();
        var token = await api.QueryAsync(db => db.EveningMessages.Where(m => m.ChildId == ravi && m.Date == today).Select(m => m.Token).SingleAsync(DemoClassApi.Ct));
        (await api.Client.PostAsJsonAsync($"/api/p/{token}/reason", new ReasonRequest(ReasonCategory.Unwell, Guid.NewGuid()), DemoClassApi.Ct)).EnsureSuccessStatusCode();
        for (var i = 0; i < 100 && await api.QueryAsync(db => db.TodaysMessages.AnyAsync(t => t.Status == "generating", DemoClassApi.Ct)); i++)
        {
            await Task.Delay(100, DemoClassApi.Ct);
        }

        // Once it has gone, another lesson is refused (D-124).
        var refused = await api.Client.PutAsJsonAsync($"{lessons}/{today:yyyy-MM-dd}",
            new SaveLessonRequest(Guid.NewGuid(), ["M3.SUB.MAKE"]), DemoClassApi.Ct);
        Assert.Equal(System.Net.HttpStatusCode.Conflict, refused.StatusCode);
        var deliveriesBefore = await api.QueryAsync(db => db.ParentDeliveries.CountAsync(DemoClassApi.Ct));
        Assert.True(deliveriesBefore > 0);

        var requestId = Guid.NewGuid();
        var reset = await api.Client.PostAsJsonAsync("/api/demo/reset-today", new DemoResetDayRequest(requestId), DemoClassApi.Ct);

        Assert.True(reset.IsSuccessStatusCode);
        Assert.Equal(new DemoResetDayResponse(today), await reset.Content.ReadFromJsonAsync<DemoResetDayResponse>(DemoClassApi.Ct));
        var after = await api.QueryAsync(async db => new
        {
            Evening = await db.EveningMessages.CountAsync(m => m.Date == today, DemoClassApi.Ct),
            Deliveries = await db.ParentDeliveries.CountAsync(DemoClassApi.Ct),
            Todays = await db.TodaysMessages.CountAsync(t => t.Date == today, DemoClassApi.Ct),
            Lessons = await db.Lessons.CountAsync(l => l.Date == today, DemoClassApi.Ct),
            Reasons = await db.AbsenceReasons.CountAsync(r => r.Date == today, DemoClassApi.Ct),
            Register = await db.Attendance.Where(a => a.Date == today).Select(a => a.Source).ToListAsync(DemoClassApi.Ct),
            YesterdayLesson = await db.Lessons.CountAsync(l => l.Date == yesterday, DemoClassApi.Ct),
            YesterdayRegister = await db.Attendance.CountAsync(a => a.Date == yesterday, DemoClassApi.Ct),
        });
        Assert.Equal((0, 0, 0, 0, 0), (after.Evening, after.Deliveries, after.Todays, after.Lessons, after.Reasons));
        Assert.Equal((1, 36), (after.YesterdayLesson, after.YesterdayRegister));
        // On a school day the simulated SATS register is back, for every child; on a holiday there is none.
        var schoolDay = await api.ScopedAsync<SlateAi.Api.Features.Attendance.Source.SimulatedSatsSource, bool>(s => Task.FromResult(s.IsSchoolDay(today)));
        Assert.Equal(schoolDay ? 36 : 0, after.Register.Count);
        Assert.All(after.Register, source => Assert.Equal(AttendanceSources.Sats, source));

        // A new lesson saves again; replaying the reset doesn't remove it.
        (await api.Client.PutAsJsonAsync($"{lessons}/{today:yyyy-MM-dd}",
            new SaveLessonRequest(Guid.NewGuid(), ["M3.SUB.MAKE"]), DemoClassApi.Ct)).EnsureSuccessStatusCode();
        (await api.Client.PostAsJsonAsync("/api/demo/reset-today", new DemoResetDayRequest(requestId), DemoClassApi.Ct)).EnsureSuccessStatusCode();
        Assert.Equal(1, await api.QueryAsync(db => db.Lessons.CountAsync(l => l.Date == today, DemoClassApi.Ct)));
    }

    [Fact]
    public async Task Reset_puts_a_used_demo_class_back_to_the_story_and_keeps_shared_audio_and_the_ai_log()
    {
        // T-18 on a real database: the delete set and its cascades, children back from the seed, the story's days, the
        // pre-made catch-up with its clips, and a replay changing nothing. Only Postgres proves the SQL and the cascades.
        // The seeded snapshot is Hindi; a Kannada copy goes to Manjunath, the one story child (T-93).
        var snapshotFile = $"demo-snapshot-kn-{Guid.NewGuid():N}.json";
        var seeded = JsonNode.Parse(await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "data/seed/demo-catchup.json"), DemoClassApi.Ct))!;
        seeded["language"] = "kn";
        await File.WriteAllTextAsync(Path.Combine(AppContext.BaseDirectory, snapshotFile), seeded.ToJsonString(), DemoClassApi.Ct);
        await using var api = await DemoClassApi.StartAsync(postgres, new Dictionary<string, string?> { ["Demo:SnapshotFile"] = snapshotFile });
        var today = SchoolClock.Today(TimeProvider.System);
        var ananya = api.ChildIds[0];  // roll 1, deleted by the teacher
        var bhavana = api.ChildIds[1]; // roll 2, family stopped messages, consent changed to No
        var manjunath = api.ChildIds[14];  // roll 15, Kannada: the story child, owns the pre-made catch-up

        // A class that has been used: today's register, a lesson, a deleted child, a stop, a changed consent, a package
        // with its own audio, a shared disclosure clip and an AI call.
        (await api.MarkAsync(Guid.NewGuid(), today, bhavana)).EnsureSuccessStatusCode();
        // Every confirmed lesson starts today's message when a family is due (T-139), so its background AI calls are
        // waited for below before Reset: the AI log must come through Reset whole.
        (await api.Client.PutAsJsonAsync($"/api/classes/{api.ClassId}/lessons/{today:yyyy-MM-dd}",
            new SaveLessonRequest(Guid.NewGuid(), ["M3.SUB.MAKE"]), DemoClassApi.Ct)).EnsureSuccessStatusCode();
        (await api.Client.DeleteAsync(
            $"/api/classes/{api.ClassId}/children/{ananya}?clientRequestId={Guid.NewGuid()}", DemoClassApi.Ct)).EnsureSuccessStatusCode();
        var oldPackage = Guid.NewGuid();
        var sharedClip = Guid.NewGuid();
        var savedAt = new DateTimeOffset(2026, 11, 15, 10, 0, 0, TimeSpan.Zero);
        await api.QueryAsync(async db =>
        {
            await db.Children.Where(c => c.Id == bhavana).ExecuteUpdateAsync(
                s => s.SetProperty(c => c.ParentStoppedAt, DateTimeOffset.UtcNow)
                    .SetProperty(c => c.Consent, "no")
                    .SetProperty(c => c.ConsentChangedAt, DateTimeOffset.UtcNow),
                DemoClassApi.Ct);
            // A number and a home language saved on the admin page (T-118, D-110; T-122, D-113): Reset keeps both.
            await db.Children.Where(c => c.Id == manjunath).ExecuteUpdateAsync(
                s => s.SetProperty(c => c.ParentPhone, "+447700900123").SetProperty(c => c.ParentPhoneSetAt, savedAt)
                    .SetProperty(c => c.ParentPhone2, "+919845012345").SetProperty(c => c.ParentPhone2SetAt, savedAt)
                    .SetProperty(c => c.HomeLanguage, "ta").SetProperty(c => c.HomeLanguageSetAt, savedAt),
                DemoClassApi.Ct);
            db.CatchUpPackages.Add(new CatchUpPackage
            {
                Id = oldPackage, ClassId = api.ClassId, WeekOf = today, TargetKey = "M3.SUB.NOBORROW", Language = "kn",
                Status = PackageStatus.Ready, Activity = "{}", CheckItem = "{}",
                CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow,
            });
            db.AudioClips.Add(Clip(Guid.NewGuid(), "script", oldPackage, null));
            db.AudioClips.Add(Clip(sharedClip, "disclosure", null, "another-wording"));
            db.AiCalls.Add(new AiCall { Id = Guid.NewGuid(), CreatedAt = DateTimeOffset.UtcNow, Purpose = "a3-script", Model = "test" });
            return await db.SaveChangesAsync(DemoClassApi.Ct);
        });

        // Let the background drafting finish, so the AI log no longer changes while Reset runs.
        for (var i = 0; i < 100 && await api.QueryAsync(db => db.TodaysMessages.AnyAsync(t => t.Status == "generating", DemoClassApi.Ct)); i++)
        {
            await Task.Delay(100, DemoClassApi.Ct);
        }

        var aiCallsBefore = await api.QueryAsync(db => db.AiCalls.CountAsync(DemoClassApi.Ct));
        var requestId = Guid.NewGuid();
        var response = await api.Client.PostAsJsonAsync("/api/demo/reset", new DemoResetRequest(requestId), DemoClassApi.Ct);
        var body = await response.Content.ReadFromJsonAsync<DemoResetResponse>(DemoClassApi.Ct);

        Assert.True(response.IsSuccessStatusCode);
        Assert.True(body!.PreMade);

        // The seed's 36 children, with the deleted one back and the stop and consent change undone.
        Assert.Equal(36, await api.QueryAsync(db => db.Children.CountAsync(DemoClassApi.Ct)));
        var restored = await api.QueryAsync(db => db.Children.SingleAsync(c => c.Id == bhavana, DemoClassApi.Ct));
        Assert.Null(restored.ParentStoppedAt);
        Assert.Equal("yes", restored.Consent);
        Assert.True(await api.QueryAsync(db => db.Children.AnyAsync(c => c.Id == ananya, DemoClassApi.Ct)));
        var kept = await api.QueryAsync(db => db.Children.SingleAsync(c => c.Id == manjunath, DemoClassApi.Ct));
        Assert.Equal(("+447700900123", (DateTimeOffset?)savedAt), (kept.ParentPhone, kept.ParentPhoneSetAt));
        Assert.Equal(("+919845012345", (DateTimeOffset?)savedAt), (kept.ParentPhone2, kept.ParentPhone2SetAt)); // T-140
        Assert.Null(restored.ParentPhone2);
        Assert.Equal(("ta", (DateTimeOffset?)savedAt), (kept.HomeLanguage, kept.HomeLanguageSetAt));
        Assert.Null(restored.HomeLanguageSetAt);

        // Five school days before today, each a full SATS register and a confirmed lesson.
        var days = await api.QueryAsync(db => db.Lessons.OrderBy(l => l.Date).ToListAsync(DemoClassApi.Ct));
        Assert.Equal(DemoStory.Lessons, days.Select(l => l.CompetencyIds.Single()));
        Assert.All(days, l => Assert.Equal(LessonValues.Confirmed, l.Status));
        Assert.All(days, l => Assert.True(l.Date < today));
        var lessonDates = days.Select(l => l.Date).ToList();
        Assert.Equal(5 * 36, await api.QueryAsync(db => db.Attendance.CountAsync(a => lessonDates.Contains(a.Date), DemoClassApi.Ct)));
        // Earlier weeks of register only (T-63): full registers, all before the first lesson day, no lessons.
        var earlier = await api.QueryAsync(db => db.Attendance.Where(a => !lessonDates.Contains(a.Date)).Select(a => a.Date).ToListAsync(DemoClassApi.Ct));
        Assert.NotEmpty(earlier);
        Assert.Equal(0, earlier.Count % 36);
        Assert.All(earlier.Where(d => d != today), d => Assert.True(d < lessonDates.Min()));
        // Today's register (T-93): full, only Manjunath away, as if SATS sent it at 9:00 IST; no lesson yet.
        var todays = await api.QueryAsync(db => db.Attendance.Where(a => a.Date == today).ToListAsync(DemoClassApi.Ct));
        Assert.Equal(36, todays.Count);
        Assert.Equal([manjunath], todays.Where(a => !a.Present).Select(a => a.ChildId));
        Assert.All(todays, a => Assert.Equal(SchoolClock.At(today, new TimeOnly(9, 0)), a.ReceivedAt));
        Assert.DoesNotContain(today, lessonDates);

        // The old package and its own audio are gone; the shared clip and the AI log stay; Manjunath has the pre-made one.
        Assert.False(await api.QueryAsync(db => db.CatchUpPackages.AnyAsync(p => p.Id == oldPackage, DemoClassApi.Ct)));
        Assert.True(await api.QueryAsync(db => db.AudioClips.AnyAsync(c => c.Id == sharedClip, DemoClassApi.Ct)));
        Assert.True(aiCallsBefore >= 1);
        Assert.Equal(aiCallsBefore, await api.QueryAsync(db => db.AiCalls.CountAsync(DemoClassApi.Ct)));
        var package = await api.QueryAsync(db => db.CatchUpPackages.SingleAsync(DemoClassApi.Ct));
        Assert.Equal(("kn", PackageStatus.Ready), (package.Language, package.Status));
        Assert.Equal(manjunath, await api.QueryAsync(db => db.CatchUpAssignments.Select(a => a.ChildId).SingleAsync(DemoClassApi.Ct)));
        var clipIds = DemoSnapshot.ClipIds(package);
        Assert.Equal(clipIds.Count, await api.QueryAsync(db => db.AudioClips.CountAsync(c => clipIds.Contains(c.Id), DemoClassApi.Ct)));

        // A replay of the same request changes nothing, even after the class was used again.
        (await api.MarkAsync(Guid.NewGuid(), today, bhavana)).EnsureSuccessStatusCode();
        (await api.Client.PostAsJsonAsync("/api/demo/reset", new DemoResetRequest(requestId), DemoClassApi.Ct)).EnsureSuccessStatusCode();
        Assert.False(await api.QueryAsync(db => db.Attendance.Where(a => a.Date == today && a.ChildId == bhavana).Select(a => a.Present).SingleAsync(DemoClassApi.Ct)));
        File.Delete(Path.Combine(AppContext.BaseDirectory, snapshotFile));
    }

    private static AudioClip Clip(Guid id, string kind, Guid? packageId, string? textHash) => new()
    {
        Id = id, Kind = kind, Language = "kn", PackageId = packageId, TextHash = textHash,
        ContentType = "audio/ogg", Bytes = [1, 2, 3], Seconds = 1, CreatedAt = DateTimeOffset.UtcNow,
    };
}
