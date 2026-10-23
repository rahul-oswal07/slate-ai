namespace SlateAi.Api.Data.Entities;

/// <summary>
/// Why a child was away, as the family told us from the evening message (FR-21, D-23): a category only, never
/// symptoms, a transcript or audio. One per child per absent day; the latest tap wins. Only the teacher sees it (T6).
/// </summary>
public sealed class AbsenceReason
{
    public Guid Id { get; set; }
    public Guid ClassId { get; set; }
    public Guid ChildId { get; set; }

    /// <summary>The evening message's date: the day of absence.</summary>
    public DateOnly Date { get; set; }

    /// <summary>"unwell", "travelOrWork", "festival", "neededAtHome" or "other".</summary>
    public required string Category { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
