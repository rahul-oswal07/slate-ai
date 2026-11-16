using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using SlateAi.Api.Features.Admin;
using SlateAi.Api.Tests.Infrastructure;

namespace SlateAi.Api.Tests.Features.Admin;

/// <summary>
/// The admin teachers page (T-133): each teacher's voice consent and greeting, and Clear consent, which deletes her
/// greeting (D-44) and sends her back to the first-run Welcome to record it again.
/// </summary>
public sealed class AdminTeacherEndpointTests
{
    private const string Url = "/api/admin/teachers";
    private static readonly CancellationToken Ct = TestContext.Current.CancellationToken;
    private static readonly Guid Shobha = Guid.NewGuid();
    private static readonly Guid Monika = Guid.NewGuid();
    private static readonly DateTimeOffset ConsentAt = new(2026, 11, 15, 4, 30, 0, TimeSpan.Zero);

    private readonly FakeAdminTeacherStore _store = new();

    public AdminTeacherEndpointTests()
    {
        _store.Rows.AddRange(
        [
            new AdminTeacherRow(Monika, "Monika", "Shobha", 3, null, null),
            new AdminTeacherRow(Shobha, "Shobha", "Shobha", 3, ConsentAt, 9.6),
        ]);
    }

    [Fact]
    public async Task Lists_each_teacher_with_her_class_consent_and_greeting()
    {
        await using var factory = Create();
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync(Url, Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
        Assert.Equal(
            [
                new AdminTeacher(Monika, "Monika", "Shobha · Grade 3", null, null),
                new AdminTeacher(Shobha, "Shobha", "Shobha · Grade 3", ConsentAt, 9.6),
            ],
            (await response.Content.ReadFromJsonAsync<List<AdminTeacher>>(Ct))!);
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    [InlineData("Development")]
    public async Task Opens_without_a_key_in_every_environment_and_is_not_indexed_D112(string environment)
    {
        await using var factory = Create(environment);
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync(Url, Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("noindex", Assert.Single(response.Headers.GetValues("X-Robots-Tag")));
    }

    [Fact]
    public async Task A_teacher_without_a_class_has_no_class_label()
    {
        _store.Rows.Add(new AdminTeacherRow(Guid.NewGuid(), "Asha", null, null, null, null));
        await using var factory = Create();
        using var client = factory.CreateHttpsClient();

        var teachers = (await client.GetFromJsonAsync<List<AdminTeacher>>(Url, Ct))!;

        Assert.Null(teachers.Single(t => t.Name == "Asha").ClassLabel);
    }

    [Fact]
    public async Task Clearing_consent_returns_her_without_consent_or_greeting()
    {
        await using var factory = Create();
        using var client = factory.CreateHttpsClient();

        var response = await client.PostAsJsonAsync($"{Url}/{Shobha}/clear-consent", new ClearConsentRequest(Guid.NewGuid()), Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
        Assert.Equal(
            new AdminTeacher(Shobha, "Shobha", "Shobha · Grade 3", null, null),
            await response.Content.ReadFromJsonAsync<AdminTeacher>(Ct));
        Assert.Equal(1, _store.Clears);
    }

    [Fact]
    public async Task A_replayed_request_clears_once()
    {
        await using var factory = Create();
        using var client = factory.CreateHttpsClient();
        var request = new ClearConsentRequest(Guid.NewGuid());

        await client.PostAsJsonAsync($"{Url}/{Shobha}/clear-consent", request, Ct);
        var replay = await client.PostAsJsonAsync($"{Url}/{Shobha}/clear-consent", request, Ct);

        Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        Assert.Equal(1, _store.Clears);
    }

    [Fact]
    public async Task An_unknown_teacher_is_404()
    {
        await using var factory = Create();
        using var client = factory.CreateHttpsClient();

        var response = await client.PostAsJsonAsync($"{Url}/{Guid.NewGuid()}/clear-consent", new ClearConsentRequest(Guid.NewGuid()), Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(0, _store.Clears);
    }

    [Fact]
    public async Task Clearing_without_a_client_request_id_is_400()
    {
        await using var factory = Create();
        using var client = factory.CreateHttpsClient();

        var response = await client.PostAsJsonAsync($"{Url}/{Shobha}/clear-consent", new ClearConsentRequest(Guid.Empty), Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, _store.Clears);
    }

    private ApiFactory Create(string environment = "Testing") =>
        FakeApi.Create(services => services.AddSingleton<IAdminTeacherStore>(_store), environment: environment);

    private sealed class FakeAdminTeacherStore : IAdminTeacherStore
    {
        private readonly HashSet<Guid> _applied = [];

        public List<AdminTeacherRow> Rows { get; } = [];

        /// <summary>How many times a clear was applied (not replayed).</summary>
        public int Clears { get; private set; }

        public Task<IReadOnlyList<AdminTeacherRow>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<AdminTeacherRow>>([.. Rows.OrderBy(r => r.Name)]);

        public Task<AdminTeacherRow?> ClearConsentAsync(
            Guid clientRequestId, Guid teacherId, DateTimeOffset now, CancellationToken cancellationToken)
        {
            var index = Rows.FindIndex(r => r.Id == teacherId);
            if (index < 0)
            {
                return Task.FromResult<AdminTeacherRow?>(null);
            }

            if (_applied.Add(clientRequestId))
            {
                Rows[index] = Rows[index] with { ConsentAt = null, GreetingSeconds = null };
                Clears++;
            }

            return Task.FromResult<AdminTeacherRow?>(Rows[index]);
        }
    }
}
