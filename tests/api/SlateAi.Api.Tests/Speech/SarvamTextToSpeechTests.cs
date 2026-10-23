using System.Net;
using Microsoft.Extensions.Options;
using SlateAi.Api.Ai;
using SlateAi.Api.Speech;
using SlateAi.Api.Tests.Infrastructure;

namespace SlateAi.Api.Tests.Speech;

public sealed class SarvamTextToSpeechTests
{
    private static readonly CancellationToken Ct = TestContext.Current.CancellationToken;
    private static string WavBase64(double seconds) => Convert.ToBase64String(FakeTextToSpeech.SilentWav(seconds));

    [Fact]
    public async Task Asks_for_16_khz_wav_in_the_language_and_voice_then_encodes_opus()
    {
        var handler = new StubHttpHandler(() => StubHttpHandler.Json($$"""{"request_id":"r1","audios":["{{WavBase64(3)}}"]}"""));

        var audio = await Tts(handler).SynthesizeAsync("ఇది మీ బిడ్డ టీచర్ పంపిన పాఠం.", "te-IN", Ct);

        var (request, body) = Assert.Single(handler.Requests);
        Assert.Equal("https://sarvam.test/text-to-speech", request.RequestUri!.ToString());
        Assert.Equal("sarvam-key", Assert.Single(request.Headers.GetValues("api-subscription-key")));
        Assert.Contains("\"language_code\":\"te-IN\"", body);
        Assert.Contains("\"model\":\"bulbul:v3\"", body);
        Assert.Contains("\"speaker\":\"priya\"", body);
        Assert.Contains("\"speech_sample_rate\":16000", body);
        Assert.Contains("\"output_audio_codec\":\"wav\"", body);
        Assert.Equal("audio/ogg", audio.ContentType);
        Assert.Equal("OggS", System.Text.Encoding.ASCII.GetString(audio.Audio, 0, 4));
        Assert.Equal(3, audio.Seconds, precision: 2);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, SpeechFailure.Rejected)]
    [InlineData(HttpStatusCode.UnprocessableEntity, SpeechFailure.Rejected)]
    [InlineData(HttpStatusCode.TooManyRequests, SpeechFailure.Unavailable)]
    [InlineData(HttpStatusCode.InternalServerError, SpeechFailure.Unavailable)]
    public async Task Errors_map_like_speech_to_text(HttpStatusCode status, SpeechFailure failure)
    {
        var handler = new StubHttpHandler(() => StubHttpHandler.Json("""{"error":{"message":"bad speaker"}}""", status));

        var e = await Assert.ThrowsAsync<SpeechException>(() => Tts(handler).SynthesizeAsync("ಕಳೆ", "kn-IN", Ct));

        Assert.Equal(failure, e.Failure);
        Assert.Contains("bad speaker", e.Message);
    }

    [Fact]
    public async Task No_key_text_too_long_or_unreadable_audio_fail_cleanly()
    {
        var handler = new StubHttpHandler(() => StubHttpHandler.Json("""{"audios":["bm90IGEgd2F2"]}"""));

        Assert.Equal(SpeechFailure.NotConfigured,
            (await Assert.ThrowsAsync<SpeechException>(() => Tts(handler, apiKey: null).SynthesizeAsync("x", "kn-IN", Ct))).Failure);
        Assert.Equal(SpeechFailure.Rejected,
            (await Assert.ThrowsAsync<SpeechException>(() => Tts(handler).SynthesizeAsync(new string('ಕ', 2501), "kn-IN", Ct))).Failure);
        Assert.Empty(handler.Requests);
        Assert.Equal(SpeechFailure.Unavailable,
            (await Assert.ThrowsAsync<SpeechException>(() => Tts(handler).SynthesizeAsync("ಕಳೆ", "kn-IN", Ct))).Failure);
    }

    [Fact]
    public async Task Every_synthesis_is_logged_without_its_text()
    {
        var store = new FakeAiCallStore();
        var ok = Logged(new StubHttpHandler(() => StubHttpHandler.Json($$"""{"audios":["{{WavBase64(1)}}"]}""")), store);
        var failing = Logged(new StubHttpHandler(() => StubHttpHandler.Json("{}", HttpStatusCode.BadRequest)), store);

        var audio = await ok.SynthesizeAsync("ರಹಸ್ಯ ಪಠ್ಯ", "kn-IN", Ct);
        await Assert.ThrowsAsync<SpeechException>(() => failing.SynthesizeAsync("ರಹಸ್ಯ ಪಠ್ಯ", "kn-IN", Ct));

        Assert.Equal(2, store.Calls.Count);
        Assert.All(store.Calls, c => Assert.Equal(("tts", "bulbul:v3/priya"), (c.Purpose, c.Model)));
        Assert.Equal(store.Calls[0].Id, audio.AiCallId);
        Assert.NotNull(store.Calls[1].Error);
        Assert.All(store.Calls, c => Assert.DoesNotContain("ರಹಸ್ಯ", (c.Error ?? "") + c.Verdict));
    }

    private static SarvamTextToSpeech Tts(StubHttpHandler handler, string? apiKey = "sarvam-key") =>
        new(new HttpClient(handler), Options.Create(new SarvamOptions { ApiKey = apiKey, BaseUrl = "https://sarvam.test/" }));

    private static LoggedTextToSpeech Logged(StubHttpHandler handler, FakeAiCallStore store) => new(
        Tts(handler),
        store,
        Options.Create(new AiOptions()),
        Options.Create(new SarvamOptions()),
        TimeProvider.System);
}
