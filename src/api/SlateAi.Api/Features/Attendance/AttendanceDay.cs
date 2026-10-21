using SlateAi.Api.Features.Attendance.Source;

namespace SlateAi.Api.Features.Attendance;

public static class AttendanceDayStatus
{
    /// <summary>From SATS, possibly corrected by the teacher.</summary>
    public const string Sats = "sats";

    /// <summary>Marked by the teacher because SATS hadn't arrived.</summary>
    public const string Manual = "manual";

    /// <summary>A school day with nothing in yet, before the retry time.</summary>
    public const string Waiting = "waiting";

    /// <summary>Still nothing after the retry (D-67): nobody is marked absent automatically.</summary>
    public const string Missing = "missing";

    /// <summary>No school (Sunday, second Saturday, holiday) and nothing marked.</summary>
    public const string NoSchool = "noSchool";
}

/// <summary>Today's register as the T1 attendance card shows it.</summary>
/// <param name="ReceivedAt">When the SATS register arrived (also set for "manual" once SATS came in later).</param>
/// <param name="Corrected">The teacher changed at least one child from what SATS said.</param>
/// <param name="SatsDiffers">Status "manual" and SATS came in later with a different register (D-69).</param>
/// <param name="SyncAt">While waiting: when the register comes in (IST, D-45).</param>
/// <param name="RetryAt">While waiting: when it is checked again if late (IST, D-45).</param>
public sealed record AttendanceDayResponse(
    DateOnly Date,
    string Status,
    DateTimeOffset? ReceivedAt,
    bool Corrected,
    bool SatsDiffers,
    TimeOnly? SyncAt,
    TimeOnly? RetryAt,
    IReadOnlyList<Guid> AbsentChildIds,
    IReadOnlyList<RosterChild> Children);

public static class AttendanceDay
{
    public static AttendanceDayResponse Describe(
        DateOnly date,
        IReadOnlyList<RosterChild> roster,
        IReadOnlyList<AttendanceRow> rows,
        bool noSchool,
        DateTimeOffset now,
        SatsOptions options)
    {
        if (rows.Count == 0)
        {
            var status = noSchool ? AttendanceDayStatus.NoSchool
                : now >= SchoolClock.At(date, options.RetryAt) ? AttendanceDayStatus.Missing
                : AttendanceDayStatus.Waiting;
            var waiting = status == AttendanceDayStatus.Waiting;
            return new AttendanceDayResponse(date, status, null, false, false,
                waiting ? options.ArrivesAt : null, waiting ? options.RetryAt : null, [], roster);
        }

        var fromSats = rows.Any(r => r.Source == AttendanceSources.Sats);
        return new AttendanceDayResponse(
            date,
            fromSats ? AttendanceDayStatus.Sats : AttendanceDayStatus.Manual,
            rows.Max(r => r.ReceivedAt),
            Corrected: fromSats && rows.Any(r => r.CorrectedAt is not null),
            SatsDiffers: !fromSats && rows.Any(r => r.SourcePresent is { } sats && sats != r.Present),
            SyncAt: null,
            RetryAt: null,
            rows.Where(r => !r.Present).Select(r => r.ChildId).ToList(),
            roster);
    }
}
