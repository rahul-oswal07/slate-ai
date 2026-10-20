namespace SlateAi.Api.Data.Entities;

/// <summary>
/// One call to Claude or Sarvam, for cost tracking and the "Under the hood" panel (T-18).
/// Never stores prompt text, transcripts or names; only what was called, how big, how long and the outcome.
/// </summary>
public sealed class AiCall
{
    public Guid Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Prompt ID (e.g. "a1-lesson-mapping") or "stt".</summary>
    public required string Purpose { get; set; }

    public required string Model { get; set; }
    public int? PromptVersion { get; set; }
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public int CacheReadTokens { get; set; }
    public int LatencyMs { get; set; }

    /// <summary>Set by the caller after validating the output, e.g. "valid" or "fallback:low-confidence".</summary>
    public string? Verdict { get; set; }

    /// <summary>Failure kind and exception type when the call failed; null on success.</summary>
    public string? Error { get; set; }
}
