using System.Text.Json;
using SlateAi.Api.Curriculum;
using SlateAi.Api.Features.LessonContents;

namespace SlateAi.Api.Features.TextbookImport;

/// <summary>What the admin said about the book at upload (T-146).</summary>
/// <param name="Book">"textbook", "workbook" or "nalikali", as <see cref="TextbookPage.Book"/>.</param>
public sealed record ImportBook(string Title, int Grade, string Subject, string Medium, string Book, int? Part);

/// <summary>One page of the book as A10's outline read it, checked by code.</summary>
/// <param name="Number">1-based page number in the PDF.</param>
/// <param name="PrintedPage">The number printed on the page, if it has one.</param>
/// <param name="Kind"><see cref="PageKinds"/>.</param>
/// <param name="ChapterStart">Set when a chapter begins on this page.</param>
public sealed record OutlinePage(int Number, int? PrintedPage, string Kind, ChapterStart? ChapterStart);

/// <param name="Number">The chapter's number as printed, if any (0 = bridge course).</param>
public sealed record ChapterStart(int? Number, string TitleKn, string TitleEn);

public static class PageKinds
{
    public const string Front = "front";
    public const string Contents = "contents";
    public const string Lesson = "lesson";
    public const string Exercises = "exercises";
    public const string Answers = "answers";
    public const string Other = "other";

    public static readonly string[] All = [Front, Contents, Lesson, Exercises, Answers, Other];

    /// <summary>Pages a lesson can be on; the rest are left out of every chapter.</summary>
    public static bool Teaches(string kind) => kind is Lesson or Exercises or Other;
}

/// <summary>A chapter of the book and the PDF pages that teach it.</summary>
public sealed record OutlineChapter(int Number, string NameKn, string NameEn, IReadOnlyList<int> Pages);

/// <summary>The book's chapters, and the printed number of each PDF page that has one.</summary>
public sealed record BookOutline(
    IReadOnlyList<OutlineChapter> Chapters, IReadOnlyDictionary<int, int> PrintedPages, IReadOnlyList<string> Flags);

/// <summary>An exercise the book has that code can't check (not compare, extremes, add or sub): kept as words (D-125).</summary>
/// <param name="Page">PDF page.</param>
public sealed record OtherExercise(int Page, int? PrintedPage, string Summary);

/// <summary>A picture in the book, described in our own words; the picture itself is never stored (D-125).</summary>
/// <param name="Position">top, middle, bottom or full.</param>
/// <param name="Teaches">What the picture is for in the lesson.</param>
public sealed record PictureNote(int Page, int? PrintedPage, string Position, string Description, string Teaches);

/// <summary>One lesson as A10 found it on some pages, checked by code; the ID is made later, across the whole book.</summary>
/// <param name="Key">The ID after <c>M{grade}.</c>, e.g. "ADD.CARRY".</param>
/// <param name="Pages">PDF pages.</param>
/// <param name="BookExamples">1–2 example problems as the book prints them (<see cref="Competency.Examples"/>).</param>
public sealed record ExtractedLesson(
    string Key,
    string NameKn,
    string NameEn,
    string? NameTe,
    string? NameTa,
    string? NameHi,
    IReadOnlyList<string> KeyWordsKn,
    IReadOnlyList<int> Pages,
    IReadOnlyList<string> BookExamples,
    LessonContentText? Content,
    ExerciseList? Exercises,
    IReadOnlyList<OtherExercise> OtherExercises,
    IReadOnlyList<PictureNote> Pictures,
    PracticeRule? Practice,
    IReadOnlyList<string> Flags);

/// <summary>
/// What the importer found about a lesson that isn't part of <see cref="Competency"/>, stored as
/// <c>CurriculumLessons.Extras</c> for the review page (T-147). Pages are PDF page numbers, with the printed one where known.
/// </summary>
public sealed record LessonExtras(
    IReadOnlyList<int> Pages,
    IReadOnlyList<int> PrintedPages,
    string? Description,
    IReadOnlyList<string> Examples,
    IReadOnlyList<OtherExercise> OtherExercises,
    IReadOnlyList<PictureNote> Pictures);

/// <summary>A lesson of the draft: checked by the loader's rules, with what the admin should look at.</summary>
public sealed record DraftLesson(Competency Competency, LessonExtras Extras, IReadOnlyList<string> Flags);

/// <summary>The draft curriculum the import writes (T-146): every lesson needs review before it can be published (T-147).</summary>
public sealed record DraftCurriculum(IReadOnlyList<Chapter> Chapters, IReadOnlyList<DraftLesson> Lessons);

/// <summary>Builds JSON schemas for structured output; every object is closed and every property required.</summary>
internal static class JsonSchema
{
    public static Dictionary<string, object> Object(params (string Name, object Type)[] properties) => new()
    {
        ["type"] = "object",
        ["additionalProperties"] = false,
        ["required"] = properties.Select(p => p.Name).ToArray(),
        ["properties"] = properties.ToDictionary(p => p.Name, p => p.Type),
    };

    public static Dictionary<string, object> Array(object items) => new() { ["type"] = "array", ["items"] = items };

    public static Dictionary<string, object> String() => new() { ["type"] = "string" };

    public static Dictionary<string, object> Integer() => new() { ["type"] = "integer" };

    public static Dictionary<string, object> Boolean() => new() { ["type"] = "boolean" };

    public static Dictionary<string, object> Enum(IEnumerable<string> values) => new() { ["type"] = "string", ["enum"] = values.ToArray() };

    public static JsonElement ToElement(object schema) => JsonSerializer.SerializeToElement(schema);

    /// <summary>Trimmed and cut to <paramref name="max"/> characters; null when blank.</summary>
    public static string? Clean(string? text, int max)
    {
        var trimmed = text?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed.Length > max ? trimmed[..max].TrimEnd() : trimmed;
    }
}
