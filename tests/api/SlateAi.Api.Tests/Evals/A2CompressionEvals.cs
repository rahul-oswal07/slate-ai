using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SlateAi.Api.Ai;
using SlateAi.Api.Curriculum;
using SlateAi.Api.Features.CatchUps.Compression;
using SlateAi.Api.Tests.Infrastructure;

namespace SlateAi.Api.Tests.Evals;

/// <summary>
/// Live A2 evaluation (docs/quality.md §2): real Claude, the shipped prompt and curriculum, inputs built by the
/// same planner code as production. Costs credits; run with <c>dotnet test --filter Category=Eval</c>. Writes
/// <c>evals/results/&lt;date&gt;-a2-compression.json</c>, including Claude's reasons for review.
/// </summary>
[Trait("Category", "Eval")]
public sealed class A2CompressionEvals
{
    private const double Target = 0.8;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    [Fact]
    public async Task Claude_picks_the_best_target_from_the_candidates_on_at_least_80_percent_of_cases()
    {
        var root = RepoRoot();
        var file = JsonSerializer.Deserialize<CaseFile>(
            await File.ReadAllTextAsync(Path.Combine(root, "evals", "cases", "a2-compression.json"), TestContext.Current.CancellationToken),
            Json)!;
        var key = new ConfigurationBuilder().AddUserSecrets<Program>().AddEnvironmentVariables().Build()["Anthropic:ApiKey"];
        Assert.False(string.IsNullOrWhiteSpace(key), "Set Anthropic:ApiKey in the API's user-secrets to run evals.");

        await using var factory = FakeApi.Create(
            services => services.AddSingleton<IAiCallStore>(new FakeAiCallStore()),
            settings: new Dictionary<string, string?> { ["Ai:Provider"] = "Claude", ["Anthropic:ApiKey"] = key });
        await using var scope = factory.Services.CreateAsyncScope();
        var compressor = scope.ServiceProvider.GetRequiredService<Compressor>();
        var curriculum = scope.ServiceProvider.GetRequiredService<CurriculumGraph>();
        var prompt = scope.ServiceProvider.GetRequiredService<PromptLibrary>().Get(Compressor.PromptId);
        var model = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<AiOptions>>().Value.ModelFor(prompt.ModelKey);

        var results = new List<CaseResult>();
        var monday = new DateOnly(2026, 10, 19);
        foreach (var c in file.Cases)
        {
            var nextWeek = CompressionPlanner.NextWeek(curriculum, c.LastTaught);
            var input = new CompressionInput(
                Guid.NewGuid(),
                c.Missed.Select((id, day) => new MissedCompetency(id, [monday.AddDays(day)])).ToList(),
                nextWeek,
                curriculum.CandidateSet(c.Missed, nextWeek));
            var result = await compressor.CompressAsync(input, TestContext.Current.CancellationToken);
            var targets = result.Targets.Select(t => t.Id).ToList();
            var accepted = new[] { c.Best }.Concat(c.AlsoAcceptable ?? []).ToHashSet();
            var pass = result.Source == CompressionSource.Ai
                && targets.All(input.Candidates.Contains)
                && targets.Any(accepted.Contains);
            results.Add(new CaseResult(c.Id, pass, input.Candidates, targets, c.Best, result.Verdict, result.Targets, result.Dropped));
        }

        var score = results.Count(r => r.Pass) / (double)results.Count;
        var report = new Report(DateTimeOffset.UtcNow, prompt.Id, prompt.Version, model, results.Count, score, Target, results);
        var output = Path.Combine(root, "evals", "results", $"{DateTime.UtcNow:yyyy-MM-dd}-a2-compression.json");
        await File.WriteAllTextAsync(output, JsonSerializer.Serialize(report, Json) + "\n", TestContext.Current.CancellationToken);

        Assert.True(score >= Target,
            $"A2 {score:P0} < {Target:P0}: " + string.Join(", ", results.Where(r => !r.Pass).Select(r => r.Id)));
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "evals", "cases")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new DirectoryNotFoundException("evals/ not found above the test output.");
    }

    private sealed record CaseFile(IReadOnlyList<EvalCase> Cases);

    private sealed record EvalCase(string Id, IReadOnlyList<string> Missed, IReadOnlyList<string> LastTaught, string Best, IReadOnlyList<string>? AlsoAcceptable);

    private sealed record CaseResult(
        string Id, bool Pass, IReadOnlyList<string> Candidates, IReadOnlyList<string> Targets, string Best, string Verdict,
        IReadOnlyList<CompressionItem> TargetReasons, IReadOnlyList<CompressionItem> Dropped);

    private sealed record Report(
        DateTimeOffset RunAt, string Prompt, int PromptVersion, string Model, int Cases, double Score, double Target,
        IReadOnlyList<CaseResult> Results);
}
