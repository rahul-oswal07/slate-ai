using System.Net;
using Microsoft.Extensions.Options;
using SlateAi.Api.Ai;
using SlateAi.Api.Speech;
using SlateAi.Api.Tests.Infrastructure;

namespace SlateAi.Api.Tests.Speech;

public sealed class SarvamSpeechToTextTests
{
    [Fact]
    public async Task Sends_the_audio_model_language_and_key_terms_and_reads_the_transcript()
    {
        var handler = new StubHttpHandler(() => StubHttpHandler.Json(
            """{"request_id":"req-1","transcript":"ಇಂದು ಸಂಕಲನ","language_code":"kn-IN"}"""));

        var result = await Transcribe(handler, keyTerms: ["ಸಂಕಲನ", "ದಶಕ"]);

        Assert.Equal(new Transcription("ಇಂದು ಸಂಕಲನ", "kn-IN", "req-1", Guid.Empty), result);
        var (request, body) = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("https://sarvam.test/speech-to-text", request.RequestUri!.ToString());
        Assert.Equal("sarvam-key", Assert.Single(request.Headers.GetValues("api-subscription-key")));
        Assert.Contains("name=file; filename=note.webm", body);
        Assert.Contains("audio/webm", body);
        Assert.Contains("name=model\r\n\r\nsaaras:v4", body.Replace("Content-Type: text/plain; charset=utf-8\r\n", ""));
        Assert.Contains("kn-IN", body);
        Assert.Contains("name=keyterms", body);
        Assert.Contains("\\u0CB8\\u0C82\\u0C95\\u0CB2\\u0CA8", body); // ಸಂಕಲನ, JSON-escaped
    }

    [Fact]
    public async Task Leaves_out_key_terms_when_there_are_none()
    {
        var handler = new StubHttpHandler(() => StubHttpHandler.Json("""{"transcript":""}"""));

        await Transcribe(handler, keyTerms: []);

        Assert.DoesNotContain("keyterms", Assert.Single(handler.Requests).Body);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, SpeechFailure.Rejected)]
    [InlineData(HttpStatusCode.UnprocessableEntity, SpeechFailure.Rejected)]
    [InlineData(HttpStatusCode.TooManyRequests, SpeechFailure.Unavailable)]
    [InlineData(HttpStatusCode.ServiceUnavailable, SpeechFailure.Unavailable)]
    [InlineData(HttpStatusCode.Forbidden, SpeechFailure.Unavailable)]
    public async Task Maps_error_statuses(HttpStatusCode status, SpeechFailure expected)
    {
        var handler = new StubHttpHandler(() => StubHttpHandler.Json("""{"error":{"message":"x"}}""", status));

        var error = await Assert.ThrowsAsync<SpeechException>(() => Transcribe(handler, keyTerms: []));

        Assert.Equal(expected, error.Failure);
    }

    [Fact]
    public async Task Without_an_api_key_nothing_is_sent()
    {
        var handler = new StubHttpHandler(() => StubHttpHandler.Json("{}"));

        var error = await Assert.ThrowsAsync<SpeechException>(() => Transcribe(handler, keyTerms: [], apiKey: null));

        Assert.Equal(SpeechFailure.NotConfigured, error.Failure);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task Every_transcription_is_logged_including_failures()
    {
        var store = new FakeAiCallStore();
        var ok = Logged(new StubHttpHandler(() => StubHttpHandler.Json("""{"transcript":"x"}""")), store);
        var failing = Logged(new StubHttpHandler(() => StubHttpHandler.Json("{}", HttpStatusCode.BadRequest)), store);

        var result = await ok.TranscribeAsync(
            new MemoryStream([1]), "a.webm", "audio/webm", "kn-IN", [], TestContext.Current.CancellationToken);
        await Assert.ThrowsAsync<SpeechException>(() => failing.TranscribeAsync(
            new MemoryStream([1]), "a.webm", "audio/webm", "kn-IN", [], TestContext.Current.CancellationToken));

        Assert.Equal(2, store.Calls.Count);
        Assert.All(store.Calls, c => Assert.Equal(("stt", "saaras:v4"), (c.Purpose, c.Model)));
        Assert.Equal(store.Calls[0].Id, result.AiCallId);
        Assert.Null(store.Calls[0].Error);
        Assert.Equal("Rejected: Sarvam returned 400.", store.Calls[1].Error);
    }

    private static Task<Transcription> Transcribe(
        StubHttpHandler handler, IReadOnlyList<string> keyTerms, string? apiKey = "sarvam-key") =>
        Sarvam(handler, apiKey).TranscribeAsync(
            new MemoryStream([1, 2, 3]), "note.webm", "audio/webm", "kn-IN", keyTerms, TestContext.Current.CancellationToken);

    private static SarvamSpeechToText Sarvam(StubHttpHandler handler, string? apiKey = "sarvam-key") => new(
        new HttpClient(handler),
        Options.Create(new SarvamOptions { ApiKey = apiKey, BaseUrl = "https://sarvam.test/" }));

    private static LoggedSpeechToText Logged(StubHttpHandler handler, FakeAiCallStore store) => new(
        Sarvam(handler),
        store,
        Options.Create(new AiOptions()),
        Options.Create(new SarvamOptions()),
        TimeProvider.System);
}
