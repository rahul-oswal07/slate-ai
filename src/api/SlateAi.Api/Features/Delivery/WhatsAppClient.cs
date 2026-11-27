using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace SlateAi.Api.Features.Delivery;

/// <summary>
/// WhatsApp Cloud API. Step 1 (T-40): a template message with body parameters and a URL button whose suffix is the
/// parent link token. Step 2 (T-106): the template's quick reply payload, the messages sent inside the 24-hour window
/// (<see cref="ChatMessage"/>) and the media upload for voice notes. Logs kind, status, error code and latency only: no
/// text, no numbers. The returned message ID is already hashed (<see cref="ProviderMessageIds"/>), because the raw wamid
/// encodes the phone number.
/// </summary>
public sealed class WhatsAppClient(HttpClient http, IOptions<WhatsAppOptions> options, ILogger<WhatsAppClient> logger)
{
    /// <summary>Graph error codes worth trying again: rate limits (130429, 131048, 131056) and temporary errors (131000, 1, 2).</summary>
    private static readonly HashSet<int> RetryCodes = [1, 2, 4, 130429, 131000, 131048, 131056];

    /// <param name="buttonSuffix">The URL button's suffix (the link token), or null for a template without one.</param>
    /// <param name="quickReplyPayload">
    /// Step 2 (T-106): the payload of the quick reply at button 0, which comes back to the webhook when the family taps
    /// it; the URL button is then button 1. Null for the step 1 templates.
    /// </param>
    public Task<ChannelResult> SendTemplateAsync(
        string to, string template, string language, IReadOnlyList<string> parameters, string? buttonSuffix,
        CancellationToken cancellationToken, string? quickReplyPayload = null)
    {
        // A template without variables (Meta's hello_world) takes no components at all.
        List<Component> components = [];
        if (parameters.Count > 0)
        {
            components.Add(new Component("body", null, null, parameters.Select(p => new Parameter("text", p)).ToList()));
        }

        if (quickReplyPayload is not null)
        {
            components.Add(new Component("button", "quick_reply", "0", [new Parameter("payload", Payload: quickReplyPayload)]));
        }

        if (buttonSuffix is not null)
        {
            components.Add(new Component(
                "button", "url", quickReplyPayload is null ? "0" : "1", [new Parameter("text", buttonSuffix)]));
        }

        var body = new SendRequest(
            To: to.TrimStart('+'),
            Type: "template",
            Template: new Template(template, new Language(language), components.Count > 0 ? components : null));
        return PostAsync(body, $"template {template} ({language})", cancellationToken);
    }

    /// <summary>
    /// One message inside the 24-hour window (T-106): text, a voice note by its uploaded media ID, reply buttons or a
    /// list. The caller checks the window; outside it WhatsApp refuses them (error 131047).
    /// </summary>
    public Task<ChannelResult> SendChatAsync(string to, ChatMessage message, CancellationToken cancellationToken)
    {
        var number = to.TrimStart('+');
        var body = message switch
        {
            // No link preview: it would load the video's page image on the family's data.
            ChatText text => new SendRequest(number, "text", Text: new TextMessage(text.Body, PreviewUrl: false)),
            ChatAudio audio => new SendRequest(number, "audio", Audio: new MediaRef(audio.MediaId)),
            ChatButtons buttons => new SendRequest(number, "interactive", Interactive: new Interactive(
                "button",
                new InteractiveBody(buttons.Body),
                new InteractiveAction(Buttons: buttons.Options.Select(o => new ReplyButton(new Reply(o.Id, o.Title))).ToList()))),
            ChatList list => new SendRequest(number, "interactive", Interactive: new Interactive(
                "list",
                new InteractiveBody(list.Body),
                new InteractiveAction(
                    Button: list.Button,
                    Sections: [new Section(list.Button, list.Rows.Select(r => new Reply(r.Id, r.Title)).ToList())]))),
            _ => throw new ArgumentOutOfRangeException(nameof(message), message.GetType().Name, "Not a chat message."),
        };
        return PostAsync(body, body.Interactive?.Type ?? body.Type, cancellationToken);
    }

    /// <summary>
    /// Uploads a clip for voice notes (T-106): <c>POST /{phone-number-id}/media</c>. Returns its media ID (valid 30 days)
    /// as <see cref="ChannelResult.ProviderMessageId"/>, not hashed: a media ID holds no number.
    /// </summary>
    public async Task<ChannelResult> UploadMediaAsync(byte[] bytes, string contentType, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (!Configured(settings))
        {
            return ChannelResult.Failed("not-configured");
        }

        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        var form = new MultipartFormDataContent
        {
            { new StringContent("whatsapp"), "messaging_product" },
            { new StringContent(contentType), "type" },
            { file, "file", contentType == "audio/ogg" ? "lesson.ogg" : "lesson" },
        };
        var request = new HttpRequestMessage(HttpMethod.Post, Url(settings, "media")) { Content = form };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.AccessToken);
        return await SendAsync(request, $"media upload ({bytes.Length / 1024} KB)", media: true, cancellationToken);
    }

    private Task<ChannelResult> PostAsync(SendRequest body, string what, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (!Configured(settings))
        {
            return Task.FromResult(ChannelResult.Failed("not-configured"));
        }

        var request = new HttpRequestMessage(HttpMethod.Post, Url(settings, "messages"))
        {
            Content = JsonContent.Create(body, options: Json),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.AccessToken);
        return SendAsync(request, what, media: false, cancellationToken);
    }

    private async Task<ChannelResult> SendAsync(
        HttpRequestMessage request, string what, bool media, CancellationToken cancellationToken)
    {
        using var _ = request;
        var watch = Stopwatch.StartNew();
        ChannelResult result;
        try
        {
            using var response = await http.SendAsync(request, cancellationToken);
            result = await ReadAsync(response, media, cancellationToken);
            logger.LogInformation(
                "WhatsApp {What}: HTTP {Status}, {Outcome} {Error} in {LatencyMs} ms",
                what, (int)response.StatusCode, result.Outcome, result.Error, watch.ElapsedMilliseconds);
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            result = ChannelResult.Retry(e is TaskCanceledException ? "timeout" : "network");
            logger.LogWarning("WhatsApp {What}: {Error} after {LatencyMs} ms", what, result.Error, watch.ElapsedMilliseconds);
        }

        return result;
    }

    private static bool Configured(WhatsAppOptions settings) =>
        !string.IsNullOrWhiteSpace(settings.PhoneNumberId) && !string.IsNullOrWhiteSpace(settings.AccessToken);

    private static string Url(WhatsAppOptions settings, string edge) =>
        $"{settings.BaseUrl.TrimEnd('/')}/{settings.ApiVersion}/{settings.PhoneNumberId}/{edge}";

    private static async Task<ChannelResult> ReadAsync(HttpResponseMessage response, bool media, CancellationToken cancellationToken)
    {
        SendResponse? parsed = null;
        try
        {
            parsed = await response.Content.ReadFromJsonAsync<SendResponse>(Json, cancellationToken);
        }
        catch (JsonException)
        {
            // An HTML error page or empty body: the status code decides below.
        }

        if (response.IsSuccessStatusCode && media)
        {
            return parsed?.Id is { Length: > 0 } mediaId ? ChannelResult.Sent(mediaId) : ChannelResult.Failed("no-media-id");
        }

        if (response.IsSuccessStatusCode)
        {
            return parsed?.Messages is [{ Id: { Length: > 0 } id }, ..]
                ? ChannelResult.Sent(ProviderMessageIds.Hash(id))
                : ChannelResult.Failed("no-message-id");
        }

        if (parsed?.Error is { } error)
        {
            var code = $"graph-{error.Code}";
            return RetryCodes.Contains(error.Code) || (int)response.StatusCode >= 500
                ? ChannelResult.Retry(code)
                : ChannelResult.Failed(code);
        }

        var http = $"http-{(int)response.StatusCode}";
        return response.StatusCode == HttpStatusCode.TooManyRequests || (int)response.StatusCode >= 500
            ? ChannelResult.Retry(http)
            : ChannelResult.Failed(http);
    }

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private sealed record SendRequest(
        string To,
        string Type,
        Template? Template = null,
        TextMessage? Text = null,
        MediaRef? Audio = null,
        Interactive? Interactive = null)
    {
        public string MessagingProduct => "whatsapp";
        public string RecipientType => "individual";
    }

    private sealed record Template(string Name, Language Language, IReadOnlyList<Component>? Components);

    private sealed record Language(string Code);

    private sealed record Component(string Type, string? SubType, string? Index, IReadOnlyList<Parameter> Parameters);

    private sealed record Parameter(string Type, string? Text = null, string? Payload = null);

    private sealed record TextMessage(string Body, bool PreviewUrl);

    private sealed record MediaRef(string Id);

    private sealed record Interactive(string Type, InteractiveBody Body, InteractiveAction Action);

    private sealed record InteractiveBody(string Text);

    private sealed record InteractiveAction(
        IReadOnlyList<ReplyButton>? Buttons = null, string? Button = null, IReadOnlyList<Section>? Sections = null);

    private sealed record ReplyButton(Reply Reply)
    {
        public string Type => "reply";
    }

    private sealed record Reply(string Id, string Title);

    private sealed record Section(string Title, IReadOnlyList<Reply> Rows);

    private sealed record SendResponse(IReadOnlyList<SentMessage>? Messages, GraphError? Error, string? Id = null);

    private sealed record SentMessage(string? Id);

    private sealed record GraphError(int Code);
}

/// <summary>A message sent inside the 24-hour window (T-106). Built by code from approved records only (guardrail 3).</summary>
public abstract record ChatMessage;

/// <summary>A plain text message.</summary>
public sealed record ChatText(string Body) : ChatMessage;

/// <summary>A voice note: an Ogg Opus clip already uploaded (<see cref="WhatsAppClient.UploadMediaAsync"/>).</summary>
public sealed record ChatAudio(string MediaId) : ChatMessage;

/// <summary>Up to 3 reply buttons under a body; each tap comes back to the webhook with its <see cref="ChatOption.Id"/>.</summary>
public sealed record ChatButtons(string Body, IReadOnlyList<ChatOption> Options) : ChatMessage;

/// <summary>A list message: one button that opens up to 10 rows (FR-21's 5 reasons don't fit 3 buttons).</summary>
public sealed record ChatList(string Body, string Button, IReadOnlyList<ChatOption> Rows) : ChatMessage;

/// <param name="Id">Comes back on the tap (≤ 200 characters).</param>
/// <param name="Title">What the button or row says (≤ 20 characters for a button, 24 for a row).</param>
public sealed record ChatOption(string Id, string Title);
