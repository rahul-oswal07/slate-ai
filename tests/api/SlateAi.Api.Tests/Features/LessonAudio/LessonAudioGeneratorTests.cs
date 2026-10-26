using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SlateAi.Api.Ai;
using SlateAi.Api.Curriculum;
using SlateAi.Api.Features.Audio;
using SlateAi.Api.Features.CatchUps.Verify;
using SlateAi.Api.Features.EveningMessages;
using SlateAi.Api.Features.LessonAudio;
using SlateAi.Api.Tests.Features.Audio;
using SlateAi.Api.Tests.Features.CatchUps;
using SlateAi.Api.Tests.Features.EveningMessages;
using SlateAi.Api.Tests.Infrastructure;

namespace SlateAi.Api.Tests.Features.LessonAudio;

public sealed class LessonAudioGeneratorTests
{
    private static readonly CancellationToken Ct = TestContext.Current.CancellationToken;
    private static readonly Guid ClassId = Guid.NewGuid();
    private static readonly Guid OtherClassId = Guid.NewGuid();
    private static readonly DateOnly Today = new(2026, 10, 20);

    internal const string Good =
        "{\"script\":\"ఈ రోజు మేము ముఖ విలువ నేర్చుకున్నాం. 89 లో 8 ముఖ విలువ, 80 స్థాన విలువ. పుస్తకం 15 వ పేజీలో హోంవర్క్ చేసి మీ టీచర్‌కు చూపించండి.\"}";

    private readonly FakeLessonAudioStore _store = new();
    private readonly FakeAudioStore _audio = new();
    private readonly FakeAiCallStore _aiCalls = new();
    private readonly CatchUpWriterTests.RecordingTts _tts = new();
    private readonly ScriptWriterTests.StubLlm _llm;
    private readonly FakeEveningStore _evening = new(ClassId, Guid.NewGuid());
    private readonly FakeEveningStore _otherEvening = new(OtherClassId, Guid.NewGuid());

    public LessonAudioGeneratorTests()
    {
        _llm = new ScriptWriterTests.StubLlm(_aiCalls) { Json = Good };
        foreach (var evening in new[] { _evening, _otherEvening })
        {
            evening.Lessons.Add(new ConfirmedLesson(Guid.NewGuid(), Today, ["M3.PV.FACE"]));
            evening.Children.Add(new EveningChild(Guid.NewGuid(), 1, "Ravi", "te", "yes", false));
        }
    }

    [Fact]
    public async Task Makes_the_clip_keeps_its_words_and_logs_the_verdict()
    {
        var row = await RowAsync(ClassId, "te");

        await Generator(_evening).GenerateAsync(new LessonAudioJob(row, false), Ct);

        var made = _store.Rows.Single();
        Assert.Equal(LessonAudioStatus.Ready, made.Status);
        var clip = _audio.Clips.Single(c => c.Info.Kind == AudioKind.Lesson);
        Assert.Equal(clip.Info.Id, made.ClipId);
        Assert.StartsWith("ఈ రోజు మేము", _audio.Transcripts[clip.Info.Id]);
        Assert.Equal("valid", Assert.Single(_aiCalls.Calls).Verdict);
        Assert.Equal(2, _tts.Texts.Count); // the disclosure and the lesson
        Assert.DoesNotContain("Ravi", _llm.Requests.Single().UserContent);
    }

    [Fact]
    public async Task Another_class_that_learned_the_same_lesson_reuses_the_clip_without_calling_Claude_or_the_voice()
    {
        var first = await RowAsync(ClassId, "te");
        await Generator(_evening).GenerateAsync(new LessonAudioJob(first, false), Ct);
        var second = await RowAsync(OtherClassId, "te");

        await Generator(_otherEvening).GenerateAsync(new LessonAudioJob(second, false), Ct);

        Assert.Single(_llm.Requests);
        Assert.Equal(2, _tts.Texts.Count);
        Assert.Single(_audio.Clips, c => c.Info.Kind == AudioKind.Lesson);
        var rows = _store.Rows;
        Assert.All(rows, r => Assert.Equal(LessonAudioStatus.Ready, r.Status));
        Assert.Equal(rows[0].ClipId, rows[1].ClipId);
        Assert.Equal("reused", rows[1].Verdict);
    }

    [Fact]
    public async Task Output_that_fails_the_checks_twice_is_failed_with_the_reason_and_nothing_is_spoken()
    {
        _llm.Json = "{\"script\":\"ఈ రోజు ముఖ విలువ. 57 లో 5.\"}"; // 57 is not in the lesson's examples
        var row = await RowAsync(ClassId, "te");

        await Generator(_evening).GenerateAsync(new LessonAudioJob(row, false), Ct);

        var made = _store.Rows.Single();
        Assert.Equal(LessonAudioStatus.Failed, made.Status);
        Assert.Equal("fallback:numbers", made.Verdict);
        Assert.Equal(2, _llm.Requests.Count);
        Assert.Contains("fallback:numbers", _llm.Requests[1].UserContent); // the retry is told why
        Assert.Empty(_tts.Texts);
        Assert.Empty(_audio.Clips);
    }

    [Fact]
    public async Task A_voice_that_is_down_fails_the_row_so_she_can_try_again()
    {
        _tts.Fail = true;
        var row = await RowAsync(ClassId, "te");

        await Generator(_evening).GenerateAsync(new LessonAudioJob(row, false), Ct);

        Assert.Equal(("failed", "error:voice"), (_store.Rows.Single().Status, _store.Rows.Single().Verdict));
    }

    [Fact]
    public async Task Audio_over_the_limit_is_failed_and_not_kept()
    {
        _tts.Seconds = 60; // disclosure 60 + lesson 60 > 95
        var row = await RowAsync(ClassId, "te");

        await Generator(_evening).GenerateAsync(new LessonAudioJob(row, false), Ct);

        Assert.Equal("fallback:audio-too-long", _store.Rows.Single().Verdict);
        Assert.DoesNotContain(_audio.Clips, c => c.Info.Kind == AudioKind.Lesson);
    }

    [Fact]
    public async Task Try_again_makes_a_new_clip_and_leaves_the_shared_one_alone()
    {
        var row = await RowAsync(ClassId, "te");
        await Generator(_evening).GenerateAsync(new LessonAudioJob(row, false), Ct);
        var shared = _store.Rows.Single().ClipId;
        await _store.RestartAsync(ClassId, Today, "te", DateTimeOffset.UnixEpoch, Ct);

        await Generator(_evening).GenerateAsync(new LessonAudioJob(row, true), Ct);

        Assert.NotEqual(shared, _store.Rows.Single().ClipId);
        Assert.Equal(2, _audio.Clips.Count(c => c.Info.Kind == AudioKind.Lesson));
        Assert.Equal(2, _llm.Requests.Count);
    }

    [Fact]
    public async Task Without_a_confirmed_lesson_for_that_day_nothing_is_asked_or_spoken()
    {
        _evening.Lessons.Clear();
        var row = await RowAsync(ClassId, "te");

        await Generator(_evening).GenerateAsync(new LessonAudioJob(row, false), Ct);

        Assert.Equal("fallback:no-lesson", _store.Rows.Single().Verdict);
        Assert.Empty(_llm.Requests);
        Assert.Empty(_tts.Texts);
    }

    [Fact]
    public async Task A_row_unticked_while_generating_is_not_brought_back()
    {
        var row = await RowAsync(ClassId, "te");
        await _store.RemoveAsync(ClassId, Today, Ct);

        await Generator(_evening).GenerateAsync(new LessonAudioJob(row, false), Ct);

        Assert.Empty(_store.Rows);
        Assert.Empty(_llm.Requests);
    }

    private async Task<Guid> RowAsync(Guid classId, string language) =>
        (await _store.StartAsync(classId, Today, [language], DateTimeOffset.UnixEpoch, Ct)).Single();

    private LessonAudioGenerator Generator(IEveningStore evening)
    {
        var curriculum = new CurriculumGraph([LessonScriptTests.Lesson], []);
        var prompts = PromptLibrary.Load(
            [("a8-lesson-recap.md", "---\nid: a8-lesson-recap\nversion: 1\nmodel_key: Script\n---\nPrompt.")], ["Script"]);
        return new LessonAudioGenerator(
            _store,
            evening,
            new LessonScriptWriter(_llm, _aiCalls, prompts),
            _tts,
            _audio,
            new DisclosureClips(_tts, _audio, CatchUpWriterTests.Disclosure),
            curriculum,
            prompts,
            Options.Create(new VerifierOptions()),
            new FixedClock(DateTimeOffset.UnixEpoch),
            NullLogger<LessonAudioGenerator>.Instance);
    }
}
