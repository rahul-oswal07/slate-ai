using SlateAi.Api.Curriculum;

namespace SlateAi.Api.Features.TextbookImport;

/// <summary>
/// Turns the checked outline and lessons into a draft curriculum (T-146): IDs, teaching order, textbook pages and
/// prerequisites are made by code. Every lesson passes <see cref="CurriculumLoader"/>'s rules; a field that doesn't is
/// dropped and flagged. Pure.
/// </summary>
public static class DraftAssembler
{
    /// <summary>
    /// The book's lessons in order, without prerequisites yet. IDs are <c>M{grade}.{key}</c>, made unique across the book
    /// (a second ADD.CARRY becomes ADD.CARRY2, flagged).
    /// </summary>
    public static IReadOnlyList<DraftLesson> Lessons(
        ImportBook book, BookOutline outline, IReadOnlyList<(OutlineChapter Chapter, IReadOnlyList<ExtractedLesson> Lessons)> chapters)
    {
        var taken = new HashSet<string>(StringComparer.Ordinal);
        var lessons = new List<DraftLesson>();
        var sequence = 0;
        foreach (var (chapter, extracted) in chapters)
        {
            foreach (var lesson in extracted)
            {
                var flags = lesson.Flags.ToList();
                var id = $"M{book.Grade}.{lesson.Key}";
                if (!taken.Add(id))
                {
                    var n = 2;
                    while (!taken.Add($"{id}{n}"))
                    {
                        n++;
                    }

                    flags.Add($"Another lesson in the book already has the ID {id}, so this one is {id}{n}.");
                    id = $"{id}{n}";
                }

                var printed = lesson.Pages.Select(p => outline.PrintedPages.TryGetValue(p, out var n) ? n : (int?)null).OfType<int>().ToList();
                var page = printed.Count > 0 ? new TextbookPage(book.Book, book.Part, printed.Min()) : null;
                if (page is null)
                {
                    flags.Add("No printed page number was read for this lesson, so families won't be told a page.");
                }

                var competency = new Competency(
                    id,
                    lesson.NameKn,
                    lesson.NameEn,
                    [],
                    NipunOutcome: null,
                    chapter.Number,
                    ++sequence,
                    lesson.BookExamples,
                    page,
                    lesson.NameTe,
                    lesson.NameTa,
                    lesson.NameHi,
                    lesson.KeyWordsKn.Count > 0 ? lesson.KeyWordsKn : null,
                    lesson.Practice,
                    lesson.Exercises);
                competency = Repair(competency, flags);

                lessons.Add(new DraftLesson(
                    competency,
                    new LessonExtras(
                        lesson.Pages,
                        printed.Distinct().Order().ToList(),
                        lesson.Content?.Description,
                        lesson.Content?.Examples ?? [],
                        lesson.OtherExercises,
                        lesson.Pictures),
                    flags));
            }
        }

        return lessons;
    }

    /// <summary>The draft with prerequisites (null = A10 couldn't give them: none, and every lesson says so).</summary>
    /// <exception cref="CurriculumException">The draft breaks a rule code should already have kept: a bug, not bad input.</exception>
    public static DraftCurriculum Finish(
        BookOutline outline, IReadOnlyList<DraftLesson> lessons, IReadOnlyDictionary<string, IReadOnlyList<string>>? prerequisites)
    {
        var finished = lessons.Select(l => prerequisites is null
                ? l with { Flags = [.. l.Flags, "Prerequisites couldn't be worked out; add them by hand."] }
                : l with { Competency = l.Competency with { Prerequisites = prerequisites.GetValueOrDefault(l.Competency.Id) ?? [] } })
            .ToList();
        var chapters = outline.Chapters
            .Where(c => finished.Any(l => l.Competency.Chapter == c.Number))
            .Select(c => new Chapter(c.Number, c.NameKn, c.NameEn))
            .ToList();

        // The same checks a curriculum file gets, across the whole draft: a failure here is a bug in this class.
        CurriculumLoader.Load([("draft", new CurriculumFile(0, "", null, finished.Select(l => l.Competency).ToList(), chapters))]);
        return new DraftCurriculum(chapters, finished);
    }

    /// <summary>Drops whatever breaks a rule on its own (practice, exercises, key words, other names), flagging each.</summary>
    private static Competency Repair(Competency competency, List<string> flags)
    {
        (Func<Competency, bool> Has, Func<Competency, Competency> Drop, string What)[] fixes =
        [
            (c => c.Practice is not null, c => c with { Practice = null }, "practice rule"),
            (c => c.Exercises is not null, c => c with { Exercises = null }, "exercises"),
            (c => c.KeyWordsKn is not null, c => c with { KeyWordsKn = null }, "key words"),
            (c => c.NameTe is not null || c.NameTa is not null || c.NameHi is not null,
                c => c with { NameTe = null, NameTa = null, NameHi = null }, "Telugu, Tamil and Hindi names"),
        ];
        foreach (var (has, drop, what) in fixes)
        {
            if (CurriculumLoader.Problems(competency) is not { Count: > 0 } problems || !has(competency))
            {
                continue;
            }

            // Keep the field unless leaving it out is what fixes a problem.
            var without = drop(competency);
            var left = CurriculumLoader.Problems(without);
            if (left.Count < problems.Count)
            {
                var fixedProblem = problems.First(p => !left.Contains(p));
                flags.Add($"The {what} broke a rule ({fixedProblem}) and {(what.EndsWith('s') ? "were" : "was")} left out.");
                competency = without;
            }
        }

        return competency;
    }
}
