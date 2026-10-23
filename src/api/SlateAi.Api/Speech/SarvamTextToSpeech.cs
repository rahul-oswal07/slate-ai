using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace SlateAi.Api.Speech;

/// <summary>
/// Sarvam REST text-to-speech (Bulbul, D-12): <c>POST /text-to-speech</c> as 16-bit WAV, then encoded to Opus 24 kbps
/// here, because Sarvam's own MP3 is a fixed 128 kbps (~860 KB a catch-up, over FR-09's 300 KB).
/// </summary>
public sealed class SarvamTextToSpeech(HttpClient http, IOptions<SarvamOptions> options) : ITextToSpeech
{
    /// <summary>Bulbul v3's limit per request.</summary>
    public const int MaxCharacters = 2500;

    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

    public async Task<SpokenAudio> SynthesizeAsync(string text, string languageCode, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            throw new SpeechException(SpeechFailure.NotConfigured, "Sarvam:ApiKey is not set.");
        }

        if (string.IsNullOrWhiteSpace(text) || text.Length > MaxCharacters)
        {
            throw new SpeechException(SpeechFailure.Rejected, $"Text must be 1 to {MaxCharacters} characters.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(new Uri(settings.BaseUrl), "text-to-speech"))
        {
            Content = JsonContent.Create(
                new SarvamTtsRequest(
                    text, languageCode, settings.TtsModel, settings.TtsSpeaker, settings.TtsPace, settings.TtsSampleRate, "wav"),
                options: Json),
        };
        request.Headers.Add("api-subscription-key", settings.ApiKey);

        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(request, cancellationToken);
        }
        catch (Exception e) when (e is HttpRequestException
                                      || (e is TaskCanceledException && !cancellationToken.IsCancellationRequested))
        {
            throw new SpeechException(SpeechFailure.Unavailable, $"Sarvam call failed: {e.GetType().Name}", e);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var failure = response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity
                    ? SpeechFailure.Rejected
                    : SpeechFailure.Unavailable;
                var reason = await ReadErrorAsync(response, cancellationToken);
                throw new SpeechException(
                    failure,
                    reason is null ? $"Sarvam returned {(int)response.StatusCode}." : $"Sarvam returned {(int)response.StatusCode}: {reason}");
            }

            var body = await response.Content.ReadFromJsonAsync<SarvamTtsResponse>(Json, cancellationToken);
            if (body?.Audios is not [var base64, ..] || string.IsNullOrWhiteSpace(base64))
            {
                throw new SpeechException(SpeechFailure.Unavailable, "Sarvam returned no audio.");
            }

            try
            {
                var (ogg, seconds) = OpusEncoding.FromWav(Convert.FromBase64String(base64));
                return new SpokenAudio(ogg, OpusEncoding.ContentType, seconds, Guid.Empty);
            }
            catch (Exception e) when (e is FormatException or InvalidDataException)
            {
                throw new SpeechException(SpeechFailure.Unavailable, "Sarvam returned audio we can't read.", e);
            }
        }
    }

    private static async Task<string?> ReadErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var body = await response.Content.ReadFromJsonAsync<SarvamError>(Json, cancellationToken);
            return string.IsNullOrWhiteSpace(body?.Error?.Message) ? null : body.Error.Message.Trim();
        }
        catch (Exception e) when (e is JsonException or NotSupportedException or InvalidOperationException)
        {
            return null;
        }
    }

    private sealed record SarvamTtsRequest(
        string Text, string LanguageCode, string Model, string Speaker, double Pace, int SpeechSampleRate, string OutputAudioCodec);

    private sealed record SarvamTtsResponse(string? RequestId, List<string>? Audios);

    private sealed record SarvamError(SarvamErrorDetail? Error);

    private sealed record SarvamErrorDetail(string? Message);
}
