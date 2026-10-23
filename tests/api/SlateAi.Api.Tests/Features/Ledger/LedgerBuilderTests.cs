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

    [Fact]
    public void Status_is_learning_only_the_answer_first_then_a_sent_link_and_a_stop_is_the_channel()
    {
        // Mon: subtraction; Tue: place value. Each child misses both; the catch-up covers subtraction only.
        var sentAt = new DateTimeOffset(2026, 10, 16, 18, 0, 0, TimeSpan.FromHours(5.5));
        var answered = new LedgerChildRow(Guid.NewGuid(), 1, "Answered");
        var wrong = new LedgerChildRow(Guid.NewGuid(), 2, "Wrong");
        var stoppedAfterAnswer = new LedgerChildRow(Guid.NewGuid(), 3, "StoppedAfter", StoppedAt: sentAt.AddDays(1));
        var stoppedSent = new LedgerChildRow(Guid.NewGuid(), 4, "Stopped", StoppedAt: sentAt.AddDays(1));
        var sent = new LedgerChildRow(Guid.NewGuid(), 5, "Sent");
        var unsent = new LedgerChildRow(Guid.NewGuid(), 6, "Unsent");
        var lastWeek = new LedgerChildRow(Guid.NewGuid(), 7, "LastWeek");
        LedgerChildRow[] all = [answered, wrong, stoppedAfterAnswer, stoppedSent, sent, unsent, lastWeek];
        CatchUpRow CatchUp(LedgerChildRow child, bool isSent, string? outcome, DateOnly? week = null) =>
            new(child.Id, week ?? Mon, ["M3.SUB.BORROW"], "te", isSent ? sentAt : null, outcome, outcome is null ? null : sentAt.AddHours(1));
        var data = new LedgerData(
            all,
            [.. all.Select(c => new RegisterRow(c.Id, Mon, false)), .. all.Select(c => new RegisterRow(c.Id, Mon.AddDays(1), false))],
            [
                new LessonRow(Guid.NewGuid(), Mon, ["M3.SUB.BORROW"]),
                new LessonRow(Guid.NewGuid(), Mon.AddDays(1), ["M3.PV.HUNDREDS"]),
            ],
            [
                CatchUp(answered, true, "caughtUp"),
                CatchUp(wrong, true, "needsBuddy"),
                CatchUp(stoppedAfterAnswer, true, "caughtUp"),
                CatchUp(stoppedSent, true, null),
                CatchUp(sent, true, null),
                CatchUp(unsent, false, null),
                CatchUp(lastWeek, true, "caughtUp", Mon.AddDays(-7)),
            ]);

        var ledger = LedgerBuilder.Build(ClassId, data, Mon.AddDays(4));
        LedgerChild Row(LedgerChildRow child) => ledger.Children.Single(c => c.ChildId == child.Id);
        string StatusOf(LedgerChildRow child, int lesson) => Row(child).MissedLessons[lesson].Status;

        Assert.Equal(LedgerStatus.CaughtUp, StatusOf(answered, 0));
        Assert.Equal(LedgerStatus.NeedsBuddy, StatusOf(wrong, 0));
        Assert.Equal(LedgerStatus.CaughtUp, StatusOf(stoppedAfterAnswer, 0)); // a stop never hides caught up (B-08)
        Assert.Equal(LedgerStatus.Sent, StatusOf(stoppedSent, 0));
        Assert.Equal(LedgerStatus.Sent, StatusOf(sent, 0));
        Assert.Equal(LedgerStatus.Missed, StatusOf(unsent, 0));
        Assert.Equal(LedgerStatus.Missed, StatusOf(lastWeek, 0)); // another week's catch-up
        Assert.Equal(LedgerStatus.Missed, StatusOf(answered, 1)); // not in the catch-up
        Assert.Equal(LedgerChannel.Stopped, Row(stoppedAfterAnswer).Channel);
        Assert.Equal(LedgerChannel.Stopped, Row(stoppedSent).Channel);
        Assert.Null(Row(answered).Channel);
    }

    [Theory]
    [InlineData("yes", false, null)]
    [InlineData("nophone", false, LedgerChannel.NoPhone)]
    [InlineData("no", false, LedgerChannel.NoConsent)]
    [InlineData("no", true, LedgerChannel.Stopped)]
    [InlineData("yes", true, LedgerChannel.Stopped)]
    public void Channel_comes_from_the_consent_form_and_a_stop_wins(string consent, bool stopped, string? channel)
    {
        var child = new LedgerChildRow(Guid.NewGuid(), 1, "A", "te", consent, stopped ? DateTimeOffset.UnixEpoch : null);

        Assert.Equal(channel, Assert.Single(LedgerBuilder.Build(ClassId, new LedgerData([child], [], []), Mon).Children).Channel);
    }

    [Fact]
    public void This_week_counts_days_away_lists_the_lessons_and_builds_the_history_in_day_order()
    {
        // Last Friday absent (not this week); this week: absent Mon and Tue, reason for Mon, catch-up sent Wed
        // evening, answered Thu, stopped Thu. Today is Thursday.
        var ist = TimeSpan.FromHours(5.5);
        var away = new LedgerChildRow(Guid.NewGuid(), 4, "Ravi", "te", "yes", new DateTimeOffset(2026, 10, 15, 20, 0, 0, ist));
        var data = new LedgerData(
            [away, Present],
            [
                new RegisterRow(away.Id, Mon.AddDays(-3), false),
                new RegisterRow(away.Id, Mon, false),
                new RegisterRow(away.Id, Mon.AddDays(1), false),
                new RegisterRow(away.Id, Mon.AddDays(2), true),
                new RegisterRow(Present.Id, Mon, true),
            ],
            [
                new LessonRow(Guid.NewGuid(), Mon.AddDays(-3), ["M3.PV.TENS"]),
                new LessonRow(Guid.NewGuid(), Mon, ["M3.SUB.BORROW"]),
                new LessonRow(Guid.NewGuid(), Mon.AddDays(2), ["M3.ADD.CARRY"]),
            ],
            [new CatchUpRow(away.Id, Mon, ["M3.SUB.BORROW"], "te",
                new DateTimeOffset(2026, 10, 14, 18, 30, 0, ist), "needsBuddy", new DateTimeOffset(2026, 10, 15, 19, 0, 0, ist))],
            [new ReasonRow(away.Id, Mon, "unwell"), new ReasonRow(away.Id, Mon.AddDays(-3), "festival")]);

        var ledger = LedgerBuilder.Build(ClassId, data, Mon.AddDays(3));
        var ravi = ledger.Children.Single(c => c.ChildId == away.Id);

        Assert.Equal(Mon, ledger.WeekOf);
        Assert.Equal([Mon, Mon.AddDays(2)], ledger.Lessons.Select(l => l.Date));
        Assert.Equal(2, ravi.DaysAwayThisWeek);
        Assert.Equal("te", ravi.HomeLanguage);
        Assert.Equal(
            [
                (Mon, LedgerEventKind.Away),
                (Mon, LedgerEventKind.Reason),
                (Mon.AddDays(1), LedgerEventKind.Away),
                (Mon.AddDays(2), LedgerEventKind.CatchUpSent),
                (Mon.AddDays(3), LedgerEventKind.Answered),
                (Mon.AddDays(3), LedgerEventKind.Stopped),
            ],
            ravi.History.Select(e => (e.Date, e.Kind)));
        Assert.Equal(["M3.SUB.BORROW"], ravi.History[0].CompetencyIds);
        Assert.Empty(ravi.History[2].CompetencyIds!); // away on a day with no confirmed lesson
        Assert.Equal("unwell", ravi.History[1].Category);
        Assert.Equal("te", ravi.History[3].Language);
        Assert.Equal("needsBuddy", ravi.History[4].Outcome);
        Assert.Equal(0, ledger.Children.Single(c => c.ChildId == Present.Id).DaysAwayThisWeek);
        Assert.Empty(ledger.Children.Single(c => c.ChildId == Present.Id).History);
    }

    private static IEnumerable<RegisterRow> Day(DateOnly date, LedgerChildRow[] absent) =>
        new[] { Away, Back, Present }.Select(c => new RegisterRow(c.Id, date, !absent.Contains(c)));
}
