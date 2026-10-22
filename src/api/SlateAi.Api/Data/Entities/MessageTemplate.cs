namespace SlateAi.Api.Data.Entities;

/// <summary>
/// The evening message the teacher approves once (FR-20, D-21): fixed text per home language with the slots
/// <c>{child}</c>, <c>{lesson}</c> and <c>{page}</c>, filled by code. One row per class and language.
/// </summary>
public sealed class MessageTemplate
{
    public Guid Id { get; set; }
    public Guid ClassId { get; set; }

    /// <summary>"kn", "te" or "ta".</summary>
    public required string Language { get; set; }

    /// <summary>Version of the wording file the text came from; approving a stale version is refused.</summary>
    public required string Version { get; set; }

    public required string Text { get; set; }

    /// <summary>How <c>{page}</c> is written, e.g. "Part {part}, page {page}".</summary>
    public required string PageFormat { get; set; }

    /// <summary>How <c>{page}</c> is written when the book has no parts, e.g. "page {page}".</summary>
    public required string PageFormatNoPart { get; set; }

    /// <summary>"draft" or "approved".</summary>
    public required string Status { get; set; }

    public DateTimeOffset? ApprovedAt { get; set; }
    public Guid? ApprovedByTeacherId { get; set; }
}
