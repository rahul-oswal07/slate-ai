namespace SlateAi.Api.Data.Entities;

/// <summary>
/// One row per child per school day. A date with no rows for a class is not a school day
/// (holiday, break), so it never counts as an absence. Provenance rules: D-69, <c>RegisterMerge</c>.
/// </summary>
public sealed class Attendance
{
    public Guid ClassId { get; set; }
    public DateOnly Date { get; set; }
    public Guid ChildId { get; set; }

    /// <summary>The effective value: SATS, the teacher's correction, or her own marking.</summary>
    public bool Present { get; set; }

    /// <summary>"sats" or "manual" (the teacher marked the day before SATS arrived).</summary>
    public string Source { get; set; } = "manual";

    /// <summary>What SATS said; null until SATS sent this date. Never written back to SATS.</summary>
    public bool? SourcePresent { get; set; }

    public DateTimeOffset? ReceivedAt { get; set; }

    public DateTimeOffset? CorrectedAt { get; set; }
}
