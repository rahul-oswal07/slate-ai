using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using SlateAi.Api.Features.Attendance;
using SlateAi.Api.Features.CatchUps;
using SlateAi.Api.Features.Lessons;

namespace SlateAi.Api.Tests.Db;

[Trait("Category", "Db")]
[Collection(DbCollection.Name)]
public sealed class CatchUpApprovalDbTests(PostgresFixture postgres)
{
    [Fact]
    public async Task Nothing_is_sent_unless_approved_at_its_version_and_only_consenting_families_get_a_link()
    {
        // Guardrail 3 on a real database (CLAUDE.md: the approval rule belongs in the Db suite).
        await using var api = await DemoClassApi.StartAsync(postgres);
        var today = SchoolClock.Today(TimeProvider.System);
        var ravi = api.ChildIds[3];    // roll 4, consent yes
        var noForm = api.ChildIds[11]; // roll 12, consent no
        (await api.MarkAsync(Guid.NewGuid(), today, ravi, noForm)).EnsureSuccessStatusCode();
        (await api.Client.PutAsJsonAsync($"/api/classes/{api.ClassId}/lessons/{today:yyyy-MM-dd}",
            new SaveLessonRequest(Guid.NewGuid(), ["M3.SUB.NOBORROW"]), DemoClassApi.Ct)).EnsureSuccessStatusCode();
        (await api.Client.PostAsJsonAsync($"/api/classes/{api.ClassId}/catchups/generate",
            new GenerateCatchUpsRequest(Guid.NewGuid()), DemoClassApi.Ct)).EnsureSuccessStatusCode();

        // Both children speak different languages, so wait for whichever packages exist, then make them ready at v3.
        await WaitAsync(api, async () => await api.QueryAsync(db => db.CatchUpPackages.AllAsync(p => p.Status != PackageStatus.Generating, DemoClassApi.Ct)));
        await api.QueryAsync(db => db.CatchUpPackages.ExecuteUpdateAsync(
            s => s.SetProperty(p => p.Status, PackageStatus.Ready).SetProperty(p => p.ScriptVersion, 3), DemoClassApi.Ct));
        var packageId = await api.QueryAsync(db => db.CatchUpAssignments.Where(a => a.ChildId == ravi).Select(a => a.PackageId).SingleAsync(DemoClassApi.Ct));
        var url = $"/api/packages/{packageId}";

        var unapproved = await api.Client.PostAsJsonAsync($"{url}/send", new SendPackageRequest(Guid.NewGuid()), DemoClassApi.Ct);
        Assert.Equal(HttpStatusCode.Conflict, unapproved.StatusCode);

        var stale = await api.Client.PostAsJsonAsync($"{url}/approve", new ApprovePackageRequest(Guid.NewGuid(), 2), DemoClassApi.Ct);
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);

        // Two approvals at once: both see "approved" afterwards, but only one is applied (advisory lock).
        var approvals = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ =>
            api.Client.PostAsJsonAsync($"{url}/approve", new ApprovePackageRequest(Guid.NewGuid(), 3), DemoClassApi.Ct)));
        Assert.Contains(approvals, r => r.StatusCode == HttpStatusCode.OK);
        Assert.Contains(approvals, r => r.StatusCode == HttpStatusCode.Conflict);
        Assert.Equal(1, await api.QueryAsync(db => db.ProcessedRequests.CountAsync(r => r.Endpoint == "package-approve", DemoClassApi.Ct)));

        var sent = await api.Client.PostAsJsonAsync($"{url}/send", new SendPackageRequest(Guid.NewGuid()), DemoClassApi.Ct);
        Assert.Equal(HttpStatusCode.OK, sent.StatusCode);
        var tokens = await api.QueryAsync(db => db.CatchUpAssignments.Where(a => a.Token != null).Select(a => new { a.ChildId, a.Token }).ToListAsync(DemoClassApi.Ct));
        var only = Assert.Single(tokens);
        Assert.Equal(ravi, only.ChildId);
        Assert.Equal(PackageStatus.Sent, await api.QueryAsync(db => db.CatchUpPackages.Where(p => p.Id == packageId).Select(p => p.Status).SingleAsync(DemoClassApi.Ct)));
    }

    private static async Task WaitAsync(DemoClassApi api, Func<Task<bool>> done)
    {
        for (var i = 0; i < 50 && !await done(); i++)
        {
            await Task.Delay(100, DemoClassApi.Ct);
        }
    }
}
