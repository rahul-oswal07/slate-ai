namespace SlateAi.Api.Speech;

public interface ISpeechToText
{
    /// <param name="languageCode">BCP-47, e.g. "kn-IN".</param>
    /// <param name="keyTerms">Words to bias recognition towards (e.g. competency names), at most 50.</param>
    /// <exception cref="SpeechException">Not configured, rejected or unavailable.</exception>
    Task<Transcription> TranscribeAsync(
        Stream audio,
        string fileName,
        string contentType,
        string languageCode,
        IReadOnlyList<string> keyTerms,
        CancellationToken cancellationToken);
}

/// <param name="AiCallId">The <c>AiCall</c> row for this transcription.</param>
public sealed record Transcription(string Text, string? LanguageCode, string? RequestId, Guid AiCallId);

public enum SpeechFailure
{
    NotConfigured,

    /// <summary>The provider refused the input (bad audio, too long, unsupported format): don't retry.</summary>
    Rejected,

    /// <summary>Provider down, rate-limited or timed out: retrying later may work.</summary>
    Unavailable,
}

public sealed class SpeechException(SpeechFailure failure, string message, Exception? inner = null)
    : Exception(message, inner)
{
    public SpeechFailure Failure { get; } = failure;
}

public sealed class SarvamOptions
{
    public const string Section = "Sarvam";

    /// <summary>From user-secrets or the environment only, never appsettings.</summary>
    public string? ApiKey { get; set; }

    public string BaseUrl { get; set; } = "https://api.sarvam.ai/";

    /// <summary>Sarvam's current default STT model (D-12 named Saarika; Sarvam has since moved to Saaras).</summary>
    public string SttModel { get; set; } = "saaras:v4";

    /// <summary>Text-to-speech model (Bulbul, D-12).</summary>
    public string TtsModel { get; set; } = "bulbul:v3";

    /// <summary>Bulbul voice; must belong to <see cref="TtsModel"/>. Try others if a language sounds unnatural (plan R2).</summary>
    public string TtsSpeaker { get; set; } = "priya";

    public double TtsPace { get; set; } = 1.0;

    /// <summary>Sarvam's raw output rate; we encode it to Opus ourselves (Sarvam's own MP3 is a fixed 128 kbps).</summary>
    public int TtsSampleRate { get; set; } = 16000;
}
