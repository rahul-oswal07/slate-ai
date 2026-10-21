using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using SlateAi.Api.Features.Attendance;
using SlateAi.Api.Features.Attendance.Source;
using SlateAi.Api.Features.Demo;
using SlateAi.Api.Tests.Features.Attendance;
using SlateAi.Api.Tests.Infrastructure;

namespace SlateAi.Api.Tests.Features.Demo;

public sealed class DemoEndpointTests
{
    private static readonly Guid ClassId = Guid.NewGuid();
    private static readonly Guid[] Children = Enumerable.Range(0, 36).Select(_ => Guid.NewGuid()).ToArray();
    private static readonly DateOnly Today = new(2026, 10, 21);
    private static readonly DateTimeOffset Now = SchoolClock.At(Today, new TimeOnly(11, 0));
    private static readonly CancellationToken Ct = TestContext.Current.CancellationToken;

    private readonly FakeAttendanceStore _store = new(ClassId, Children);

    [Fact]
    public async Task Not_mapped_unless_demo_is_enabled()
    {
        await using var factory = Create(enabled: false);
        using var client = factory.CreateTeacherClient(ClassId);

        var response = await client.PostAsJsonAsync("/api/demo/sats-feed", new SatsFeedRequest(ClassId, Today, "arrive"), Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Waiting_removes_the_register_and_holds_sats_back_until_arrive()
    {
        await using var factory = Create();
        using var client = factory.CreateTeacherClient(ClassId);
        var url = $"/api/classes/{ClassId}/attendance/{Today:yyyy-MM-dd}";
        Assert.Equal("sats", (await client.GetFromJsonAsync<AttendanceDayResponse>(url, Ct))!.Status);

        var waiting = await client.PostAsJsonAsync("/api/demo/sats-feed", new SatsFeedRequest(ClassId, Today, "waiting"), Ct);
        var afterWaiting = await client.GetFromJsonAsync<AttendanceDayResponse>(url, Ct);
        var arrive = await client.PostAsJsonAsync("/api/demo/sats-feed", new SatsFeedRequest(ClassId, Today, "arrive"), Ct);
        var arrived = await arrive.Content.ReadFromJsonAsync<AttendanceDayResponse>(Ct);

        Assert.Equal(HttpStatusCode.OK, waiting.StatusCode);
        Assert.Equal("waiting", afterWaiting!.Status);
        Assert.Equal("sats", arrived!.Status);
        Assert.Equal(Now, arrived.ReceivedAt);
    }

    [Fact]
    public async Task Only_for_the_signed_in_teachers_class_and_known_modes()
    {
        await using var factory = Create();
        using var other = factory.CreateTeacherClient(Guid.NewGuid());
        using var teacher = factory.CreateTeacherClient(ClassId);
        using var anonymous = factory.CreateHttpsClient();

        var notHers = await other.PostAsJsonAsync("/api/demo/sats-feed", new SatsFeedRequest(ClassId, Today, "arrive"), Ct);
        var badMode = await teacher.PostAsJsonAsync("/api/demo/sats-feed", new SatsFeedRequest(ClassId, Today, "later"), Ct);
        var signedOut = await anonymous.PostAsJsonAsync("/api/demo/sats-feed", new SatsFeedRequest(ClassId, Today, "arrive"), Ct);

        Assert.Equal(HttpStatusCode.NotFound, notHers.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, badMode.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, signedOut.StatusCode);
    }

    private ApiFactory Create(bool enabled = true) =>
        FakeApi.Create(
            services =>
            {
                services.AddSingleton<IAttendanceStore>(_store);
                services.AddSingleton<TimeProvider>(new FixedClock(Now));
            },
            settings: new Dictionary<string, string?> { ["Demo:Enabled"] = enabled.ToString() });
}
