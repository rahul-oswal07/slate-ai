using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using SlateAi.Api.Curriculum;
using SlateAi.Api.Features.Attendance;
using SlateAi.Api.Features.Buddy;
using SlateAi.Api.Features.Ledger;
using SlateAi.Api.Tests.Infrastructure;

namespace SlateAi.Api.Tests.Features.Buddy;

public sealed class BuddyEndpointTests
{
    private static readonly CancellationToken Ct = TestContext.Current.CancellationToken;
    private static readonly Guid ClassId = Guid.NewGuid();
    private static readonly DateOnly Wed = new(2026, 10, 21);
    private static readonly DateOnly Thu = Wed.AddDays(1);
    private static readonly LedgerChildRow Ravi = new(Guid.NewGuid(), 4, "Ravi");
    private static readonly LedgerChildRow Asha = new(Guid.NewGuid(), 1, "Asha");
    private readonly FakeBuddyStore _store = new();
    private readonly LedgerData _data = new(
        [Ravi, Asha],
        [new RegisterRow(Ravi.Id, Wed, false), new RegisterRow(Asha.Id, Wed, true), new RegisterRow(Ravi.Id, Thu, true), new RegisterRow(Asha.Id, Thu, true)],
        [new LessonRow(Guid.NewGuid(), Wed, ["M3.SUB.BORROW"])]);

    private string Url => $"/api/classes/{ClassId}/buddy-cards/{Thu:yyyy-MM-dd}";

    [Fact]
    public async Task Today_has_a_card_for_the_child_who_came_back()
    {
        await using var factory = Create();
        using var client = factory.CreateTeacherClient(ClassId);

        var response = (await client.GetFromJsonAsync<BuddyCardsResponse>(Url, Ct))!;

        var card = Assert.Single(response.Cards);
        Assert.Equal(Thu, response.Date);
        Assert.Equal(("Ravi", 1, "Asha", 1), (card.FirstName, card.DaysAway, card.Buddy!.FirstName, card.Buddy.PresentDays));
        Assert.Equal(3, card.Sums.Count);
        Assert.Equal(new TextbookPage("textbook", 2, 15), card.Homework);
    }

    [Fact]
    public async Task Ticks_save_undo_and_a_replay_changes_nothing()
    {
        await using var factory = Create();
        using var client = factory.CreateTeacherClient(ClassId);
        var homework = new SaveBuddyTicksRequest(Guid.NewGuid(), HomeworkReturned: true, Done: null);

        var ticked = await (await client.PutAsJsonAsync($"{Url}/{Ravi.Id}", homework, Ct)).Content.ReadFromJsonAsync<BuddyCardView>(Ct);
        var done = await (await client.PutAsJsonAsync($"{Url}/{Ravi.Id}", new SaveBuddyTicksRequest(Guid.NewGuid(), null, true), Ct))
            .Content.ReadFromJsonAsync<BuddyCardView>(Ct);
        var undo = await (await client.PutAsJsonAsync($"{Url}/{Ravi.Id}", new SaveBuddyTicksRequest(Guid.NewGuid(), false, false), Ct))
            .Content.ReadFromJsonAsync<BuddyCardView>(Ct);
        var replay = await (await client.PutAsJsonAsync($"{Url}/{Ravi.Id}", homework, Ct)).Content.ReadFromJsonAsync<BuddyCardView>(Ct);

        Assert.True(ticked!.HomeworkReturned);
        Assert.False(ticked.Done);
        Assert.True(done!.HomeworkReturned && done.Done);
        Assert.False(undo!.HomeworkReturned || undo.Done);
        Assert.False(replay!.HomeworkReturned); // the earlier request again: no effect
    }

    [Fact]
    public async Task A_child_who_did_not_come_back_is_404_and_bad_requests_are_400()
    {
        await using var factory = Create();
        using var client = factory.CreateTeacherClient(ClassId);

        var notBack = await client.PutAsJsonAsync($"{Url}/{Asha.Id}", new SaveBuddyTicksRequest(Guid.NewGuid(), true, null), Ct);
        var noRequestId = await client.PutAsJsonAsync($"{Url}/{Ravi.Id}", new SaveBuddyTicksRequest(Guid.Empty, true, null), Ct);
        var future = await client.GetAsync($"/api/classes/{ClassId}/buddy-cards/{Thu.AddDays(1):yyyy-MM-dd}", Ct);

        Assert.Equal(HttpStatusCode.NotFound, notBack.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, noRequestId.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, future.StatusCode);
        Assert.Empty(_store.Ticks);
    }

    [Fact]
    public async Task Another_teacher_or_signed_out_gets_nothing()
    {
        await using var factory = Create();
        using var other = factory.CreateTeacherClient(Guid.NewGuid());
        using var anonymous = factory.CreateHttpsClient();

        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync(Url, Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(Url, Ct)).StatusCode);
    }

    private ApiFactory Create() =>
        FakeApi.Create(services =>
        {
            services.AddSingleton<ILedgerStore>(new FakeLedger(ClassId, _data));
            services.AddSingleton<IBuddyStore>(_store);
            services.AddSingleton(BuddyCardBuilderTests.Book);
            services.AddSingleton<TimeProvider>(new FixedClock(SchoolClock.At(Thu, new TimeOnly(11, 0))));
        });

    private sealed class FakeLedger(Guid classId, LedgerData data) : ILedgerStore
    {
        public Task<LedgerData?> LoadAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(id == classId ? data : null);
    }

    private sealed class FakeBuddyStore : IBuddyStore
    {
        private readonly HashSet<Guid> _processed = [];

        public Dictionary<(Guid ChildId, DateOnly Date), BuddyTicks> Ticks { get; } = [];

        public Task<IReadOnlyList<CatchUpTargets>> GetTargetsAsync(Guid classId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<CatchUpTargets>>([]);

        public Task<IReadOnlyList<BuddyTicks>> GetTicksAsync(Guid classId, DateOnly date, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<BuddyTicks>>(Ticks.Where(t => t.Key.Date == date).Select(t => t.Value).ToList());

        public Task SaveTicksAsync(
            Guid classId, Guid childId, DateOnly date, BuddyTickChange change, Guid clientRequestId, DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            if (_processed.Add(clientRequestId))
            {
                var current = Ticks.GetValueOrDefault((childId, date)) ?? new BuddyTicks(childId, false, false);
                Ticks[(childId, date)] = current with
                {
                    HomeworkReturned = change.HomeworkReturned ?? current.HomeworkReturned,
                    Done = change.Done ?? current.Done,
                };
            }

            return Task.CompletedTask;
        }
    }
}
