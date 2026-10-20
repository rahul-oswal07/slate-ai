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
        file.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
        using var form = new MultipartFormDataContent
        {
            { file, "file", fileName },
            { new StringContent(settings.SttModel), "model" },
            { new StringContent(languageCode), "language_code" },
        };
        if (keyTerms.Count > 0)
        {
            form.Add(new StringContent(JsonSerializer.Serialize(keyTerms.Take(50))), "keyterms");
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
                throw new SpeechException(failure, $"Sarvam returned {(int)response.StatusCode}.");
            }

            var body = await response.Content.ReadFromJsonAsync<SarvamTranscript>(Json, cancellationToken)
                       ?? throw new SpeechException(SpeechFailure.Unavailable, "Sarvam returned an empty body.");
            return new Transcription(body.Transcript ?? "", body.LanguageCode, body.RequestId, Guid.Empty);
        }
    }

    private sealed record SarvamTranscript(string? Transcript, string? LanguageCode, string? RequestId);
}
