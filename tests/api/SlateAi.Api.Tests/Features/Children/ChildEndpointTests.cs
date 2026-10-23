using System.Net;
using Microsoft.Extensions.DependencyInjection;
using SlateAi.Api.Features.Children;
using SlateAi.Api.Tests.Infrastructure;

namespace SlateAi.Api.Tests.Features.Children;

public sealed class ChildEndpointTests
{
    private static readonly CancellationToken Ct = TestContext.Current.CancellationToken;
    private static readonly Guid ClassId = Guid.NewGuid();
    private static readonly Guid Ravi = Guid.NewGuid();
    private readonly FakeChildStore _store = new();

    public ChildEndpointTests() => _store.Children.Add((ClassId, Ravi));

    [Fact]
    public async Task Deleting_removes_the_child_and_a_replay_is_204_again()
    {
        await using var factory = Create();
        using var client = factory.CreateTeacherClient(ClassId);
        var url = $"/api/classes/{ClassId}/children/{Ravi}?clientRequestId={Guid.NewGuid()}";

        var first = await client.DeleteAsync(url, Ct);
        var replay = await client.DeleteAsync(url, Ct);
        var again = await client.DeleteAsync($"/api/classes/{ClassId}/children/{Ravi}?clientRequestId={Guid.NewGuid()}", Ct);

        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, replay.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, again.StatusCode); // already gone, new request
        Assert.Empty(_store.Children);
    }

    [Fact]
    public async Task Without_a_request_id_is_400_and_nothing_is_deleted()
    {
        await using var factory = Create();
        using var client = factory.CreateTeacherClient(ClassId);

        var missing = await client.DeleteAsync($"/api/classes/{ClassId}/children/{Ravi}", Ct);
        var empty = await client.DeleteAsync($"/api/classes/{ClassId}/children/{Ravi}?clientRequestId={Guid.Empty}", Ct);

        Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, empty.StatusCode);
        Assert.Single(_store.Children);
    }

    [Fact]
    public async Task Another_teacher_or_signed_out_cannot_delete()
    {
        await using var factory = Create();
        using var other = factory.CreateTeacherClient(Guid.NewGuid());
        using var anonymous = factory.CreateHttpsClient();
        var url = $"/api/classes/{ClassId}/children/{Ravi}?clientRequestId={Guid.NewGuid()}";

        Assert.Equal(HttpStatusCode.NotFound, (await other.DeleteAsync(url, Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.DeleteAsync(url, Ct)).StatusCode);
        Assert.Single(_store.Children);
    }

    [Fact]
    public async Task A_child_of_another_class_is_404()
    {
        var elsewhere = Guid.NewGuid();
        _store.Children.Add((Guid.NewGuid(), elsewhere));
        await using var factory = Create();
        using var client = factory.CreateTeacherClient(ClassId);

        var response = await client.DeleteAsync($"/api/classes/{ClassId}/children/{elsewhere}?clientRequestId={Guid.NewGuid()}", Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(2, _store.Children.Count);
    }

    private ApiFactory Create() => FakeApi.Create(services => services.AddSingleton<IChildStore>(_store));

    private sealed class FakeChildStore : IChildStore
    {
        private readonly HashSet<Guid> _processed = [];

        public List<(Guid ClassId, Guid ChildId)> Children { get; } = [];

        public Task<ChildDelete> DeleteAsync(Guid classId, Guid childId, Guid clientRequestId, CancellationToken cancellationToken)
        {
            if (_processed.Contains(clientRequestId))
            {
                return Task.FromResult(ChildDelete.Replay);
            }

            if (Children.RemoveAll(c => c == (classId, childId)) == 0)
            {
                return Task.FromResult(ChildDelete.NotFound);
            }

            _processed.Add(clientRequestId);
            return Task.FromResult(ChildDelete.Deleted);
        }
    }
}
