namespace SlateAi.Api.Data.Entities;

/// <summary>
/// An eval case for A1 (T-102): what A1 matched for her note and what she confirmed, saved once per confirmation of a
/// matched note. Only curriculum IDs, A1's confidence and A1's name-scrubbed summary and examples (copied from the
/// day's <see cref="Lesson"/>); never a name, the transcript or the audio. Not linked to the lesson row, so it outlives
/// a "Change", a new note and the demo reset.
/// </summary>
public sealed class LessonFeedback
{
    public Guid Id { get; set; }
    public Guid ClassId { get; set; }
    public DateOnly Date { get; set; }

    /// <summary>The note the match came from (the lesson's <c>MappingStartedAt</c> at the time).</summary>
    public DateTimeOffset MappingStartedAt { get; set; }

    /// <summary>Version of <c>prompts/a1-lesson-mapping.md</c> when she confirmed.</summary>
    public int PromptVersion { get; set; }

    /// <summary>"mapped" or "low".</summary>
    public required string MatchStatus { get; set; }

    /// <summary>A1's ranked match.</summary>
    public List<string> MatchedCompetencyIds { get; set; } = [];

    public double? Confidence { get; set; }

    /// <summary>What she confirmed, in book order.</summary>
    public List<string> ChosenCompetencyIds { get; set; } = [];

    /// <summary>"voice" (from the matched list) or "picked" (from the lesson list).</summary>
    public required string ConfirmedVia { get; set; }

    /// <summary>"accepted", "edited" or "replaced".</summary>
    public required string Outcome { get; set; }

    public string? SummaryKn { get; set; }
    public string? SummaryEn { get; set; }
    public List<string> ExamplesUsed { get; set; } = [];
    public DateTimeOffset CreatedAt { get; set; }
}
