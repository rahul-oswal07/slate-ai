using SlateAi.Api.Speech;

namespace SlateAi.Api.Tests.Speech;

public sealed class OpusEncodingTests
{
    [Fact]
    public void A_minute_of_16_khz_speech_audio_is_about_three_kilobytes_a_second()
    {
        var (ogg, seconds) = OpusEncoding.FromWav(FakeTextToSpeech.SilentWav(60));

        Assert.Equal("OggS", System.Text.Encoding.ASCII.GetString(ogg, 0, 4));
        Assert.Equal(60, seconds, precision: 3);
        Assert.InRange(ogg.Length, 1, 60 * 3 * 1024 + 4096); // ≤ 24 kbps plus container overhead
    }

    [Fact]
    public void Duration_follows_the_sample_rate_and_only_16_bit_wav_is_accepted()
    {
        Assert.Equal(2, Wav.ReadMono16(FakeTextToSpeech.SilentWav(2, sampleRate: 8000)).Samples.Length / 8000.0, precision: 3);
        Assert.Throws<InvalidDataException>(() => Wav.ReadMono16("not a wav at all"u8.ToArray()));
    }

    [Fact]
    public void Two_clips_join_into_one_ogg_opus_stream_as_long_as_both_T106()
    {
        // D-127: the AI disclosure and the lesson as one WhatsApp voice note.
        var (disclosure, _) = OpusEncoding.FromWav(FakeTextToSpeech.SilentWav(5));
        var (lesson, _) = OpusEncoding.FromWav(FakeTextToSpeech.SilentWav(12));

        var (joined, seconds) = OpusEncoding.Join([disclosure, lesson]);

        Assert.Equal("OggS", System.Text.Encoding.ASCII.GetString(joined, 0, 4));
        Assert.InRange(seconds, 16.9, 17.1);
        // One stream, not two chained: decoding it again gives the same length.
        Assert.InRange(OpusEncoding.Join([joined]).Seconds, 16.9, 17.1);
        Assert.InRange(joined.Length, 1, 17 * 3 * 1024 + 4096);
    }
}
