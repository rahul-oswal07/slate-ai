using System.Text.Json;
using System.Text.RegularExpressions;
using SlateAi.Api.Curriculum;
using SlateAi.Api.Features.LessonContents;

namespace SlateAi.Api.Features.TextbookImport;

/// <summary>
/// Stage 2 of the import (T-146): A10 reads one chapter's pages (a part at a time) and lists its lessons. Code checks
/// every field: numbers through <see cref="ExerciseAnswers.Check"/>, words through <see cref="LessonContentRules"/>, pages
/// against the part. Anything that fails is dropped and flagged for the admin, never passed through (guardrail 2). Pure.
/// </summary>
public static partial class TextbookLessons
{
    public const string PromptId = "a10-textbook-lessons";

    /// <summary>Pages per call: a chapter longer than this goes in parts.</summary>
    public const int MaxPagesPerPart = 12;

    private const int MaxName = 100;
    private const int MaxKeyWords = 8;
    private const int MaxKeyWord = 40;
    private const int MaxBookExamples = 2;
    private const int MaxBookExample = 80;
    private const int MaxPerKind = 40;
    private const int MaxNotes = 30;
    private const int MaxSummary = 200;
    private const int MaxDescription = 300;
    private const int MaxKeySegments = 4;
    private const int MaxKeySegment = 20;

    private static readonly string[] Positions = ["top", "middle", "bottom", "full"];
    private static readonly string[] PracticeKinds = ["none", .. PracticeRule.Kinds];
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static JsonElement Schema()
    {
        var numbers = JsonSchema.Array(JsonSchema.Array(JsonSchema.Integer()));
        var pageNote = JsonSchema.Object(("page", JsonSchema.Integer()), ("summary", JsonSchema.String()));
        var picture = JsonSchema.Object(
            ("page", JsonSchema.Integer()),
            ("position", JsonSchema.Enum(Positions)),
            ("description", JsonSchema.String()),
            ("teaches", JsonSchema.String()));
        var lesson = JsonSchema.Object(
            ("key", JsonSchema.String()),
            ("nameKn", JsonSchema.String()),
            ("nameEn", JsonSchema.String()),
            ("nameTe", JsonSchema.String()),
            ("nameTa", JsonSchema.String()),
            ("nameHi", JsonSchema.String()),
            ("keyWordsKn", JsonSchema.Array(JsonSchema.String())),
            ("pages", JsonSchema.Array(JsonSchema.Integer())),
            ("bookExamples", JsonSchema.Array(JsonSchema.String())),
            ("description", JsonSchema.String()),
            ("examples", JsonSchema.Array(JsonSchema.String())),
            ("exercises", JsonSchema.Object(("compare", numbers), ("extremes", numbers), ("add", numbers), ("sub", numbers))),
            ("otherExercises", JsonSchema.Array(pageNote)),
            ("pictures", JsonSchema.Array(picture)),
            ("practice", JsonSchema.Object(
                ("kind", JsonSchema.Enum(PracticeKinds)),
                ("digits", JsonSchema.Integer()),
                ("carry", JsonSchema.Enum(["none", "ones", "any"])),
                ("borrow", JsonSchema.Enum(["none", "tens"])))));
        return JsonSchema.ToElement(JsonSchema.Object(("lessons", JsonSchema.Array(lesson))));
    }

    /// <summary>The lessons on this part's pages, checked; null when the output can't be used at all.</summary>
    /// <param name="pages">The part's PDF pages in order: A10 numbers them from 1.</param>
    /// <param name="printed">PDF page → the number printed on it, from the outline.</param>
    public static IReadOnlyList<ExtractedLesson>? Validate(
        string json, IReadOnlyList<int> pages, IReadOnlyDictionary<int, int> printed)
    {
        LessonsOutput? output;
        try
        {
            output = JsonSerializer.Deserialize<LessonsOutput>(json, Json);
        }
        catch (JsonException)
        {
            return null;
        }

        if (output?.Lessons is null)
        {
            return null;
        }

        int? Page(int? index) => index is { } i && i >= 1 && i <= pages.Count ? pages[i - 1] : null;
        int? Printed(int page) => printed.TryGetValue(page, out var p) ? p : null;

        var lessons = new List<ExtractedLesson>();
        foreach (var l in output.Lessons)
        {
            var flags = new List<string>();
            var nameKn = JsonSchema.Clean(l.NameKn, MaxName);
            var nameEn = JsonSchema.Clean(l.NameEn, MaxName);
            if (nameKn is null && nameEn is null)
            {
                continue; // nothing to call it: not a lesson
            }

            if (nameKn is null || nameEn is null)
            {
                flags.Add(nameKn is null ? "No Kannada name was read; the English one is used." : "No English name was read; the Kannada one is used.");
            }

            var lessonPages = (l.Pages ?? []).Select(Page).OfType<int>().Distinct().Order().ToList();
            if (lessonPages.Count == 0)
            {
                flags.Add("No pages were given; the part's first page is used.");
                lessonPages = [pages[0]];
            }

            var (content, contentFlag) = Content(l.Description, l.Examples);
            if (contentFlag is not null)
            {
                flags.Add(contentFlag);
            }

            var (exercises, exerciseFlags) = Exercises(l.Exercises);
            flags.AddRange(exerciseFlags);

            var keyWords = (l.KeyWordsKn ?? [])
                .Select(w => JsonSchema.Clean(w, MaxKeyWord)).OfType<string>()
                .Distinct().Take(MaxKeyWords).ToList();

            lessons.Add(new ExtractedLesson(
                Key(l.Key, nameEn ?? ""),
                nameKn ?? nameEn!,
                nameEn ?? nameKn!,
                JsonSchema.Clean(l.NameTe, MaxName),
                JsonSchema.Clean(l.NameTa, MaxName),
                JsonSchema.Clean(l.NameHi, MaxName),
                keyWords,
                lessonPages,
                (l.BookExamples ?? []).Select(e => JsonSchema.Clean(e, MaxBookExample)).OfType<string>().Take(MaxBookExamples).ToList(),
                content,
                exercises,
                (l.OtherExercises ?? [])
                    .Select(o => (Page: Page(o.Page) ?? lessonPages[0], Summary: JsonSchema.Clean(o.Summary, MaxSummary)))
                    .Where(o => o.Summary is not null)
                    .Take(MaxNotes)
                    .Select(o => new OtherExercise(o.Page, Printed(o.Page), o.Summary!))
                    .ToList(),
                (l.Pictures ?? [])
                    .Select(p => (Page: Page(p.Page) ?? lessonPages[0], p.Position, Description: JsonSchema.Clean(p.Description, MaxDescription), p.Teaches))
                    .Where(p => p.Description is not null)
                    .Take(MaxNotes)
                    .Select(p => new PictureNote(
                        p.Page, Printed(p.Page), Positions.Contains(p.Position) ? p.Position! : "middle", p.Description!,
                        JsonSchema.Clean(p.Teaches, MaxSummary) ?? ""))
                    .ToList(),
                Practice(l.Practice),
                flags));
        }

        return lessons;
    }

    /// <summary>
    /// One chapter's lessons from all its parts: a lesson that carries on into a later part (same key) is one lesson, with
    /// the pages, key words, exercises, other exercises and pictures of every part; names and words come from the first.
    /// </summary>
    public static IReadOnlyList<ExtractedLesson> Merge(IEnumerable<IReadOnlyList<ExtractedLesson>> parts)
    {
        var merged = new List<ExtractedLesson>();
        foreach (var lesson in parts.SelectMany(p => p))
        {
            var index = merged.FindIndex(m => m.Key == lesson.Key);
            if (index < 0)
            {
                merged.Add(lesson);
                continue;
            }

            var first = merged[index];
            merged[index] = first with
            {
                KeyWordsKn = first.KeyWordsKn.Concat(lesson.KeyWordsKn).Distinct().Take(MaxKeyWords).ToList(),
                Pages = first.Pages.Concat(lesson.Pages).Distinct().Order().ToList(),
                BookExamples = first.BookExamples.Concat(lesson.BookExamples).Distinct().Take(MaxBookExamples).ToList(),
                Content = first.Content ?? lesson.Content,
                Exercises = Join(first.Exercises, lesson.Exercises),
                OtherExercises = first.OtherExercises.Concat(lesson.OtherExercises).Take(MaxNotes).ToList(),
                Pictures = first.Pictures.Concat(lesson.Pictures).Take(MaxNotes).ToList(),
                Practice = first.Practice ?? lesson.Practice,
                Flags = first.Flags.Concat(lesson.Flags).Distinct().ToList(),
            };
        }

        return merged;
    }

    /// <summary>
    /// What A10 gets with a part's pages: the book, the chapter, the printed page numbers, and the lessons found in earlier
    /// parts of the chapter (by key) so one that carries on keeps its key.
    /// </summary>
    public static string UserContent(
        ImportBook book, OutlineChapter chapter, int part, int parts, IReadOnlyList<int> pages,
        IReadOnlyDictionary<int, int> printed, IReadOnlyList<ExtractedLesson> earlier) =>
        JsonSerializer.Serialize(new
        {
            grade = book.Grade,
            subject = book.Subject,
            medium = book.Medium,
            chapter = new { number = chapter.Number, nameKn = chapter.NameKn, nameEn = chapter.NameEn },
            part,
            parts,
            pages = pages.Select((p, i) => new { page = i + 1, printedPage = printed.TryGetValue(p, out var n) ? n : (int?)null }),
            earlierLessons = earlier.Select(l => new { l.Key, l.NameEn }),
        }, Json);

    /// <summary>
    /// The ID part after <c>M{grade}.</c>: upper case letters and digits in 1–4 dot-separated parts, e.g. ADD.CARRY. A key
    /// that isn't usable is made from the English name.
    /// </summary>
    public static string Key(string? key, string nameEn)
    {
        static string Clean(string text)
        {
            var upper = GradePrefix().Replace(text.Trim().ToUpperInvariant(), "");
            var segments = upper.Split('.', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => NotKeyChars().Replace(s, ""))
                .Where(s => s.Length > 0)
                .Select(s => s.Length > MaxKeySegment ? s[..MaxKeySegment] : s)
                .Take(MaxKeySegments);
            return string.Join('.', segments);
        }

        var cleaned = Clean(key ?? "");
        if (cleaned.Length > 0)
        {
            return cleaned;
        }

        var words = NotKeyChars().Replace(nameEn.ToUpperInvariant().Replace(' ', '.'), "");
        return Clean(words) is { Length: > 0 } fromName ? fromName : "LESSON";
    }

    private static (LessonContentText? Content, string? Flag) Content(string? description, List<string?>? examples)
    {
        var (content, errors) = LessonContentRules.Normalise(description, examples?.Take(LessonContentRules.MaxExamples).ToList());
        if (errors.Count > 0)
        {
            return (null, "The description was left out: " + string.Join(" ", errors.SelectMany(e => e.Value.Select(m => $"{e.Key}: {m}"))));
        }

        return content is null ? (null, "No description was written.") : (content, null);
    }

    private static (ExerciseList? Exercises, List<string> Flags) Exercises(ExercisesOutput? output)
    {
        var flags = new List<string>();

        List<IReadOnlyList<int>> Keep(string kind, List<List<int>>? items, Func<IReadOnlyList<IReadOnlyList<int>>, ExerciseList> wrap)
        {
            var kept = new List<IReadOnlyList<int>>();
            foreach (var item in (items ?? []).Take(MaxPerKind))
            {
                IReadOnlyList<int> numbers = item ?? [];
                if (ExerciseAnswers.Check(wrap([numbers])).FirstOrDefault() is { } problem)
                {
                    flags.Add($"Exercise left out ({kind} {string.Join(", ", numbers)}): {problem}.");
                }
                else if (!kept.Any(k => k.SequenceEqual(numbers)))
                {
                    kept.Add(numbers);
                }
            }

            return kept;
        }

        var compare = Keep("compare", output?.Compare, items => new ExerciseList(Compare: items));
        var extremes = Keep("extremes", output?.Extremes, items => new ExerciseList(Extremes: items));
        var add = Keep("add", output?.Add, items => new ExerciseList(Add: items));
        var sub = Keep("sub", output?.Sub, items => new ExerciseList(Sub: items));
        var list = new ExerciseList(
            compare.Count > 0 ? compare : null,
            extremes.Count > 0 ? extremes : null,
            add.Count > 0 ? add : null,
            sub.Count > 0 ? sub : null);
        return (list.HasAny ? list : null, flags);
    }

    private static ExerciseList? Join(ExerciseList? a, ExerciseList? b)
    {
        if (a is null || b is null)
        {
            return a ?? b;
        }

        static IReadOnlyList<IReadOnlyList<int>>? Both(IReadOnlyList<IReadOnlyList<int>>? x, IReadOnlyList<IReadOnlyList<int>>? y)
        {
            var all = (x ?? []).Concat(y ?? []).ToList();
            var distinct = all.Where((item, i) => !all.Take(i).Any(earlier => earlier.SequenceEqual(item))).Take(MaxPerKind).ToList();
            return distinct.Count > 0 ? distinct : null;
        }

        return new ExerciseList(Both(a.Compare, b.Compare), Both(a.Extremes, b.Extremes), Both(a.Add, b.Add), Both(a.Sub, b.Sub));
    }

    /// <summary>A practice rule only when A10 chose one; the loader's rules check it later (digits, carry, borrow).</summary>
    private static PracticeRule? Practice(PracticeOutput? output) =>
        output?.Kind is null or "none" || !PracticeRule.Kinds.Contains(output.Kind)
            ? null
            : new PracticeRule(
                output.Kind,
                output.Digits ?? 2,
                output.Kind == "add" ? output.Carry ?? "none" : null,
                output.Kind == "sub" ? output.Borrow ?? "none" : null);

    [GeneratedRegex(@"^M[1-5]\.")]
    private static partial Regex GradePrefix();

    [GeneratedRegex("[^A-Z0-9.]")]
    private static partial Regex NotKeyChars();

    private sealed record LessonsOutput(List<LessonOutput>? Lessons);

    private sealed record LessonOutput(
        string? Key,
        string? NameKn,
        string? NameEn,
        string? NameTe,
        string? NameTa,
        string? NameHi,
        List<string?>? KeyWordsKn,
        List<int?>? Pages,
        List<string?>? BookExamples,
        string? Description,
        List<string?>? Examples,
        ExercisesOutput? Exercises,
        List<PageNoteOutput>? OtherExercises,
        List<PictureOutput>? Pictures,
        PracticeOutput? Practice);

    private sealed record ExercisesOutput(List<List<int>>? Compare, List<List<int>>? Extremes, List<List<int>>? Add, List<List<int>>? Sub);

    private sealed record PageNoteOutput(int? Page, string? Summary);

    private sealed record PictureOutput(int? Page, string? Position, string? Description, string? Teaches);

    private sealed record PracticeOutput(string? Kind, int? Digits, string? Carry, string? Borrow);
}
