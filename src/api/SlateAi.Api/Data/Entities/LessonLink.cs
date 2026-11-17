namespace SlateAi.Api.Data.Entities;

/// <summary>
/// The video link for one lesson (T-94, D-106), set on the admin page by whoever checked it. One per curriculum lesson
/// for every class; code copies it into a new today's message draft, never the AI.
/// </summary>
public sealed class LessonLink
{
    /// <summary>The curriculum the lesson is in (T-147); <see cref="Guid.Empty"/> for the built-in one.</summary>
    public Guid CurriculumId { get; set; }

    /// <summary>The curriculum competency ID, e.g. "M3.BRIDGE.COMPARE".</summary>
    public required string CompetencyId { get; set; }

    /// <summary>An absolute https URL (<c>LessonLinkRules</c>).</summary>
    public required string Url { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
