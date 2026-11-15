using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using SlateAi.Api.Features.Admin;
using SlateAi.Api.Tests.Infrastructure;

namespace SlateAi.Api.Tests.Features.Admin;

/// <summary>The admin children page: who is in each class and the number their family's WhatsApp goes to (T-118, D-110).</summary>
public sealed class AdminChildEndpointTests
{
    private const string Key = "a-test-admin-key-of-32-characters";
    private const string Url = "/api/admin/children";
    private static readonly CancellationToken Ct = TestContext.Current.CancellationToken;
    private static readonly Guid ClassId = Guid.NewGuid();
    private static readonly Guid Manjunath = Guid.NewGuid();
    private static readonly Guid Ravi = Guid.NewGuid();
    private static readonly Guid Ananya = Guid.NewGuid();

    private readonly FakeAdminChildStore _store = new();

    public AdminChildEndpointTests()
    {
        _store.Rows.AddRange(
        [
            new AdminChildRow(Ananya, 1, "Ananya", "kn", "no", false, null),
            new AdminChildRow(Manjunath, 15, "Manjunath", "kn", "yes", false, "+919876543210"),
            new AdminChildRow(Ravi, 24, "Ravi", "te", "yes", true, null),
        ]);
    }

    [Fact]
    public async Task Lists_each_class_with_its_children_and_where_each_number_comes_from()
    {
        await using var factory = Create(Key, recipients: new() { [Ravi.ToString("N")] = "+919812345678" });
        using var client = Client(factory, Key);

        var response = await client.GetAsync(Url, Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
        var cls = Assert.Single((await response.Content.ReadFromJsonAsync<AdminChildrenResponse>(Ct))!.Classes);
        Assert.Equal("Shobha · Grade 3", cls.Label);
        Assert.Equal(
            [
                new AdminChild(Ananya, 1, "Ananya", "kn", "no", false, null, null),
                new AdminChild(Manjunath, 15, "Manjunath", "kn", "yes", false, "+919876543210", PhoneFrom.Saved),
                new AdminChild(Ravi, 24, "Ravi", "te", "yes", true, "+919812345678", PhoneFrom.Setting),
            ],
            cls.Children);
    }

    [Fact]
    public async Task A_saved_number_comes_before_the_server_setting()
    {
        await using var factory = Create(Key, recipients: new() { [Manjunath.ToString()] = "+919800000000" });
        using var client = Client(factory, Key);

        var cls = Assert.Single((await client.GetFromJsonAsync<AdminChildrenResponse>(Url, Ct))!.Classes);

        var manjunath = cls.Children.Single(c => c.Id == Manjunath);
        Assert.Equal(("+919876543210", PhoneFrom.Saved), (manjunath.Phone, manjunath.PhoneFrom));
    }

    [Fact]
    public async Task Saving_stores_the_number_as_E164_and_returns_the_child()
    {
        await using var factory = Create(Key);
        using var client = Client(factory, Key);

        var response = await client.PutAsJsonAsync($"{Url}/{Ananya}/phone", new SetParentPhoneRequest(Guid.NewGuid(), "98450 12345"), Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var child = (await response.Content.ReadFromJsonAsync<AdminChild>(Ct))!;
        Assert.Equal(("+919845012345", PhoneFrom.Saved), (child.Phone, child.PhoneFrom));
        Assert.Equal("+919845012345", _store.Rows.Single(r => r.Id == Ananya).SavedPhone);
    }

    [Fact]
    public async Task Clearing_falls_back_to_the_server_setting()
    {
        await using var factory = Create(Key, recipients: new() { [Manjunath.ToString("N")] = "+919800000000" });
        using var client = Client(factory, Key);

        var response = await client.PutAsJsonAsync($"{Url}/{Manjunath}/phone", new SetParentPhoneRequest(Guid.NewGuid(), " "), Ct);

        var child = (await response.Content.ReadFromJsonAsync<AdminChild>(Ct))!;
        Assert.Equal(("+919800000000", PhoneFrom.Setting), (child.Phone, child.PhoneFrom));
        Assert.Null(_store.Rows.Single(r => r.Id == Manjunath).SavedPhone);
    }

    [Fact]
    public async Task A_replayed_save_changes_nothing()
    {
        await using var factory = Create(Key);
        using var client = Client(factory, Key);
        var requestId = Guid.NewGuid();

        await client.PutAsJsonAsync($"{Url}/{Ananya}/phone", new SetParentPhoneRequest(requestId, "9845012345"), Ct);
        await client.PutAsJsonAsync($"{Url}/{Ananya}/phone", new SetParentPhoneRequest(requestId, "9000000009"), Ct);

        Assert.Equal("+919845012345", _store.Rows.Single(r => r.Id == Ananya).SavedPhone);
    }

    [Theory]
    [InlineData("12345", "phone")]
    [InlineData("5876543210", "phone")]
    public async Task An_invalid_number_is_400_and_nothing_is_saved(string phone, string field)
    {
        await using var factory = Create(Key);
        using var client = Client(factory, Key);

        var response = await client.PutAsJsonAsync($"{Url}/{Ananya}/phone", new SetParentPhoneRequest(Guid.NewGuid(), phone), Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(field, await response.Content.ReadAsStringAsync(Ct));
        Assert.Null(_store.Rows.Single(r => r.Id == Ananya).SavedPhone);
    }

    [Fact]
    public async Task A_missing_client_request_id_is_400()
    {
        await using var factory = Create(Key);
        using var client = Client(factory, Key);

        var response = await client.PutAsJsonAsync($"{Url}/{Ananya}/phone", new SetParentPhoneRequest(Guid.Empty, "9845012345"), Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task An_unknown_child_is_404()
    {
        await using var factory = Create(Key);
        using var client = Client(factory, Key);

        var response = await client.PutAsJsonAsync($"{Url}/{Guid.NewGuid()}/phone", new SetParentPhoneRequest(Guid.NewGuid(), "9845012345"), Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("a-test-admin-key-of-32-characterX")]
    public async Task Without_the_right_key_both_are_404_and_nothing_is_saved(string? sent)
    {
        await using var factory = Create(Key);
        using var client = Client(factory, sent);

        var list = await client.GetAsync(Url, Ct);
        var save = await client.PutAsJsonAsync($"{Url}/{Ananya}/phone", new SetParentPhoneRequest(Guid.NewGuid(), "9845012345"), Ct);

        Assert.Equal((HttpStatusCode.NotFound, HttpStatusCode.NotFound), (list.StatusCode, save.StatusCode));
        Assert.Null(_store.Rows.Single(r => r.Id == Ananya).SavedPhone);
    }

    [Fact]
    public async Task In_development_the_page_opens_without_the_key()
    {
        // D-102, like the other admin pages.
        await using var factory = Create(Key, environment: "Development");
        using var client = Client(factory, key: null);

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(Url, Ct)).StatusCode);
    }

    private static HttpClient Client(ApiFactory factory, string? key)
    {
        var client = factory.CreateHttpsClient();
        if (key is not null)
        {
            client.DefaultRequestHeaders.Add(AdminEndpoints.KeyHeader, key);
        }

        return client;
    }

    private ApiFactory Create(string? key, Dictionary<string, string>? recipients = null, string environment = "Testing")
    {
        var settings = new Dictionary<string, string?> { ["Admin:Key"] = key };
        foreach (var (child, number) in recipients ?? [])
        {
            settings[$"WhatsApp:Recipients:{child}"] = number;
        }

        return FakeApi.Create(services => services.AddSingleton<IAdminChildStore>(_store), settings: settings, environment: environment);
    }

    private sealed class FakeAdminChildStore : IAdminChildStore
    {
        private readonly HashSet<Guid> _applied = [];

        public List<AdminChildRow> Rows { get; } = [];

        public Task<IReadOnlyList<AdminChildClass>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<AdminChildClass>>([new AdminChildClass(ClassId, "Shobha", 3, [.. Rows.OrderBy(r => r.RollNo)])]);

        public Task<AdminChildRow?> SetPhoneAsync(
            Guid clientRequestId, Guid childId, string? phone, DateTimeOffset now, CancellationToken cancellationToken)
        {
            var index = Rows.FindIndex(r => r.Id == childId);
            if (index < 0)
            {
                return Task.FromResult<AdminChildRow?>(null);
            }

            if (_applied.Add(clientRequestId))
            {
                Rows[index] = Rows[index] with { SavedPhone = phone };
            }

            return Task.FromResult<AdminChildRow?>(Rows[index]);
        }
    }
}
