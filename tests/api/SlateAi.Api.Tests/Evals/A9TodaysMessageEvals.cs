using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SlateAi.Api.Ai;
using SlateAi.Api.Curriculum;
using SlateAi.Api.Features.LessonContents;
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
    public Task Todays_messages_pass_the_checks_after_at_most_one_retry_on_at_least_85_percent_of_cases() =>
        RunAsync("a9-todays-message");

    /// <summary>Lessons without the book's exercises (T-139, D-121): the lesson prompt, no picture question.</summary>
    [Fact]
    public Task Lesson_todays_messages_pass_the_checks_after_at_most_one_retry_on_at_least_85_percent_of_cases() =>
        RunAsync("a9-todays-message-lesson");

    /// <param name="name">The case file in <c>evals/cases/</c> and the results file's suffix.</param>
    private static async Task RunAsync(string name)
    {
        var root = RepoRoot();
        var file = JsonSerializer.Deserialize<CaseFile>(
            await File.ReadAllTextAsync(Path.Combine(root, "evals", "cases", $"{name}.json"), TestContext.Current.CancellationToken),
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
        // The shipped descriptions and worked examples (T-141, D-123), as the startup load puts them in the database.
        var (entries, _) = LessonContentFile.Load(Path.Combine(AppContext.BaseDirectory, "data", "lesson-content", "grade3-maths.json"))
            .Check(curriculum);
        var content = entries.ToDictionary(
            e => e.CompetencyId, e => new StoredLessonContent(e.Content.Description, e.Content.Examples, e.Source));

        var results = await Task.WhenAll(file.Cases.Select(async c =>
        {
            await using var scope = services.CreateAsyncScope();
            var writer = scope.ServiceProvider.GetRequiredService<TodaysMessageWriter>();
            var lessons = c.Lessons.Select(curriculum.Get).OfType<Competency>().ToList();
            // Sums too (D-116): their cases are written by a9-todays-message-sums; compare cases still by v4. A case without
            // a pair is a lesson without the book's exercises (T-139): no picture, written by a9-todays-message-lesson.
            var picture = c.Pair is { } pair ? TodaysPicturePicker.Pick(lessons, pair, arithmetic: true)! : null;
            var attempts = new List<Attempt>();
            IReadOnlyList<string>? feedback = null;
            for (var i = 0; i < 2; i++)
            {
                var written = await writer.WriteAsync(
                    c.Languages, lessons, picture, feedback, names, TestContext.Current.CancellationToken,
                    c.Teacher is { } t ? new TeacherKannada(t.Concept, t.Activity, t.Note) : null,
                    c.FromClass is { } f ? new TodaysFromClass(f.SummaryKn, f.Examples) : null,
                    content);
                attempts.Add(new Attempt(written.Texts is not null, written.Verdict, written.Detail, written.Texts));
                if (written.Texts is not null)
                {
                    break;
                }

                feedback = [$"output: {written.Verdict}" + (written.Detail is { } detail ? $" ({detail})" : "")];
            }

            // T-141: about a minute (Kannada words), and the concept never compares or sums the picture question's numbers.
            var kannada = attempts[^1].Texts?.GetValueOrDefault("kn")?.Concept;
            var words = kannada?.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
            var leaks = picture is not null && attempts[^1].Texts is { } texts
                && texts.Values.Any(t => t.Concept.Contains(picture.A.ToString()) && t.Concept.Contains(picture.B.ToString()));
            return new CaseResult(c.Id, picture, attempts[0].Passed, attempts[^1].Passed, attempts, words, leaks);
        }));

        var firstPass = results.Count(r => r.FirstPass) / (double)results.Length;
        var passed = results.Count(r => r.Passed) / (double)results.Length;
        var used = results.Select(r => TodaysMessageWriter.PromptFor(r.Picture)).Distinct().Order().Select(prompts.Get).ToList();
        var fresh = results.Where(r => r.Passed && r.KannadaConceptWords is not null).Select(r => r.KannadaConceptWords!.Value).ToList();
        var report = new Report(
            DateTimeOffset.UtcNow, string.Join(" + ", used.Select(p => $"{p.Id} v{p.Version}")), aiOptions.ModelFor(used[0].ModelKey),
            results.Length, firstPass, passed, Target, fresh.Count > 0 ? fresh.Average() : null, results.Count(r => r.PictureInConcept), results);
        var output = Path.Combine(root, "evals", "results", $"{DateTime.UtcNow:yyyy-MM-dd}-{name}.json");
        await File.WriteAllTextAsync(output, JsonSerializer.Serialize(report, Json) + "\n", TestContext.Current.CancellationToken);

        Assert.DoesNotContain(results, r => r.PictureInConcept);
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

    private sealed record EvalCase(
        string Id, IReadOnlyList<string> Languages, IReadOnlyList<string> Lessons, int? Pair = null, TeacherCase? Teacher = null,
        FromClassCase? FromClass = null);

    /// <summary>What she said in class (T-120, prompt v4), as code would pass it after <see cref="TodaysFromClassFilter"/>.</summary>
    private sealed record FromClassCase(string? SummaryKn, IReadOnlyList<string> Examples);

    private sealed record TeacherCase(string Concept, IReadOnlyList<string> Activity, string? Note);

    private sealed record Attempt(bool Passed, string Verdict, string? Detail, IReadOnlyDictionary<string, TodaysText>? Texts);

    /// <param name="KannadaConceptWords">Null when Kannada was hers or nothing passed.</param>
    /// <param name="PictureInConcept">Both picture numbers in one language's concept: it may give the answer away (T-141).</param>
    private sealed record CaseResult(
        string Id, TodaysPicture? Picture, bool FirstPass, bool Passed, IReadOnlyList<Attempt> Attempts, int? KannadaConceptWords = null,
        bool PictureInConcept = false);

    private sealed record Report(
        DateTimeOffset RunAt, string Prompt, string Model, int Cases, double FirstPass, double Passed, double Target,
        double? AverageKannadaConceptWords, int PictureInConcept, IReadOnlyList<CaseResult> Results);
}
