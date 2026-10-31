using SlateAi.Api.Features.Attendance;

namespace SlateAi.Api.Features.Demo;

/// <summary>
/// The app's clock when <c>Demo:Today</c> is set (T-75, D-88; staging only): every day is that school day, at the
/// real time of day in IST, so the demo week is always behind whenever it is opened. Timers stay real (the base
/// <see cref="TimeProvider.CreateTimer"/>), so the 6 pm job still runs at 6 pm. At midnight IST it goes back to 00:00
/// of the same day.
/// </summary>
public sealed class DemoClock(DateOnly today, TimeProvider real) : TimeProvider
{
    /// <summary>The header that tells the web app which date is today (<c>istToday()</c> in <c>schoolTime.ts</c>).</summary>
    public const string Header = "X-School-Today";

    public DateOnly Today { get; } = today;

    public override DateTimeOffset GetUtcNow()
    {
        var now = real.GetUtcNow().ToOffset(SchoolClock.Ist);
        return SchoolClock.At(Today, TimeOnly.FromDateTime(now.DateTime)).ToUniversalTime();
    }
}
