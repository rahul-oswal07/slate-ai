using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SlateAi.Api.Ai;
using SlateAi.Api.Curriculum;
using SlateAi.Api.Features.TodaysMessage;
using SlateAi.Api.Tests.Infrastructure;

namespace SlateAi.Api.Tests.Evals;

/// <summary>
/// Live A9 evaluation (T-88): real Claude (Opus writes), the shipped prompt and curriculum, production's validator and its
/// one retry. Reports the share of today's messages that pass the code checks (at most one retry). Costs credits; run with
/// <c>dotnet test --filter Category=Eval</c>. Writes <c>evals/results/&lt;date&gt;-a9-todays-message.json</c> with every message.
/// </summary>
[Trait("Category", "Eval")]
public sealed class A9TodaysMessageEvals
{
    private const double Target = 0.85;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    [Fact]
    public async Task Todays_messages_pass_the_checks_after_at_most_one_retry_on_at_least_85_percent_of_cases()
    {
        var root = RepoRoot();
        var file = JsonSerializer.Deserialize<CaseFile>(
            await File.ReadAllTextAsync(Path.Combine(root, "evals", "cases", "a9-todays-message.json"), TestContext.Current.CancellationToken),
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
            var writer = scope.ServiceProvider.GetRequiredService<TodaysMessageWriter>();
            var lessons = c.Lessons.Select(curriculum.Get).OfType<Competency>().ToList();
            var picture = TodaysPicturePicker.Pick(lessons, c.Pair)!;
            var attempts = new List<Attempt>();
            IReadOnlyList<string>? feedback = null;
            for (var i = 0; i < 2; i++)
            {
                var written = await writer.WriteAsync(
                    c.Languages, lessons, picture, feedback, names, TestContext.Current.CancellationToken,
                    c.Teacher is { } t ? new TeacherKannada(t.Concept, t.Activity, t.Note) : null);
                attempts.Add(new Attempt(written.Texts is not null, written.Verdict, written.Detail, written.Texts));
                if (written.Texts is not null)
                {
                    break;
                }

                feedback = [$"output: {written.Verdict}" + (written.Detail is { } detail ? $" ({detail})" : "")];
            }

            return new CaseResult(c.Id, picture, attempts[0].Passed, attempts[^1].Passed, attempts);
        }));

        var firstPass = results.Count(r => r.FirstPass) / (double)results.Length;
        var passed = results.Count(r => r.Passed) / (double)results.Length;
        var prompt = prompts.Get(TodaysMessageWriter.PromptId);
        var report = new Report(
            DateTimeOffset.UtcNow, $"{prompt.Id} v{prompt.Version}", aiOptions.ModelFor(prompt.ModelKey),
            results.Length, firstPass, passed, Target, results);
        var output = Path.Combine(root, "evals", "results", $"{DateTime.UtcNow:yyyy-MM-dd}-a9-todays-message.json");
        await File.WriteAllTextAsync(output, JsonSerializer.Serialize(report, Json) + "\n", TestContext.Current.CancellationToken);

        Assert.True(passed >= Target,
            $"A9 pass rate {passed:P0} < {Target:P0}: " + string.Join("; ", results.Where(r => !r.Passed).Select(r => $"{r.Id} ({r.Attempts[^1].Verdict} {r.Attempts[^1].Detail})")));
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

    private sealed record EvalCase(string Id, IReadOnlyList<string> Languages, IReadOnlyList<string> Lessons, int Pair, TeacherCase? Teacher = null);

    private sealed record TeacherCase(string Concept, IReadOnlyList<string> Activity, string? Note);

    private sealed record Attempt(bool Passed, string Verdict, string? Detail, IReadOnlyDictionary<string, TodaysText>? Texts);

    private sealed record CaseResult(string Id, TodaysPicture Picture, bool FirstPass, bool Passed, IReadOnlyList<Attempt> Attempts);

    private sealed record Report(
        DateTimeOffset RunAt, string Prompt, string Model, int Cases, double FirstPass, double Passed, double Target,
        IReadOnlyList<CaseResult> Results);
}
