using System.Text.Json;
using Microsoft.Extensions.Options;
using SlateAi.Api.Features.CatchUps.Verify;
using SlateAi.Api.Speech;

namespace SlateAi.Api.Features.Audio;

/// <summary>One segment the player plays, in order (D-18, D-58: greeting · computer voice · lesson).</summary>
public sealed record AudioSegment(string Kind, string Url, double Seconds);

/// <param name="DisclosureText">The disclosure as written; the parent's transcript starts with it (FR-09).</param>
public sealed record AudioSegments(IReadOnlyList<AudioSegment> Segments, double TotalSeconds, int TotalBytes, string DisclosureText)
{
    public static string Url(Guid clipId) => $"/api/audio/{clipId}";

    /// <summary>Greeting (when she recorded one) → disclosure → script.</summary>
    public static AudioSegments Order(AudioInfo? greeting, AudioInfo disclosure, AudioInfo script, string disclosureText)
    {
        var clips = new[] { greeting, disclosure, script }.OfType<AudioInfo>().ToList();
        return new AudioSegments(
            clips.Select(c => new AudioSegment(c.Kind, Url(c.Id), Math.Round(c.Seconds, 1))).ToList(),
            Math.Round(clips.Sum(c => c.Seconds), 1),
            clips.Sum(c => c.SizeBytes),
            disclosureText);
    }
}

/// <summary>The disclosure wording per language (<c>data/seed/disclosure.json</c>).</summary>
public sealed record DisclosureWording(string Version, IReadOnlyDictionary<string, string> Languages)
{
    public const string File = "data/seed/disclosure.json";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static DisclosureWording Load(string path) =>
        JsonSerializer.Deserialize<DisclosureWording>(System.IO.File.ReadAllText(path), Json)
        ?? throw new InvalidOperationException($"Disclosure wording '{path}' is empty.");
}

/// <summary>
/// Makes a catch-up's audio (FR-09): the script through text-to-speech, the language's disclosure clip (made once
/// per wording and reused, D-18, see <see cref="DisclosureClips"/>), and the real-audio check: total ≤ 95 s and ≤ 300 KB (spec §5.4 check 3).
/// </summary>
public sealed class PackageAudio(
    ITextToSpeech tts,
    IAudioStore store,
    DisclosureClips disclosure,
    IOptions<VerifierOptions> options,
    ILogger<PackageAudio> logger)
{
    public const string CheckName = "audio";

    /// <returns>The segments (null if audio couldn't be made) and the audio check for the package's review.</returns>
    public async Task<(AudioSegments? Audio, VerifierCheck Check)> MakeAsync(
        Guid packageId, Guid classId, string language, string script, CancellationToken cancellationToken)
    {
        if (disclosure.Text(language) is not { } disclosureText)
        {
            return (null, new VerifierCheck(CheckName, false, $"no disclosure wording for {language}"));
        }

        try
        {
            var disclosureClip = await disclosure.GetOrMakeAsync(language, cancellationToken);
            var spoken = await tts.SynthesizeAsync(script, DisclosureClips.LanguageCode(language), cancellationToken);
            var scriptClip = await store.ReplaceScriptAsync(packageId, language, spoken, cancellationToken);
            // Her own greeting plays first when she has recorded one. It is fixed here, at generation: an approved
            // catch-up never changes under her (guardrail 3), and she hears it in the preview before approving.
            var greeting = await store.FindClassGreetingAsync(classId, cancellationToken);
            var audio = AudioSegments.Order(greeting, disclosureClip, scriptClip, disclosureText);

            var words = script.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
            logger.LogInformation(
                "Catch-up audio {Language}: {Words} words in {Seconds:F1} s ({Rate:F2} words/s); total {Total:F1} s, {Bytes} bytes",
                language, words, spoken.Seconds, words / Math.Max(spoken.Seconds, 0.1), audio.TotalSeconds, audio.TotalBytes);
            return (audio, Check(audio));
        }
        catch (SpeechException e)
        {
            // The AiCall row records the failure; the words are kept for review.
            logger.LogWarning("Catch-up audio for package {PackageId} failed: {Failure}", packageId, e.Failure);
            return (null, new VerifierCheck(CheckName, false, "the audio could not be made"));
        }
    }

    private VerifierCheck Check(AudioSegments audio)
    {
        var o = options.Value;
        var seconds = (int)Math.Ceiling(audio.TotalSeconds);
        var kb = (int)Math.Ceiling(audio.TotalBytes / 1024.0);
        if (seconds > o.MaxAudioSeconds)
        {
            return new VerifierCheck(CheckName, false, $"audio is {seconds} seconds, limit is {o.MaxAudioSeconds}");
        }

        return kb > o.MaxAudioKb
            ? new VerifierCheck(CheckName, false, $"audio is {kb} KB, limit is {o.MaxAudioKb}")
            : new VerifierCheck(CheckName, true, $"{seconds} seconds, {kb} KB");
    }
}
