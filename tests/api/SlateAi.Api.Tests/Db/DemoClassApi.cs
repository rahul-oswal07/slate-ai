using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SlateAi.Api.Data;
using SlateAi.Api.Features.Attendance;
using SlateAi.Api.Tests.Infrastructure;

namespace SlateAi.Api.Tests.Db;

/// <summary>The real API on a fresh migrated and seeded database, plus helpers for the demo class.</summary>
public sealed class DemoClassApi : IAsyncDisposable
{
    private readonly ApiFactory _factory;

    private DemoClassApi(ApiFactory factory, Guid classId, IReadOnlyList<Guid> childIds)
    {
        _factory = factory;
        Client = factory.CreateTeacherClient(classId);
        ClassId = classId;
        ChildIds = childIds;
    }

    public HttpClient Client { get; }
    public Guid ClassId { get; }

    /// <summary>Demo children ordered by roll number.</summary>
    public IReadOnlyList<Guid> ChildIds { get; }

    public static CancellationToken Ct => TestContext.Current.CancellationToken;

    public static async Task<DemoClassApi> StartAsync(PostgresFixture postgres)
    {
        var factory = new ApiFactory(await postgres.CreateEmptyDatabaseAsync(Ct));
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var classId = await db.Classes.Select(c => c.Id).SingleAsync(Ct);
        var childIds = await db.Children.OrderBy(c => c.RollNo).Select(c => c.Id).ToListAsync(Ct);
        return new DemoClassApi(factory, classId, childIds);
    }

    public Task<HttpResponseMessage> MarkAsync(Guid clientRequestId, DateOnly date, params Guid[] absent) =>
        Client.PostAsJsonAsync(
            $"/api/classes/{ClassId}/attendance", new MarkAttendanceRequest(clientRequestId, date, absent), Ct);

    public async Task<T> QueryAsync<T>(Func<AppDbContext, Task<T>> query)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        return await query(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    /// <summary>Runs <paramref name="work"/> with a service from a fresh scope (e.g. a store).</summary>
    public async Task ScopedAsync<TService>(Func<TService, Task> work)
        where TService : notnull
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        await work(scope.ServiceProvider.GetRequiredService<TService>());
    }

    public async Task<TResult> ScopedAsync<TService, TResult>(Func<TService, Task<TResult>> work)
        where TService : notnull
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        return await work(scope.ServiceProvider.GetRequiredService<TService>());
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await _factory.DisposeAsync();
    }
}
