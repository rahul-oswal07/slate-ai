using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SlateAi.Api.Features.Audio;
using SlateAi.Api.Features.CatchUps.Verify;
using SlateAi.Api.Tests.Features.CatchUps;

namespace SlateAi.Api.Tests.Features.Audio;

public sealed class PackageAudioTests
{
    private static readonly CancellationToken Ct = TestContext.Current.CancellationToken;
    private static readonly Guid ClassId = Guid.NewGuid();
    private readonly FakeAudioStore _store = new();
    private readonly CatchUpWriterTests.RecordingTts _tts = new();

    [Fact]
    public async Task Segments_play_disclosure_then_script_and_the_disclosure_is_made_once_per_wording()
    {
        var first = await Audio().MakeAsync(Guid.NewGuid(), ClassId, "te", "ఈ రోజు ಕಳೆ నేర్చుకుందాం.", Ct);
        var second = await Audio().MakeAsync(Guid.NewGuid(), ClassId, "te", "ఇంకో పాఠం.", Ct);

        Assert.True(first.Check.Passed, first.Check.Detail);
        Assert.Equal(["disclosure", "script"], first.Audio!.Segments.Select(s => s.Kind));
        Assert.StartsWith("ఇది మీ బిడ్డ", first.Audio.DisclosureText);
        Assert.Equal(first.Audio.Segments[0].Url, second.Audio!.Segments[0].Url); // the same disclosure clip
        Assert.Equal(3, _tts.Texts.Count); // disclosure once + two scripts
        Assert.Single(_store.Clips, c => c.Info.Kind == "disclosure");
    }

    [Fact]
    public async Task Changed_wording_makes_a_new_disclosure_clip()
    {
        await Audio().MakeAsync(Guid.NewGuid(), ClassId, "te", "పాఠం.", Ct);
        var changed = new DisclosureWording("2", new Dictionary<string, string> { ["te"] = "కొత్త వాక్యం." });

        await Audio(changed).MakeAsync(Guid.NewGuid(), ClassId, "te", "పాఠం.", Ct);

        Assert.Equal(2, _store.Clips.Count(c => c.Info.Kind == "disclosure"));
    }

    [Fact]
    public async Task A_catch_up_starts_with_the_class_teachers_greeting_when_she_recorded_one()
    {
        var greeting = _store.AddGreeting(Guid.NewGuid(), ClassId);

        var (audio, check) = await Audio().MakeAsync(Guid.NewGuid(), ClassId, "te", "పాఠం.", Ct);

        Assert.Equal(["greeting", "disclosure", "script"], audio!.Segments.Select(s => s.Kind));
        Assert.Equal(AudioSegments.Url(greeting.Id), audio.Segments[0].Url);
        Assert.True(check.Passed, check.Detail); // her 8 s still fits the 95 s / 300 KB limits
    }

    [Fact]
    public async Task Another_class_greeting_is_not_used()
    {
        _store.AddGreeting(Guid.NewGuid(), Guid.NewGuid());

        var (audio, _) = await Audio().MakeAsync(Guid.NewGuid(), ClassId, "te", "పాఠం.", Ct);

        Assert.Equal(["disclosure", "script"], audio!.Segments.Select(s => s.Kind));
    }

    [Fact]
    public async Task A_greeting_counts_towards_the_length_check()
    {
        _store.AddGreeting(Guid.NewGuid(), ClassId, seconds: 12);
        _tts.Seconds = 45; // disclosure 45 + script 45 + greeting 12 = 102 s

        var (_, check) = await Audio().MakeAsync(Guid.NewGuid(), ClassId, "te", "పాఠం.", Ct);

        Assert.False(check.Passed);
        Assert.Equal("audio is 102 seconds, limit is 95", check.Detail);
    }

    [Fact]
    public void A_recorded_greeting_plays_first()
    {
        var greeting = new AudioInfo(Guid.NewGuid(), "greeting", "kn", 8, 20_000, null);
        var disclosure = new AudioInfo(Guid.NewGuid(), "disclosure", "te", 5, 15_000, null);
        var script = new AudioInfo(Guid.NewGuid(), "script", "te", 50, 140_000, null);

        var audio = AudioSegments.Order(greeting, disclosure, script, "…");

        Assert.Equal(["greeting", "disclosure", "script"], audio.Segments.Select(s => s.Kind));
        Assert.Equal(63, audio.TotalSeconds);
        Assert.Equal(175_000, audio.TotalBytes);
    }

    [Fact]
    public async Task Audio_over_95_seconds_fails_the_check_with_the_numbers()
    {
        _tts.Seconds = 100;

        var (audio, check) = await Audio().MakeAsync(Guid.NewGuid(), ClassId, "te", "పాఠం.", Ct);

        Assert.NotNull(audio);
        Assert.False(check.Passed);
        Assert.Equal("audio is 200 seconds, limit is 95", check.Detail); // disclosure 100 + script 100 with this fake
    }

    [Fact]
    public async Task When_the_voice_is_down_there_is_no_audio_and_the_check_says_so()
    {
        _tts.Fail = true;

        var (audio, check) = await Audio().MakeAsync(Guid.NewGuid(), ClassId, "te", "పాఠం.", Ct);

        Assert.Null(audio);
        Assert.Equal("the audio could not be made", check.Detail);
    }

    private PackageAudio Audio(DisclosureWording? wording = null) => new(
        _tts, _store, new DisclosureClips(_tts, _store, wording ?? CatchUpWriterTests.Disclosure), Options.Create(new VerifierOptions()),
        NullLogger<PackageAudio>.Instance);
}
