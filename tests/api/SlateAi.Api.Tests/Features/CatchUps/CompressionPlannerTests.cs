using SlateAi.Api.Features.CatchUps.Compression;
using SlateAi.Api.Features.Ledger;

namespace SlateAi.Api.Tests.Features.CatchUps;

public sealed class CompressionPlannerTests
{
    private static readonly Guid ClassId = Guid.NewGuid();
    private static readonly Guid Ravi = Guid.NewGuid();
    private static readonly Guid Asha = Guid.NewGuid();

    // Week of Mon 19 Oct 2026; Friday is 23 Oct.
    private static readonly DateOnly LastFriday = new(2026, 10, 16);
    private static readonly DateOnly Monday = new(2026, 10, 19);
    private static readonly DateOnly Friday = new(2026, 10, 23);

    [Theory]
    [InlineData("2026-10-19", "2026-10-19")] // Monday
    [InlineData("2026-10-22", "2026-10-19")] // Thursday
    [InlineData("2026-10-25", "2026-10-19")] // Sunday still belongs to that week
    public void Week_runs_from_monday_to_the_day(string day, string monday)
    {
        var date = DateOnly.Parse(day);

        Assert.Equal((DateOnly.Parse(monday), date), CompressionPlanner.Week(date));
    }

    [Fact]
    public void Next_week_is_the_next_five_lessons_after_the_latest_taught_and_fewer_at_the_end()
    {
        Assert.Equal(
            ["M3.PV.EXPAND", "M3.ADD.NOCARRY", "M3.ADD.CARRY", "M3.SUB.NOBORROW", "M3.SUB.CHECK"],
            CompressionPlanner.NextWeek(CompressionBook.Graph, ["M3.PV.HTO"]));
        Assert.Equal(["M3.SUB.WORD"], CompressionPlanner.NextWeek(CompressionBook.Graph, ["M3.SUB.CHECK", "M3.SUB.BORROW"]));
        Assert.Empty(CompressionPlanner.NextWeek(CompressionBook.Graph, ["M3.SUB.WORD"]));
    }

    [Fact]
    public void Ravis_week_gives_the_missed_lessons_in_order_and_the_candidates_next_week_needs()
    {
        // Ravi is away Tue–Thu (the demo story); Friday's lesson is the latest taught.
        var data = Data(
            lessons:
            [
                (LastFriday, ["M3.PV.EXPAND"]),
                (Monday, ["M3.ADD.NOCARRY"]),
                (Monday.AddDays(1), ["M3.SUB.NOBORROW"]),
                (Monday.AddDays(2), ["M3.SUB.CHECK"]),
                (Monday.AddDays(3), ["M3.SUB.BORROW"]),
                (Friday, ["M3.SUB.BORROW"]),
            ],
            absent: [(Ravi, LastFriday), (Ravi, Monday.AddDays(1)), (Ravi, Monday.AddDays(2)), (Ravi, Monday.AddDays(3))]);

        var input = Assert.Single(Plan(data));

        Assert.Equal(Ravi, input.ChildId);
        Assert.Equal(["M3.SUB.NOBORROW", "M3.SUB.CHECK", "M3.SUB.BORROW"], input.Missed.Select(m => m.Id));
        Assert.Equal([Monday.AddDays(3)], input.Missed[2].Dates);
        Assert.Equal(["M3.SUB.WORD"], input.NextWeek);
        Assert.Equal(["M3.SUB.BORROW", "M3.SUB.NOBORROW"], input.Candidates); // nearest first; CHECK isn't needed
    }

    [Fact]
    public void Children_who_missed_nothing_this_week_are_left_out()
    {
        var data = Data(
            lessons: [(LastFriday, ["M3.PV.HTO"]), (Monday, ["M3.PV.EXPAND"])],
            absent: [(Asha, LastFriday)]);

        Assert.Empty(Plan(data));
    }

    [Fact]
    public void A_lesson_taught_twice_is_one_missed_item_with_both_days()
    {
        var data = Data(
            lessons: [(Monday, ["M3.PV.HTO"]), (Monday.AddDays(1), ["M3.PV.HTO", "M3.PV.EXPAND"])],
            absent: [(Asha, Monday), (Asha, Monday.AddDays(1))]);

        var input = Assert.Single(Plan(data));

        Assert.Equal(["M3.PV.HTO", "M3.PV.EXPAND"], input.Missed.Select(m => m.Id));
        Assert.Equal([Monday, Monday.AddDays(1)], input.Missed[0].Dates);
    }

    private static IReadOnlyList<CompressionInput> Plan(LedgerData data) =>
        CompressionPlanner.Plan(LedgerBuilder.Build(ClassId, data), data.Lessons, Friday, CompressionBook.Graph);

    private static LedgerData Data((DateOnly Date, string[] Ids)[] lessons, (Guid Child, DateOnly Date)[] absent)
    {
        var children = new[] { new LedgerChildRow(Ravi, 4, "Ravi"), new LedgerChildRow(Asha, 1, "Asha") };
        var register = lessons.Select(l => l.Date).Distinct()
            .SelectMany(date => children.Select(c => new RegisterRow(c.Id, date, !absent.Contains((c.Id, date)))))
            .ToList();
        return new LedgerData(children, register, lessons.Select(l => new LessonRow(Guid.NewGuid(), l.Date, l.Ids)).ToList());
    }
}
