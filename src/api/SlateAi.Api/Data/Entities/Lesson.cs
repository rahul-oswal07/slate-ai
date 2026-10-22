namespace SlateAi.Api.Data.Entities;

/// <summary>
/// What was taught on a day: one row per class per day (D-55). Only <c>Status = confirmed</c> reaches missed
/// lessons. Neither the transcript nor the audio is stored (privacy; her recording stays on her phone).
/// </summary>
public sealed class Lesson
{
    public Guid Id { get; set; }
    public Guid ClassId { get; set; }
    public DateOnly Date { get; set; }

    /// <summary>"voice", "photo" or "picked".</summary>
    public required string Source { get; set; }

    /// <summary>Curriculum IDs such as "M3.ADD.CARRY": the match (ranked) until confirmed, then what she confirmed.</summary>
    public List<string> CompetencyIds { get; set; } = [];

    /// <summary>A1's one-line summary of her note (FR-03), Kannada and English (D-56).</summary>
    public string? SummaryKn { get; set; }

    public string? SummaryEn { get; set; }

    /// <summary>Examples she used, as A1 quoted them from the (name-scrubbed) note.</summary>
    public List<string> ExamplesUsed { get; set; } = [];

    /// <summary>A1's confidence 0–1; never shown to her (T2: confidence in words).</summary>
    public double? Confidence { get; set; }

    /// <summary>When the voice note was queued for matching; a stale "processing" reads as unclear.</summary>
    public DateTimeOffset? MappingStartedAt { get; set; }

    /// <summary>"processing", "mapped", "low", "unclear" or "confirmed".</summary>
    public required string Status { get; set; }
}
