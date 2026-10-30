using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SlateAi.Api.Ai;
using SlateAi.Api.Features.Lessons;
using SlateAi.Api.Features.Lessons.Voice;
using SlateAi.Api.Tests.Infrastructure;

namespace SlateAi.Api.Tests.Evals;

/// <summary>
/// Live A1 evaluation (docs/quality.md §2): real Claude, the shipped prompt and curriculum, names scrubbed as in
/// production. Costs credits; run with <c>dotnet test --filter Category=Eval</c>. Needs <c>Anthropic:ApiKey</c> in
/// the API's user-secrets. Writes <c>evals/results/&lt;date&gt;-a1-lesson-mapping.json</c> (no transcripts).
/// </summary>
[Trait("Category", "Eval")]
public sealed class A1LessonMappingEvals
{
    private const double Target = 0.8;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    [Fact]
    public async Task Top_1_lesson_matches_on_at_least_80_percent_of_cases()
    {
        var root = RepoRoot();
        var file = JsonSerializer.Deserialize<CaseFile>(
            await File.ReadAllTextAsync(Path.Combine(root, "evals", "cases", "a1-lesson-mapping.json"), TestContext.Current.CancellationToken),
            Json)!;
        var key = new ConfigurationBuilder().AddUserSecrets<Program>().AddEnvironmentVariables().Build()["Anthropic:ApiKey"];
        Assert.False(string.IsNullOrWhiteSpace(key), "Set Anthropic:ApiKey in the API's user-secrets to run evals.");

        await using var factory = FakeApi.Create(
            services => services.AddSingleton<IAiCallStore>(new FakeAiCallStore()),
            settings: new Dictionary<string, string?> { ["Ai:Provider"] = "Claude", ["Anthropic:ApiKey"] = key });
        await using var scope = factory.Services.CreateAsyncScope();
        var mapper = scope.ServiceProvider.GetRequiredService<LessonMapper>();
        var prompt = scope.ServiceProvider.GetRequiredService<PromptLibrary>().Get(LessonMapper.PromptId);
        var model = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<AiOptions>>().Value.ModelFor(prompt.ModelKey);
        var names = new ClassNames(["Ravi", "Ananya", "Bhavana", "Chetan"], "Shobha");

        var results = new List<CaseResult>();
        foreach (var c in file.Cases)
        {
            var photo = c.Photo is null
                ? null
                : new LlmImage(
                    await File.ReadAllBytesAsync(Path.Combine(root, "evals", "cases", c.Photo), TestContext.Current.CancellationToken),
                    "image/png");
            var result = await mapper.MapTranscriptAsync(c.Transcript, names, TestContext.Current.CancellationToken, photo);
            var first = result.CompetencyIds.FirstOrDefault();
            var pass = c.ExpectLow
                ? result.Status is LessonValues.Low or LessonValues.Unclear
                : first is not null && (first == c.Expected || (c.AlsoAcceptable ?? []).Contains(first));
            // The scrubber turns a copied name into {child}, so a placeholder here means Claude copied a name off the photo.
            var wording = string.Join(" ", [result.SummaryKn, result.SummaryEn, .. result.ExamplesUsed]);
            pass &= !c.NoNames || !(wording.Contains("{child}") || names.Children.Any(n => wording.Contains(n)));
            results.Add(new CaseResult(c.Id, pass, result.Status, result.CompetencyIds, c.Expected, result.Verdict));
        }

        var score = results.Count(r => r.Pass) / (double)results.Count;
        var report = new Report(DateTimeOffset.UtcNow, prompt.Id, prompt.Version, model, results.Count, score, Target, results);
        var output = Path.Combine(root, "evals", "results", $"{DateTime.UtcNow:yyyy-MM-dd}-a1-lesson-mapping.json");
        await File.WriteAllTextAsync(output, JsonSerializer.Serialize(report, Json) + "\n", TestContext.Current.CancellationToken);

        Assert.True(score >= Target,
            $"A1 top-1 {score:P0} < {Target:P0}: " + string.Join(", ", results.Where(r => !r.Pass).Select(r => r.Id)));
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        // evals/cases, not just "evals": macOS paths ignore case and this test lives in Evals/.
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "evals", "cases")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new DirectoryNotFoundException("evals/ not found above the test output.");
    }

    private sealed record CaseFile(IReadOnlyList<EvalCase> Cases);

    private sealed record EvalCase(
        string Id,
        string Transcript,
        string? Expected,
        IReadOnlyList<string>? AlsoAcceptable,
        bool ExpectLow,
        string? Photo = null,
        bool NoNames = false);

    private sealed record CaseResult(
        string Id, bool Pass, string Status, IReadOnlyList<string> CompetencyIds, string? Expected, string Verdict);

    private sealed record Report(
        DateTimeOffset RunAt, string Prompt, int PromptVersion, string Model, int Cases, double Score, double Target,
        IReadOnlyList<CaseResult> Results);
}
