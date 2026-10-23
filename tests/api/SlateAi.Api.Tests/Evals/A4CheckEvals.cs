using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SlateAi.Api.Ai;
using SlateAi.Api.Features.CatchUps;
using SlateAi.Api.Features.CatchUps.Script;
using SlateAi.Api.Features.CatchUps.Verify;
using SlateAi.Api.Tests.Infrastructure;

namespace SlateAi.Api.Tests.Evals;

/// <summary>
/// Live evaluation of A4's content check (Haiku): passes good scripts and catches a wrong topic, shaming, a
/// mismatched summary and a too-hard script. Writes <c>evals/results/&lt;date&gt;-a4-check.json</c>.
/// </summary>
[Trait("Category", "Eval")]
public sealed class A4CheckEvals
{
    private const int MinCorrect = 5;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    [Fact]
    public async Task Haiku_tells_good_scripts_from_flawed_ones()
    {
        var root = RepoRoot();
        var file = JsonSerializer.Deserialize<CaseFile>(
            await File.ReadAllTextAsync(Path.Combine(root, "evals", "cases", "a4-check.json"), TestContext.Current.CancellationToken),
            Json)!;
        var key = new ConfigurationBuilder().AddUserSecrets<Program>().AddEnvironmentVariables().Build()["Anthropic:ApiKey"];
        Assert.False(string.IsNullOrWhiteSpace(key), "Set Anthropic:ApiKey in the API's user-secrets to run evals.");

        await using var factory = FakeApi.Create(
            services => services.AddSingleton<IAiCallStore>(new FakeAiCallStore()),
            settings: new Dictionary<string, string?> { ["Ai:Provider"] = "Claude", ["Anthropic:ApiKey"] = key });
        await using var scope = factory.Services.CreateAsyncScope();
        var content = scope.ServiceProvider.GetRequiredService<ContentCheck>();
        var prompt = scope.ServiceProvider.GetRequiredService<PromptLibrary>().Get(ContentCheck.PromptId);
        var model = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<AiOptions>>().Value.ModelFor(prompt.ModelKey);

        var results = new List<CaseResult>();
        foreach (var c in file.Cases)
        {
            var input = new VerifyInput(
                c.Language, c.Targets,
                new ScriptContent(c.Script, [], c.Activity, c.ObjectOption, "", c.GlossKn, c.GlossEn, []),
                new ActivityData(c.Targets[0], "", "", [], 0, null, null),
                new CheckData(c.Targets[0], "", "", [], 0, 0, null),
                []);
            var check = await content.RunAsync(input, TestContext.Current.CancellationToken);
            results.Add(new CaseResult(c.Id, check.Passed == c.ExpectPass, c.ExpectPass, check.Passed, check.Detail, c.Note));
        }

        var correct = results.Count(r => r.Correct);
        var report = new Report(DateTimeOffset.UtcNow, prompt.Id, prompt.Version, model, results.Count, correct, MinCorrect, results);
        var output = Path.Combine(root, "evals", "results", $"{DateTime.UtcNow:yyyy-MM-dd}-a4-check.json");
        await File.WriteAllTextAsync(output, JsonSerializer.Serialize(report, Json) + "\n", TestContext.Current.CancellationToken);

        Assert.True(correct >= MinCorrect,
            $"A4 {correct}/{results.Count}: " + string.Join("; ", results.Where(r => !r.Correct).Select(r => $"{r.Id} ({r.Detail})")));
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

    private sealed record EvalCase(
        string Id, string Language, IReadOnlyList<string> Targets, string Script, string Activity, string ObjectOption,
        string GlossKn, string GlossEn, bool ExpectPass, string Note);

    private sealed record CaseResult(string Id, bool Correct, bool Expected, bool Passed, string Detail, string Note);

    private sealed record Report(
        DateTimeOffset RunAt, string Prompt, int PromptVersion, string Model, int Cases, int Correct, int MinCorrect,
        IReadOnlyList<CaseResult> Results);
}
