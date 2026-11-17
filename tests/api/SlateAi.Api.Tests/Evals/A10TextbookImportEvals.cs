using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SlateAi.Api.Ai;
using SlateAi.Api.Curriculum;
using SlateAi.Api.Data.Entities;
using SlateAi.Api.Features.TextbookImport;
using SlateAi.Api.Tests.Features.TextbookImport;
using SlateAi.Api.Tests.Infrastructure;

namespace SlateAi.Api.Tests.Evals;

/// <summary>
/// Live A10 evaluation (T-146, guardrail 6): the real importer and real Claude read one part of the KTBS Grade 3 maths
/// textbook, and the draft is compared with the curriculum we built by hand from the same book (T-81, T-128).
/// <list type="bullet">
/// <item>Lesson recall: each hand-built lesson is matched to the imported lesson that starts closest at or before its
/// printed page (<see cref="Match"/>); it is found when that lesson's pages reach it.</item>
/// <item>Exercise recall: the hand-typed add/sub/compare/extremes items found in the matched lesson, and anywhere in the
/// draft (the hand-built ones were partly copied from the book's examples, T-128, so not all are exercises).</item>
/// </list>
/// The results also list every imported lesson (ID, name, pages, counts) so the matching can be checked by hand, and the
/// tokens used.
/// The book is not in the repo (it is marked "not to be republished"): set <c>Evals:TextbookPdf</c> (path) and
/// <c>Evals:TextbookPart</c> (1 or 2) in the API's user-secrets, with <c>Anthropic:ApiKey</c>. Costs credits: the results file records the
/// tokens (Part 1, 160 pages, took about 18 minutes); run with <c>dotnet test --filter Category=Eval</c>. Writes
/// <c>evals/results/&lt;date&gt;-a10-textbook-import.json</c>: IDs, pages and counts only, no book text.
/// </summary>
[Trait("Category", "Eval")]
public sealed class A10TextbookImportEvals
{
    private const double Target = 0.8;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    [Fact]
    public async Task The_import_finds_at_least_80_percent_of_the_hand_built_lessons()
    {
        var ct = TestContext.Current.CancellationToken;
        var config = new ConfigurationBuilder().AddUserSecrets<Program>().AddEnvironmentVariables().Build();
        var key = config["Anthropic:ApiKey"];
        var path = config["Evals:TextbookPdf"];
        Assert.False(string.IsNullOrWhiteSpace(key), "Set Anthropic:ApiKey in the API's user-secrets to run evals.");
        Assert.True(File.Exists(path), "Set Evals:TextbookPdf to the KTBS Grade 3 maths PDF (Part 1 or 2) in the API's user-secrets.");
        var part = int.TryParse(config["Evals:TextbookPart"], out var p) ? p : 1;

        var store = new FakeTextbookImportStore();
        var calls = new FakeAiCallStore();
        await using var factory = FakeApi.Create(
            services => services.AddSingleton<IAiCallStore>(calls).AddSingleton<ITextbookImportStore>(store),
            settings: new Dictionary<string, string?> { ["Ai:Provider"] = "Claude", ["Anthropic:ApiKey"] = key });
        await using var scope = factory.Services.CreateAsyncScope();
        var gold = scope.ServiceProvider.GetRequiredService<BuiltInCurriculum>().Graph.All
            .Where(c => c.TextbookPage is { Book: "textbook" } page && page.Part == part)
            .ToList();
        var prompts = scope.ServiceProvider.GetRequiredService<PromptLibrary>();
        var model = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<AiOptions>>().Value
            .ModelFor(prompts.Get(TextbookLessons.PromptId).ModelKey);

        var pdf = await File.ReadAllBytesAsync(path!, ct);
        var import = store.Add(new ImportBook($"KTBS Grade 3 maths Part {part}", 3, "maths", "kn", "textbook", part), pdf, DateTimeOffset.UtcNow);
        var started = DateTimeOffset.UtcNow;
        await scope.ServiceProvider.GetRequiredService<TextbookImporter>().RunAsync(import.Id, ct);
        var finished = store.Imports[import.Id];
        Assert.True(finished.Status == CurriculumImportStatuses.Ready, $"The import failed: {finished.Error}");
        var draft = store.Drafts[import.Id].Draft;

        var spans = draft.Lessons
            .Where(l => l.Extras.PrintedPages.Count > 0)
            .Select(l => new Span(l.Competency.Id, l.Extras.PrintedPages.Min(), l.Extras.PrintedPages.Max()))
            .ToList();
        var everywhere = draft.Lessons.SelectMany(l => Items(l.Competency.Exercises)).ToHashSet();
        var results = gold.Select(g =>
        {
            var page = g.TextbookPage!.Page;
            var match = Match(spans, page);
            var lesson = draft.Lessons.FirstOrDefault(l => l.Competency.Id == match);
            var wanted = Items(g.Exercises);
            var found = Items(lesson?.Competency.Exercises);
            return new LessonResult(
                g.Id, g.NameEn, page, match, lesson?.Competency.NameEn, match is not null,
                wanted.Count, wanted.Count(found.Contains), wanted.Count(everywhere.Contains));
        }).ToList();

        var recall = results.Count(r => r.Found) / (double)Math.Max(1, results.Count);
        var wantedItems = results.Sum(r => r.Exercises);
        var tokens = new Tokens(
            calls.Calls.Count, calls.Calls.Sum(c => (long)c.InputTokens), calls.Calls.Sum(c => (long)c.OutputTokens),
            calls.Calls.Sum(c => (long)c.CacheReadTokens),
            calls.Calls.GroupBy(c => c.Verdict ?? c.Error ?? "none").ToDictionary(g => g.Key, g => g.Count()));
        var report = new Report(
            DateTimeOffset.UtcNow,
            [TextbookOutline.PromptId, TextbookLessons.PromptId, TextbookPrerequisites.PromptId],
            prompts.Get(TextbookLessons.PromptId).Version,
            model,
            part,
            finished.PageCount,
            (int)(DateTimeOffset.UtcNow - started).TotalSeconds,
            draft.Chapters.Count,
            gold.Select(g => g.Chapter).Distinct().Count(),
            draft.Lessons.Count,
            draft.Lessons.Count(l => l.Flags.Count > 0),
            draft.Lessons.Sum(l => Items(l.Competency.Exercises).Count),
            draft.Lessons.Sum(l => l.Extras.OtherExercises.Count),
            draft.Lessons.Sum(l => l.Extras.Pictures.Count),
            draft.Lessons.Count(l => l.Competency.Prerequisites.Count > 0),
            finished.Notes,
            recall,
            wantedItems == 0 ? null : results.Sum(r => r.ExercisesFound) / (double)wantedItems,
            wantedItems == 0 ? null : results.Sum(r => r.ExercisesInDraft) / (double)wantedItems,
            Target,
            tokens,
            results,
            draft.Lessons.Select(l => new ImportedLesson(
                l.Competency.Id, l.Competency.NameEn, l.Competency.Chapter,
                l.Extras.PrintedPages.Count > 0 ? l.Extras.PrintedPages.Min() : null,
                l.Extras.PrintedPages.Count > 0 ? l.Extras.PrintedPages.Max() : null,
                Items(l.Competency.Exercises).Count, l.Extras.OtherExercises.Count, l.Extras.Pictures.Count,
                l.Competency.Prerequisites, l.Flags.Count)).ToList());
        var output = Path.Combine(RepoRoot(), "evals", "results", $"{DateTime.UtcNow:yyyy-MM-dd}-a10-textbook-import.json");
        await File.WriteAllTextAsync(output, JsonSerializer.Serialize(report, Json) + "\n", ct);

        Assert.True(recall >= Target,
            $"A10 lesson recall {recall:P0} < {Target:P0}; missed: " + string.Join(", ", results.Where(r => !r.Found).Select(r => r.Id)));
    }

    /// <summary>A lesson's printed pages, first to last.</summary>
    internal sealed record Span(string Id, int First, int Last);

    /// <summary>
    /// The imported lesson a hand-built lesson printed on <paramref name="page"/> is: the one that starts closest at or
    /// before it and still reaches it (±1, for a page number read one off), else one that starts on the next page. A lesson
    /// usually starts on the page where the previous one ends, so "any lesson whose pages include it" picks the previous
    /// one. Null when none.
    /// </summary>
    internal static string? Match(IReadOnlyList<Span> spans, int page) =>
        spans.Where(s => s.First <= page && s.Last >= page - 1).OrderBy(s => s.First).LastOrDefault()?.Id
        ?? spans.FirstOrDefault(s => s.First == page + 1)?.Id;

    private static List<string> Items(ExerciseList? exercises) =>
    [
        .. (exercises?.Compare ?? []).Select(i => "compare " + string.Join(",", i)),
        .. (exercises?.Extremes ?? []).Select(i => "extremes " + string.Join(",", i.Order())),
        .. (exercises?.Add ?? []).Select(i => "add " + string.Join(",", i)),
        .. (exercises?.Sub ?? []).Select(i => "sub " + string.Join(",", i)),
    ];

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "evals", "cases")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new DirectoryNotFoundException("evals/ not found above the test output.");
    }

    private sealed record LessonResult(
        string Id, string Name, int Page, string? MatchedId, string? MatchedName, bool Found, int Exercises, int ExercisesFound,
        int ExercisesInDraft);

    private sealed record ImportedLesson(
        string Id, string Name, int Chapter, int? FirstPage, int? LastPage, int TypedExercises, int OtherExercises, int Pictures,
        IReadOnlyList<string> Prerequisites, int Flags);

    /// <summary>What the run used, from the AI call log: calls, tokens, and how many of each verdict.</summary>
    private sealed record Tokens(int Calls, long Input, long Output, long CacheRead, IReadOnlyDictionary<string, int> Verdicts);

    private sealed record Report(
        DateTimeOffset RunAt,
        IReadOnlyList<string> Prompts,
        int PromptVersion,
        string Model,
        int Part,
        int Pages,
        int Seconds,
        int Chapters,
        int ExpectedChapters,
        int Lessons,
        int FlaggedLessons,
        int TypedExercises,
        int OtherExercises,
        int Pictures,
        int LessonsWithPrerequisites,
        IReadOnlyList<string> Notes,
        double LessonRecall,
        double? ExerciseRecall,
        double? ExerciseRecallInDraft,
        double Target,
        Tokens Tokens,
        IReadOnlyList<LessonResult> Results,
        IReadOnlyList<ImportedLesson> Imported);
}

/// <summary>The eval's matching, checked without Claude: it runs with the normal tests.</summary>
public sealed class A10MatchTests
{
    private static readonly A10TextbookImportEvals.Span[] Book =
    [
        new("M3.GEO.2D", 3, 5),
        new("M3.GEO.3D", 5, 9), // starts on the page where 2D ends
        new("M3.GEO.TILES", 9, 12),
        new("M3.ADD.NOCARRY", 78, 80),
    ];

    [Theory]
    [InlineData(3, "M3.GEO.2D")]
    [InlineData(5, "M3.GEO.3D")] // not 2D, though 2D's pages include 5
    [InlineData(9, "M3.GEO.TILES")]
    [InlineData(77, "M3.ADD.NOCARRY")] // printed page read one off
    [InlineData(13, "M3.GEO.TILES")] // one past the end
    [InlineData(40, null)]
    public void A_hand_built_lesson_matches_the_lesson_that_starts_closest_before_its_page(int page, string? expected) =>
        Assert.Equal(expected, A10TextbookImportEvals.Match(Book, page));
}
