namespace SlateAi.Api.Features.Attendance;

public static class AttendanceSources
{
    public const string Sats = "sats";
    public const string Manual = "manual";
}

/// <summary>One child's stored attendance for a date, with where it came from (D-69).</summary>
/// <param name="Present">The value everything uses (ledger, missed lessons).</param>
/// <param name="Source">"sats" when the row came from SATS (maybe corrected), "manual" when the teacher marked it first.</param>
/// <param name="SourcePresent">What SATS said; null until SATS has sent this date.</param>
/// <param name="ReceivedAt">When the SATS register arrived.</param>
/// <param name="CorrectedAt">When the teacher last changed it from SATS, or marked it herself.</param>
public sealed record AttendanceRow(
    Guid ChildId,
    bool Present,
    string Source,
    bool? SourcePresent,
    DateTimeOffset? ReceivedAt,
    DateTimeOffset? CorrectedAt);

/// <summary>How SATS registers and teacher markings combine (D-69). Pure, so the rules are unit-tested.</summary>
public static class RegisterMerge
{
    /// <summary>
    /// A SATS register arrives. With nothing stored, it becomes the register. If the teacher already marked
    /// the day herself, her marking stands and the SATS values are only kept alongside.
    /// </summary>
    public static IReadOnlyList<AttendanceRow> FromSource(
        IReadOnlyList<AttendanceRow> existing,
        IReadOnlyCollection<Guid> roster,
        IReadOnlySet<Guid> absent,
        DateTimeOffset receivedAt)
    {
        var byChild = existing.ToDictionary(r => r.ChildId);
        return roster.Select(childId =>
        {
            var sourcePresent = !absent.Contains(childId);
            return byChild.TryGetValue(childId, out var row)
                ? row with { SourcePresent = sourcePresent, ReceivedAt = receivedAt }
                : new AttendanceRow(childId, sourcePresent, AttendanceSources.Sats, sourcePresent, receivedAt, null);
        }).ToList();
    }

    /// <summary>
    /// The teacher saves the register. Over SATS rows this is a correction: a row is marked corrected only
    /// while it differs from SATS, so putting it back clears the mark. With nothing stored, she marked it herself.
    /// </summary>
    public static IReadOnlyList<AttendanceRow> FromTeacher(
        IReadOnlyList<AttendanceRow> existing,
        IReadOnlyCollection<Guid> roster,
        IReadOnlySet<Guid> absent,
        DateTimeOffset now)
    {
        var byChild = existing.ToDictionary(r => r.ChildId);
        return roster.Select(childId =>
        {
            var present = !absent.Contains(childId);
            if (!byChild.TryGetValue(childId, out var row))
            {
                return new AttendanceRow(childId, present, AttendanceSources.Manual, null, null, now);
            }

            if (row.Source == AttendanceSources.Manual)
            {
                return row with { Present = present, CorrectedAt = now };
            }

            var correctedAt = present == row.SourcePresent ? null
                : present == row.Present && row.CorrectedAt is not null ? row.CorrectedAt
                : now;
            return row with { Present = present, CorrectedAt = correctedAt };
        }).ToList();
    }
}
