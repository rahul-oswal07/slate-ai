using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using SlateAi.Api.Features.Attendance;
using SlateAi.Api.Features.Ledger;
using SlateAi.Api.Features.Week;
using SlateAi.Api.Tests.Features.Buddy;
using SlateAi.Api.Tests.Infrastructure;

namespace SlateAi.Api.Tests.Features.Week;

public sealed class WeekEndpointTests
{
    private static readonly CancellationToken Ct = TestContext.Current.CancellationToken;
    private static readonly Guid ClassId = Guid.NewGuid();
    private static readonly DateOnly Mon = new(2026, 10, 19);
    private static readonly LessonRow Tue = new(Guid.NewGuid(), Mon.AddDays(1), ["M3.ADD.CARRY"]);
    private readonly FakeWeekStore _store = new();
    private readonly LedgerData _data;

    public WeekEndpointTests()
    {
        // 6 of 10 away on Tuesday (addition), all back Wednesday.
        var children = Enumerable.Range(1, 10).Select(i => new LedgerChildRow(Guid.NewGuid(), i, $"C{i}")).ToList();
        _data = new LedgerData(
            children,
            [.. children.Select((c, i) => new RegisterRow(c.Id, Tue.Date, i >= 6)), .. children.Select(c => new RegisterRow(c.Id, Mon.AddDays(2), true))],
            [Tue]);
    }

    private static string Url(DateOnly date) => $"/api/classes/{ClassId}/week/{date:yyyy-MM-dd}";

    [Fact]
    public async Task Any_day_of_the_week_returns_that_week_with_the_nudge()
    {
        await using var factory = Create();
        using var client = factory.CreateTeacherClient(ClassId);

        var week = (await client.GetFromJsonAsync<WeekResponse>(Url(Mon.AddDays(3)), Ct))!;

        Assert.Equal(Mon, week.From);
        Assert.Equal((6, 6, 6), (week.ThisWeek.OneDay, week.ThisWeek.BackWithinTwoDays, week.ThisWeek.Total));
        Assert.Equal((Tue.Id, 6, 10), (week.Nudge!.LessonId, week.Nudge.N, week.Nudge.Total));
        Assert.Equal(6, week.Reasons.NotGiven);
        Assert.Null(week.Decision);
    }

    [Fact]
    public async Task Plan_change_and_skip_are_stored_and_a_replay_changes_nothing()
    {
        await using var factory = Create();
        using var client = factory.CreateTeacherClient(ClassId);
        var plan = new PacingDecisionRequest(Guid.NewGuid(), "recap", Tue.Id);

        var planned = await (await client.PutAsJsonAsync($"{Url(Mon)}/pacing", plan, Ct)).Content.ReadFromJsonAsync<WeekResponse>(Ct);
        var skipped = await (await client.PutAsJsonAsync($"{Url(Mon)}/pacing", new PacingDecisionRequest(Guid.NewGuid(), "skip", Tue.Id), Ct))
            .Content.ReadFromJsonAsync<WeekResponse>(Ct);
        var replay = await (await client.PutAsJsonAsync($"{Url(Mon)}/pacing", plan, Ct)).Content.ReadFromJsonAsync<WeekResponse>(Ct);

        Assert.Equal("recap", planned!.Decision);
        Assert.Equal("skip", skipped!.Decision);
        Assert.Equal("skip", replay!.Decision);
    }

    [Fact]
    public async Task A_decision_for_another_lesson_is_409_and_bad_input_is_400()
    {
        await using var factory = Create();
        using var client = factory.CreateTeacherClient(ClassId);

        var wrong = await client.PutAsJsonAsync($"{Url(Mon)}/pacing", new PacingDecisionRequest(Guid.NewGuid(), "recap", Guid.NewGuid()), Ct);
        var badDecision = await client.PutAsJsonAsync($"{Url(Mon)}/pacing", new PacingDecisionRequest(Guid.NewGuid(), "later", Tue.Id), Ct);
        var noId = await client.PutAsJsonAsync($"{Url(Mon)}/pacing", new PacingDecisionRequest(Guid.Empty, "recap", Tue.Id), Ct);
        var future = await client.GetAsync(Url(Mon.AddDays(7)), Ct);

        Assert.Equal(HttpStatusCode.Conflict, wrong.StatusCode);
        Assert.NotNull((await wrong.Content.ReadFromJsonAsync<WeekResponse>(Ct))!.Nudge); // the latest week to show
        Assert.Equal(HttpStatusCode.BadRequest, badDecision.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, noId.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, future.StatusCode);
        Assert.Empty(_store.Decisions);
    }

    [Fact]
    public async Task Another_teacher_or_signed_out_gets_nothing()
    {
        await using var factory = Create();
        using var other = factory.CreateTeacherClient(Guid.NewGuid());
        using var anonymous = factory.CreateHttpsClient();

        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync(Url(Mon), Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(Url(Mon), Ct)).StatusCode);
    }

    private ApiFactory Create() =>
        FakeApi.Create(services =>
        {
            services.AddSingleton<ILedgerStore>(new FakeLedger(ClassId, _data));
            services.AddSingleton<IWeekStore>(_store);
            services.AddSingleton(BuddyCardBuilderTests.Book);
            services.AddSingleton<TimeProvider>(new FixedClock(SchoolClock.At(Mon.AddDays(4), new TimeOnly(15, 0))));
        });

    private sealed class FakeLedger(Guid classId, LedgerData data) : ILedgerStore
    {
        public Task<LedgerData?> LoadAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(id == classId ? data : null);
    }

    private sealed class FakeWeekStore : IWeekStore
    {
        private readonly HashSet<Guid> _processed = [];

        public Dictionary<(Guid ClassId, DateOnly WeekOf), DecisionRow> Decisions { get; } = [];

        public Task<DecisionRow?> GetDecisionAsync(Guid classId, DateOnly weekOf, CancellationToken cancellationToken) =>
            Task.FromResult(Decisions.GetValueOrDefault((classId, weekOf)));

        public Task SaveDecisionAsync(
            Guid classId, DateOnly weekOf, DecisionRow decision, Guid clientRequestId, DateTimeOffset now, CancellationToken cancellationToken)
        {
            if (_processed.Add(clientRequestId))
            {
                Decisions[(classId, weekOf)] = decision;
            }

            return Task.CompletedTask;
        }
    }
}
