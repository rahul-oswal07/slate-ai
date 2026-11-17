namespace SlateAi.Api.Data.Entities;

/// <summary>
/// A textbook being turned into a draft curriculum (T-146, D-125): uploaded on <c>/admin/curriculum</c>, read by Claude
/// (prompts A10) in a background job, checked by code. When it is <see cref="CurriculumImportStatuses.Ready"/>, its
/// draft is <see cref="CurriculumId"/> and the book is deleted.
/// </summary>
public sealed class CurriculumImport
{
    public Guid Id { get; set; }

    /// <summary>The upload's request ID: a replayed upload returns this import instead of starting another.</summary>
    public Guid ClientRequestId { get; set; }

    /// <summary>What the admin page calls it, e.g. "Grade 3 maths, KTBS 2026-27 Part 1".</summary>
    public required string Title { get; set; }

    /// <summary>1–5, so lesson IDs are <c>M{Grade}.…</c>.</summary>
    public int Grade { get; set; }

    public required string Subject { get; set; }

    /// <summary>Medium of instruction, e.g. "kn".</summary>
    public required string Medium { get; set; }

    /// <summary>The book a lesson's page is in: "textbook", "workbook" or "nalikali" (<c>TextbookPage.Book</c>).</summary>
    public required string Book { get; set; }

    /// <summary>The book's part (1 or 2), if it has parts.</summary>
    public int? Part { get; set; }

    public int PageCount { get; set; }

    /// <summary><see cref="CurriculumImportStatuses"/>.</summary>
    public required string Status { get; set; }

    /// <summary>While running: <see cref="CurriculumImportStages"/>.</summary>
    public string? Stage { get; set; }

    /// <summary>Calls to Claude done so far, and how many there will be (known once the outline is read).</summary>
    public int StepsDone { get; set; }

    public int StepsTotal { get; set; }

    /// <summary>Why it failed, short, e.g. "error:Unavailable" or "fallback:no-chapters". Never book text.</summary>
    public string? Error { get; set; }

    /// <summary>Notes about the whole book for the review page, e.g. "Chapter numbers were … numbered in order."</summary>
    public string[] Notes { get; set; } = [];

    /// <summary>The draft curriculum it made, once ready.</summary>
    public Guid? CurriculumId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Moves on every step: a running import that hasn't moved for a while is stale (the app restarted).</summary>
    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>
/// The uploaded book, kept once until its draft is written or the import is discarded (D-125). Each Claude call cuts its
/// few pages from it: stored as single pages, a book takes about five times its size.
/// </summary>
public sealed class CurriculumImportFile
{
    public Guid ImportId { get; set; }

    public required byte[] Pdf { get; set; }

    /// <summary>Each page's size in bytes when cut on its own, in page order, so calls stay under the request limit.</summary>
    public required int[] PageSizes { get; set; }
}

/// <summary>
/// One finished step of an import, checked by code, e.g. the outline of pages 11–20. A retry skips steps that are here,
/// so it carries on where it stopped.
/// </summary>
public sealed class CurriculumImportStep
{
    public Guid ImportId { get; set; }

    /// <summary>e.g. "outline:11-20", "lessons:3:1", "prerequisites".</summary>
    public required string Key { get; set; }

    /// <summary>The step's checked result as JSON.</summary>
    public required string Result { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}

public static class CurriculumImportStatuses
{
    public const string Queued = "queued";
    public const string Running = "running";
    public const string Ready = "ready";
    public const string Failed = "failed";
}

public static class CurriculumImportStages
{
    public const string Outline = "outline";
    public const string Lessons = "lessons";
    public const string Prerequisites = "prerequisites";
    public const string Assemble = "assemble";
}
