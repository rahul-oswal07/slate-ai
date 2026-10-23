using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SlateAi.Api.Ai;
using SlateAi.Api.Curriculum;
using SlateAi.Api.Features.CatchUps;
using SlateAi.Api.Features.CatchUps.Practice;
using SlateAi.Api.Features.CatchUps.Script;
using SlateAi.Api.Features.CatchUps.Verify;
using SlateAi.Api.Tests.Infrastructure;

namespace SlateAi.Api.Tests.Evals;

/// <summary>
/// Live A3 + A4 evaluation (docs/quality.md §2): real Claude (Opus writes, Haiku checks), the shipped prompts and
/// curriculum, numbers made by production code, production's verifier and its one retry. Reports the first-pass
/// rate (intent.md target ≥ 85%) and the ready rate after the retry. Costs credits; run with
/// <c>dotnet test --filter Category=Eval</c>. Writes <c>evals/results/&lt;date&gt;-a3-script.json</c> with every script.
/// </summary>
[Trait("Category", "Eval")]
public sealed class A3ScriptEvals
{
    private const double Target = 0.8;
    private const double FirstPassTarget = 0.85;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    [Fact]
    public async Task Catch_ups_are_ready_after_at_most_one_retry_on_at_least_80_percent_of_cases()
    {
        var root = RepoRoot();
        var file = JsonSerializer.Deserialize<CaseFile>(
            await File.ReadAllTextAsync(Path.Combine(root, "evals", "cases", "a3-script.json"), TestContext.Current.CancellationToken),
            Json)!;
        var key = new ConfigurationBuilder().AddUserSecrets<Program>().AddEnvironmentVariables().Build()["Anthropic:ApiKey"];
        Assert.False(string.IsNullOrWhiteSpace(key), "Set Anthropic:ApiKey in the API's user-secrets to run evals.");

        await using var factory = FakeApi.Create(
            services => services.AddSingleton<IAiCallStore>(new FakeAiCallStore()),
            settings: new Dictionary<string, string?> { ["Ai:Provider"] = "Claude", ["Anthropic:ApiKey"] = key });
        var services = factory.Services;
        var curriculum = services.GetRequiredService<CurriculumGraph>();
        var prompts = services.GetRequiredService<PromptLibrary>();
        var aiOptions = services.GetRequiredService<Microsoft.Extensions.Options.IOptions<AiOptions>>().Value;
        string[] names = ["Ravi", "Ananya", "Bhavana", "Chetan", "Shobha"];

        var results = await Task.WhenAll(file.Cases.Select(async c =>
        {
            await using var scope = services.CreateAsyncScope();
            var writer = scope.ServiceProvider.GetRequiredService<ScriptWriter>();
            var verifier = scope.ServiceProvider.GetRequiredService<Verifier>();
            var problems = PackagePractice.For(c.Targets, curriculum, c.Id)!;
            var activity = new ActivityData(problems.CompetencyId, problems.Activity.Kind, problems.Activity.Question, problems.Activity.Numbers, problems.Activity.Answer, null, null);
            var check = new CheckData(problems.CompetencyId, problems.Check.Kind, problems.Check.Question, problems.Check.Options, problems.Check.CorrectIndex, problems.Check.Answer, null);

            var attempts = new List<Attempt>();
            IReadOnlyList<string>? feedback = null;
            for (var i = 0; i < CatchUpWriter.Attempts; i++)
            {
                var written = await writer.WriteAsync(
                    new ScriptInput(c.Language, c.Targets, problems, c.TeacherExamples, Feedback: feedback), names, TestContext.Current.CancellationToken);
                if (written.Content is not { } content)
                {
                    attempts.Add(new Attempt(false, written.Verdict, null, []));
                    feedback = [$"output: {written.Verdict}"];
                    continue;
                }

                var review = await verifier.VerifyAsync(
                    new VerifyInput(c.Language, c.Targets, content, activity, check, names), TestContext.Current.CancellationToken);
                attempts.Add(new Attempt(review.Passed, review.Verdict, content, review.Checks));
                if (review.Passed)
                {
                    break;
                }

                feedback = review.Problems;
            }

            return new CaseResult(c.Id, attempts[0].Passed, attempts[^1].Passed, problems.Check.Question, attempts);
        }));

        var firstPass = results.Count(r => r.FirstPass) / (double)results.Length;
        var ready = results.Count(r => r.Ready) / (double)results.Length;
        var script = prompts.Get(ScriptWriter.PromptId);
        var check = prompts.Get(ContentCheck.PromptId);
        var report = new Report(
            DateTimeOffset.UtcNow, $"{script.Id} v{script.Version} + {check.Id} v{check.Version}",
            $"{aiOptions.ModelFor(script.ModelKey)} + {aiOptions.ModelFor(check.ModelKey)}",
            results.Length, firstPass, FirstPassTarget, ready, Target, results);
        var output = Path.Combine(root, "evals", "results", $"{DateTime.UtcNow:yyyy-MM-dd}-a3-script.json");
        await File.WriteAllTextAsync(output, JsonSerializer.Serialize(report, Json) + "\n", TestContext.Current.CancellationToken);

        Assert.True(ready >= Target,
            $"A3+A4 ready {ready:P0} < {Target:P0}: " + string.Join("; ", results.Where(r => !r.Ready).Select(r => $"{r.Id} ({r.Attempts[^1].Verdict})")));
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

    private sealed record EvalCase(string Id, string Language, IReadOnlyList<string> Targets, IReadOnlyList<string> TeacherExamples);

    private sealed record Attempt(bool Passed, string Verdict, ScriptContent? Content, IReadOnlyList<VerifierCheck> Checks);

    private sealed record CaseResult(string Id, bool FirstPass, bool Ready, string CheckQuestion, IReadOnlyList<Attempt> Attempts);

    private sealed record Report(
        DateTimeOffset RunAt, string Prompts, string Models, int Cases, double FirstPass, double FirstPassTarget,
        double Ready, double Target, IReadOnlyList<CaseResult> Results);
}
