using SlateAi.Api.Features.Attendance.Source;
using SlateAi.Api.Features.Demo;
using SlateAi.Api.Features.Ledger;
using SlateAi.Api.Features.Week;
using SlateAi.Api.Tests.Features.Buddy;

namespace SlateAi.Api.Tests.Features.Demo;

public sealed class DemoStoryTests
{
    private static readonly IReadOnlyList<RosterChild> Roster =
        Enumerable.Range(1, 36).Select(roll => new RosterChild(Guid.NewGuid(), roll, $"Child {roll}")).ToList();

    private static readonly Guid Manjunath = Roster.Single(c => c.RollNo == DemoStory.ManjunathRollNo).Id;

    private static readonly DateOnly Friday = new(2026, 10, 23);
    private static readonly DateOnly Monday = new(2026, 10, 19);

    [Fact]
    public void On_a_friday_the_seeded_days_are_the_five_school_days_before_it()
    {
        var days = Build(Friday);

        // Sat 17 Oct is the third Saturday, a school day.
        Assert.Equal(
            [new(2026, 10, 17), new(2026, 10, 19), new(2026, 10, 20), new(2026, 10, 21), new(2026, 10, 22)],
            days.Select(d => d.Date).ToList());
    }

    [Fact]
    public void Five_school_days_before_today_skip_sundays_and_the_second_saturday_and_leave_today_alone()
    {
        // Sat 10 Oct is the second Saturday; Sun 11 Oct is a Sunday.
        var days = Build(new DateOnly(2026, 10, 14));

        Assert.Equal(
            [new(2026, 10, 7), new(2026, 10, 8), new(2026, 10, 9), new(2026, 10, 12), new(2026, 10, 13)],
            days.Select(d => d.Date).ToList());
    }

    [Fact]
    public void Manjunath_is_away_the_two_days_before_today()
    {
        var days = Build(Friday);

        Assert.Equal([false, false, false, true, true], days.Select(d => d.AbsentChildIds.Contains(Manjunath)).ToList());
    }

    [Fact]
    public void Nobody_else_is_away_on_the_seeded_days()
    {
        // T-93, D-94: one child, one Kannada family (R3).
        var days = Build(Friday);

        Assert.Equal([Manjunath], days.SelectMany(d => d.AbsentChildIds).Distinct());
    }

    [Fact]
    public void Lessons_follow_the_book_up_to_the_one_before_carrying()
    {
        var days = Build(Friday);

        Assert.Equal(DemoStory.Lessons, days.Select(d => d.CompetencyId).ToList());
        Assert.Equal("M3.ADD.NOCARRY", days[^1].CompetencyId);
    }

    [Fact]
    public void Star_weeks_are_register_only_and_leave_last_week_and_this_week_alone()
    {
        var story = Build(Monday);
        var stars = DemoStory.StarWeeks(Monday, Roster, IsSchoolDay);
        var thisMonday = Monday;

        Assert.NotEmpty(stars);
        // Nothing in this week or last week, nothing on a story day, nothing on a day with no school.
        Assert.All(stars, d => Assert.True(d.Date < thisMonday.AddDays(-7)));
        Assert.Empty(stars.Select(d => d.Date).Intersect(story.Select(d => d.Date)));
        Assert.All(stars, d => Assert.True(IsSchoolDay(d.Date)));
        // Only roll 7 (once) and roll 10 (three days) are ever away.
        var away = stars.SelectMany(d => d.AbsentChildIds.Select(id => (d.Date, id))).GroupBy(x => x.id).ToDictionary(g => g.Key, g => g.Count());
        Assert.Equal(
            new Dictionary<Guid, int> { [Roster.Single(c => c.RollNo == DemoStory.OneDayAwayRollNo).Id] = 1, [Roster.Single(c => c.RollNo == DemoStory.ThreeDaysAwayRollNo).Id] = 3 },
            away);
    }

    [Theory]
    [MemberData(nameof(Todays))]
    public void Star_weeks_do_not_change_the_return_speed_numbers(DateOnly today)
    {
        var story = Build(today);
        var stars = DemoStory.StarWeeks(today, Roster, IsSchoolDay);
        LedgerData Ledger(IEnumerable<(DateOnly Date, IReadOnlySet<Guid> Away)> days) => new(
            [.. Roster.Select(c => new LedgerChildRow(c.Id, c.RollNo, c.FirstName))],
            [.. days.SelectMany(d => Roster.Select(c => new RegisterRow(c.Id, d.Date, !d.Away.Contains(c.Id))))],
            []);
        var storyDays = story.Select(d => (d.Date, d.AbsentChildIds));
        var starDays = stars.Select(d => (d.Date, d.AbsentChildIds));

        var without = WeekBuilder.Build(Ledger(storyDays), BuddyCardBuilderTests.Book, today, null);
        var with = WeekBuilder.Build(Ledger(starDays.Concat(storyDays)), BuddyCardBuilderTests.Book, today, null);

        Assert.Equal(without.ThisWeek, with.ThisWeek);
        Assert.Equal(without.LastWeek, with.LastWeek);
    }

    public static TheoryData<DateOnly> Todays() => new()
    {
        new DateOnly(2026, 10, 19), new DateOnly(2026, 10, 21), new DateOnly(2026, 10, 24), new DateOnly(2026, 10, 23),
    };

    [Fact]
    public void Star_weeks_never_touch_the_story_child()
    {
        var stars = DemoStory.StarWeeks(Friday, Roster, IsSchoolDay);

        Assert.All(stars, d => Assert.DoesNotContain(Manjunath, d.AbsentChildIds));
    }

    private static bool IsSchoolDay(DateOnly date) =>
        date.DayOfWeek != DayOfWeek.Sunday && !(date.DayOfWeek == DayOfWeek.Saturday && (date.Day - 1) / 7 == 1);

    private static IReadOnlyList<DemoDay> Build(DateOnly today) =>
        DemoStory.Build(
            today,
            Roster,
            IsSchoolDay);
}
