using SlateAi.Api.Features.Ledger;

namespace SlateAi.Api.Tests.Features.Ledger;

public sealed class LedgerBuilderTests
{
    private static readonly DateOnly Mon = new(2026, 10, 12);
    private static readonly Guid ClassId = Guid.NewGuid();
    private static readonly LedgerChildRow Away = new(Guid.NewGuid(), 1, "A");
    private static readonly LedgerChildRow Back = new(Guid.NewGuid(), 2, "B");
    private static readonly LedgerChildRow Present = new(Guid.NewGuid(), 3, "C");

    [Fact]
    public void Lists_lessons_on_absent_days_and_how_long_each_child_was_away()
    {
        // Mon: Away and Back absent. Tue: Back returns. Wed: no register (holiday). Thu: Away still away.
        var data = new LedgerData(
            [Present, Back, Away],
            [
                .. Day(Mon, absent: [Away, Back]),
                .. Day(Mon.AddDays(1), absent: [Away]),
                .. Day(Mon.AddDays(3), absent: [Away]),
            ],
            [
                new LessonRow(Guid.NewGuid(), Mon.AddDays(1), ["M3.ADD.CARRY"]),
                new LessonRow(Guid.NewGuid(), Mon, ["M3.PV.TENS"]),
            ]);

        var ledger = LedgerBuilder.Build(ClassId, data);

        Assert.Equal([1, 2, 3], ledger.Children.Select(c => c.RollNo));
        Assert.Equal(Mon.AddDays(3), ledger.LatestSchoolDay);

        var away = ledger.Children[0];
        Assert.Equal(["M3.PV.TENS", "M3.ADD.CARRY"], away.MissedLessons.SelectMany(l => l.CompetencyIds));
        Assert.All(away.MissedLessons, l => Assert.Equal("missed", l.Status));
        Assert.Equal(3, away.CurrentAbsenceDays);

        var back = ledger.Children[1];
        Assert.Equal(["M3.PV.TENS"], back.MissedLessons.SelectMany(l => l.CompetencyIds));
        Assert.Equal(0, back.CurrentAbsenceDays);
        Assert.Equal(Mon.AddDays(1), Assert.Single(back.Absences).ReturnedOn);

        var present = ledger.Children[2];
        Assert.Empty(present.MissedLessons);
        Assert.Empty(present.Absences);

        Assert.Equal(
            new ReturnSpeed(Mon.AddDays(-3), Mon.AddDays(3), 1, 0, 1, 0, BackWithinTwoDays: 1, Total: 2),
            ledger.ReturnSpeed);
    }

    [Fact]
    public void No_register_yet_means_no_return_speed()
    {
        var ledger = LedgerBuilder.Build(ClassId, new LedgerData([Away], [], []));

        Assert.Null(ledger.LatestSchoolDay);
        Assert.Null(ledger.ReturnSpeed);
        Assert.Empty(Assert.Single(ledger.Children).MissedLessons);
    }

    [Fact]
    public void A_lesson_on_a_day_without_a_register_is_not_missed()
    {
        var data = new LedgerData(
            [Away],
            [.. Day(Mon, absent: [Away])],
            [new LessonRow(Guid.NewGuid(), Mon.AddDays(1), ["M3.ADD.CARRY"])]);

        Assert.Empty(LedgerBuilder.Build(ClassId, data).Children[0].MissedLessons);
    }

    private static IEnumerable<RegisterRow> Day(DateOnly date, LedgerChildRow[] absent) =>
        new[] { Away, Back, Present }.Select(c => new RegisterRow(c.Id, date, !absent.Contains(c)));
}
