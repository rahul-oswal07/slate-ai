namespace SlateAi.Api.Speech;

public interface ITextToSpeech
{
    /// <summary>Speaks <paramref name="text"/> (no names in it, D-50) and returns Ogg Opus audio (24 kbps).</summary>
    /// <param name="languageCode">BCP-47, e.g. "te-IN".</param>
    /// <exception cref="SpeechException">Not configured, rejected or unavailable.</exception>
    Task<SpokenAudio> SynthesizeAsync(string text, string languageCode, CancellationToken cancellationToken);
}

/// <param name="Audio">Encoded audio of <paramref name="ContentType"/>.</param>
/// <param name="Seconds">Exact, from the samples, not estimated.</param>
/// <param name="AiCallId">The <c>AiCall</c> row for this synthesis.</param>
public sealed record SpokenAudio(byte[] Audio, string ContentType, double Seconds, Guid AiCallId);
