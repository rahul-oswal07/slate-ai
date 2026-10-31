using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using SlateAi.Api.Features.Attendance;
using SlateAi.Api.Features.Attendance.Source;
using SlateAi.Api.Tests.Infrastructure;

namespace SlateAi.Api.Tests.Features.Attendance;

public sealed class AttendanceDayEndpointTests
{
    private static readonly Guid ClassId = Guid.NewGuid();
    private static readonly Guid[] Children = Enumerable.Range(0, 36).Select(_ => Guid.NewGuid()).ToArray();
    private static readonly DateOnly Today = new(2026, 10, 21);
    private static readonly CancellationToken Ct = TestContext.Current.CancellationToken;

    private readonly MovableClock _clock = new() { Now = Ist(12, 30) };
    private readonly FakeAttendanceStore _store;
    private readonly FakeSource _source = new();

    public AttendanceDayEndpointTests() => _store = new FakeAttendanceStore(ClassId, Children, _clock);

    [Fact]
    public async Task Before_noon_it_is_waiting_and_names_both_sync_times()
    {
        _clock.Now = Ist(11, 0);

        var day = await GetDayAsync();

        Assert.Equal("waiting", day.Status);
        Assert.Equal(new TimeOnly(12, 0), day.SyncAt);
        Assert.Equal(new TimeOnly(14, 0), day.RetryAt);
        Assert.Equal(0, _source.Calls);
        Assert.Equal(36, day.Children.Count);
    }

    [Fact]
    public async Task After_noon_the_sats_register_is_fetched_once_and_stored()
    {
        _source.Result = new SourceResult.Arrived(new HashSet<Guid> { Children[3], Children[9] }, Ist(12, 0));

        var first = await GetDayAsync();
        var second = await GetDayAsync();

        Assert.Equal("sats", first.Status);
        Assert.Equal(Ist(12, 0), first.ReceivedAt);
        Assert.Equivalent(new[] { Children[3], Children[9] }, first.AbsentChildIds);
        Assert.False(first.Corrected);
        Assert.Equal(first.AbsentChildIds, second.AbsentChildIds);
        Assert.Equal(1, _store.SourceSaves);
    }

    [Fact]
    public async Task Still_nothing_after_the_2_pm_retry_is_missing_and_nobody_is_marked_absent()
    {
        _clock.Now = Ist(14, 15);
        _source.Result = new SourceResult.NotYet();

        var day = await GetDayAsync();

        Assert.Equal("missing", day.Status);
        Assert.Empty(day.AbsentChildIds);
        Assert.Null(day.SyncAt);
        Assert.Empty(_store.Day(Today));
    }

    [Fact]
    public async Task Late_between_noon_and_2_pm_is_still_waiting()
    {
        _source.Result = new SourceResult.NotYet();

        Assert.Equal("waiting", (await GetDayAsync()).Status);
    }

    [Fact]
    public async Task No_school_is_its_own_status()
    {
        _source.Result = new SourceResult.NoSchool();

        var day = await GetDayAsync();

        Assert.Equal("noSchool", day.Status);
        Assert.Null(day.RetryAt);
    }

    [Theory]
    [InlineData("2026-10-18", true)] // Sunday
    [InlineData("2026-10-10", true)] // second Saturday
    [InlineData("2026-10-17", false)] // an ordinary Saturday is a school day
    [InlineData("2026-10-21", false)] // Wednesday
    public async Task The_demo_class_is_told_when_the_school_is_usually_shut(string date, bool dayOff)
    {
        _source.Result = new SourceResult.Arrived(new HashSet<Guid> { Children[3] }, Ist(12, 0));
        await using var factory = Create(sats: o => o.EveryDayClassIds = [ClassId]);
        using var client = factory.CreateTeacherClient(ClassId);

        var day = await client.GetFromJsonAsync<AttendanceDayResponse>($"/api/classes/{ClassId}/attendance/{date}", Ct);

        Assert.Equal(dayOff, day!.DemoDayOff);
    }

    [Fact]
    public async Task Other_classes_are_never_told_it_is_a_demo_day_off()
    {
        _source.Result = new SourceResult.NoSchool();

        var day = await GetDayAsync(new DateOnly(2026, 10, 18));

        Assert.Equal("noSchool", day.Status);
        Assert.False(day.DemoDayOff);
    }

    [Fact]
    public async Task A_correction_shows_as_corrected()
    {
        _source.Result = new SourceResult.Arrived(new HashSet<Guid> { Children[3] }, Ist(12, 0));
        await GetDayAsync();

        await PostAsync([Children[4]]);
        var day = await GetDayAsync();

        Assert.Equal("sats", day.Status);
        Assert.True(day.Corrected);
        Assert.Equal([Children[4]], day.AbsentChildIds);
    }

    [Fact]
    public async Task Late_sats_after_manual_marking_keeps_hers_and_flags_the_difference()
    {
        _clock.Now = Ist(11, 0);
        await PostAsync([Children[0]]);
        _clock.Now = Ist(14, 0);
        _source.Result = new SourceResult.Arrived(new HashSet<Guid> { Children[1] }, Ist(12, 0));

        var day = await GetDayAsync();

        Assert.Equal("manual", day.Status);
        Assert.Equal([Children[0]], day.AbsentChildIds);
        Assert.True(day.SatsDiffers);
        Assert.Equal(Ist(12, 0), day.ReceivedAt);
    }

    [Fact]
    public async Task Late_sats_that_agrees_with_her_is_not_flagged()
    {
        _clock.Now = Ist(11, 0);
        await PostAsync([Children[0]]);
        _clock.Now = Ist(14, 0);
        _source.Result = new SourceResult.Arrived(new HashSet<Guid> { Children[0] }, Ist(12, 0));

        Assert.False((await GetDayAsync()).SatsDiffers);
    }

    [Fact]
    public async Task A_held_date_stays_waiting()
    {
        await using var factory = Create(holds => holds.Hold(ClassId, Today));
        using var client = factory.CreateTeacherClient(ClassId);

        var day = await client.GetFromJsonAsync<AttendanceDayResponse>($"/api/classes/{ClassId}/attendance/{Today:yyyy-MM-dd}", Ct);

        Assert.Equal("waiting", day!.Status);
        Assert.Equal(0, _source.Calls);
    }

    [Fact]
    public async Task Future_date_is_400_and_another_class_is_404()
    {
        await using var factory = Create();
        using var client = factory.CreateTeacherClient(ClassId);
        using var other = factory.CreateTeacherClient(Guid.NewGuid());

        var future = await client.GetAsync($"/api/classes/{ClassId}/attendance/{Today.AddDays(1):yyyy-MM-dd}", Ct);
        var notHers = await other.GetAsync($"/api/classes/{ClassId}/attendance/{Today:yyyy-MM-dd}", Ct);

        Assert.Equal(HttpStatusCode.BadRequest, future.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, notHers.StatusCode);
    }

    private static DateTimeOffset Ist(int hour, int minute) => SchoolClock.At(Today, new TimeOnly(hour, minute));

    private ApiFactory Create(Action<SatsDemoHolds>? holds = null, Action<SatsOptions>? sats = null) =>
        FakeApi.Create(services =>
        {
            if (sats is not null)
            {
                services.Configure(sats);
            }

            var demoHolds = new SatsDemoHolds();
            holds?.Invoke(demoHolds);
            services.AddSingleton<IAttendanceStore>(_store);
            services.AddSingleton<IAttendanceSource>(_source);
            services.AddSingleton(demoHolds);
            services.AddSingleton<TimeProvider>(_clock);
        });

    private async Task<AttendanceDayResponse> GetDayAsync(DateOnly? date = null)
    {
        await using var factory = Create();
        using var client = factory.CreateTeacherClient(ClassId);
        var day = await client.GetFromJsonAsync<AttendanceDayResponse>(
            $"/api/classes/{ClassId}/attendance/{date ?? Today:yyyy-MM-dd}", Ct);
        return day!;
    }

    private async Task PostAsync(Guid[] absent)
    {
        await using var factory = Create();
        using var client = factory.CreateTeacherClient(ClassId);
        var response = await client.PostAsJsonAsync(
            $"/api/classes/{ClassId}/attendance", new MarkAttendanceRequest(Guid.NewGuid(), Today, absent), Ct);
        response.EnsureSuccessStatusCode();
    }

    private sealed class FakeSource : IAttendanceSource
    {
        public SourceResult Result { get; set; } = new SourceResult.NotYet();
        public int Calls { get; private set; }

        public Task<SourceResult> FetchAsync(
            Guid classId, DateOnly date, IReadOnlyList<RosterChild> roster, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(Result);
        }
    }
}
