using System.Text.Json;
using Microsoft.Extensions.Options;
using SlateAi.Api.Curriculum;
using SlateAi.Api.Data;
using SlateAi.Api.Data.Entities;

namespace SlateAi.Api.Features.LessonContents;

/// <summary>
/// <c>data/lesson-content/grade3-maths.json</c> (T-141, D-123): one description and 1–4 worked examples per curriculum
/// lesson, written from the textbook in our own words (the book is not to be republished; its numbers and methods are
/// facts). <c>LessonContentFileTests</c> checks it covers the curriculum and every sum in it is right.
/// </summary>
public sealed record LessonContentFile(string Version, IReadOnlyList<LessonContentFile.Lesson> Lessons)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <param name="Pages">The book's printed pages it was read from; documentation only.</param>
    public sealed record Lesson(string Id, string Description, IReadOnlyList<string> Examples, string Source, IReadOnlyList<int>? Pages);

    public static LessonContentFile Load(string path) =>
        JsonSerializer.Deserialize<LessonContentFile>(File.ReadAllText(path), Json)
        ?? throw new InvalidOperationException($"Lesson content file '{path}' is empty.");

    /// <summary>The lessons that pass the rules and are in the curriculum, with the problems of those that don't.</summary>
    public (IReadOnlyList<LessonContentEntry> Entries, IReadOnlyList<string> Problems) Check(CurriculumGraph curriculum)
    {
        var entries = new List<LessonContentEntry>();
        var problems = new List<string>();
        foreach (var lesson in Lessons)
        {
            var (content, errors) = LessonContentRules.Normalise(lesson.Description, lesson.Examples.ToList<string?>());
            if (curriculum.Get(lesson.Id) is null)
            {
                problems.Add($"{lesson.Id}: not in the curriculum");
            }
            else if (lesson.Source is not (LessonContentSources.Textbook or LessonContentSources.Written))
            {
                problems.Add($"{lesson.Id}: source must be textbook or written");
            }
            else if (content is null)
            {
                problems.Add($"{lesson.Id}: {string.Join(" ", errors.Select(e => $"{e.Key}: {string.Join(" ", e.Value)}"))}".TrimEnd());
            }
            else
            {
                entries.Add(new LessonContentEntry(lesson.Id, content, lesson.Source));
            }
        }

        return (entries, problems);
    }
}

/// <summary>Puts the content file into <c>LessonContents</c> at startup, never over an edit made on the admin page.</summary>
public sealed class LessonContentLoader(
    ILessonContentStore store,
    CurriculumGraph curriculum,
    IOptions<DatabaseOptions> options,
    TimeProvider clock,
    ILogger<LessonContentLoader> logger)
{
    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        var path = Path.Combine(AppContext.BaseDirectory, options.Value.LessonContentFile);
        if (!File.Exists(path))
        {
            logger.LogWarning("No lesson content file at {Path}; today's message uses the curriculum examples only", path);
            return;
        }

        var (entries, problems) = LessonContentFile.Load(path).Check(curriculum);
        foreach (var problem in problems)
        {
            logger.LogWarning("Lesson content skipped: {Problem}", problem);
        }

        var result = await store.SyncAsync(entries, clock.GetUtcNow(), cancellationToken);
        logger.LogInformation(
            "Lesson content loaded: {Added} added, {Updated} updated, {Kept} kept as edited",
            result.Added, result.Updated, result.KeptEdited);
    }
}
