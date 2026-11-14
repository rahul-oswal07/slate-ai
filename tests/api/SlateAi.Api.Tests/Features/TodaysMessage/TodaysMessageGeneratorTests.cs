using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SlateAi.Api.Ai;
using SlateAi.Api.Curriculum;
using SlateAi.Api.Features.Audio;
using SlateAi.Api.Features.CatchUps.Verify;
using SlateAi.Api.Features.EveningMessages;
using SlateAi.Api.Features.TodaysMessage;
using SlateAi.Api.Tests.Features.Audio;
using SlateAi.Api.Tests.Features.CatchUps;
using SlateAi.Api.Tests.Features.EveningMessages;
using SlateAi.Api.Tests.Infrastructure;

namespace SlateAi.Api.Tests.Features.TodaysMessage;

public sealed class TodaysMessageGeneratorTests
{
    private static readonly CancellationToken Ct = TestContext.Current.CancellationToken;
    private static readonly Guid ClassId = Guid.NewGuid();
    private static readonly DateOnly Today = new(2026, 11, 16);

    private static readonly DisclosureWording Disclosure = new("1", new Dictionary<string, string>
    {
        ["kn"] = "ಇದು ನಿಮ್ಮ ಮಗುವಿನ ಶಿಕ್ಷಕರು ಕಳುಹಿಸಿದ ಪಾಠ. ಕಂಪ್ಯೂಟರ್ ಧ್ವನಿ ಓದುತ್ತದೆ.",
        ["hi"] = "यह आपके बच्चे के शिक्षक का भेजा पाठ है। इसे कंप्यूटर की आवाज़ पढ़ती है।",
    });

    private readonly FakeTodaysMessageStore _store = new();
    private readonly FakeAudioStore _audio = new();
    private readonly FakeAiCallStore _aiCalls = new();
    private readonly CatchUpWriterTests.RecordingTts _tts = new();
    private readonly ScriptWriterTests.StubLlm _llm;
    private readonly FakeEveningStore _evening = new(ClassId, Guid.NewGuid());

    public TodaysMessageGeneratorTests()
    {
        _llm = new ScriptWriterTests.StubLlm(_aiCalls) { Json = TodaysMessageTests.Output(TodaysMessageTests.Kannada, TodaysMessageTests.Hindi) };
        _evening.Children.Add(new EveningChild(Guid.NewGuid(), 15, "Manjunath", "kn", "yes", false));
        _evening.Children.Add(new EveningChild(Guid.NewGuid(), 24, "Rohan", "hi", "yes", false));
    }

    [Fact]
    public async Task Drafts_every_language_voices_each_concept_and_picks_the_pair_by_code()
    {
        var id = await StartAsync();

        await Generator().GenerateAsync(new TodaysJob(id, ["kn", "hi"]), Ct);

        var row = _store.Rows.Single();
        Assert.Equal((TodaysMessageStatus.Ready, 1, "valid"), (row.Status, row.Version, row.Verdict));
        Assert.Equal(new TodaysPicture("compare", 48, 33, ">"), row.Sections!.Picture);
        Assert.Equal(["kn", "hi"], row.Sections.Languages.Order().Reverse());
        Assert.Equal(3, row.Sections.Activity.Steps["hi"].Count);
        Assert.Equal(2, row.Sections.Concept.Clips.Count);
        Assert.All(row.Sections.Concept.Clips.Values, c => Assert.Contains(_audio.Clips, a => a.Info.Id == c && a.Info.Kind == AudioKind.Lesson));
        Assert.Equal("valid", Assert.Single(_aiCalls.Calls).Verdict);
    }

    [Fact]
    public async Task No_child_name_reaches_Claude_or_the_voice()
    {
        var id = await StartAsync();

        await Generator().GenerateAsync(new TodaysJob(id, ["kn", "hi"]), Ct);

        var request = _llm.Requests.Single().UserContent;
        Assert.DoesNotContain("Manjunath", request);
        Assert.DoesNotContain("Rohan", request);
        Assert.DoesNotContain(ClassId.ToString(), request);
        Assert.DoesNotContain(_tts.Texts, t => t.Contains("Manjunath") || t.Contains("Rohan"));
    }

    [Fact]
    public async Task Output_with_a_childs_name_is_retried_once_then_fails_and_nothing_is_voiced()
    {
        _llm.Json = TodaysMessageTests.Output(TodaysMessageTests.Kannada.Replace("ಮಗುವನ್ನು ಕೇಳಿ", "Manjunath ಕೇಳಿ"), TodaysMessageTests.Hindi);
        var id = await StartAsync();

        await Generator().GenerateAsync(new TodaysJob(id, ["kn", "hi"]), Ct);

        var row = _store.Rows.Single();
        Assert.Equal((TodaysMessageStatus.Failed, "fallback:name"), (row.Status, row.Verdict));
        Assert.Null(row.Sections);
        Assert.Equal(2, _llm.Requests.Count);
        Assert.Contains("fallback:name", _llm.Requests[1].UserContent);
        Assert.Empty(_tts.Texts);
    }

    [Fact]
    public async Task A_lesson_without_the_books_exercises_asks_nothing()
    {
        var id = (await _store.StartAsync(ClassId, Today, [TodaysMessageTests.NoExercises.Id], DateTimeOffset.UnixEpoch, Ct))!.Value;

        await Generator().GenerateAsync(new TodaysJob(id, ["kn"]), Ct);

        Assert.Equal("fallback:no-exercises", _store.Rows.Single().Verdict);
        Assert.Empty(_llm.Requests);
    }

    [Fact]
    public async Task A_family_language_without_the_ai_notice_fails_before_asking()
    {
        var id = await StartAsync();

        await Generator().GenerateAsync(new TodaysJob(id, ["kn", "te"]), Ct);

        Assert.Equal("fallback:no-disclosure", _store.Rows.Single().Verdict);
        Assert.Empty(_llm.Requests);
    }

    [Fact]
    public async Task A_concept_too_long_to_send_fails()
    {
        _tts.Seconds = 500;
        var id = await StartAsync();

        await Generator().GenerateAsync(new TodaysJob(id, ["kn", "hi"]), Ct);

        Assert.Equal("fallback:audio-too-long", _store.Rows.Single().Verdict);
    }

    [Fact]
    public async Task Her_kannada_edit_with_only_kannada_families_asks_no_AI_and_keeps_her_words()
    {
        var id = await EditedAsync("ಮೊದಲು ಕಟ್ಟುಗಳನ್ನು ಎಣಿಸಿ. ನಂತರ ಬಿಡಿ ಕಡ್ಡಿಗಳನ್ನು ಎಣಿಸಿ.", note: null, languages: ["kn"]);

        await Generator().GenerateAsync(new TodaysJob(id, ["kn"]), Ct);

        var row = _store.Rows.Single();
        Assert.Equal((TodaysMessageStatus.Ready, 2), (row.Status, row.Version));
        Assert.Equal("ಮೊದಲು ಕಟ್ಟುಗಳನ್ನು ಎಣಿಸಿ. ನಂತರ ಬಿಡಿ ಕಡ್ಡಿಗಳನ್ನು ಎಣಿಸಿ.", row.Sections!.Concept.Text["kn"]);
        Assert.True(row.Sections.Teacher);
        Assert.Empty(_llm.Requests);
        Assert.Contains("ಮೊದಲು ಕಟ್ಟುಗಳನ್ನು ಎಣಿಸಿ. ನಂತರ ಬಿಡಿ ಕಡ್ಡಿಗಳನ್ನು ಎಣಿಸಿ.", _tts.Texts);
    }

    [Fact]
    public async Task Her_kannada_edit_is_written_into_the_other_languages_by_A9_and_never_rewritten()
    {
        _llm.Json = TodaysMessageTests.Output(TodaysMessageTests.Hindi.Replace("\"]}", "\"],\"note\":\"हमने पत्तों पर संख्याएँ लिखकर तुलना की।\"}"));
        var id = await EditedAsync("ಮೊದಲು ಕಟ್ಟುಗಳನ್ನು ಎಣಿಸಿ.", note: "ನಾವು ಎಲೆಗಳ ಮೇಲೆ ಸಂಖ್ಯೆ ಬರೆದು ಹೋಲಿಸಿದೆವು.", languages: ["kn", "hi"]);

        await Generator().GenerateAsync(new TodaysJob(id, ["kn", "hi"]), Ct);

        var row = _store.Rows.Single();
        Assert.Equal(TodaysMessageStatus.Ready, row.Status);
        Assert.Equal("ಮೊದಲು ಕಟ್ಟುಗಳನ್ನು ಎಣಿಸಿ.", row.Sections!.Concept.Text["kn"]);
        Assert.Equal("ನಾವು ಎಲೆಗಳ ಮೇಲೆ ಸಂಖ್ಯೆ ಬರೆದು ಹೋಲಿಸಿದೆವು.", row.Sections.Note!.Text["kn"]);
        Assert.StartsWith("हमने", row.Sections.Note.Text["hi"]);
        var request = _llm.Requests.Single().UserContent;
        Assert.Contains("teacherKannada", request);
        Assert.Contains("ಮೊದಲು ಕಟ್ಟುಗಳನ್ನು ಎಣಿಸಿ.", request);
        Assert.Contains("\"code\":\"hi\"", request);
        Assert.DoesNotContain("\"code\":\"kn\"", request); // she wrote the Kannada; A9 only writes the others
    }

    [Fact]
    public async Task A_new_draft_after_her_edit_keeps_her_picture_pair()
    {
        var id = await EditedAsync("ಮೊದಲು ಕಟ್ಟುಗಳನ್ನು ಎಣಿಸಿ.", note: null, languages: ["kn"], picture: new TodaysPicture("compare", 86, 86, "="));

        await Generator().GenerateAsync(new TodaysJob(id, ["kn"]), Ct);

        Assert.Equal(new TodaysPicture("compare", 86, 86, "="), _store.Rows.Single().Sections!.Picture);
    }

    /// <summary>A ready v1 draft whose Kannada she has just edited (as the service saves it), back to generating.</summary>
    private async Task<Guid> EditedAsync(string concept, string? note, IReadOnlyList<string> languages, TodaysPicture? picture = null)
    {
        var id = await StartAsync();
        var draft = TodaysMessageTests.Sections();
        var sections = draft.WithKannada(TodaysSectionKind.Concept, concept, null)!;
        if (note is not null)
        {
            sections = sections.WithKannada(TodaysSectionKind.Note, note, null)!;
        }

        sections = sections with
        {
            Concept = sections.Concept with { Text = sections.Concept.Text.Where(t => languages.Contains(t.Key)).ToDictionary() },
            Picture = picture ?? sections.Picture,
        };
        await _store.SetReadyAsync(id, sections, "valid", DateTimeOffset.UnixEpoch, Ct);
        await _store.ReviseAsync(ClassId, Today, Guid.NewGuid(), 1, s => s, redraft: true, DateTimeOffset.UnixEpoch, Ct);
        return id;
    }

    private async Task<Guid> StartAsync() =>
        (await _store.StartAsync(ClassId, Today, [TodaysMessageTests.Compare.Id], DateTimeOffset.UnixEpoch, Ct))!.Value;

    private TodaysMessageGenerator Generator()
    {
        var curriculum = new CurriculumGraph([TodaysMessageTests.Compare, TodaysMessageTests.NoExercises], []);
        var prompts = PromptLibrary.Load(
            [("a9-todays-message.md", "---\nid: a9-todays-message\nversion: 1\nmodel_key: Script\n---\nPrompt.")], ["Script"]);
        return new TodaysMessageGenerator(
            _store,
            _evening,
            new TodaysMessageWriter(_llm, _aiCalls, prompts),
            _tts,
            _audio,
            new DisclosureClips(_tts, _audio, Disclosure),
            curriculum,
            prompts,
            Options.Create(new VerifierOptions()),
            new FixedClock(DateTimeOffset.UnixEpoch),
            NullLogger<TodaysMessageGenerator>.Instance);
    }
}
