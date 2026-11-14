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
    private const string Key = "a-test-admin-key-of-32-characters";
    private const string Url = "/api/admin/whatsapp/test";
    private const string Number = "+919812345678";
    private static readonly CancellationToken Ct = TestContext.Current.CancellationToken;
    private static readonly Guid Ravi = Guid.NewGuid();

    private readonly StubHttpHandler _http = new(
        () => StubHttpHandler.Json("""{"messaging_product":"whatsapp","messages":[{"id":"wamid.HELLO"}]}"""));

    [Fact]
    public async Task Sends_hello_world_to_the_registered_phone_without_echoing_the_number()
    {
        await using var factory = Create(Key);
        using var client = AdminClient(factory, Key);

        var response = await client.PostAsJsonAsync(Url, new WhatsAppTestRequest(Guid.NewGuid(), Ravi), Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var text = await response.Content.ReadAsStringAsync(Ct);
        Assert.DoesNotContain("9812345678", text);
        Assert.DoesNotContain("wamid", text);
        Assert.Equal(new WhatsAppTestResponse("sent", null, ProviderMessageIds.Hash("wamid.HELLO"), false), JsonSerializer.Deserialize<WhatsAppTestResponse>(text, JsonSerializerOptions.Web));

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
        await using var factory = Create(Key);
        using var client = AdminClient(factory, Key);
        var request = new WhatsAppTestRequest(Guid.NewGuid(), Ravi);

        await client.PostAsJsonAsync(Url, request, Ct);
        var replay = await (await client.PostAsJsonAsync(Url, request, Ct)).Content.ReadFromJsonAsync<WhatsAppTestResponse>(Ct);

        Assert.True(replay!.Replay);
        Assert.Equal(ProviderMessageIds.Hash("wamid.HELLO"), replay.ProviderMessageId);
        Assert.Single(_http.Requests);
    }

    [Fact]
    public async Task A_child_without_a_registered_number_is_skipped()
    {
        await using var factory = Create(Key);
        using var client = AdminClient(factory, Key);

        var result = await (await client.PostAsJsonAsync(Url, new WhatsAppTestRequest(Guid.NewGuid(), Guid.NewGuid()), Ct))
            .Content.ReadFromJsonAsync<WhatsAppTestResponse>(Ct);

        Assert.Equal(("skipped", "no-recipient"), (result!.Outcome, result.Error));
        Assert.Empty(_http.Requests);
    }

    [Fact]
    public async Task Needs_a_client_request_id()
    {
        await using var factory = Create(Key);
        using var client = AdminClient(factory, Key);

        var response = await client.PostAsJsonAsync(Url, new WhatsAppTestRequest(Guid.Empty, Ravi), Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(_http.Requests);
    }

    [Fact]
    public async Task The_app_sends_deliveries_as_hello_world_until_T104()
    {
        await using var factory = Create(Key, channel: DeliveryChannels.WhatsApp);
        using var scope = factory.Services.CreateScope();
        var channel = scope.ServiceProvider.GetRequiredService<IParentChannel>();

        var result = await channel.SendAsync(
            new DeliveryMessage(Guid.NewGuid(), Ravi, DeliveryKinds.Evening, "te", ["Ravi", "lesson", "page 15"], "tok_1", "3"), Ct);

        Assert.Equal(DeliveryStatus.Sent, result.Outcome);
        Assert.Contains("\"name\":\"hello_world\"", Assert.Single(_http.Requests).Body);
    }

    [Theory]
    [InlineData(Key, "wrong-key-wrong-key-wrong-key-xx")]
    [InlineData(Key, null)]
    [InlineData(null, Key)]
    public async Task Without_the_admin_key_it_is_not_there(string? serverKey, string? sentKey)
    {
        await using var factory = Create(serverKey);
        using var client = AdminClient(factory, sentKey);

        var response = await client.PostAsJsonAsync(Url, new WhatsAppTestRequest(Guid.NewGuid(), Ravi), Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Empty(_http.Requests);
    }

    [Fact]
    public async Task In_development_it_still_needs_the_admin_key_D102()
    {
        await using var factory = Create(key: null, environment: "Development");
        using var client = AdminClient(factory, key: null);

        var response = await client.PostAsJsonAsync(Url, new WhatsAppTestRequest(Guid.NewGuid(), Ravi), Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Empty(_http.Requests);
    }

    private ApiFactory Create(string? key, string channel = DeliveryChannels.Demo, string environment = "Testing") => FakeApi.Create(
        services => services.AddHttpClient<WhatsAppClient>().ConfigurePrimaryHttpMessageHandler(() => _http),
        settings: new Dictionary<string, string?>
        {
            ["Admin:Key"] = key,
            ["WhatsApp:PhoneNumberId"] = "1234567890",
            ["WhatsApp:AccessToken"] = "test-token",
            [$"WhatsApp:Recipients:{Ravi}"] = Number,
            ["Delivery:Channel"] = channel,
        },
        environment: environment);

    private static HttpClient AdminClient(ApiFactory factory, string? key)
    {
        var client = factory.CreateClient();
        if (key is not null)
        {
            client.DefaultRequestHeaders.Add(AdminEndpoints.KeyHeader, key);
        }

        return client;
    }
}
