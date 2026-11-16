using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SlateAi.Api.Ai;
using SlateAi.Api.Data.Entities;
using SlateAi.Api.Data.Seed;
using SlateAi.Api.Features.Attendance;
using SlateAi.Api.Features.CatchUps;
using SlateAi.Api.Features.Demo;
using SlateAi.Api.Tests.Ai;
using SlateAi.Api.Tests.Features.Attendance;
using SlateAi.Api.Tests.Features.CatchUps;
using SlateAi.Api.Tests.Infrastructure;

namespace SlateAi.Api.Tests.Features.Demo;

/// <summary>
/// T-134, D-120: the demo teacher can act as if a past school day is today. The web sends <c>X-School-Date</c>; for the
/// demo class the request's clock is that day at the real IST time of day.
/// </summary>
public sealed class SchoolDateTests
{
    private static readonly CancellationToken Ct = TestContext.Current.CancellationToken;

    // 05:00 UTC on 16 Nov is 10:30 IST on 16 Nov.
    private static readonly DateTimeOffset RealNow = new(2026, 11, 16, 5, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly RealToday = new(2026, 11, 16);
    private static readonly DateOnly Wednesday = new(2026, 11, 12);
    private static readonly Guid DemoClassId =
        DemoSeedFile.LoadAsync("data/seed/demo-class.json", Ct).GetAwaiter().GetResult().Class.Id;

    private readonly SchoolDateClock _clock = new(new FixedClock(RealNow));

    [Fact]
    public void The_clock_gives_the_chosen_day_at_the_real_time_of_day_and_real_time_without_one()
    {
        Assert.Equal(RealNow, _clock.GetUtcNow());

        using (SchoolDate.Use(Wednesday))
        {
            Assert.Equal(SchoolClock.At(Wednesday, new TimeOnly(10, 30)), _clock.GetUtcNow());
            Assert.Equal(Wednesday, SchoolClock.Today(_clock));
            Assert.Equal(RealNow, SchoolClock.RealNow(_clock));
        }

        Assert.Equal(RealToday, SchoolClock.Today(_clock));
        Assert.Null(SchoolDate.Current);
    }

    [Theory]
    [InlineData("2026-11-12", "GET", "/api/classes/x/ledger", true, true, "2026-11-12")]
    [InlineData("2026-11-12", "POST", "/api/p/token/answers", true, true, "2026-11-12")]
    [InlineData("2026-11-12", "GET", "/api/admin/teachers", true, true, null)]
    [InlineData("2026-11-12", "POST", "/api/demo/reset", true, true, null)]
    [InlineData("2026-11-12", "GET", "/api/classes/x/ledger", false, true, null)]
    [InlineData("2026-11-12", "GET", "/api/classes/x/ledger", true, false, null)]
    [InlineData("2026-11-16", "GET", "/api/classes/x/ledger", true, true, null)]
    [InlineData("2026-11-17", "GET", "/api/classes/x/ledger", true, true, null)]
    [InlineData("2026-07-18", "GET", "/api/classes/x/ledger", true, true, null)]
    [InlineData("2026-07-19", "GET", "/api/classes/x/ledger", true, true, "2026-07-19")]
    [InlineData("12-11-2026", "GET", "/api/classes/x/ledger", true, true, null)]
    [InlineData("", "GET", "/api/classes/x/ledger", true, true, null)]
    public void Applies_only_to_the_demo_class_with_the_demo_on_for_a_past_day_within_120_days(
        string header, string method, string path, bool demoClass, bool demoEnabled, string? expected)
    {
        var date = SchoolDate.Resolve(header, method, path, demoClass, demoEnabled, switchOn: true, RealToday);

        Assert.Equal(expected is null ? null : DateOnly.Parse(expected), date);
    }

    [Fact]
    public void Nothing_applies_while_the_admin_switch_is_off()
    {
        Assert.Null(SchoolDate.Resolve("2026-11-12", "GET", "/api/classes/x/ledger", true, true, switchOn: false, RealToday));
    }

    [Fact]
    public async Task With_the_switch_off_the_demo_class_keeps_the_real_date()
    {
        var store = new FakeAttendanceStore(DemoClassId, Enumerable.Range(0, 3).Select(_ => Guid.NewGuid()).ToList());
        await using var factory = Create(store, switchOn: false);
        using var client = factory.CreateTeacherClient(DemoClassId);

        var response = await PostRegisterAsync(client, RealToday, "2026-11-12");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(response.Headers.Contains(SchoolDate.Header));
    }

    [Fact]
    public async Task For_the_demo_class_the_request_acts_as_the_chosen_day_and_says_so()
    {
        // Saving today's real register is "a future date" when the request acts as Wednesday: proof the clock moved.
        var store = new FakeAttendanceStore(DemoClassId, Enumerable.Range(0, 3).Select(_ => Guid.NewGuid()).ToList());
        await using var factory = Create(store);
        using var client = factory.CreateTeacherClient(DemoClassId);

        var asWednesday = await PostRegisterAsync(client, RealToday, "2026-11-12");
        var onWednesday = await PostRegisterAsync(client, Wednesday, "2026-11-12");
        var realDay = await PostRegisterAsync(client, RealToday, null);

        Assert.Equal(HttpStatusCode.BadRequest, asWednesday.StatusCode);
        Assert.Equal("2026-11-12", Assert.Single(asWednesday.Headers.GetValues(SchoolDate.Header)));
        Assert.Equal(HttpStatusCode.OK, onWednesday.StatusCode);
        Assert.Equal(HttpStatusCode.OK, realDay.StatusCode);
        Assert.False(realDay.Headers.Contains(SchoolDate.Header));
    }

    [Fact]
    public async Task Another_class_keeps_the_real_date()
    {
        var other = Guid.NewGuid();
        var store = new FakeAttendanceStore(other, Enumerable.Range(0, 3).Select(_ => Guid.NewGuid()).ToList());
        await using var factory = Create(store);
        using var client = factory.CreateTeacherClient(other);

        var response = await PostRegisterAsync(client, RealToday, "2026-11-12");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(response.Headers.Contains(SchoolDate.Header));
    }

    [Fact]
    public async Task The_AI_log_keeps_the_real_time_so_under_the_hood_still_lists_the_call()
    {
        var store = new FakeAiCallStore();
        var inner = new LoggedLlmClientTests.StubLlm(_ => new LlmResponse("{}", "m", new LlmUsage(1, 1, 0), Guid.Empty));
        var client = new LoggedLlmClient(inner, store, Options.Create(new AiOptions()), _clock);

        using (SchoolDate.Use(Wednesday))
        {
            await client.CompleteJsonAsync(
                new LlmRequest(new Prompt("p", 1, "Mapping", "t"), "x", JsonDocument.Parse("{}").RootElement), Ct);
        }

        Assert.Equal(RealNow, Assert.Single(store.Calls).CreatedAt);
    }

    [Fact]
    public async Task A_catch_up_sent_on_a_chosen_day_is_sent_that_day_but_its_link_lasts_14_real_days()
    {
        var classId = Guid.NewGuid();
        var store = new FakeCatchUpStore(classId);
        var package = new CatchUpPackage
        {
            Id = Guid.NewGuid(), ClassId = classId, TargetKey = "k", Language = "kn", Status = PackageStatus.Approved,
            ScriptVersion = 1, ApprovedScriptVersion = 1, Activity = "a", CheckItem = "c",
        };
        var child = Guid.NewGuid();
        store.Packages.Add(package);
        store.Families[child] = ("Ravi", "yes", false);
        store.Assignments.Add(new CatchUpAssignment
        {
            Id = Guid.NewGuid(), ClassId = classId, ChildId = child, PackageId = package.Id, CompressionRationale = "r",
        });
        var sender = new CatchUpSender(store, _clock, NullLogger<CatchUpSender>.Instance);

        using (SchoolDate.Use(Wednesday))
        {
            await sender.SendAsync(package.Id, Ct);
        }

        var assignment = Assert.Single(store.Assignments);
        Assert.Equal(Wednesday, SchoolClock.DateOf(assignment.SentAt!.Value));
        Assert.Equal(RealNow + CatchUpSender.LinkLife, assignment.ExpiresAt);
    }

    private ApiFactory Create(FakeAttendanceStore store, bool switchOn = true) => FakeApi.Create(services =>
    {
        services.AddSingleton<IAttendanceStore>(store);
        services.AddSingleton<TimeProvider>(_clock);
        services.AddSingleton<ISchoolDateSwitchStore>(new FakeSchoolDateSwitchStore(switchOn));
    });

    private static Task<HttpResponseMessage> PostRegisterAsync(HttpClient client, DateOnly date, string? schoolDate)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/classes/{client.DefaultRequestHeaders.GetValues(TestAuth.ClassHeader).Single()}/attendance")
        {
            Content = JsonContent.Create(new MarkAttendanceRequest(Guid.NewGuid(), date, [])),
        };
        if (schoolDate is not null)
        {
            request.Headers.Add(SchoolDate.Header, schoolDate);
        }

        return client.SendAsync(request, Ct);
    }
}
