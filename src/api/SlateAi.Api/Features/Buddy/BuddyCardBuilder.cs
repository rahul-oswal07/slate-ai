using SlateAi.Api.Curriculum;
using SlateAi.Api.Features.CatchUps.Compression;
using SlateAi.Api.Features.CatchUps.Practice;
using SlateAi.Api.Features.Ledger;

namespace SlateAi.Api.Features.Buddy;

/// <param name="PresentDays">How many of the child's missed days the buddy was in class (all of them when possible, D-59).</param>
public sealed record BuddyView(Guid ChildId, string FirstName, int PresentDays);

/// <summary>A sum made by code (guardrail 4); the answer is for the teacher.</summary>
public sealed record BuddySum(string Question, int Answer);

/// <summary>
/// One return-day card (T5, FR-12). Never carries the family's channel or the picture-check result (B-20).
/// </summary>
/// <param name="Buddy">Null when no classmate was in class on any missed day: "Teacher-led: 10 minutes" (D-59).</param>
/// <param name="CompetencyId">The lesson the sums practise; null when no lesson was recorded on the missed days.</param>
/// <param name="PracticeKind">The lesson's practice kind (add, sub, expanded, count, placeValue, biggest, smallest), so the card words the three items to suit: sums to work out, numbers to make with sticks, or questions.</param>
public sealed record BuddyCardView(
    Guid ChildId,
    string FirstName,
    int RollNo,
    int DaysAway,
    BuddyView? Buddy,
    int MissedDays,
    string? CompetencyId,
    string? PracticeKind,
    IReadOnlyList<BuddySum> Sums,
    IReadOnlyList<string> KeyWordsKn,
    TextbookPage? Homework,
    bool HomeworkReturned,
    bool Done);

public sealed record BuddyCardsResponse(DateOnly Date, IReadOnlyList<BuddyCardView> Cards);

/// <summary>A child's catch-up targets for a week (A2's choice, most important first).</summary>
public sealed record CatchUpTargets(Guid ChildId, DateOnly WeekOf, IReadOnlyList<string> TargetIds);

/// <summary>The teacher's stored ticks on a card.</summary>
public sealed record BuddyTicks(Guid ChildId, bool HomeworkReturned, bool Done);

/// <summary>
/// Builds today's buddy cards from attendance, confirmed lessons and catch-up targets. Pure and deterministic: no AI,
/// the same inputs give the same buddy and the same sums.
/// </summary>
public static class BuddyCardBuilder
{
    public const int SumsPerCard = 3;
    public const int KeyWords = 3;

    public static IReadOnlyList<BuddyCardView> Build(
        LedgerData data,
        IReadOnlyList<CatchUpTargets> targets,
        IReadOnlyList<BuddyTicks> ticks,
        CurriculumGraph curriculum,
        DateOnly today)
    {
        var register = data.Register.ToLookup(r => r.ChildId);
        var presentOn = data.Register.Where(r => r.Present).Select(r => (r.ChildId, r.Date)).ToHashSet();
        var lessons = data.Lessons.ToLookup(l => l.Date);
        var tickByChild = ticks.ToDictionary(t => t.ChildId);

        // Back today: in class today, and their latest absence spell ended today.
        var returning = data.Children
            .OrderBy(c => c.RollNo)
            .Select(child =>
            {
                var days = register[child.Id].Where(d => d.Date <= today).ToList();
                var spell = AbsenceSpells.For(days.Select(d => (d.Date, d.Present))).LastOrDefault();
                return (Child: child, Spell: spell, Missed: spell is null
                    ? []
                    : days.Where(d => !d.Present && d.Date >= spell.From && d.Date <= spell.To).Select(d => d.Date).OrderBy(d => d).ToList());
            })
            .Where(r => presentOn.Contains((r.Child.Id, today)) && r.Spell?.ReturnedOn == today)
            .ToList();
        var returningIds = returning.Select(r => r.Child.Id).ToHashSet();
        var candidates = data.Children
            .Where(c => presentOn.Contains((c.Id, today)) && !returningIds.Contains(c.Id))
            .OrderBy(c => c.RollNo)
            .ToList();
        var buddyRoles = new Dictionary<Guid, int>();

        return returning.Select(r =>
        {
            var buddy = PickBuddy(r.Missed, candidates, presentOn, buddyRoles);
            var competency = Target(r.Child.Id, r.Missed, lessons, targets, curriculum);
            var tick = tickByChild.GetValueOrDefault(r.Child.Id);
            return new BuddyCardView(
                r.Child.Id,
                r.Child.FirstName,
                r.Child.RollNo,
                r.Spell!.SchoolDays,
                buddy,
                r.Missed.Count,
                competency?.Id,
                competency?.Practice?.Kind,
                competency is null ? [] : Sums(competency, r.Child.Id, today),
                (competency?.KeyWordsKn ?? []).Take(KeyWords).ToList(),
                competency?.TextbookPage,
                tick?.HomeworkReturned ?? false,
                tick?.Done ?? false);
        }).ToList();
    }

    /// <summary>
    /// D-59: the classmate in class on the most missed days; ties go to whoever has fewer buddy roles today (one
    /// child shouldn't pair with everyone), then the lowest roll number (candidates come in roll order).
    /// </summary>
    private static BuddyView? PickBuddy(
        IReadOnlyList<DateOnly> missed,
        IReadOnlyList<LedgerChildRow> candidates,
        HashSet<(Guid, DateOnly)> presentOn,
        Dictionary<Guid, int> buddyRoles)
    {
        var best = candidates
            .Select(c => (Child: c, Present: missed.Count(d => presentOn.Contains((c.Id, d)))))
            .Where(c => c.Present > 0)
            .OrderByDescending(c => c.Present)
            .ThenBy(c => buddyRoles.GetValueOrDefault(c.Child.Id))
            .FirstOrDefault();
        if (best.Child is null)
        {
            return null;
        }

        buddyRoles[best.Child.Id] = buddyRoles.GetValueOrDefault(best.Child.Id) + 1;
        return new BuddyView(best.Child.Id, best.Child.FirstName, best.Present);
    }

    /// <summary>
    /// The catch-up's first target for the week of the last missed day (the lesson the family's picture check
    /// tested); before catch-ups exist, the latest missed lesson. Null when no lesson was confirmed on those days.
    /// </summary>
    private static Competency? Target(
        Guid childId,
        IReadOnlyList<DateOnly> missed,
        ILookup<DateOnly, LessonRow> lessons,
        IReadOnlyList<CatchUpTargets> targets,
        CurriculumGraph curriculum)
    {
        if (missed.Count == 0)
        {
            return null;
        }

        var week = CompressionPlanner.Week(missed[^1]).From;
        var fromCatchUp = targets.FirstOrDefault(t => t.ChildId == childId && t.WeekOf == week)?.TargetIds
            .Select(curriculum.Get).FirstOrDefault(c => c is not null);
        if (fromCatchUp is not null)
        {
            return fromCatchUp;
        }

        return missed
            .OrderByDescending(d => d)
            .SelectMany(d => lessons[d])
            .SelectMany(l => l.CompetencyIds)
            .Select(curriculum.Get)
            .FirstOrDefault(c => c is not null);
    }

    /// <summary>Three different sums from the lesson's practice rule, seeded by child and day so a reload shows the same ones.</summary>
    private static IReadOnlyList<BuddySum> Sums(Competency competency, Guid childId, DateOnly today)
    {
        if (competency.Practice is not { } rule)
        {
            return [];
        }

        var rng = new Random(PackagePractice.Seed($"buddy:{childId}:{today:yyyy-MM-dd}"));
        var sums = new List<BuddySum>();
        for (var attempt = 0; attempt < 30 && sums.Count < SumsPerCard; attempt++)
        {
            var problem = ProblemGenerator.Make(rule, rng);
            if (sums.All(s => s.Question != problem.Question))
            {
                sums.Add(new BuddySum(problem.Question, problem.Answer));
            }
        }

        return sums;
    }
}
