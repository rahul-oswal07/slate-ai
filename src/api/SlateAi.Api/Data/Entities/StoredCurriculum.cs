namespace SlateAi.Api.Data.Entities;

/// <summary>
/// A curriculum kept in the database (T-145, D-125), e.g. one made from a textbook by the importer (T-146). A class whose
/// <see cref="Class.CurriculumId"/> is null uses the built-in one from <c>data/curriculum/*.json</c>, which is not stored
/// here. A <see cref="CurriculumStatuses.Published"/> curriculum never changes, so its graph can be cached by ID; a change
/// is a new curriculum.
/// </summary>
public sealed class StoredCurriculum
{
    public Guid Id { get; set; }

    /// <summary>What the admin page calls it, e.g. "Grade 3 maths, KTBS 2026-27".</summary>
    public required string Title { get; set; }

    public int Grade { get; set; }

    /// <summary>e.g. "maths".</summary>
    public required string Subject { get; set; }

    /// <summary>Medium of instruction as a language code, e.g. "kn" (as <see cref="Class.Medium"/>).</summary>
    public required string Medium { get; set; }

    /// <summary><see cref="CurriculumStatuses"/>.</summary>
    public required string Status { get; set; }

    /// <summary>The chapter titles, a JSON array of <c>Curriculum.Chapter</c>.</summary>
    public required string Chapters { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? PublishedAt { get; set; }

    /// <summary>The <see cref="CurriculumImport"/> that made it from a textbook (T-146), if any.</summary>
    public Guid? ImportId { get; set; }
}

/// <summary>One lesson of a <see cref="StoredCurriculum"/>.</summary>
public sealed class StoredCurriculumLesson
{
    public Guid CurriculumId { get; set; }

    /// <summary>e.g. "M3.ADD.CARRY"; unique within the curriculum, not across curricula.</summary>
    public required string CompetencyId { get; set; }

    /// <summary>Teaching order, copied out of <see cref="Competency"/> so rows can be listed in order.</summary>
    public int Sequence { get; set; }

    /// <summary>The lesson as a JSON <c>Curriculum.Competency</c>, checked by <c>CurriculumLoader</c> like a file's.</summary>
    public required string Competency { get; set; }

    /// <summary>
    /// <see cref="CurriculumReviewStatuses"/>: a lesson made from a textbook needs an admin to check it before the
    /// curriculum can be published (T-147).
    /// </summary>
    public string ReviewStatus { get; set; } = CurriculumReviewStatuses.Reviewed;

    /// <summary>What the admin should look at, from the importer's checks, e.g. "exercise dropped: add 600 + 500".</summary>
    public string[] Flags { get; set; } = [];

    /// <summary>
    /// What the importer found that is not part of <c>Competency</c>, as JSON <c>LessonExtras</c>: the printed pages, the
    /// description and examples (our own words), the book's other exercises and its pictures, described (T-146, D-125).
    /// </summary>
    public string? Extras { get; set; }
}

public static class CurriculumReviewStatuses
{
    public const string NeedsReview = "needs-review";
    public const string Reviewed = "reviewed";
}

public static class CurriculumStatuses
{
    /// <summary>Being made or reviewed; no class can use it.</summary>
    public const string Draft = "draft";

    /// <summary>Checked and fixed: classes can use it, and it never changes again.</summary>
    public const string Published = "published";
}
