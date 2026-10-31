using Microsoft.Extensions.Options;
using SlateAi.Api.Ai;
using SlateAi.Api.Data.Entities;

namespace SlateAi.Api.Speech;

/// <summary>Records every transcription in <c>AiCall</c> (purpose "stt") next to the Claude calls.</summary>
public sealed class LoggedSpeechToText(
    ISpeechToText inner, IAiCallStore store, IOptions<AiOptions> ai, IOptions<SarvamOptions> sarvam, TimeProvider clock)
    : ISpeechToText
{
    public async Task<Transcription> TranscribeAsync(
        Stream audio,
        string fileName,
        string contentType,
        string languageCode,
        IReadOnlyList<string> keyTerms,
        CancellationToken cancellationToken)
    {
        var call = new AiCall
        {
            Id = Guid.NewGuid(),
            CreatedAt = clock.GetUtcNow(),
            Purpose = "stt",
            ChildIds = AiCallScope.Current?.ToArray(),
            Model = ai.Value.UseFakes ? "fake" : sarvam.Value.SttModel,
        };
        var started = clock.GetTimestamp();
        try
        {
            var transcription = await inner.TranscribeAsync(
                audio, fileName, contentType, languageCode, keyTerms, cancellationToken);
            return transcription with { AiCallId = call.Id };
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
