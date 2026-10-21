namespace SlateAi.Api.Features.Attendance.Source;

public sealed class SatsOptions
{
    public const string Section = "Attendance:Sats";

    /// <summary>When the simulated register "arrives" each school day, IST.</summary>
    public TimeOnly ArrivesAt { get; set; } = new(9, 5);

    /// <summary>The time Today promises to check again while nothing has come in, IST.</summary>
    public TimeOnly RecheckAt { get; set; } = new(10, 0);

    /// <summary>Share of children the simulation marks absent on an ordinary day.</summary>
    public double AbsenceRate { get; set; } = 0.08;

    /// <summary>School holidays, on top of Sundays and second Saturdays.</summary>
    public List<DateOnly> Holidays { get; set; } = [];

    /// <summary>Scripted weekly absences for the demo story, relative to the app's output directory.</summary>
    public string ScenarioFile { get; set; } = "data/seed/sats-scenario.json";
}
