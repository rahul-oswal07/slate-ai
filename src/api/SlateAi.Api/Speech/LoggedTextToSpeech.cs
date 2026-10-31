using Microsoft.Extensions.Options;
using SlateAi.Api.Ai;
using SlateAi.Api.Data.Entities;

namespace SlateAi.Api.Speech;

/// <summary>Records every synthesis in <c>AiCall</c> (purpose "tts"). The text itself is never logged.</summary>
public sealed class LoggedTextToSpeech(
    ITextToSpeech inner, IAiCallStore store, IOptions<AiOptions> ai, IOptions<SarvamOptions> sarvam, TimeProvider clock)
    : ITextToSpeech
{
    public async Task<SpokenAudio> SynthesizeAsync(string text, string languageCode, CancellationToken cancellationToken)
    {
        var call = new AiCall
        {
            Id = Guid.NewGuid(),
            CreatedAt = clock.GetUtcNow(),
            Purpose = "tts",
            ChildIds = AiCallScope.Current?.ToArray(),
            Model = ai.Value.UseFakes ? "fake" : $"{sarvam.Value.TtsModel}/{sarvam.Value.TtsSpeaker}",
        };
        var started = clock.GetTimestamp();
        try
        {
            var audio = await inner.SynthesizeAsync(text, languageCode, cancellationToken);
            return audio with { AiCallId = call.Id };
        }
        catch (SpeechException e)
        {
            call.Error = AiCallText.Error($"{e.Failure}: {e.InnerException?.GetType().Name ?? e.Message}");
            throw;
        }
        catch (Exception e)
        {
            call.Error = e.GetType().Name;
            throw;
        }
        finally
        {
            call.LatencyMs = (int)clock.GetElapsedTime(started).TotalMilliseconds;
            await store.AddAsync(call, CancellationToken.None);
        }
    }
}
