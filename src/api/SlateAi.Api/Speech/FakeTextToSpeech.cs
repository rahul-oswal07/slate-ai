namespace SlateAi.Api.Speech;

/// <summary>
/// Silent Opus audio whose length follows the text (about 2 words a second), so length checks behave like real
/// audio when <c>Ai:Provider=Fake</c> and in tests.
/// </summary>
public sealed class FakeTextToSpeech : ITextToSpeech
{
    public Task<SpokenAudio> SynthesizeAsync(string text, string languageCode, CancellationToken cancellationToken)
    {
        var words = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
        return Task.FromResult(Silence(Math.Max(1, words / 2.0)));
    }

    /// <summary>Silent 16 kHz audio of <paramref name="seconds"/>, encoded like the real thing.</summary>
    public static SpokenAudio Silence(double seconds)
    {
        var (ogg, length) = OpusEncoding.FromWav(SilentWav(seconds));
        return new SpokenAudio(ogg, OpusEncoding.ContentType, length, Guid.Empty);
    }

    /// <summary>A 16 kHz, 16-bit mono WAV of silence.</summary>
    public static byte[] SilentWav(double seconds, int sampleRate = 16000)
    {
        var dataBytes = (int)(seconds * sampleRate) * 2;
        using var stream = new MemoryStream();
        using var w = new BinaryWriter(stream);
        w.Write("RIFF"u8);
        w.Write(36 + dataBytes);
        w.Write("WAVEfmt "u8);
        w.Write(16);
        w.Write((short)1);
        w.Write((short)1);
        w.Write(sampleRate);
        w.Write(sampleRate * 2);
        w.Write((short)2);
        w.Write((short)16);
        w.Write("data"u8);
        w.Write(dataBytes);
        w.Write(new byte[dataBytes]);
        w.Flush();
        return stream.ToArray();
    }
}
