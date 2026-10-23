using SlateAi.Api.Features.Ledger;
using SlateAi.Api.Features.Parent;
using SlateAi.Api.Features.Week;
using SlateAi.Api.Tests.Features.Buddy;

namespace SlateAi.Api.Tests.Features.Week;

public sealed class WeekBuilderTests
{
    // This week: Mon 19 to Sat 24 Oct 2026. Last week: Mon 12 to Sat 17.
    private static readonly DateOnly Mon = new(2026, 10, 19);
    private static readonly DateOnly LastMon = Mon.AddDays(-7);

    [Fact]
    public void Headline_bars_and_table_reconcile_with_the_cards_example()
    {
        // T6 §7 (B-09): this week spells of 1, 2, 3, 4+ days: 7, 3, 1, 1 → 10 of 12; last week 5, 4, 2, 2 → 9 of 13.
        var spells = Spells(Mon, 7, 3, 1, 1).Concat(Spells(LastMon, 5, 4, 2, 2)).ToList();
        var data = Data(spells);

        var week = WeekBuilder.Build(data, BuddyCardBuilderTests.Book, Mon, null);

        Assert.Equal((7, 3, 1, 1, 10, 12), Numbers(week.ThisWeek));
        Assert.Equal((5, 4, 2, 2, 9, 13), Numbers(week.LastWeek));
        foreach (var w in new[] { week.ThisWeek, week.LastWeek })
        {
            Assert.Equal(w.Total, w.OneDay + w.TwoDays + w.ThreeDays + w.FourPlusDays);
            Assert.Equal(w.BackWithinTwoDays, w.OneDay + w.TwoDays); // everyone here came back
        }

        Assert.Equal((Mon, Mon.AddDays(5)), (week.From, week.To));
        Assert.False(week.Empty);
    }

    [Fact]
    public void A_spell_belongs_to_the_week_it_started_and_a_child_still_away_is_not_back_within_two()
    {
        // Ravi: away Fri 16 + Mon 19 (started last week), back Tue. Asha: away Fri 23 + Sat 24, still away.
        // Kiran: away Wed 21, a holiday Thu 22 (no register), away Fri 23, back Sat: one 2-day spell.
        var ravi = Guid.NewGuid();
        var asha = Guid.NewGuid();
        var kiran = Guid.NewGuid();
        var register = new List<RegisterRow>
        {
            new(ravi, LastMon.AddDays(4), false), new(ravi, Mon, false), new(ravi, Mon.AddDays(1), true),
            new(asha, Mon.AddDays(3), true), new(asha, Mon.AddDays(4), false), new(asha, Mon.AddDays(5), false),
            new(kiran, Mon.AddDays(1), true), new(kiran, Mon.AddDays(2), false), new(kiran, Mon.AddDays(4), false), new(kiran, Mon.AddDays(5), true),
        };
        var data = new LedgerData(
            [new LedgerChildRow(ravi, 1, "Ravi"), new LedgerChildRow(asha, 2, "Asha"), new LedgerChildRow(kiran, 3, "Kiran")], register, []);

        var week = WeekBuilder.Build(data, BuddyCardBuilderTests.Book, Mon, null);

        Assert.Equal((0, 2, 0, 0, 1, 2), Numbers(week.ThisWeek)); // Asha (2 days so far, still away) + Kiran
        Assert.Equal((0, 1, 0, 0, 1, 1), Numbers(week.LastWeek)); // Ravi, started Friday
    }

    [Fact]
    public void Reasons_count_each_absent_child_once_latest_reason_and_add_up_to_the_absent_children()
    {
        var children = Enumerable.Range(1, 6).Select(i => new LedgerChildRow(Guid.NewGuid(), i, $"C{i}")).ToList();
        var register = children.Take(5).Select(c => new RegisterRow(c.Id, Mon.AddDays(1), false))
            .Concat(children.Select(c => new RegisterRow(c.Id, Mon.AddDays(2), true)))
            .Append(new RegisterRow(children[0].Id, Mon.AddDays(3), false))
            .ToList();
        var reasons = new List<ReasonRow>
        {
            new(children[0].Id, Mon.AddDays(1), ReasonCategory.Festival),
            new(children[0].Id, Mon.AddDays(3), ReasonCategory.Unwell), // latest wins
            new(children[1].Id, Mon.AddDays(1), ReasonCategory.Unwell),
            new(children[2].Id, Mon.AddDays(1), ReasonCategory.NeededAtHome),
            new(children[3].Id, LastMon, ReasonCategory.Other), // last week: not this week's reason
        };
        var data = new LedgerData(children, register, [], null, reasons);

        var r = WeekBuilder.Build(data, BuddyCardBuilderTests.Book, Mon, null).Reasons;

        Assert.Equal(new WeekReasons(Unwell: 2, TravelOrWork: 0, Festival: 0, NeededAtHome: 1, Other: 0, NotGiven: 2), r);
        Assert.Equal(5, r.Unwell + r.TravelOrWork + r.Festival + r.NeededAtHome + r.Other + r.NotGiven); // 5 absent children
    }

    [Fact]
    public void Nudge_shows_at_five_children_on_the_lesson_most_missed_with_the_next_lesson_in_the_book()
    {
        // D-60. Tue (addition): 5 away; Wed (subtraction): 4 away. 20 in the class.
        var children = Enumerable.Range(1, 20).Select(i => new LedgerChildRow(Guid.NewGuid(), i, $"C{i}")).ToList();
        var tue = new LessonRow(Guid.NewGuid(), Mon.AddDays(1), ["M3.ADD.CARRY"]);
        var wed = new LessonRow(Guid.NewGuid(), Mon.AddDays(2), ["M3.ADD.CARRY"]);
        var register = children.Select((c, i) => new RegisterRow(c.Id, tue.Date, i >= 5))
            .Concat(children.Select((c, i) => new RegisterRow(c.Id, wed.Date, i < 5 || i >= 9)))
            .ToList();

        var week = WeekBuilder.Build(new LedgerData(children, register, [tue, wed]), BuddyCardBuilderTests.Book, Mon, null);
        var four = WeekBuilder.Build(
            new LedgerData(children, register.Where(r => !(r.Date == tue.Date && r.ChildId == children[0].Id)).ToList(), [tue, wed]),
            BuddyCardBuilderTests.Book, Mon, null);

        var nudge = Assert.IsType<PacingNudge>(week.Nudge);
        Assert.Equal((tue.Id, 5, 20, 10), (nudge.LessonId, nudge.N, nudge.Total, nudge.Minutes));
        Assert.Equal("M3.SUB.BORROW", nudge.NextCompetencyId); // after addition (seq 12) comes subtraction (seq 15)
        Assert.Null(four.Nudge); // 4 on each lesson: on track
    }

    [Fact]
    public void Nudge_ties_go_to_the_latest_lesson_and_the_end_of_the_book_has_no_next()
    {
        var children = Enumerable.Range(1, 10).Select(i => new LedgerChildRow(Guid.NewGuid(), i, $"C{i}")).ToList();
        var tue = new LessonRow(Guid.NewGuid(), Mon.AddDays(1), ["M3.ADD.CARRY"]);
        var wed = new LessonRow(Guid.NewGuid(), Mon.AddDays(2), ["M3.SUB.BORROW"]);
        var register = children.Select((c, i) => new RegisterRow(c.Id, tue.Date, i >= 5))
            .Concat(children.Select((c, i) => new RegisterRow(c.Id, wed.Date, i < 5)))
            .ToList();

        var nudge = WeekBuilder.Build(new LedgerData(children, register, [tue, wed]), BuddyCardBuilderTests.Book, Mon, null).Nudge!;

        Assert.Equal(wed.Id, nudge.LessonId);
        Assert.Null(nudge.NextCompetencyId); // subtraction with borrowing is the last lesson in this book
    }

    [Fact]
    public void Decision_is_shown_only_for_the_nudges_lesson_and_no_absences_is_empty()
    {
        var spells = Spells(Mon, 5, 0, 0, 0).ToList();
        var lesson = new LessonRow(Guid.NewGuid(), Mon, ["M3.ADD.CARRY"]);
        var data = Data(spells) with { Lessons = [lesson] };
        var quiet = new LedgerData([new LedgerChildRow(Guid.NewGuid(), 1, "A")], [new RegisterRow(Guid.NewGuid(), Mon, true)], []);

        Assert.Equal("recap", WeekBuilder.Build(data, BuddyCardBuilderTests.Book, Mon, new DecisionRow(lesson.Id, "recap")).Decision);
        Assert.Null(WeekBuilder.Build(data, BuddyCardBuilderTests.Book, Mon, new DecisionRow(Guid.NewGuid(), "recap")).Decision);
        var empty = WeekBuilder.Build(quiet, BuddyCardBuilderTests.Book, Mon.AddDays(3), null);
        Assert.True(empty.Empty);
        Assert.Equal(Mon, empty.From); // any day of the week means that week
    }

    private static (int, int, int, int, int, int) Numbers(ReturnSpeed w) =>
        (w.OneDay, w.TwoDays, w.ThreeDays, w.FourPlusDays, w.BackWithinTwoDays, w.Total);

    /// <summary>One child per spell: away from Monday for the spell's length, then back.</summary>
    private static IEnumerable<(Guid Child, DateOnly From, int Days)> Spells(DateOnly monday, int one, int two, int three, int four) =>
        new[] { (1, one), (2, two), (3, three), (4, four) }
            .SelectMany(x => Enumerable.Range(0, x.Item2).Select(_ => (Guid.NewGuid(), monday, x.Item1)));

    private static LedgerData Data(IReadOnlyList<(Guid Child, DateOnly From, int Days)> spells)
    {
        var register = spells.SelectMany(s =>
            Enumerable.Range(0, s.Days).Select(d => new RegisterRow(s.Child, s.From.AddDays(d), false))
                .Append(new RegisterRow(s.Child, s.From.AddDays(s.Days), true)));
        return new LedgerData(
            spells.Select((s, i) => new LedgerChildRow(s.Child, i + 1, $"C{i}")).ToList(), register.ToList(), []);
    }
}
