using Microsoft.Extensions.Options;
using SlateAi.Api.Features.Attendance.Source;

namespace SlateAi.Api.Features.Attendance;

/// <summary>
/// Loads a day's register, fetching it from the attendance source first when it is due and nothing from SATS is
/// stored yet (D-45, D-69). Used by Today and by the 6 pm evening-message job, so the job finds the register
/// even when nobody opened Today.
/// </summary>
public sealed class AttendanceDayLoader(
    IAttendanceStore store,
    IAttendanceSource source,
    SatsDemoHolds holds,
    IOptions<SatsOptions> options,
    TimeProvider clock)
{
    /// <summary>The day as Today shows it, or null if the class doesn't exist.</summary>
    public async Task<AttendanceDayResponse?> LoadAsync(Guid classId, DateOnly date, CancellationToken cancellationToken)
    {
        var roster = await store.GetRosterAsync(classId, cancellationToken);
        if (roster is null)
        {
            return null;
        }

        var now = clock.GetUtcNow();
        var rows = await store.GetDayAsync(classId, date, cancellationToken);
        var noSchool = false;
        var satsDue = now >= SchoolClock.At(date, options.Value.ArrivesAt) && !holds.IsHeld(classId, date);
        if (satsDue && rows.All(r => r.SourcePresent is null))
        {
            switch (await source.FetchAsync(classId, date, roster, cancellationToken))
            {
                case SourceResult.Arrived register:
                    rows = await store.SaveSourceRegisterAsync(
                        classId, date, roster.Select(c => c.Id).ToList(), register, cancellationToken);
                    break;
                case SourceResult.NoSchool:
                    noSchool = true;
                    break;
            }
        }

        return AttendanceDay.Describe(classId, date, roster, rows, noSchool, now, options.Value);
    }
}
