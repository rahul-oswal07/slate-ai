namespace SlateAi.Api.Data.Entities;

/// <summary>
/// What one curriculum lesson is about, for every class (T-141, D-123): a short description of how the book teaches it
/// and a few worked examples, in English. A9 builds today's message's spoken concept on it. Loaded from
/// <c>data/lesson-content/</c> at startup and editable on <c>/admin/lesson-links</c>; an edit is never overwritten.
/// </summary>
public sealed class LessonContent
{
    /// <summary>The curriculum competency ID, e.g. "M3.ADD.CARRY". No FK: the curriculum isn't in the database.</summary>
    public required string CompetencyId { get; set; }

    public required string Description { get; set; }

    /// <summary>1–4 worked examples, one sentence each (<c>LessonContentRules</c>).</summary>
    public required string[] Examples { get; set; }

    /// <summary><see cref="LessonContentSources"/>: from the textbook, written for the app, or edited on the admin page.</summary>
    public required string Source { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>Set by an edit on the admin page; the startup load then leaves the row alone.</summary>
    public DateTimeOffset? EditedAt { get; set; }
}

public static class LessonContentSources
{
    public const string Textbook = "textbook";
    public const string Written = "written";
    public const string Edited = "edited";
}
