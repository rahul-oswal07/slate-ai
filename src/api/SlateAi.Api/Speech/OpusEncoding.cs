using Concentus;
using Concentus.Enums;
using Concentus.Oggfile;

namespace SlateAi.Api.Speech;

/// <summary>
/// Turns Sarvam's 16 kHz PCM (WAV) into Ogg Opus at 24 kbps: ~3 KB a second, so a 90 s catch-up stays under 300 KB
/// (FR-09; spec: "Opus/MP3 at 24–32 kbps"). Pure C# (Concentus), since the chiseled container has no ffmpeg.
/// </summary>
public static class OpusEncoding
{
    public const string ContentType = "audio/ogg";
    public const int Bitrate = 24000;

    /// <summary>Encodes a 16-bit mono WAV; returns the Ogg Opus bytes and the exact duration from the samples.</summary>
    public static (byte[] Ogg, double Seconds) FromWav(byte[] wav)
    {
        var (samples, sampleRate) = Wav.ReadMono16(wav);
        var encoder = OpusCodecFactory.CreateEncoder(sampleRate, 1, OpusApplication.OPUS_APPLICATION_VOIP);
        encoder.Bitrate = Bitrate;
        using var output = new MemoryStream();
        var ogg = new OpusOggWriteStream(encoder, output, null, sampleRate);
        ogg.WriteSamples(samples, 0, samples.Length);
        ogg.Finish();
        return (output.ToArray(), samples.Length / (double)sampleRate);
    }

    /// <summary>The sample rate clips are decoded at to be joined: Sarvam's and the browser recorder's voice band.</summary>
    public const int JoinSampleRate = 16000;

    /// <summary>
    /// Joins Ogg Opus clips into one, in order (T-106, D-127: the AI disclosure and the lesson as one WhatsApp voice note).
    /// Each clip is decoded to 16 kHz mono and the samples are encoded again at <see cref="Bitrate"/>, so the result is a
    /// single plain Ogg Opus stream (a chained Ogg would play only its first part on many phones). Returns the bytes and
    /// the duration from the samples.
    /// </summary>
    public static (byte[] Ogg, double Seconds) Join(IReadOnlyList<byte[]> clips)
    {
        var samples = new List<short>();
        foreach (var clip in clips)
        {
            var decoder = OpusCodecFactory.CreateDecoder(JoinSampleRate, 1);
            using var input = new MemoryStream(clip, writable: false);
            var ogg = new OpusOggReadStream(decoder, input);
            while (ogg.HasNextPacket)
            {
                if (ogg.DecodeNextPacket() is { Length: > 0 } packet)
                {
                    samples.AddRange(packet);
                }
            }
        }

        var all = samples.ToArray();
        var encoder = OpusCodecFactory.CreateEncoder(JoinSampleRate, 1, OpusApplication.OPUS_APPLICATION_VOIP);
        encoder.Bitrate = Bitrate;
        using var output = new MemoryStream();
        var writer = new OpusOggWriteStream(encoder, output, null, JoinSampleRate);
        writer.WriteSamples(all, 0, all.Length);
        writer.Finish();
        return (output.ToArray(), all.Length / (double)JoinSampleRate);
    }
}

/// <summary>Minimal RIFF/WAVE reader for Sarvam's output: PCM, 16-bit, mono (first channel kept if more).</summary>
public static class Wav
{
    public static (short[] Samples, int SampleRate) ReadMono16(byte[] wav)
    {
        if (wav.Length < 12 || wav[0] != 'R' || wav[1] != 'I' || wav[2] != 'F' || wav[3] != 'F' || wav[8] != 'W')
        {
            throw new InvalidDataException("Not a WAV file.");
        }

        int channels = 1, sampleRate = 0, bits = 0, at = 12;
        while (at + 8 <= wav.Length)
        {
            var id = System.Text.Encoding.ASCII.GetString(wav, at, 4);
            var size = BitConverter.ToInt32(wav, at + 4);
            var body = at + 8;
            if (id == "fmt ")
            {
                channels = BitConverter.ToInt16(wav, body + 2);
                sampleRate = BitConverter.ToInt32(wav, body + 4);
                bits = BitConverter.ToInt16(wav, body + 14);
            }
            else if (id == "data")
            {
                if (bits != 16 || sampleRate == 0)
                {
                    throw new InvalidDataException($"Expected 16-bit PCM, got {bits}-bit.");
                }

                // Some encoders write 0 or 0xFFFFFFFF for a streamed data size: take what is there.
                var length = size <= 0 || body + size > wav.Length ? wav.Length - body : size;
                var frame = 2 * channels;
                var samples = new short[length / frame];
                for (var i = 0; i < samples.Length; i++)
                {
                    samples[i] = BitConverter.ToInt16(wav, body + i * frame);
                }

                return (samples, sampleRate);
            }

            at = body + size + (size & 1);
        }

        throw new InvalidDataException("WAV has no data chunk.");
    }
}
