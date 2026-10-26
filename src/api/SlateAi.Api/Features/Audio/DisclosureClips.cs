using System.Security.Cryptography;
using System.Text;
using SlateAi.Api.Speech;

namespace SlateAi.Api.Features.Audio;

/// <summary>
/// The spoken AI disclosure (FR-09, D-18) per language: made once per wording with text-to-speech and shared by every
/// catch-up and lesson audio that plays it.
/// </summary>
public sealed class DisclosureClips(ITextToSpeech tts, IAudioStore store, DisclosureWording wording)
{
    public static string LanguageCode(string language) => $"{language}-IN";

    /// <summary>The disclosure as written, or null if the language has none.</summary>
    public string? Text(string language) => wording.Languages.GetValueOrDefault(language);

    /// <summary>The clip already made for this wording, or null. Never makes one (the family's page uses this).</summary>
    public async Task<AudioInfo?> FindAsync(string language, CancellationToken cancellationToken) =>
        Text(language) is { } text
            ? await store.FindSharedAsync(AudioKind.Disclosure, language, Hash(text), cancellationToken)
            : null;

    /// <summary>The clip for this wording, made now if it doesn't exist yet. Call only when <see cref="Text"/> is not null.</summary>
    public async Task<AudioInfo> GetOrMakeAsync(string language, CancellationToken cancellationToken)
    {
        var text = Text(language) ?? throw new InvalidOperationException($"No disclosure wording for {language}.");
        var hash = Hash(text);
        if (await store.FindSharedAsync(AudioKind.Disclosure, language, hash, cancellationToken) is { } clip)
        {
            return clip;
        }

        var spoken = await tts.SynthesizeAsync(text, LanguageCode(language), cancellationToken);
        return await store.SaveSharedAsync(AudioKind.Disclosure, language, hash, spoken, cancellationToken);
    }

    private static string Hash(string text) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
}
