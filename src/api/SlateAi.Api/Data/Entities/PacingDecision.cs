namespace SlateAi.Api.Data.Entities;

/// <summary>
/// The teacher's answer to the week's pacing nudge (T6, FR-13): plan a 10-minute recap, or not this week. One per
/// class per week; Change replaces it. A planned recap shows on Today the following week.
/// </summary>
public sealed class PacingDecision
{
    public Guid Id { get; set; }
    public Guid ClassId { get; set; }

    /// <summary>The Monday of the week the nudge was about.</summary>
    public DateOnly WeekOf { get; set; }

    /// <summary>The lesson the nudge named (the one most children missed).</summary>
    public Guid LessonId { get; set; }

    /// <summary>"recap" or "skip".</summary>
    public required string Decision { get; set; }

    public DateTimeOffset DecidedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
