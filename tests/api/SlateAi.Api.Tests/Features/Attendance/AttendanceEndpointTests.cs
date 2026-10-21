using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using SlateAi.Api.Features.Attendance;
using SlateAi.Api.Tests.Infrastructure;

namespace SlateAi.Api.Tests.Features.Attendance;

public sealed class AttendanceEndpointTests
{
    private static readonly Guid ClassId = Guid.NewGuid();
    private static readonly Guid[] Children = Enumerable.Range(0, 36).Select(_ => Guid.NewGuid()).ToArray();

    // 19:00 UTC on 18 Oct is 00:30 IST on 19 Oct, so "today" is the 19th.
    private static readonly FixedClock Clock = new(new DateTimeOffset(2026, 10, 18, 19, 0, 0, TimeSpan.Zero));
    private static readonly DateOnly TodayIst = new(2026, 10, 19);

    private readonly FakeAttendanceStore _store = new(ClassId, Children);

    [Fact]
    public async Task Saves_the_register_with_listed_children_absent()
    {
        var response = await PostAsync(ClassId, Guid.NewGuid(), TodayIst, Children[..4]);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(new MarkAttendanceResponse(TodayIst, 32, 4),
            await response.Content.ReadFromJsonAsync<MarkAttendanceResponse>(TestContext.Current.CancellationToken));
        var saved = Assert.Single(_store.Saved);
        Assert.Equal(Children.Length, saved.ChildIds.Count);
        Assert.True(saved.Absent.SetEquals(Children[..4]));
    }

    [Fact]
    public async Task Duplicate_absent_ids_count_once()
    {
        await PostAsync(ClassId, Guid.NewGuid(), TodayIst, [Children[0], Children[0]]);

        Assert.Single(Assert.Single(_store.Saved).Absent);
    }

    [Fact]
    public async Task Passes_the_client_request_id_through_for_idempotency()
    {
        var requestId = Guid.NewGuid();

        await PostAsync(ClassId, requestId, TodayIst, []);

        Assert.Equal(requestId, Assert.Single(_store.Saved).ClientRequestId);
    }

    [Fact]
    public async Task Rejects_a_child_from_another_class_without_saving()
    {
        var response = await PostAsync(ClassId, Guid.NewGuid(), TodayIst, [Guid.NewGuid()]);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(_store.Saved);
    }

    [Fact]
    public async Task Uses_the_india_date_for_today_and_rejects_tomorrow()
    {
        Assert.Equal(HttpStatusCode.OK, (await PostAsync(ClassId, Guid.NewGuid(), TodayIst, [])).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await PostAsync(ClassId, Guid.NewGuid(), TodayIst.AddDays(1), [])).StatusCode);
    }

    [Fact]
    public async Task Rejects_a_missing_client_request_id()
    {
        var response = await PostAsync(ClassId, Guid.Empty, TodayIst, []);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(_store.Saved);
    }

    [Fact]
    public async Task Unknown_class_is_404()
    {
        var response = await PostAsync(Guid.NewGuid(), Guid.NewGuid(), TodayIst, []);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Requires_a_signed_in_teacher()
    {
        var response = await PostAsync(ClassId, Guid.NewGuid(), TodayIst, [], signedInClassId: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Empty(_store.Saved);
    }

    [Fact]
    public async Task Another_teachers_class_is_404()
    {
        var response = await PostAsync(ClassId, Guid.NewGuid(), TodayIst, [], signedInClassId: Guid.NewGuid());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Empty(_store.Saved);
    }

    private Task<HttpResponseMessage> PostAsync(Guid classId, Guid requestId, DateOnly date, Guid[] absent) =>
        PostAsync(classId, requestId, date, absent, signedInClassId: classId);

    private async Task<HttpResponseMessage> PostAsync(
        Guid classId, Guid requestId, DateOnly date, Guid[] absent, Guid? signedInClassId)
    {
        await using var factory = FakeApi.Create(services =>
        {
            services.AddSingleton<IAttendanceStore>(_store);
            services.AddSingleton<TimeProvider>(Clock);
        });
        using var client = signedInClassId is { } id ? factory.CreateTeacherClient(id) : factory.CreateHttpsClient();
        return await client.PostAsJsonAsync(
            $"/api/classes/{classId}/attendance",
            new MarkAttendanceRequest(requestId, date, absent),
            TestContext.Current.CancellationToken);
    }

    private sealed record SaveCall(Guid ClientRequestId, IReadOnlyCollection<Guid> ChildIds, IReadOnlySet<Guid> Absent);

    private sealed class FakeAttendanceStore(Guid classId, IReadOnlyList<Guid> children) : IAttendanceStore
    {
        public List<SaveCall> Saved { get; } = [];

        public Task<IReadOnlyList<Guid>?> GetChildIdsAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(id == classId ? children : null);

        public Task<MarkAttendanceResponse> SaveRegisterAsync(
            Guid id,
            Guid clientRequestId,
            DateOnly date,
            IReadOnlyCollection<Guid> childIds,
            IReadOnlySet<Guid> absent,
            CancellationToken cancellationToken)
        {
            Saved.Add(new SaveCall(clientRequestId, childIds, absent));
            return Task.FromResult(new MarkAttendanceResponse(date, childIds.Count - absent.Count, absent.Count));
        }
    }
}
