using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace SlateAi.Api.Features.Delivery;

/// <summary>
/// WhatsApp Cloud API, the one call step 1 needs: a template message with body parameters and a URL button whose
/// suffix is the parent link token (T-40). Logs status, error code and latency only: no text, no numbers.
/// </summary>
public sealed class WhatsAppClient(HttpClient http, IOptions<WhatsAppOptions> options, ILogger<WhatsAppClient> logger)
{
    /// <summary>Graph error codes worth trying again: rate limits (130429, 131048, 131056) and temporary errors (131000, 1, 2).</summary>
    private static readonly HashSet<int> RetryCodes = [1, 2, 4, 130429, 131000, 131048, 131056];

    public async Task<ChannelResult> SendTemplateAsync(
        string to, string template, string language, IReadOnlyList<string> parameters, string? buttonSuffix,
        CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.PhoneNumberId) || string.IsNullOrWhiteSpace(settings.AccessToken))
        {
            return ChannelResult.Failed("not-configured");
        }

        // A template without variables (Meta's hello_world) takes no components at all.
        List<Component> components = [];
        if (parameters.Count > 0)
        {
            components.Add(new Component("body", null, null, parameters.Select(p => new Parameter("text", p)).ToList()));
        }

        if (buttonSuffix is not null)
        {
            components.Add(new Component("button", "url", "0", [new Parameter("text", buttonSuffix)]));
        }

        var body = new SendRequest(
            To: to.TrimStart('+'),
            Template: new Template(template, new Language(language), components.Count > 0 ? components : null));
        using var request = new HttpRequestMessage(
            HttpMethod.Post, $"{settings.BaseUrl.TrimEnd('/')}/{settings.ApiVersion}/{settings.PhoneNumberId}/messages")
        {
            Content = JsonContent.Create(body, options: Json),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.AccessToken);

        var watch = Stopwatch.StartNew();
        ChannelResult result;
        try
        {
            using var response = await http.SendAsync(request, cancellationToken);
            result = await ReadAsync(response, cancellationToken);
            logger.LogInformation(
                "WhatsApp template {Template} ({Language}): HTTP {Status}, {Outcome} {Error} in {LatencyMs} ms",
                template, language, (int)response.StatusCode, result.Outcome, result.Error, watch.ElapsedMilliseconds);
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            result = ChannelResult.Retry(e is TaskCanceledException ? "timeout" : "network");
            logger.LogWarning(
                "WhatsApp template {Template} ({Language}): {Error} after {LatencyMs} ms",
                template, language, result.Error, watch.ElapsedMilliseconds);
        }

        return result;
    }

    private static async Task<ChannelResult> ReadAsync(HttpResponseMessage response, CancellationToken cancellationToken)
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

        if (response.IsSuccessStatusCode)
        {
            return parsed?.Messages is [{ Id: { Length: > 0 } id }, ..]
                ? ChannelResult.Sent(id)
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

    private sealed record SendRequest(string To, Template Template)
    {
        public string MessagingProduct => "whatsapp";
        public string RecipientType => "individual";
        public string Type => "template";
    }

    private sealed record Template(string Name, Language Language, IReadOnlyList<Component>? Components);

    private sealed record Language(string Code);

    private sealed record Component(string Type, string? SubType, string? Index, IReadOnlyList<Parameter> Parameters);

    private sealed record Parameter(string Type, string Text);

    private sealed record SendResponse(IReadOnlyList<SentMessage>? Messages, GraphError? Error);

    private sealed record SentMessage(string? Id);

    private sealed record GraphError(int Code);
}
