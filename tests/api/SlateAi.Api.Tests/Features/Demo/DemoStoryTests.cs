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

    private static readonly Guid Ravi = Roster.Single(c => c.RollNo == DemoStory.RaviRollNo).Id;
    private static readonly Guid Manjunath = Roster.Single(c => c.RollNo == DemoStory.ManjunathRollNo).Id;
    private static readonly Guid Rohan = Roster.Single(c => c.RollNo == DemoStory.RohanRollNo).Id;

    private static readonly DateOnly Friday = new(2026, 10, 23);
    private static readonly DateOnly Monday = new(2026, 10, 19);

    [Fact]
    public void On_a_friday_ravi_has_been_away_monday_to_thursday_and_present_last_week()
    {
        var days = Build(Friday);

        // Sat 17 Oct is the third Saturday, a school day.
        Assert.Equal(
            [new(2026, 10, 17), new(2026, 10, 19), new(2026, 10, 20), new(2026, 10, 21), new(2026, 10, 22)],
            days.Select(d => d.Date).ToList());
        Assert.Equal(
            [false, true, true, true, true],
            days.Select(d => d.AbsentChildIds.Contains(Ravi)).ToList());
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
    public void On_a_monday_every_seeded_day_is_last_week_so_ravi_was_present()
    {
        var days = Build(Monday);

        Assert.All(days, d => Assert.True(d.Date < Monday));
        Assert.All(days, d => Assert.DoesNotContain(Ravi, d.AbsentChildIds));
    }

    [Fact]
    public void Manjunath_is_away_the_two_days_before_today_and_rohan_only_today()
    {
        var days = Build(Friday);

        Assert.Equal([false, false, false, true, true], days.Select(d => d.AbsentChildIds.Contains(Manjunath)).ToList());
        Assert.All(days, d => Assert.DoesNotContain(Rohan, d.AbsentChildIds));
    }

    [Fact]
    public void Nobody_else_is_away_on_the_seeded_days()
    {
        var days = Build(Friday);

        Assert.Equal(new[] { Ravi, Manjunath }.Order(), days.SelectMany(d => d.AbsentChildIds).Distinct().Order());
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
    public void Star_weeks_never_touch_the_story_children()
    {
        var stars = DemoStory.StarWeeks(Friday, Roster, IsSchoolDay);

        Assert.All(stars, d => Assert.DoesNotContain(Ravi, d.AbsentChildIds));
        Assert.All(stars, d => Assert.DoesNotContain(Manjunath, d.AbsentChildIds));
        Assert.All(stars, d => Assert.DoesNotContain(Rohan, d.AbsentChildIds));
    }

    [Fact]
    public void The_video_story_on_a_saturday_is_the_whole_week_one_pattern_per_language()
    {
        var days = DemoStory.Video(new DateOnly(2026, 10, 31), Roster);

        Assert.Equal(
            [new(2026, 10, 26), new(2026, 10, 27), new(2026, 10, 28), new(2026, 10, 29), new(2026, 10, 30)],
            days.Select(d => d.Date).ToList());
        Assert.Equal(DemoStory.Lessons, days.Select(d => d.CompetencyId).ToList());
        int[][] away =
        [
            [DemoStory.AnanyaRollNo, DemoStory.RaviRollNo],
            [DemoStory.AnanyaRollNo, DemoStory.RaviRollNo],
            [DemoStory.AnanyaRollNo, DemoStory.RohanRollNo],
            [DemoStory.AnanyaRollNo, DemoStory.ArunRollNo],
            [DemoStory.AnanyaRollNo, DemoStory.ArunRollNo],
        ];
        Assert.Equal(
            away.Select(rolls => rolls.Order().ToList()),
            days.Select(d => Roster.Where(c => d.AbsentChildIds.Contains(c.Id)).Select(c => c.RollNo).Order().ToList()));
    }

    [Theory]
    [InlineData("2026-10-28", 2)] // Wednesday: Monday and Tuesday so far
    [InlineData("2026-10-26", 0)] // Monday: nothing yet
    public void Earlier_in_the_week_the_video_story_has_only_the_days_so_far(string today, int count)
    {
        Assert.Equal(count, DemoStory.Video(DateOnly.Parse(today), Roster).Count);
    }

    [Fact]
    public void The_parent_phone_is_rohan_for_the_judges_and_ananya_in_the_video() =>
        Assert.Equal(
            (DemoStory.RohanRollNo, DemoStory.AnanyaRollNo),
            (DemoStory.ParentRollNo(video: false), DemoStory.ParentRollNo(video: true)));

    private static bool IsSchoolDay(DateOnly date) =>
        date.DayOfWeek != DayOfWeek.Sunday && !(date.DayOfWeek == DayOfWeek.Saturday && (date.Day - 1) / 7 == 1);

    private static IReadOnlyList<DemoDay> Build(DateOnly today) =>
        DemoStory.Build(
            today,
            Roster,
            IsSchoolDay);
}
