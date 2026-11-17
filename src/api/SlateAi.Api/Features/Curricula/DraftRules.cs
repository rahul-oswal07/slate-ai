using SlateAi.Api.Curriculum;
using SlateAi.Api.Data.Entities;
using SlateAi.Api.Features.LessonContents;
using SlateAi.Api.Features.TextbookImport;

namespace SlateAi.Api.Features.Curricula;

/// <summary>A stored curriculum as the review page reads it (T-147).</summary>
/// <param name="Book">The book its pages are in, from its import ("textbook" if it has none).</param>
/// <param name="Notes">About the whole book, from its import.</param>
/// <param name="ClassIds">Classes that follow it.</param>
public sealed record CurriculumDraft(
    StoredCurriculum Meta,
    string Book,
    int? Part,
    IReadOnlyList<string> Notes,
    IReadOnlyList<Chapter> Chapters,
    IReadOnlyList<DraftLessonRow> Lessons,
    IReadOnlyList<Guid> ClassIds);

/// <summary>One lesson of a stored curriculum with what the review page needs.</summary>
public sealed record DraftLessonRow(Competency Competency, LessonExtras? Extras, string ReviewStatus, IReadOnlyList<string> Flags);

/// <summary>An admin's edit of one draft lesson (T-147). The ID, chapter and order don't change.</summary>
/// <param name="Page">The printed page; the book and part stay the curriculum's.</param>
/// <param name="Reviewed">Mark the lesson checked (or not yet).</param>
public sealed record EditDraftLessonRequest(
    Guid ClientRequestId,
    string? NameKn,
    string? NameEn,
    string? NameTe,
    string? NameTa,
    string? NameHi,
    IReadOnlyList<string?>? KeyWordsKn,
    int? Page,
    IReadOnlyList<string>? Prerequisites,
    PracticeRule? Practice,
    ExerciseList? Exercises,
    IReadOnlyList<string?>? BookExamples,
    string? Description,
    IReadOnlyList<string?>? Examples,
    IReadOnlyList<OtherExercise>? OtherExercises,
    IReadOnlyList<PictureNote>? Pictures,
    bool Reviewed);

/// <summary>
/// What an admin may change in a draft and when a draft can be published (T-147, D-125). Every lesson keeps passing the
/// curriculum file's rules (<see cref="CurriculumLoader"/>); prerequisites stay earlier lessons, so a draft never gets a
/// cycle. Pure.
/// </summary>
public static class DraftRules
{
    private const int MaxName = 100;
    private const int MaxKeyWords = 8;
    private const int MaxKeyWord = 40;
    private const int MaxBookExamples = 2;
    private const int MaxBookExample = 80;
    private const int MaxNotes = 30;
    private const int MaxSummary = 200;
    private const int MaxDescription = 300;
    private const int MaxPage = 2000;
    private static readonly string[] Positions = ["top", "middle", "bottom", "full"];

    /// <summary>The lesson after the edit, or the errors by field (nothing changes then).</summary>
    public static (DraftLessonRow? Lesson, Dictionary<string, string[]> Errors) Apply(
        CurriculumDraft draft, DraftLessonRow lesson, EditDraftLessonRequest edit)
    {
        var errors = new Dictionary<string, string[]>();
        var current = lesson.Competency;

        var sequence = draft.Lessons.ToDictionary(l => l.Competency.Id, l => l.Competency.Sequence);
        var prerequisites = (edit.Prerequisites ?? []).Distinct().ToList();
        if (prerequisites.Any(p => !sequence.TryGetValue(p, out var before) || before >= current.Sequence))
        {
            errors["prerequisites"] = ["Only lessons that come earlier in this curriculum."];
        }
        else if (prerequisites.Count > TextbookPrerequisites.MaxPerLesson)
        {
            errors["prerequisites"] = [$"At most {TextbookPrerequisites.MaxPerLesson}."];
        }

        if (edit.Page is { } page && page is < 1 or > MaxPage)
        {
            errors["page"] = [$"A page from 1 to {MaxPage}, or none."];
        }

        var (content, contentErrors) = LessonContentRules.Normalise(edit.Description, edit.Examples);
        foreach (var (field, messages) in contentErrors)
        {
            errors[field] = messages;
        }

        if ((edit.Pictures ?? []).Any(p => !Positions.Contains(p.Position)))
        {
            errors["pictures"] = ["Position is top, middle, bottom or full."];
        }

        var keyWords = Clean(edit.KeyWordsKn, MaxKeyWord).Distinct().Take(MaxKeyWords).ToList();
        var exercises = edit.Exercises is { HasAny: true } list ? list : null;
        var competency = current with
        {
            NameKn = Clean(edit.NameKn, MaxName) ?? "",
            NameEn = Clean(edit.NameEn, MaxName) ?? "",
            NameTe = Clean(edit.NameTe, MaxName),
            NameTa = Clean(edit.NameTa, MaxName),
            NameHi = Clean(edit.NameHi, MaxName),
            KeyWordsKn = keyWords.Count > 0 ? keyWords : null,
            TextbookPage = edit.Page is { } p ? new TextbookPage(current.TextbookPage?.Book ?? draft.Book, current.TextbookPage?.Part ?? draft.Part, p) : null,
            Prerequisites = prerequisites,
            Practice = edit.Practice is { Kind: not (null or "none") } practice ? practice : null,
            Exercises = exercises,
            Examples = Clean(edit.BookExamples, MaxBookExample).Take(MaxBookExamples).ToList(),
        };
        var problems = CurriculumLoader.Problems(competency);
        if (problems.Count > 0)
        {
            errors["lesson"] = [.. problems];
        }

        if (errors.Count > 0)
        {
            return (null, errors);
        }

        var extras = new LessonExtras(
            lesson.Extras?.Pages ?? [],
            lesson.Extras?.PrintedPages ?? [],
            content?.Description,
            content?.Examples ?? [],
            (edit.OtherExercises ?? [])
                .Select(o => o with { Summary = Clean(o.Summary, MaxSummary) ?? "" })
                .Where(o => o.Summary.Length > 0).Take(MaxNotes).ToList(),
            (edit.Pictures ?? [])
                .Select(p => p with { Description = Clean(p.Description, MaxDescription) ?? "", Teaches = Clean(p.Teaches, MaxSummary) ?? "" })
                .Where(p => p.Description.Length > 0).Take(MaxNotes).ToList());
        return (lesson with
        {
            Competency = competency,
            Extras = extras,
            ReviewStatus = edit.Reviewed ? CurriculumReviewStatuses.Reviewed : CurriculumReviewStatuses.NeedsReview,
        }, errors);
    }

    /// <summary>
    /// Why the draft can't be published yet: lessons not reviewed, or a curriculum rule it breaks. Empty = it can.
    /// </summary>
    public static IReadOnlyList<string> PublishProblems(CurriculumDraft draft)
    {
        if (draft.Lessons.Count == 0)
        {
            return ["The curriculum has no lessons."];
        }

        var problems = draft.Lessons
            .Where(l => l.ReviewStatus != CurriculumReviewStatuses.Reviewed)
            .Select(l => $"{l.Competency.Id}: not reviewed yet.")
            .ToList();
        try
        {
            CurriculumLoader.Load([(draft.Meta.Title, new CurriculumFile(
                draft.Meta.Grade, draft.Meta.Subject, null, draft.Lessons.Select(l => l.Competency).ToList(), draft.Chapters))]);
        }
        catch (CurriculumException e)
        {
            problems.AddRange(e.Errors);
        }

        return problems;
    }

    /// <summary>The lesson IDs a class has used that <paramref name="target"/> doesn't have: moving the class there would lose them.</summary>
    public static IReadOnlyList<string> Missing(IEnumerable<string> used, IReadOnlySet<string> target) =>
        used.Where(id => !target.Contains(id)).Distinct().Order(StringComparer.Ordinal).ToList();

    /// <summary>The answers code works out for the lesson's exercises, shown to the admin only (guardrail 4).</summary>
    public static ExerciseAnswerList Answers(ExerciseList? exercises) => new(
        (exercises?.Compare ?? []).Select(p => p.Count == 2 ? ExerciseAnswers.CompareSign(p[0], p[1]) : "").ToList(),
        (exercises?.Extremes ?? []).Select(set => set.Count > 0 ? ExerciseAnswers.Extremes(set) is var (lo, hi) ? new[] { lo, hi } : [] : []).ToList(),
        (exercises?.Add ?? []).Select(p => p.Count == 2 ? ExerciseAnswers.Sum(p[0], p[1]) : 0).ToList(),
        (exercises?.Sub ?? []).Select(p => p.Count == 2 ? ExerciseAnswers.Difference(p[0], p[1]) : 0).ToList());

    private static string? Clean(string? text, int max)
    {
        var trimmed = text?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed.Length > max ? trimmed[..max].TrimEnd() : trimmed;
    }

    private static IEnumerable<string> Clean(IEnumerable<string?>? texts, int max) =>
        (texts ?? []).Select(t => Clean(t, max)).OfType<string>();
}

/// <summary>Code's answers to a lesson's exercises, in the same order: the sign, [smallest, biggest], the sum, the difference.</summary>
public sealed record ExerciseAnswerList(
    IReadOnlyList<string> Compare, IReadOnlyList<int[]> Extremes, IReadOnlyList<int> Add, IReadOnlyList<int> Sub);
