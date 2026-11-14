using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using SlateAi.Api.Data.Entities;
using SlateAi.Api.Features.Attendance;
using SlateAi.Api.Features.CatchUps;
using SlateAi.Api.Features.Demo;
using SlateAi.Api.Features.Lessons;

namespace SlateAi.Api.Tests.Db;

[Trait("Category", "Db")]
[Collection(DbCollection.Name)]
public sealed class DemoResetDbTests(PostgresFixture postgres)
{
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
        (await api.Client.PutAsJsonAsync($"/api/classes/{api.ClassId}/lessons/{today:yyyy-MM-dd}",
            new SaveLessonRequest(Guid.NewGuid(), ["M3.SUB.NOBORROW"]), DemoClassApi.Ct)).EnsureSuccessStatusCode();
        (await api.Client.DeleteAsync(
            $"/api/classes/{api.ClassId}/children/{ananya}?clientRequestId={Guid.NewGuid()}", DemoClassApi.Ct)).EnsureSuccessStatusCode();
        var oldPackage = Guid.NewGuid();
        var sharedClip = Guid.NewGuid();
        await api.QueryAsync(async db =>
        {
            await db.Children.Where(c => c.Id == bhavana).ExecuteUpdateAsync(
                s => s.SetProperty(c => c.ParentStoppedAt, DateTimeOffset.UtcNow)
                    .SetProperty(c => c.Consent, "no")
                    .SetProperty(c => c.ConsentChangedAt, DateTimeOffset.UtcNow),
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
        // Today's register (T-93): full, only Manjunath away, as if SATS sent it at 12:00 IST; no lesson yet.
        var todays = await api.QueryAsync(db => db.Attendance.Where(a => a.Date == today).ToListAsync(DemoClassApi.Ct));
        Assert.Equal(36, todays.Count);
        Assert.Equal([manjunath], todays.Where(a => !a.Present).Select(a => a.ChildId));
        Assert.All(todays, a => Assert.Equal(SchoolClock.At(today, new TimeOnly(12, 0)), a.ReceivedAt));
        Assert.DoesNotContain(today, lessonDates);

        // The old package and its own audio are gone; the shared clip and the AI log stay; Manjunath has the pre-made one.
        Assert.False(await api.QueryAsync(db => db.CatchUpPackages.AnyAsync(p => p.Id == oldPackage, DemoClassApi.Ct)));
        Assert.True(await api.QueryAsync(db => db.AudioClips.AnyAsync(c => c.Id == sharedClip, DemoClassApi.Ct)));
        Assert.Equal(1, await api.QueryAsync(db => db.AiCalls.CountAsync(DemoClassApi.Ct)));
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
