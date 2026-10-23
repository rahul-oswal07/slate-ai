namespace SlateAi.Api.Features.Attendance;

/// <summary>School days and times are in India Standard Time.</summary>
public static class SchoolClock
{
    // India has no daylight saving, so a fixed offset avoids depending on tzdata in the chiseled image.
    public static readonly TimeSpan Ist = TimeSpan.FromHours(5.5);

    public static DateOnly Today(TimeProvider clock) => DateOnly.FromDateTime(clock.GetUtcNow().ToOffset(Ist).DateTime);

    /// <summary>The school day (IST) an instant falls on.</summary>
    public static DateOnly DateOf(DateTimeOffset instant) => DateOnly.FromDateTime(instant.ToOffset(Ist).DateTime);

    /// <summary>The instant <paramref name="time"/> IST on <paramref name="date"/>.</summary>
    public static DateTimeOffset At(DateOnly date, TimeOnly time) => new(date.ToDateTime(time), Ist);
}
