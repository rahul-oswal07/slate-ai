using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SlateAi.Api.Ai;
using SlateAi.Api.Features.Audio;
using SlateAi.Api.Speech;
using SlateAi.Api.Tests.Infrastructure;

namespace SlateAi.Api.Tests.Evals;

/// <summary>
/// Live Sarvam text-to-speech check (T-11): the disclosure and one A3 script per language from the latest A3 eval,
/// spoken by the configured Bulbul voice. Reports seconds, KB and measured words/second (to tune
/// Verifier:WordsPerSecond) and writes the Ogg Opus files to <c>evals/results/audio/</c> for a listen on an Android phone.
/// Costs Sarvam credits; run with <c>dotnet test --filter Category=Eval</c>.
/// </summary>
[Trait("Category", "Eval")]
public sealed class TtsSmokeEvals
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    [Fact]
    public async Task Disclosure_and_scripts_are_spoken_within_95_seconds_and_300_KB()
    {
        var root = RepoRoot();
        var config = new ConfigurationBuilder().AddUserSecrets<Program>().AddEnvironmentVariables().Build();
        var key = config["Sarvam:ApiKey"];
        Assert.False(string.IsNullOrWhiteSpace(key), "Set Sarvam:ApiKey in the API's user-secrets to run this.");

        await using var factory = FakeApi.Create(
            services => services.AddSingleton<IAiCallStore>(new FakeAiCallStore()),
            settings: new Dictionary<string, string?> { ["Ai:Provider"] = "Claude", ["Sarvam:ApiKey"] = key });
        await using var scope = factory.Services.CreateAsyncScope();
        var tts = scope.ServiceProvider.GetRequiredService<ITextToSpeech>();
        var disclosure = scope.ServiceProvider.GetRequiredService<DisclosureWording>();

        var latestA3 = Directory.GetFiles(Path.Combine(root, "evals", "results"), "*-a3-script.json").Order().Last();
        using var a3 = JsonDocument.Parse(await File.ReadAllTextAsync(latestA3, TestContext.Current.CancellationToken));
        var scripts = a3.RootElement.GetProperty("results").EnumerateArray()
            .Select(r => (Id: r.GetProperty("id").GetString()!, Content: r.GetProperty("attempts").EnumerateArray().Last().GetProperty("content")))
            .Where(r => r.Content.ValueKind == JsonValueKind.Object)
            .GroupBy(r => r.Id[^2..])
            .ToDictionary(g => g.Key, g => g.First());

        var output = Path.Combine(root, "evals", "results", "audio");
        Directory.CreateDirectory(output);
        var results = new List<Result>();
        foreach (var language in new[] { "kn", "te", "ta", "hi" })
        {
            var spokenDisclosure = await tts.SynthesizeAsync(disclosure.Languages[language], $"{language}-IN", TestContext.Current.CancellationToken);
            await File.WriteAllBytesAsync(Path.Combine(output, $"disclosure-{language}.ogg"), spokenDisclosure.Audio, TestContext.Current.CancellationToken);

            var (id, content) = scripts[language];
            var script = content.GetProperty("script").GetString()!;
            var spoken = await tts.SynthesizeAsync(script, $"{language}-IN", TestContext.Current.CancellationToken);
            await File.WriteAllBytesAsync(Path.Combine(output, $"{id}.ogg"), spoken.Audio, TestContext.Current.CancellationToken);

            var words = script.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
            var total = spokenDisclosure.Seconds + spoken.Seconds;
            var kb = (spokenDisclosure.Audio.Length + spoken.Audio.Length) / 1024.0;
            results.Add(new Result(
                language, id, Math.Round(spokenDisclosure.Seconds, 1), words, Math.Round(spoken.Seconds, 1),
                Math.Round(words / spoken.Seconds, 2), Math.Round(total, 1), Math.Round(kb, 1), total <= 95 && kb <= 300));
        }

        var sarvam = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<SarvamOptions>>().Value;
        var report = new { RunAt = DateTimeOffset.UtcNow, Voice = $"{sarvam.TtsModel}/{sarvam.TtsSpeaker}", sarvam.TtsSampleRate, Results = results };
        await File.WriteAllTextAsync(
            Path.Combine(root, "evals", "results", $"{DateTime.UtcNow:yyyy-MM-dd}-tts.json"),
            JsonSerializer.Serialize(report, Json) + "\n", TestContext.Current.CancellationToken);

        Assert.All(results, r => Assert.True(r.WithinLimits, $"{r.Language}: {r.TotalSeconds} s, {r.TotalKb} KB"));
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

    private sealed record Result(
        string Language, string Script, double DisclosureSeconds, int Words, double ScriptSeconds, double WordsPerSecond,
        double TotalSeconds, double TotalKb, bool WithinLimits);
}
