using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using SlateAi.Api.Features.Admin;
using SlateAi.Api.Features.Delivery;
using SlateAi.Api.Tests.Infrastructure;

namespace SlateAi.Api.Tests.Features.Delivery;

public sealed class WhatsAppTestEndpointTests
{
    private const string Url = "/api/admin/whatsapp/test";
    private const string Number = "+919812345678";
    private static readonly CancellationToken Ct = TestContext.Current.CancellationToken;
    private static readonly Guid Ravi = Guid.NewGuid();

    private readonly StubHttpHandler _http = new(
        () => StubHttpHandler.Json("""{"messaging_product":"whatsapp","messages":[{"id":"wamid.HELLO"}]}"""));

    [Fact]
    public async Task Sends_hello_world_to_the_saved_number_without_echoing_it()
    {
        await using var factory = Create();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(Url, new WhatsAppTestRequest(Guid.NewGuid(), Ravi), Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var text = await response.Content.ReadAsStringAsync(Ct);
        Assert.DoesNotContain("9812345678", text);
        Assert.DoesNotContain("wamid", text);
        var result = JsonSerializer.Deserialize<WhatsAppTestResponse>(text, JsonSerializerOptions.Web)!;
        Assert.Equal(("sent", (string?)null, ProviderMessageIds.Hash("wamid.HELLO"), false), (result.Outcome, result.Error, result.ProviderMessageId, result.Replay));
        Assert.Equal([new WhatsAppTestNumber(1, "sent", null, ProviderMessageIds.Hash("wamid.HELLO"))], result.Numbers);

        using var body = JsonDocument.Parse(Assert.Single(_http.Requests).Body);
        var template = body.RootElement.GetProperty("template");
        Assert.Equal("hello_world", template.GetProperty("name").GetString());
        Assert.Equal("en_US", template.GetProperty("language").GetProperty("code").GetString());
        Assert.False(template.TryGetProperty("components", out _));
        Assert.Equal("919812345678", body.RootElement.GetProperty("to").GetString());
    }

    [Fact]
    public async Task A_repeated_client_request_id_does_not_send_again()
    {
        await using var factory = Create();
        using var client = factory.CreateClient();
        var request = new WhatsAppTestRequest(Guid.NewGuid(), Ravi);

        await client.PostAsJsonAsync(Url, request, Ct);
        var replay = await (await client.PostAsJsonAsync(Url, request, Ct)).Content.ReadFromJsonAsync<WhatsAppTestResponse>(Ct);

        Assert.True(replay!.Replay);
        Assert.Equal(ProviderMessageIds.Hash("wamid.HELLO"), replay.ProviderMessageId);
        Assert.Single(_http.Requests);
    }

    [Fact]
    public async Task A_child_without_a_saved_number_is_skipped_D114()
    {
        await using var factory = Create();
        using var client = factory.CreateClient();

        var result = await (await client.PostAsJsonAsync(Url, new WhatsAppTestRequest(Guid.NewGuid(), Guid.NewGuid()), Ct))
            .Content.ReadFromJsonAsync<WhatsAppTestResponse>(Ct);

        Assert.Equal(("skipped", "no-recipient"), (result!.Outcome, result.Error));
        Assert.Empty(_http.Requests);
    }

    [Fact]
    public async Task Needs_a_client_request_id()
    {
        await using var factory = Create();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(Url, new WhatsAppTestRequest(Guid.Empty, Ravi), Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(_http.Requests);
    }

    [Fact]
    public async Task The_app_sends_todays_message_as_its_approved_template_and_skips_the_plain_evening()
    {
        // T-104, with the shipped settings: hello_world is gone; the plain evening has no template yet (D-107).
        await using var factory = Create(channel: DeliveryChannels.WhatsApp);
        using var scope = factory.Services.CreateScope();
        var channel = scope.ServiceProvider.GetRequiredService<IParentChannel>();

        var todays = await channel.SendAsync(
            new DeliveryMessage(Guid.NewGuid(), Ravi, DeliveryKinds.Todays, "te", ["Ravi", "lesson", "4"], "tok_1", "4", Number), Ct);
        var plain = await channel.SendAsync(
            new DeliveryMessage(Guid.NewGuid(), Ravi, DeliveryKinds.Evening, "te", ["Ravi", "lesson", "page 15"], "tok_2", "4", Number), Ct);

        Assert.Equal(DeliveryStatus.Sent, todays.Outcome);
        Assert.Contains("\"name\":\"todays_lesson_link_v1\"", Assert.Single(_http.Requests).Body);
        Assert.Equal(ChannelResult.Skipped("no-template"), plain);
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Development")]
    public async Task It_sends_without_a_key_D112(string environment)
    {
        await using var factory = Create(environment: environment);
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(Url, new WhatsAppTestRequest(Guid.NewGuid(), Ravi), Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Single(_http.Requests);
    }

    [Fact]
    public async Task With_two_saved_numbers_it_sends_to_both_and_says_how_each_went_T140()
    {
        await using var factory = Create(second: "+919845012345");
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(Url, new WhatsAppTestRequest(Guid.NewGuid(), Ravi), Ct);

        var text = await response.Content.ReadAsStringAsync(Ct);
        Assert.DoesNotContain("9845012345", text);
        var result = JsonSerializer.Deserialize<WhatsAppTestResponse>(text, JsonSerializerOptions.Web)!;
        Assert.Equal([1, 2], result.Numbers.Select(n => n.Slot));
        Assert.All(result.Numbers, n => Assert.Equal("sent", n.Outcome));
        Assert.Equal(["919812345678", "919845012345"], _http.Requests.Select(r => JsonDocument.Parse(r.Body).RootElement.GetProperty("to").GetString()));
    }

    private ApiFactory Create(string channel = DeliveryChannels.Demo, string environment = "Testing", string? second = null) => FakeApi.Create(
        services =>
        {
            services.AddHttpClient<WhatsAppClient>().ConfigurePrimaryHttpMessageHandler(() => _http);
            services.AddSingleton<IAdminChildStore>(new SavedNumbers(
                new() { [Ravi] = Number }, second is null ? null : new() { [Ravi] = second }));
        },
        settings: new Dictionary<string, string?>
        {
            ["WhatsApp:PhoneNumberId"] = "1234567890",
            ["WhatsApp:AccessToken"] = "test-token",
            ["Delivery:Channel"] = channel,
        },
        environment: environment);

    /// <summary>Only the saved numbers matter to the test send (D-114); the page's other calls aren't used here.</summary>
    /// <summary>Only the saved numbers matter to the test send (D-114, T-140); the page's other calls aren't used here.</summary>
    private sealed class SavedNumbers(Dictionary<Guid, string> numbers, Dictionary<Guid, string>? second = null) : IAdminChildStore
    {
        public Task<IReadOnlyList<SavedNumber>> GetSavedPhonesAsync(Guid childId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<SavedNumber>>(
                new[] { new SavedNumber(1, numbers.GetValueOrDefault(childId)), new SavedNumber(2, second?.GetValueOrDefault(childId)) }
                    .Where(n => n.Phone is not null).ToList());

        public Task<IReadOnlyList<AdminChildClass>> ListAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<AdminChildRow?> SetPhoneAsync(
            Guid clientRequestId, Guid childId, int slot, string? phone, DateTimeOffset now, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<AdminChildRow?> SetLanguageAsync(
            Guid clientRequestId, Guid childId, string language, DateTimeOffset now, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
