using SlateAi.Api.Features.Attendance.Source;
using SlateAi.Api.Features.CatchUps.Compression;

namespace SlateAi.Api.Features.Demo;

/// <summary>One school day the demo Reset writes: the register as SATS sent it and the lesson she confirmed.</summary>
public sealed record DemoDay(DateOnly Date, IReadOnlySet<Guid> AbsentChildIds, string CompetencyId);

/// <summary>A school day with a register and no lesson: the earlier weeks that give the stars card something to show (T-63).</summary>
public sealed record DemoRegisterDay(DateOnly Date, IReadOnlySet<Guid> AbsentChildIds);

/// <summary>
/// Manjunath's story on the real calendar (T-18, T-93, D-94): the school days before today, so the demo lines up
/// whatever day a judge opens it. Pure; the Reset store writes what this returns. One child, one Kannada family (R3):
/// Manjunath is away the two school days before today and today, so this week makes exactly one catch-up (Kannada) and
/// today's lesson (Nalikali p 4) goes into today's message. Today itself comes from the simulated feed, whose scenario
/// marks only Manjunath absent every day; the Reset writes today's register too, stamped at the feed's 12:00, so Today
/// is ready at any hour (R6). The demo class has a register even on Sundays and holidays (T-69). No random absences.
/// </summary>
public static class DemoStory
{
    /// <summary>Manjunath: Kannada, consent Yes, the demo's family phone. Away the two school days before today and today.</summary>
    public const int ManjunathRollNo = 15;

    /// <summary>The children the story follows; one of them owns the pre-made catch-up when its language matches.</summary>
    public static readonly IReadOnlySet<int> StoryRollNos = new HashSet<int> { ManjunathRollNo };

    /// <summary>The lessons taught on the seeded days, oldest first; today's next in the book is <c>M3.ADD.CARRY</c>.</summary>
    public static readonly IReadOnlyList<string> Lessons =
        ["M3.PV.HTO", "M3.PV.EXPAND", "M3.NUM.COMPARE", "M3.NUM.ORDER", "M3.ADD.NOCARRY"];

    /// <summary>
    /// The <see cref="Lessons"/>.Count school days before <paramref name="today"/>, oldest first. Manjunath is away on
    /// the last two; everyone else is present.
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
        var manjunath = roster.SingleOrDefault(c => c.RollNo == ManjunathRollNo)?.Id;

        return dates.Select((date, i) =>
        {
            var absent = new HashSet<Guid>();
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
