using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using SlateAi.Api.Features.Attendance;

namespace SlateAi.Api.Tests.Db;

[Trait("Category", "Db")]
[Collection(DbCollection.Name)]
public sealed class AttendanceDbTests(PostgresFixture postgres)
{
    private static readonly DateOnly Day = new(2026, 10, 12);

    [Fact]
    public async Task A_replay_after_a_correction_does_not_undo_it()
    {
        await using var api = await DemoClassApi.StartAsync(postgres);
        var original = Guid.NewGuid();
        await api.MarkAsync(original, Day, [.. api.ChildIds.Take(4)]);
        await api.MarkAsync(Guid.NewGuid(), Day, api.ChildIds[0]);

        // The offline outbox resends the original after the teacher corrected it.
        var replay = await api.MarkAsync(original, Day, [.. api.ChildIds.Take(4)]);

        Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        Assert.Equal(new MarkAttendanceResponse(Day, 35, 1),
            await replay.Content.ReadFromJsonAsync<MarkAttendanceResponse>(DemoClassApi.Ct));
        Assert.Equal(36, await api.QueryAsync(db => db.Attendance.CountAsync(DemoClassApi.Ct)));
    }

    [Fact]
    public async Task Concurrent_replays_both_succeed_and_apply_once()
    {
        await using var api = await DemoClassApi.StartAsync(postgres);
        var requestId = Guid.NewGuid();
        Guid[] absent = [.. api.ChildIds.Take(3)];

        var responses = await Task.WhenAll(
            api.MarkAsync(requestId, Day, absent),
            api.MarkAsync(requestId, Day, absent));

        Assert.All(responses, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));
        Assert.Equal(36, await api.QueryAsync(db => db.Attendance.CountAsync(DemoClassApi.Ct)));
        Assert.Equal(1, await api.QueryAsync(db => db.ProcessedRequests.CountAsync(DemoClassApi.Ct)));
    }
}
