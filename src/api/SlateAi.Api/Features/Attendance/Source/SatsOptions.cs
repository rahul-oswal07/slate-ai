namespace SlateAi.Api.Features.Attendance.Source;

public sealed class SatsOptions
{
    public const string Section = "Attendance:Sats";

    /// <summary>When the register comes in each school day, IST (D-45 sync time).</summary>
    public TimeOnly ArrivesAt { get; set; } = new(12, 0);

    /// <summary>The one retry if it is late, IST (D-45); after it, Today says it is missing (D-67).</summary>
    public TimeOnly RetryAt { get; set; } = new(14, 0);

    /// <summary>
    /// Share of children the simulation marks absent on an ordinary day. The app runs the demo with 0 (appsettings.json),
    /// so only the scripted absences happen and the demo makes exactly three catch-ups (T-18).
    /// </summary>
    public double AbsenceRate { get; set; } = 0.08;

    /// <summary>School holidays, on top of Sundays and second Saturdays.</summary>
    public List<DateOnly> Holidays { get; set; } = [];

    /// <summary>Scripted weekly absences for the demo story, relative to the app's output directory.</summary>
    public string ScenarioFile { get; set; } = "data/seed/sats-scenario.json";
}
