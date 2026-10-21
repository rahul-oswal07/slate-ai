using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using SlateAi.Api.Features.Ledger;
using SlateAi.Api.Tests.Infrastructure;

namespace SlateAi.Api.Tests.Features.Ledger;

public sealed class LedgerEndpointTests
{
    private static readonly Guid ClassId = Guid.NewGuid();

    [Fact]
    public async Task Returns_the_computed_ledger_for_the_class()
    {
        var child = new LedgerChildRow(Guid.NewGuid(), 1, "A");
        var day = new DateOnly(2026, 10, 12);
        var data = new LedgerData([child], [new RegisterRow(child.Id, day, Present: false)], []);

        var response = await GetAsync(ClassId, data);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var ledger = await response.Content.ReadFromJsonAsync<LedgerResponse>(TestContext.Current.CancellationToken);
        Assert.Equal(ClassId, ledger!.ClassId);
        Assert.Equal(1, Assert.Single(ledger.Children).CurrentAbsenceDays);
    }

    [Fact]
    public async Task Unknown_class_is_404()
    {
        var response = await GetAsync(Guid.NewGuid(), new LedgerData([], [], []));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Requires_a_signed_in_teacher()
    {
        var response = await GetAsync(ClassId, new LedgerData([], [], []), signedInClassId: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Another_teachers_class_is_404()
    {
        var response = await GetAsync(ClassId, new LedgerData([], [], []), signedInClassId: Guid.NewGuid());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static Task<HttpResponseMessage> GetAsync(Guid classId, LedgerData data) =>
        GetAsync(classId, data, signedInClassId: classId);

    private static async Task<HttpResponseMessage> GetAsync(Guid classId, LedgerData data, Guid? signedInClassId)
    {
        await using var factory = FakeApi.Create(services =>
            services.AddSingleton<ILedgerStore>(new FakeLedgerStore(ClassId, data)));
        using var client = signedInClassId is { } id ? factory.CreateTeacherClient(id) : factory.CreateHttpsClient();
        return await client.GetAsync($"/api/classes/{classId}/ledger", TestContext.Current.CancellationToken);
    }

    private sealed class FakeLedgerStore(Guid classId, LedgerData data) : ILedgerStore
    {
        public Task<LedgerData?> LoadAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(id == classId ? data : null);
    }
}
