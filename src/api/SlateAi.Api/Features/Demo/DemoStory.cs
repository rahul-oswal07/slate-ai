using SlateAi.Api.Features.Attendance.Source;
using SlateAi.Api.Features.CatchUps.Compression;

namespace SlateAi.Api.Features.Demo;

/// <summary>One school day the demo Reset writes: the register as SATS sent it and the lesson she confirmed.</summary>
public sealed record DemoDay(DateOnly Date, IReadOnlySet<Guid> AbsentChildIds, string CompetencyId);

/// <summary>A school day with a register and no lesson: the earlier weeks that give the stars card something to show (T-63).</summary>
public sealed record DemoRegisterDay(DateOnly Date, IReadOnlySet<Guid> AbsentChildIds);

/// <summary>
/// Ravi's story on the real calendar (T-18): the school days before today, so the demo lines up whatever day a
/// judge opens it. Pure; the Reset store writes what this returns. Only three children are ever away, one per family
/// language and length of absence, so this week makes exactly three catch-ups (Telugu, Kannada, English). Today itself
/// is left to the simulated feed, whose scenario marks Ravi and Rohan absent every school day (no random absences in
/// the demo).
/// </summary>
public static class DemoStory
{
    /// <summary>Ravi: Telugu, consent Yes. Away all of this week up to today, so the catch-up covers the week.</summary>
    public const int RaviRollNo = 4;

    /// <summary>Manjunath: Kannada. Away the two school days before today and back today, so T5 has a buddy card.</summary>
    public const int ManjunathRollNo = 15;

    /// <summary>
    /// Rohan: English-speaking family, the demo's parent phone. Away today only (from the feed), so his family gets
    /// tonight's message, the reason buttons and an English catch-up of today's lesson.
    /// </summary>
    public const int RohanRollNo = 24;

    /// <summary>The children the story follows; one of them owns the pre-made catch-up (its language).</summary>
    public static readonly IReadOnlySet<int> StoryRollNos = new HashSet<int> { RaviRollNo, ManjunathRollNo, RohanRollNo };

    /// <summary>The lessons taught on the seeded days, oldest first; today's next in the book is <c>M3.ADD.CARRY</c>.</summary>
    public static readonly IReadOnlyList<string> Lessons =
        ["M3.PV.HTO", "M3.PV.EXPAND", "M3.PV.FACE", "M3.NUM.COMPARE", "M3.ADD.NOCARRY"];

    /// <summary>
    /// The <see cref="Lessons"/>.Count school days before <paramref name="today"/>, oldest first. Ravi is away on this
    /// week's days (present last week, so his spell starts this week) and Manjunath on the last two days; everyone else,
    /// Rohan too, is present.
    /// </summary>
    public static IReadOnlyList<DemoDay> Build(DateOnly today, IReadOnlyList<RosterChild> roster, Func<DateOnly, bool> isSchoolDay)
    {
        var dates = new List<DateOnly>();
        for (var date = today.AddDays(-1); dates.Count < Lessons.Count; date = date.AddDays(-1))
        {
            if (isSchoolDay(date))
            {
                dates.Add(date);
            }
        }

        dates.Reverse();
        var ravi = roster.SingleOrDefault(c => c.RollNo == RaviRollNo)?.Id;
        var manjunath = roster.SingleOrDefault(c => c.RollNo == ManjunathRollNo)?.Id;
        var weekStart = CompressionPlanner.Week(today).From;

        return dates.Select((date, i) =>
        {
            var absent = new HashSet<Guid>();
            Add(absent, ravi, date >= weekStart);
            Add(absent, manjunath, i >= dates.Count - 2);
            return new DemoDay(date, absent, Lessons[i]);
        }).ToList();
    }

    /// <summary>Away one day in the first star week: still earns that week's star (the rule is kind).</summary>
    public const int OneDayAwayRollNo = 7;

    /// <summary>Away three days in the second star week: no star that week, the others still count.</summary>
    public const int ThreeDaysAwayRollNo = 10;

    /// <summary>
    /// Registers for the three weeks that end before last week (4, 3 and 2 weeks before this one), so T6's star card
    /// has finished weeks to count. Register only, no lessons, and none in last week or this one, so Missed lessons and
    /// the return-speed numbers are what <see cref="Build"/> alone gives. Everyone is present except the two children
    /// above.
    /// </summary>
    public static IReadOnlyList<DemoRegisterDay> StarWeeks(DateOnly today, IReadOnlyList<RosterChild> roster, Func<DateOnly, bool> isSchoolDay)
    {
        var oneDay = roster.SingleOrDefault(c => c.RollNo == OneDayAwayRollNo)?.Id;
        var threeDays = roster.SingleOrDefault(c => c.RollNo == ThreeDaysAwayRollNo)?.Id;
        var thisMonday = CompressionPlanner.Week(today).From;
        var days = new List<DemoRegisterDay>();

        for (var weeksBack = 4; weeksBack >= 2; weeksBack--)
        {
            var monday = thisMonday.AddDays(-7 * weeksBack);
            var schoolDays = Enumerable.Range(0, 6).Select(monday.AddDays).Where(isSchoolDay).ToList();
            for (var i = 0; i < schoolDays.Count; i++)
            {
                var absent = new HashSet<Guid>();
                Add(absent, oneDay, weeksBack == 3 && i == 0);
                Add(absent, threeDays, weeksBack == 2 && i < 3);
                days.Add(new DemoRegisterDay(schoolDays[i], absent));
            }
        }

        return days;
    }

    private static void Add(HashSet<Guid> absent, Guid? childId, bool away)
    {
        if (away && childId is { } id)
        {
            absent.Add(id);
        }
    }
}
