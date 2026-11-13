using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SlateAi.Api.Features.Delivery;
using SlateAi.Api.Features.EveningMessages;
using SlateAi.Api.Tests.Infrastructure;

namespace SlateAi.Api.Tests.Features.Delivery;

public sealed class WhatsAppTests
{
    private static readonly CancellationToken Ct = TestContext.Current.CancellationToken;
    private static readonly Guid Ravi = Guid.NewGuid();
    private static readonly Guid Unregistered = Guid.NewGuid();

    private static WhatsAppOptions Settings() => new()
    {
        PhoneNumberId = "1234567890",
        AccessToken = "test-token",
        Recipients = { [Ravi.ToString().ToUpperInvariant()] = "+919812345678" },
    };

    private static DeliveryMessage Evening(Guid child, string language = "te", string wordingVersion = "3") =>
        new(Guid.NewGuid(), child, DeliveryKinds.Evening, language, ["Ravi", "పది తీసుకుని తీసివేత", "పేజీ 15"], "tok_123", wordingVersion);

    private static (WhatsAppChannel Channel, StubHttpHandler Http) Create(
        WhatsAppOptions? settings = null, params Func<HttpResponseMessage>[] responses)
    {
        var http = new StubHttpHandler(responses.Length > 0
            ? responses
            : [() => StubHttpHandler.Json("""{"messaging_product":"whatsapp","messages":[{"id":"wamid.ABC"}]}""")]);
        var options = Options.Create(settings ?? Settings());
        var client = new WhatsAppClient(new HttpClient(http), options, NullLogger<WhatsAppClient>.Instance);
        return (new WhatsAppChannel(client, options), http);
    }

    [Fact]
    public async Task Sends_the_template_with_body_parameters_and_the_token_as_the_url_button()
    {
        var (channel, http) = Create();

        var result = await channel.SendAsync(Evening(Ravi), Ct);

        Assert.Equal(ChannelResult.Sent(ProviderMessageIds.Hash("wamid.ABC")), result);
        var (request, body) = Assert.Single(http.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("https://graph.facebook.com/v23.0/1234567890/messages", request.RequestUri!.ToString());
        Assert.Equal("Bearer test-token", request.Headers.Authorization!.ToString());

        using var json = JsonDocument.Parse(body);
        var root = json.RootElement;
        Assert.Equal("whatsapp", root.GetProperty("messaging_product").GetString());
        Assert.Equal("919812345678", root.GetProperty("to").GetString());
        Assert.Equal("template", root.GetProperty("type").GetString());
        var template = root.GetProperty("template");
        Assert.Equal("evening_absence_v1", template.GetProperty("name").GetString());
        Assert.Equal("te", template.GetProperty("language").GetProperty("code").GetString());
        var components = template.GetProperty("components").EnumerateArray().ToList();
        Assert.Equal(
            ["Ravi", "పది తీసుకుని తీసివేత", "పేజీ 15"],
            components[0].GetProperty("parameters").EnumerateArray().Select(p => p.GetProperty("text").GetString()));
        Assert.Equal("button", components[1].GetProperty("type").GetString());
        Assert.Equal("url", components[1].GetProperty("sub_type").GetString());
        Assert.Equal("0", components[1].GetProperty("index").GetString());
        Assert.Equal("tok_123", components[1].GetProperty("parameters")[0].GetProperty("text").GetString());
    }

    [Fact]
    public async Task The_wamid_is_kept_only_as_a_hash_because_it_encodes_the_number()
    {
        // Shaped like a real wamid: base64 that contains the recipient's number (here the fictional 919812345678).
        const string wamid = "wamid.GAw5MTk4MTIzNDU2NzgVAgARGBJBQkNERUYwMTIzNDU2Nzg5QUIA";
        var (channel, _) = Create(null, () => StubHttpHandler.Json($$"""{"messages":[{"id":"{{wamid}}"}]}"""));

        var result = await channel.SendAsync(Evening(Ravi), Ct);

        Assert.Equal(ProviderMessageIds.Hash(wamid), result.ProviderMessageId);
        Assert.StartsWith("sha256:", result.ProviderMessageId);
        Assert.Equal(71, result.ProviderMessageId!.Length);
        Assert.DoesNotContain("wamid", result.ProviderMessageId);
    }

    [Fact]
    public async Task Catch_up_uses_its_own_template()
    {
        var (channel, http) = Create();

        await channel.SendAsync(
            new DeliveryMessage(Guid.NewGuid(), Ravi, DeliveryKinds.CatchUp, "kn", ["Ravi", "Shobha"], "tok_9"), Ct);

        Assert.Contains("\"name\":\"catchup_ready_v1\"", Assert.Single(http.Requests).Body);
    }

    [Fact]
    public async Task A_child_without_a_registered_number_is_skipped_without_calling_whatsapp()
    {
        var (channel, http) = Create();

        var result = await channel.SendAsync(Evening(Unregistered), Ct);

        Assert.Equal(ChannelResult.Skipped("no-recipient"), result);
        Assert.Empty(http.Requests);
    }

    [Fact]
    public async Task A_language_without_an_approved_template_is_skipped()
    {
        var settings = Settings();
        settings.Evening.Languages.Remove("te");
        var (channel, http) = Create(settings);

        Assert.Equal(ChannelResult.Skipped("no-template"), await channel.SendAsync(Evening(Ravi), Ct));
        Assert.Empty(http.Requests);
    }

    [Fact]
    public async Task An_evening_approved_at_another_wording_version_is_skipped()
    {
        // The Meta template copies the wording she approved at version 3; v4 text would not be what WhatsApp shows.
        var (channel, http) = Create();

        Assert.Equal(ChannelResult.Skipped("wording-version"), await channel.SendAsync(Evening(Ravi, wordingVersion: "4"), Ct));
        Assert.Empty(http.Requests);
    }

    [Fact]
    public void The_evening_template_copies_the_current_approved_wording_version()
    {
        // Bumping evening-message.json (D-49) means a new Meta template whose body copies the new text, and this default
        // (or WhatsApp:Evening:WordingVersion) moved with it; until then evening WhatsApp sends are skipped.
        var wording = EveningWording.Load(Path.Combine(AppContext.BaseDirectory, "data/seed/evening-message.json"));

        Assert.Equal(wording.Version, new WhatsAppOptions().Evening.WordingVersion);
    }

    [Fact]
    public async Task Missing_credentials_fail_without_calling_whatsapp()
    {
        var settings = Settings();
        settings.AccessToken = null;
        var (channel, http) = Create(settings);

        Assert.Equal(ChannelResult.Failed("not-configured"), await channel.SendAsync(Evening(Ravi), Ct));
        Assert.Empty(http.Requests);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, """{"error":{"code":132001,"message":"Template does not exist"}}""", "failed", "graph-132001")]
    [InlineData(HttpStatusCode.Unauthorized, """{"error":{"code":190,"message":"Token expired"}}""", "failed", "graph-190")]
    [InlineData(HttpStatusCode.BadRequest, """{"error":{"code":131048,"message":"Spam rate limit"}}""", "retry", "graph-131048")]
    [InlineData(HttpStatusCode.TooManyRequests, "", "retry", "http-429")]
    [InlineData(HttpStatusCode.BadGateway, "<html>bad gateway</html>", "retry", "http-502")]
    [InlineData(HttpStatusCode.OK, """{"messages":[]}""", "failed", "no-message-id")]
    public async Task Errors_map_to_retry_or_failed(HttpStatusCode status, string body, string outcome, string error)
    {
        var (channel, _) = Create(null, () => StubHttpHandler.Json(body, status));

        var result = await channel.SendAsync(Evening(Ravi), Ct);

        Assert.Equal(outcome, result.Outcome);
        Assert.Equal(error, result.Error);
    }

    [Fact]
    public async Task A_network_error_is_retried()
    {
        var (channel, _) = Create(null, () => throw new HttpRequestException("connection refused"));

        Assert.Equal(ChannelResult.Retry("network"), await channel.SendAsync(Evening(Ravi), Ct));
    }
}
