using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using SlateAi.Api.Data.Seed;
using SlateAi.Api.Features.Admin;
using SlateAi.Api.Tests.Infrastructure;

namespace SlateAi.Api.Tests.Features.Admin;

/// <summary>The admin children page: who is in each class and the number their family's WhatsApp goes to (T-118, D-110).</summary>
public sealed class AdminChildEndpointTests
{
    private const string Url = "/api/admin/children";
    private static readonly CancellationToken Ct = TestContext.Current.CancellationToken;
    private static readonly Guid ClassId = Guid.NewGuid();
    private static readonly Guid Manjunath = Guid.NewGuid();
    private static readonly Guid Ravi = Guid.NewGuid();
    private static readonly Guid Ananya = Guid.NewGuid();

    private static readonly Guid DemoClassId =
        DemoSeedFile.LoadAsync("data/seed/demo-class.json", Ct).GetAwaiter().GetResult().Class.Id;

    private readonly FakeAdminChildStore _store = new();

    public AdminChildEndpointTests()
    {
        _store.Rows.AddRange(
        [
            new AdminChildRow(Ananya, 1, "Ananya", "kn", "no", false, null, false),
            new AdminChildRow(Manjunath, 15, "Manjunath", "kn", "yes", false, "+919876543210", false),
            new AdminChildRow(Ravi, 24, "Ravi", "ta", "yes", true, null, true),
        ]);
    }

    [Fact]
    public async Task Lists_each_class_with_its_children_and_their_saved_numbers()
    {
        await using var factory = Create();
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync(Url, Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
        var cls = Assert.Single((await response.Content.ReadFromJsonAsync<AdminChildrenResponse>(Ct))!.Classes);
        Assert.Equal("Shobha · Grade 3", cls.Label);
        Assert.Equal(
            [
                new AdminChild(Ananya, 1, "Ananya", "kn", "no", false, null, false),
                new AdminChild(Manjunath, 15, "Manjunath", "kn", "yes", false, "+919876543210", false),
                new AdminChild(Ravi, 24, "Ravi", "ta", "yes", true, null, LanguageChanged: true),
            ],
            cls.Children);
    }

    [Fact]
    public async Task Marks_the_demo_class_so_the_page_can_link_each_childs_family_phone()
    {
        // T-130: /demo/family/{id} only knows the demo class's children, so only that class gets the links.
        _store.Classes.Add(new AdminChildClass(DemoClassId, "Shobha", 3, []));
        await using var factory = Create();
        using var client = factory.CreateHttpsClient();

        var classes = (await client.GetFromJsonAsync<AdminChildrenResponse>(Url, Ct))!.Classes;

        Assert.Equal([(ClassId, false), (DemoClassId, true)], classes.Select(c => (c.Id, c.Demo)));
    }

    [Fact]
    public async Task No_class_is_the_demo_class_when_the_demo_is_off()
    {
        _store.Classes.Add(new AdminChildClass(DemoClassId, "Shobha", 3, []));
        await using var factory = FakeApi.Create(
            services => services.AddSingleton<IAdminChildStore>(_store),
            settings: new Dictionary<string, string?> { ["Demo:Enabled"] = "false" });
        using var client = factory.CreateHttpsClient();

        var classes = (await client.GetFromJsonAsync<AdminChildrenResponse>(Url, Ct))!.Classes;

        Assert.Equal(2, classes.Count);
        Assert.All(classes, c => Assert.False(c.Demo));
    }

    [Fact]
    public async Task Saving_stores_the_number_as_E164_and_returns_the_child()
    {
        await using var factory = Create();
        using var client = factory.CreateHttpsClient();

        var response = await client.PutAsJsonAsync($"{Url}/{Ananya}/phone", new SetParentPhoneRequest(Guid.NewGuid(), "98450 12345"), Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var child = (await response.Content.ReadFromJsonAsync<AdminChild>(Ct))!;
        Assert.Equal("+919845012345", child.Phone);
        Assert.Equal("+919845012345", _store.Rows.Single(r => r.Id == Ananya).SavedPhone);
    }

    [Fact]
    public async Task Number_2_is_saved_and_cleared_on_its_own_and_number_1_stays_T140()
    {
        // T-140: two optional numbers per family (e.g. mother and father); WhatsApp goes to both.
        await using var factory = Create();
        using var client = factory.CreateHttpsClient();

        var saved = await client.PutAsJsonAsync($"{Url}/{Manjunath}/phone", new SetParentPhoneRequest(Guid.NewGuid(), "98450 12345", Slot: 2), Ct);
        var both = (await saved.Content.ReadFromJsonAsync<AdminChild>(Ct))!;
        var cleared = await client.PutAsJsonAsync($"{Url}/{Manjunath}/phone", new SetParentPhoneRequest(Guid.NewGuid(), "", Slot: 1), Ct);
        var onlyTwo = (await cleared.Content.ReadFromJsonAsync<AdminChild>(Ct))!;

        Assert.Equal(("+919876543210", "+919845012345"), (both.Phone, both.Phone2));
        Assert.Equal(((string?)null, "+919845012345"), (onlyTwo.Phone, onlyTwo.Phone2));
        var listed = (await client.GetFromJsonAsync<AdminChildrenResponse>(Url, Ct))!.Classes.Single().Children.Single(c => c.Id == Manjunath);
        Assert.Equal("+919845012345", listed.Phone2);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    public async Task A_slot_other_than_1_or_2_is_refused(int slot)
    {
        await using var factory = Create();
        using var client = factory.CreateHttpsClient();

        var response = await client.PutAsJsonAsync($"{Url}/{Ananya}/phone", new SetParentPhoneRequest(Guid.NewGuid(), "98450 12345", slot), Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Null(_store.Rows.Single(r => r.Id == Ananya).SavedPhone);
    }

    [Fact]
    public async Task Clearing_leaves_no_number_so_WhatsApp_skips_the_family_D114()
    {
        await using var factory = Create();
        using var client = factory.CreateHttpsClient();

        var response = await client.PutAsJsonAsync($"{Url}/{Manjunath}/phone", new SetParentPhoneRequest(Guid.NewGuid(), " "), Ct);

        var child = (await response.Content.ReadFromJsonAsync<AdminChild>(Ct))!;
        Assert.Null(child.Phone);
        Assert.Null(_store.Rows.Single(r => r.Id == Manjunath).SavedPhone);
    }

    [Fact]
    public async Task A_replayed_save_changes_nothing()
    {
        await using var factory = Create();
        using var client = factory.CreateHttpsClient();
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
        await using var factory = Create();
        using var client = factory.CreateHttpsClient();

        var response = await client.PutAsJsonAsync($"{Url}/{Ananya}/phone", new SetParentPhoneRequest(Guid.NewGuid(), phone), Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(field, await response.Content.ReadAsStringAsync(Ct));
        Assert.Null(_store.Rows.Single(r => r.Id == Ananya).SavedPhone);
    }

    [Fact]
    public async Task A_missing_client_request_id_is_400()
    {
        await using var factory = Create();
        using var client = factory.CreateHttpsClient();

        var response = await client.PutAsJsonAsync($"{Url}/{Ananya}/phone", new SetParentPhoneRequest(Guid.Empty, "9845012345"), Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task An_unknown_child_is_404()
    {
        await using var factory = Create();
        using var client = factory.CreateHttpsClient();

        var response = await client.PutAsJsonAsync($"{Url}/{Guid.NewGuid()}/phone", new SetParentPhoneRequest(Guid.NewGuid(), "9845012345"), Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    public async Task The_page_opens_and_saves_without_a_key_D112(string environment)
    {
        await using var factory = Create(environment: environment);
        using var client = factory.CreateHttpsClient();

        var list = await client.GetAsync(Url, Ct);
        var save = await client.PutAsJsonAsync($"{Url}/{Ananya}/phone", new SetParentPhoneRequest(Guid.NewGuid(), "9845012345"), Ct);

        Assert.Equal((HttpStatusCode.OK, HttpStatusCode.OK), (list.StatusCode, save.StatusCode));
        Assert.Equal("+919845012345", _store.Rows.Single(r => r.Id == Ananya).SavedPhone);
    }

    [Fact]
    public async Task Changing_the_home_language_stores_it_and_returns_the_child_T122()
    {
        await using var factory = Create();
        using var client = factory.CreateHttpsClient();

        var response = await client.PutAsJsonAsync($"{Url}/{Ananya}/language", new SetHomeLanguageRequest(Guid.NewGuid(), " HI "), Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
        var child = (await response.Content.ReadFromJsonAsync<AdminChild>(Ct))!;
        Assert.Equal(("hi", true), (child.HomeLanguage, child.LanguageChanged));
        Assert.Equal("hi", _store.Rows.Single(r => r.Id == Ananya).HomeLanguage);
    }

    [Theory]
    [InlineData("fr")]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("kannada")]
    public async Task Another_language_is_400_and_nothing_changes(string? language)
    {
        await using var factory = Create();
        using var client = factory.CreateHttpsClient();

        var response = await client.PutAsJsonAsync($"{Url}/{Ananya}/language", new SetHomeLanguageRequest(Guid.NewGuid(), language), Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, _store.LanguageWrites);
        Assert.Equal("kn", _store.Rows.Single(r => r.Id == Ananya).HomeLanguage);
    }

    [Fact]
    public async Task A_language_change_without_a_client_request_id_is_400()
    {
        await using var factory = Create();
        using var client = factory.CreateHttpsClient();

        var response = await client.PutAsJsonAsync($"{Url}/{Ananya}/language", new SetHomeLanguageRequest(Guid.Empty, "te"), Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, _store.LanguageWrites);
    }

    [Fact]
    public async Task A_language_change_for_an_unknown_child_is_404()
    {
        await using var factory = Create();
        using var client = factory.CreateHttpsClient();

        var response = await client.PutAsJsonAsync($"{Url}/{Guid.NewGuid()}/language", new SetHomeLanguageRequest(Guid.NewGuid(), "te"), Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task A_repeated_language_change_is_applied_once()
    {
        await using var factory = Create();
        using var client = factory.CreateHttpsClient();
        var requestId = Guid.NewGuid();

        var first = await client.PutAsJsonAsync($"{Url}/{Ananya}/language", new SetHomeLanguageRequest(requestId, "ta"), Ct);
        var replay = await client.PutAsJsonAsync($"{Url}/{Ananya}/language", new SetHomeLanguageRequest(requestId, "ta"), Ct);

        Assert.Equal((HttpStatusCode.OK, HttpStatusCode.OK), (first.StatusCode, replay.StatusCode));
        Assert.Equal(1, _store.LanguageWrites);
    }

    private ApiFactory Create(string environment = "Testing") =>
        FakeApi.Create(services => services.AddSingleton<IAdminChildStore>(_store), environment: environment);

    private sealed class FakeAdminChildStore : IAdminChildStore
    {
        private readonly HashSet<Guid> _applied = [];

        public List<AdminChildRow> Rows { get; } = [];

        /// <summary>Classes listed after the one holding <see cref="Rows"/>.</summary>
        public List<AdminChildClass> Classes { get; } = [];

        public Task<IReadOnlyList<AdminChildClass>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<AdminChildClass>>(
                [new AdminChildClass(ClassId, "Shobha", 3, [.. Rows.OrderBy(r => r.RollNo)]), .. Classes]);

        public Task<AdminChildRow?> SetPhoneAsync(
            Guid clientRequestId, Guid childId, int slot, string? phone, DateTimeOffset now, CancellationToken cancellationToken)
        {
            var index = Rows.FindIndex(r => r.Id == childId);
            if (index < 0)
            {
                return Task.FromResult<AdminChildRow?>(null);
            }

            if (_applied.Add(clientRequestId))
            {
                Rows[index] = slot == 2 ? Rows[index] with { SavedPhone2 = phone } : Rows[index] with { SavedPhone = phone };
            }

            return Task.FromResult<AdminChildRow?>(Rows[index]);
        }

        public Task<AdminChildRow?> SetLanguageAsync(
            Guid clientRequestId, Guid childId, string language, DateTimeOffset now, CancellationToken cancellationToken)
        {
            var index = Rows.FindIndex(r => r.Id == childId);
            if (index < 0)
            {
                return Task.FromResult<AdminChildRow?>(null);
            }

            if (_applied.Add(clientRequestId))
            {
                LanguageWrites++;
                Rows[index] = Rows[index] with { HomeLanguage = language, LanguageChanged = true };
            }

            return Task.FromResult<AdminChildRow?>(Rows[index]);
        }

        public int LanguageWrites { get; private set; }

        public Task<IReadOnlyList<SavedNumber>> GetSavedPhonesAsync(Guid childId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<SavedNumber>>(
                Rows.SingleOrDefault(r => r.Id == childId) is { } row
                    ? new[] { new SavedNumber(1, row.SavedPhone), new SavedNumber(2, row.SavedPhone2) }.Where(n => n.Phone is not null).ToList()
                    : []);
    }
}
