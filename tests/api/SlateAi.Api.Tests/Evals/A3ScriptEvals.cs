using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SlateAi.Api.Ai;
using SlateAi.Api.Curriculum;
using SlateAi.Api.Features.CatchUps.Practice;
using SlateAi.Api.Features.CatchUps.Script;
using SlateAi.Api.Tests.Infrastructure;

namespace SlateAi.Api.Tests.Evals;

/// <summary>
/// Live A3 evaluation (docs/quality.md §2): real Claude (Opus), the shipped prompt and curriculum, numbers made by the
/// production code. Code checks only; the Haiku rubric comes with the verifier (T-10). Costs credits; run with
/// <c>dotnet test --filter Category=Eval</c>. Writes <c>evals/results/&lt;date&gt;-a3-script.json</c> with every script.
/// </summary>
[Trait("Category", "Eval")]
public sealed class A3ScriptEvals
{
    private const double Target = 0.8;
    private const int MaxWords = 120;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private static readonly Dictionary<string, (int From, int To)> Blocks = new()
    {
        ["kn"] = (0x0C80, 0x0CFF),
        ["te"] = (0x0C00, 0x0C7F),
        ["ta"] = (0x0B80, 0x0BFF),
    };

    [Fact]
    public async Task Scripts_pass_the_code_checks_on_at_least_80_percent_of_cases()
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
        await using var scope = factory.Services.CreateAsyncScope();
        var writer = scope.ServiceProvider.GetRequiredService<ScriptWriter>();
        var curriculum = scope.ServiceProvider.GetRequiredService<CurriculumGraph>();
        var prompt = scope.ServiceProvider.GetRequiredService<PromptLibrary>().Get(ScriptWriter.PromptId);
        var model = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<AiOptions>>().Value.ModelFor(prompt.ModelKey);
        string[] names = ["Ravi", "Ananya", "Bhavana", "Chetan", "Shobha"];

        var results = await Task.WhenAll(file.Cases.Select(async c =>
        {
            var problems = PackagePractice.For(c.Targets, curriculum, c.Id)!;
            var result = await writer.WriteAsync(
                new ScriptInput(c.Language, c.Targets, problems, c.TeacherExamples), names, TestContext.Current.CancellationToken);
            var reasons = new List<string>();
            if (result.Content is not { } content)
            {
                reasons.Add(result.Verdict);
                return new CaseResult(c.Id, false, reasons, result.Verdict, problems.Check.Question, null, 0, 0);
            }

            var missing = content.KeyWordsKn.Where(w => !content.Script.Contains(w, StringComparison.Ordinal)).ToList();
            if (missing.Count > 0)
            {
                reasons.Add("key word not in script: " + string.Join(", ", missing));
            }

            var share = ScriptShare(content.Script, c.Language);
            if (share < 0.8)
            {
                reasons.Add($"only {share:P0} in the family's script");
            }

            var words = content.Script.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
            if (words > MaxWords)
            {
                reasons.Add($"{words} words > {MaxWords}");
            }

            return new CaseResult(c.Id, reasons.Count == 0, reasons, result.Verdict, problems.Check.Question, content, words, share);
        }));

        var score = results.Count(r => r.Pass) / (double)results.Length;
        var report = new Report(DateTimeOffset.UtcNow, prompt.Id, prompt.Version, model, results.Length, score, Target, results);
        var output = Path.Combine(root, "evals", "results", $"{DateTime.UtcNow:yyyy-MM-dd}-a3-script.json");
        await File.WriteAllTextAsync(output, JsonSerializer.Serialize(report, Json) + "\n", TestContext.Current.CancellationToken);

        Assert.True(score >= Target,
            $"A3 {score:P0} < {Target:P0}: " + string.Join("; ", results.Where(r => !r.Pass).Select(r => $"{r.Id} ({string.Join(", ", r.Reasons)})")));
    }

    /// <summary>Share of letters in the family's script, not counting the Kannada key words in a Telugu/Tamil script.</summary>
    private static double ScriptShare(string script, string language)
    {
        var (from, to) = Blocks[language];
        var kannada = Blocks["kn"];
        var letters = script.Where(ch => char.IsLetter(ch) || char.GetUnicodeCategory(ch) is System.Globalization.UnicodeCategory.NonSpacingMark or System.Globalization.UnicodeCategory.SpacingCombiningMark)
            .Where(ch => language == "kn" || ch < kannada.From || ch > kannada.To)
            .ToList();
        return letters.Count == 0 ? 0 : letters.Count(ch => ch >= from && ch <= to) / (double)letters.Count;
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

    private sealed record CaseResult(
        string Id, bool Pass, IReadOnlyList<string> Reasons, string Verdict, string CheckQuestion, ScriptContent? Content,
        int Words, double ScriptShare);

    private sealed record Report(
        DateTimeOffset RunAt, string Prompt, int PromptVersion, string Model, int Cases, double Score, double Target,
        IReadOnlyList<CaseResult> Results);
}
