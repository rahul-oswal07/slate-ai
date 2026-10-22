using System.Net.Http.Json;
using SlateAi.Api.Data.Entities;
using SlateAi.Api.Features.Ledger;
using SlateAi.Api.Features.Lessons;

namespace SlateAi.Api.Tests.Db;

[Trait("Category", "Db")]
[Collection(DbCollection.Name)]
public sealed class LedgerDbTests(PostgresFixture postgres)
{
    private static readonly DateOnly Mon = new(2026, 10, 12);

    [Fact]
    public async Task Ledger_query_loads_attendance_and_lessons_from_postgres()
    {
        // Proves the store's queries translate to SQL and the text[] competency column round-trips.
        await using var api = await DemoClassApi.StartAsync(postgres);
        var away = api.ChildIds[0];
        await api.QueryAsync(db =>
        {
            db.Lessons.Add(new Lesson
            {
                Id = Guid.NewGuid(),
                ClassId = api.ClassId,
                Date = Mon,
                Source = "picked",
                CompetencyIds = ["M3.PV.TENS", "M3.ADD.CARRY"],
                Status = "confirmed",
            });
            return db.SaveChangesAsync(DemoClassApi.Ct);
        });
        await api.MarkAsync(Guid.NewGuid(), Mon, away);

        var ledger = await api.Client.GetFromJsonAsync<LedgerResponse>(
            $"/api/classes/{api.ClassId}/ledger", DemoClassApi.Ct);

        Assert.Equal(36, ledger!.Children.Count);
        var row = ledger.Children.Single(c => c.ChildId == away);
        Assert.Equal(["M3.PV.TENS", "M3.ADD.CARRY"], Assert.Single(row.MissedLessons).CompetencyIds);
        Assert.Equal(1, row.CurrentAbsenceDays);
    }

    [Fact]
    public async Task Only_confirmed_lessons_count_as_missed()
    {
        await using var api = await DemoClassApi.StartAsync(postgres);
        var away = api.ChildIds[0];
        var tue = Mon.AddDays(1);
        var wed = Mon.AddDays(2);
        await api.QueryAsync(db =>
        {
            // A voice lesson still waiting for her confirmation (T2 §3).
            db.Lessons.Add(new Lesson
            {
                Id = Guid.NewGuid(),
                ClassId = api.ClassId,
                Date = tue,
                Source = "voice",
                CompetencyIds = ["M3.ADD.NOCARRY"],
                Status = "mapped",
            });
            return db.SaveChangesAsync(DemoClassApi.Ct);
        });
        await api.MarkAsync(Guid.NewGuid(), tue, away);
        await api.MarkAsync(Guid.NewGuid(), wed, away);
        var picked = await api.Client.PutAsJsonAsync(
            $"/api/classes/{api.ClassId}/lessons/{wed:yyyy-MM-dd}",
            new SaveLessonRequest(Guid.NewGuid(), ["M3.ADD.CARRY"]),
            DemoClassApi.Ct);
        picked.EnsureSuccessStatusCode();

        var ledger = await api.Client.GetFromJsonAsync<LedgerResponse>(
            $"/api/classes/{api.ClassId}/ledger", DemoClassApi.Ct);

        var missed = Assert.Single(ledger!.Children.Single(c => c.ChildId == away).MissedLessons);
        Assert.Equal(wed, missed.Date);
        Assert.Equal(["M3.ADD.CARRY"], missed.CompetencyIds);
    }
}
