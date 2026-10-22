using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace SlateAi.Api.Speech;

/// <summary>Sarvam REST speech-to-text (D-12): <c>POST /speech-to-text</c>, multipart, clips up to ~30 s.</summary>
public sealed class SarvamSpeechToText(HttpClient http, IOptions<SarvamOptions> options) : ISpeechToText
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

    public async Task<Transcription> TranscribeAsync(
        Stream audio,
        string fileName,
        string contentType,
        string languageCode,
        IReadOnlyList<string> keyTerms,
        CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            throw new SpeechException(SpeechFailure.NotConfigured, "Sarvam:ApiKey is not set.");
        }

        using var file = new StreamContent(audio);
        // Sarvam rejects parameters such as Chrome's "audio/webm;codecs=opus" ("Invalid file type").
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType.Split(';')[0].Trim());
        using var form = new MultipartFormDataContent
        {
            { file, "file", fileName },
            { new StringContent(settings.SttModel), "model" },
            { new StringContent(languageCode), "language_code" },
        };
        var terms = CleanKeyTerms(keyTerms);
        if (terms.Count > 0)
        {
            form.Add(new StringContent(JsonSerializer.Serialize(terms)), "keyterms");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(new Uri(settings.BaseUrl), "speech-to-text"))
        {
            Content = form,
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

            var body = await response.Content.ReadFromJsonAsync<SarvamTranscript>(Json, cancellationToken)
                       ?? throw new SpeechException(SpeechFailure.Unavailable, "Sarvam returned an empty body.");
            return new Transcription(body.Transcript ?? "", body.LanguageCode, body.RequestId, Guid.Empty);
        }
    }

    /// <summary>
    /// Sarvam rejects any key term containing a comma or semicolon (it reads them as lists), e.g. the lesson
    /// name "Compare 3-digit numbers (>, <, =)". Those become spaces; empty and duplicate terms are dropped.
    /// </summary>
    private static List<string> CleanKeyTerms(IReadOnlyList<string> keyTerms) =>
        keyTerms
            .Select(term => string.Join(' ', term.Split([',', ';', ' '], StringSplitOptions.RemoveEmptyEntries)))
            .Where(term => term.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .Take(50)
            .ToList();

    /// <summary>Sarvam's error message (about the request, never the audio's content), for the AiCall log.</summary>
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

    private sealed record SarvamTranscript(string? Transcript, string? LanguageCode, string? RequestId);

    private sealed record SarvamError(SarvamErrorDetail? Error);

    private sealed record SarvamErrorDetail(string? Message);
}
