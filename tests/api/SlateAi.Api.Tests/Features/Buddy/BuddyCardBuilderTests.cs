using System.Data;
using SlateAi.Api.Curriculum;
using SlateAi.Api.Features.Buddy;
using SlateAi.Api.Features.Ledger;

namespace SlateAi.Api.Tests.Features.Buddy;

public sealed class BuddyCardBuilderTests
{
    // Mon 19 to Thu 22 Oct 2026; today is Thursday.
    private static readonly DateOnly Mon = new(2026, 10, 19);
    private static readonly DateOnly Today = Mon.AddDays(3);

    internal static readonly CurriculumGraph Book = new(
    [
        new Competency("M3.SUB.BORROW", "ದಶಕ ಸಹಿತ ವ್ಯವಕಲನ", "Subtraction with borrowing", [], null, 5, 15, [],
            new TextbookPage("textbook", 2, 15), KeyWordsKn: ["ಕಳೆ", "ದಶಕ", "ಹತ್ತು", "ಬಿಡಿ"],
            Practice: new PracticeRule("sub", 3, Borrow: "tens")),
        new Competency("M3.ADD.CARRY", "ದಶಕ ಸಹಿತ ಸಂಕಲನ", "Addition with carrying", [], null, 4, 12, [],
            new TextbookPage("textbook", 1, 30), KeyWordsKn: ["ಕೂಡು", "ದಶಕ"],
            Practice: new PracticeRule("add", 3, Carry: "ones")),
        new Competency("M3.PV.READ", "ಓದು", "Reading numbers", [], null, 1, 1, [], null),
    ]);

    private static readonly LedgerChildRow Ravi = new(Guid.NewGuid(), 4, "Ravi", "te", "yes", DateTimeOffset.UnixEpoch);
    private static readonly LedgerChildRow Asha = new(Guid.NewGuid(), 1, "Asha", Consent: "nophone");
    private static readonly LedgerChildRow Manju = new(Guid.NewGuid(), 2, "Manju");
    private static readonly LedgerChildRow Kiran = new(Guid.NewGuid(), 3, "Kiran");
    private static readonly LedgerChildRow Divya = new(Guid.NewGuid(), 5, "Divya");

    [Fact]
    public void Back_today_means_present_today_after_an_absence_that_ended_today()
    {
        // Ravi: away Tue, Wed, back Thu. Divya: away Mon, back Tue (not today). Kiran: away today.
        var data = Data(
            Day(Mon, absent: [Divya]),
            Day(Mon.AddDays(1), absent: [Ravi]),
            Day(Mon.AddDays(2), absent: [Ravi]),
            Day(Today, absent: [Kiran]));

        var cards = BuddyCardBuilder.Build(data, [], [], Book, Today);

        var card = Assert.Single(cards);
        Assert.Equal(Ravi.Id, card.ChildId);
        Assert.Equal(2, card.DaysAway);
        Assert.Equal(2, card.MissedDays);
    }

    [Fact]
    public void Buddy_was_in_class_on_every_missed_day_and_is_never_away_today()
    {
        // Ravi away Tue + Wed. Asha (roll 1) missed Tue (back Wed); Kiran (roll 3) was there both days but is away
        // today; Manju (roll 2) was there both days.
        var data = Data(
            Day(Mon.AddDays(1), absent: [Ravi, Asha]),
            Day(Mon.AddDays(2), absent: [Ravi]),
            Day(Today, absent: [Kiran]));

        var buddy = Assert.Single(BuddyCardBuilder.Build(data, [], [], Book, Today)).Buddy;

        Assert.Equal((Manju.Id, 2), (buddy!.ChildId, buddy.PresentDays));
    }

    [Fact]
    public void Nobody_there_on_all_days_picks_the_most_days_and_nobody_at_all_is_teacher_led()
    {
        // D-59. Ravi away Tue + Wed: Asha and Manju missed Tue (back Wed), so each was there one day; Kiran and Divya
        // are away today. Lowest roll wins the tie.
        var partly = Data(
            Day(Mon.AddDays(1), absent: [Ravi, Asha, Manju]),
            Day(Mon.AddDays(2), absent: [Ravi]),
            Day(Today, absent: [Kiran, Divya]));
        // Everyone else was away Wed and is back today too: nobody in class on Ravi's missed day.
        var nobody = Data(
            Day(Mon.AddDays(2), absent: [Ravi, Asha, Manju, Kiran, Divya]),
            Day(Today, absent: []));

        var some = Assert.Single(BuddyCardBuilder.Build(partly, [], [], Book, Today)).Buddy;
        var none = BuddyCardBuilder.Build(nobody, [], [], Book, Today);

        Assert.Equal((Asha.Id, 1), (some!.ChildId, some.PresentDays));
        Assert.Equal(5, none.Count);
        Assert.All(none, c => Assert.Null(c.Buddy)); // "Teacher-led: 10 minutes" 
    }

    [Fact]
    public void Ties_spread_buddy_roles_across_returning_children_then_go_by_roll()
    {
        // Ravi and Divya both away Wed, back today; Asha and Manju were both there.
        var data = Data(Day(Mon.AddDays(2), absent: [Ravi, Divya, Kiran]), Day(Today, absent: [Kiran]));

        var cards = BuddyCardBuilder.Build(data, [], [], Book, Today);

        Assert.Equal([Ravi.Id, Divya.Id], cards.Select(c => c.ChildId)); // roll order
        Assert.Equal(Asha.Id, cards[0].Buddy!.ChildId);
        Assert.Equal(Manju.Id, cards[1].Buddy!.ChildId);
    }

    [Fact]
    public void Three_different_sums_by_code_with_correct_answers_and_the_same_on_every_reload()
    {
        var data = Data(Day(Mon.AddDays(2), absent: [Ravi]), Day(Today, absent: []), lessons: [(Mon.AddDays(2), "M3.SUB.BORROW")]);

        var first = Assert.Single(BuddyCardBuilder.Build(data, [], [], Book, Today));
        var again = Assert.Single(BuddyCardBuilder.Build(data, [], [], Book, Today));

        Assert.Equal("sub", first.PracticeKind);
        Assert.Equal(BuddyCardBuilder.SumsPerCard, first.Sums.Count);
        Assert.Equal(3, first.Sums.Select(s => s.Question).Distinct().Count());
        Assert.All(first.Sums, s => Assert.Equal(Compute(s.Question), s.Answer)); // guardrail 4: recomputed
        Assert.Equal(first.Sums, again.Sums);
        Assert.Equal(["ಕಳೆ", "ದಶಕ", "ಹತ್ತು"], first.KeyWordsKn);
        Assert.Equal(new TextbookPage("textbook", 2, 15), first.Homework);
    }

    [Fact]
    public void Target_is_the_weeks_catch_up_first_target_else_the_latest_missed_lesson_else_nothing()
    {
        // Away Tue (addition) and Wed (subtraction).
        var data = Data(
            Day(Mon.AddDays(1), absent: [Ravi]),
            Day(Mon.AddDays(2), absent: [Ravi]),
            Day(Today, absent: []),
            lessons: [(Mon.AddDays(1), "M3.ADD.CARRY"), (Mon.AddDays(2), "M3.SUB.BORROW")]);
        var noLessons = Data(Day(Mon.AddDays(2), absent: [Ravi]), Day(Today, absent: []));

        var fromCatchUp = BuddyCardBuilder.Build(data, [new CatchUpTargets(Ravi.Id, Mon, ["M3.ADD.CARRY", "M3.SUB.BORROW"])], [], Book, Today);
        var latest = BuddyCardBuilder.Build(data, [new CatchUpTargets(Ravi.Id, Mon.AddDays(-7), ["M3.ADD.CARRY"])], [], Book, Today);
        var nothing = Assert.Single(BuddyCardBuilder.Build(noLessons, [], [], Book, Today));

        Assert.Equal("M3.ADD.CARRY", Assert.Single(fromCatchUp).CompetencyId);
        Assert.Equal("M3.SUB.BORROW", Assert.Single(latest).CompetencyId); // last week's catch-up doesn't count
        Assert.Null(nothing.CompetencyId);
        Assert.Empty(nothing.Sums);
        Assert.Null(nothing.Homework);
    }

    [Fact]
    public void A_lesson_without_a_practice_rule_has_words_and_page_but_no_sums()
    {
        var data = Data(Day(Mon.AddDays(2), absent: [Ravi]), Day(Today, absent: []), lessons: [(Mon.AddDays(2), "M3.PV.READ")]);

        var card = Assert.Single(BuddyCardBuilder.Build(data, [], [], Book, Today));

        Assert.Equal("M3.PV.READ", card.CompetencyId);
        Assert.Null(card.PracticeKind);
        Assert.Empty(card.Sums);
    }

    [Fact]
    public void Stopped_and_no_phone_children_get_a_card_that_says_nothing_about_the_family()
    {
        // FR-12, FR-19, B-20: Ravi's family stopped messages; Asha has no phone.
        var data = Data(Day(Mon.AddDays(2), absent: [Ravi, Asha]), Day(Today, absent: []));

        var cards = BuddyCardBuilder.Build(data, [], [new BuddyTicks(Ravi.Id, true, false)], Book, Today);

        Assert.Equal([Asha.Id, Ravi.Id], cards.Select(c => c.ChildId));
        Assert.True(cards[1].HomeworkReturned);
        Assert.False(cards[1].Done);
        Assert.DoesNotContain(typeof(BuddyCardView).GetProperties(), p => p.Name is "Channel" or "Consent" or "Stopped" or "Outcome");
    }

    private static int Compute(string question)
    {
        var parts = question.Replace('−', '-').Split(' ');
        return Convert.ToInt32(new DataTable().Compute(string.Join(' ', parts), null));
    }

    private static readonly LedgerChildRow[] All = [Asha, Manju, Kiran, Ravi, Divya];

    private static IEnumerable<RegisterRow> Day(DateOnly date, LedgerChildRow[] absent) =>
        All.Select(c => new RegisterRow(c.Id, date, !absent.Contains(c)));

    private static LedgerData Data(params IEnumerable<RegisterRow>[] days) => Data(days, []);

    private static LedgerData Data(IEnumerable<RegisterRow> a, IEnumerable<RegisterRow> b, (DateOnly Date, string Id)[] lessons) =>
        Data([a, b], lessons);

    private static LedgerData Data(IEnumerable<RegisterRow> a, IEnumerable<RegisterRow> b, IEnumerable<RegisterRow> c, (DateOnly Date, string Id)[] lessons) =>
        Data([a, b, c], lessons);

    private static LedgerData Data(IEnumerable<IEnumerable<RegisterRow>> days, (DateOnly Date, string Id)[] lessons) =>
        new(All, days.SelectMany(d => d).ToList(), lessons.Select(l => new LessonRow(Guid.NewGuid(), l.Date, [l.Id])).ToList());
}
