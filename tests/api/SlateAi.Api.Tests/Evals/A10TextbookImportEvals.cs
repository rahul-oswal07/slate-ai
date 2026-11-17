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
/// <item>Lesson recall: each hand-built lesson's printed page falls within an imported lesson's pages (±1).</item>
/// <item>Exercise recall: the hand-typed add/sub/compare/extremes items found in the matching imported lesson.</item>
/// </list>
/// The book is not in the repo (it is marked "not to be republished"): set <c>Evals:TextbookPdf</c> (path) and
/// <c>Evals:TextbookPart</c> (1 or 2) in the API's user-secrets, with <c>Anthropic:ApiKey</c>. Costs credits (an estimate: about USD 5
/// for a 120-page part on Opus 5.5); run with <c>dotnet test --filter Category=Eval</c>. Writes
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
        await using var factory = FakeApi.Create(
            services => services.AddSingleton<IAiCallStore>(new FakeAiCallStore()).AddSingleton<ITextbookImportStore>(store),
            settings: new Dictionary<string, string?> { ["Ai:Provider"] = "Claude", ["Anthropic:ApiKey"] = key });
        await using var scope = factory.Services.CreateAsyncScope();
        var gold = scope.ServiceProvider.GetRequiredService<BuiltInCurriculum>().Graph.All
            .Where(c => c.TextbookPage is { Book: "textbook" } page && page.Part == part)
            .ToList();
        var prompts = scope.ServiceProvider.GetRequiredService<PromptLibrary>();
        var model = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<AiOptions>>().Value
            .ModelFor(prompts.Get(TextbookLessons.PromptId).ModelKey);

        var pages = PdfPages.Split(await File.ReadAllBytesAsync(path!, ct), TextbookImportEndpoints.MaxPages);
        var import = store.Add(new ImportBook($"KTBS Grade 3 maths Part {part}", 3, "maths", "kn", "textbook", part), pages, DateTimeOffset.UtcNow);
        var started = DateTimeOffset.UtcNow;
        await scope.ServiceProvider.GetRequiredService<TextbookImporter>().RunAsync(import.Id, ct);
        var finished = store.Imports[import.Id];
        Assert.True(finished.Status == CurriculumImportStatuses.Ready, $"The import failed: {finished.Error}");
        var draft = store.Drafts[import.Id].Draft;

        var results = gold.Select(g =>
        {
            var page = g.TextbookPage!.Page;
            var match = draft.Lessons.FirstOrDefault(l =>
                l.Extras.PrintedPages.Count > 0 && page >= l.Extras.PrintedPages.Min() - 1 && page <= l.Extras.PrintedPages.Max() + 1);
            var wanted = Items(g.Exercises);
            var found = Items(match?.Competency.Exercises);
            return new LessonResult(g.Id, page, match?.Competency.Id, match is not null, wanted.Count, wanted.Count(found.Contains));
        }).ToList();

        var recall = results.Count(r => r.Found) / (double)Math.Max(1, results.Count);
        var wantedItems = results.Sum(r => r.Exercises);
        var report = new Report(
            DateTimeOffset.UtcNow,
            [TextbookOutline.PromptId, TextbookLessons.PromptId, TextbookPrerequisites.PromptId],
            prompts.Get(TextbookLessons.PromptId).Version,
            model,
            part,
            pages.Count,
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
            Target,
            results);
        var output = Path.Combine(RepoRoot(), "evals", "results", $"{DateTime.UtcNow:yyyy-MM-dd}-a10-textbook-import.json");
        await File.WriteAllTextAsync(output, JsonSerializer.Serialize(report, Json) + "\n", ct);

        Assert.True(recall >= Target,
            $"A10 lesson recall {recall:P0} < {Target:P0}; missed: " + string.Join(", ", results.Where(r => !r.Found).Select(r => r.Id)));
    }

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

    private sealed record LessonResult(string Id, int Page, string? MatchedId, bool Found, int Exercises, int ExercisesFound);

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
        double Target,
        IReadOnlyList<LessonResult> Results);
}
