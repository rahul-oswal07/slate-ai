using System.Text.Json;

namespace SlateAi.Api.Features.TextbookImport;

/// <summary>
/// Stage 3 of the import (T-146): A10 picks each lesson's prerequisites from the book's lesson IDs. Code keeps only
/// lessons that come earlier in the book, so the graph can never have a cycle. Pure.
/// </summary>
public static class TextbookPrerequisites
{
    public const string PromptId = "a10-textbook-prerequisites";

    public const int MaxPerLesson = 4;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>IDs are an enum of the book's lessons: Claude can't name one that isn't there.</summary>
    public static JsonElement Schema(IReadOnlyList<string> ids)
    {
        var id = JsonSchema.Enum(ids);
        return JsonSchema.ToElement(JsonSchema.Object(
            ("lessons", JsonSchema.Array(JsonSchema.Object(("id", id), ("prerequisites", JsonSchema.Array(id)))))));
    }

    /// <summary>
    /// Lesson ID → its prerequisites, earlier lessons only, at most <see cref="MaxPerLesson"/>; null when the output can't
    /// be used (the draft then has none, flagged).
    /// </summary>
    /// <param name="sequence">Lesson ID → its place in the book.</param>
    public static IReadOnlyDictionary<string, IReadOnlyList<string>>? Validate(
        string json, IReadOnlyDictionary<string, int> sequence)
    {
        Output? output;
        try
        {
            output = JsonSerializer.Deserialize<Output>(json, Json);
        }
        catch (JsonException)
        {
            return null;
        }

        if (output?.Lessons is null)
        {
            return null;
        }

        var result = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        foreach (var lesson in output.Lessons)
        {
            if (lesson.Id is not { } id || !sequence.TryGetValue(id, out var place) || result.ContainsKey(id))
            {
                continue;
            }

            result[id] = (lesson.Prerequisites ?? [])
                .OfType<string>()
                .Where(p => sequence.TryGetValue(p, out var before) && before < place)
                .Distinct()
                .Take(MaxPerLesson)
                .ToList();
        }

        return result;
    }

    /// <summary>The lessons in book order, by ID, with what they teach: no pages, no names of people.</summary>
    public static string UserContent(ImportBook book, IReadOnlyList<DraftLesson> lessons) =>
        JsonSerializer.Serialize(new
        {
            grade = book.Grade,
            subject = book.Subject,
            lessons = lessons.Select(l => new
            {
                l.Competency.Id,
                l.Competency.NameEn,
                l.Competency.Chapter,
                l.Competency.Sequence,
                description = l.Extras.Description,
                l.Competency.Examples,
            }),
        }, Json);

    private sealed record Output(List<LessonOutput>? Lessons);

    private sealed record LessonOutput(string? Id, List<string?>? Prerequisites);
}
