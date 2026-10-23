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
}
