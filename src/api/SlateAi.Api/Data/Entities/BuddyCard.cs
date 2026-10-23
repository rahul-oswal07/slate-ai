namespace SlateAi.Api.Data.Entities;

/// <summary>
/// The teacher's ticks on a return-day buddy card (T5, FR-12, D-22). The card itself (buddy, sums, words, page) is
/// computed from attendance and lessons each time, so a register correction changes it; only her taps are stored.
/// </summary>
public sealed class BuddyCard
{
    public Guid Id { get; set; }
    public Guid ClassId { get; set; }
    public Guid ChildId { get; set; }

    /// <summary>The day the child came back.</summary>
    public DateOnly Date { get; set; }

    /// <summary>"Brought homework" (D-22): the textbook page was shown.</summary>
    public DateTimeOffset? HomeworkReturnedAt { get; set; }

    /// <summary>"Mark buddy time done" (B-31); Undo clears it.</summary>
    public DateTimeOffset? DoneAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
