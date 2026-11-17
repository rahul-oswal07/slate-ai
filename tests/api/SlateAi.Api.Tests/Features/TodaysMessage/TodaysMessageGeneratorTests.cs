using SlateAi.Api.Speech;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SlateAi.Api.Ai;
using SlateAi.Api.Curriculum;
using SlateAi.Api.Features.Audio;
using SlateAi.Api.Features.CatchUps.Verify;
using SlateAi.Api.Features.EveningMessages;
using SlateAi.Api.Features.LessonContents;
using SlateAi.Api.Features.TodaysMessage;
using SlateAi.Api.Tests.Features.Audio;
using SlateAi.Api.Tests.Features.CatchUps;
using SlateAi.Api.Tests.Features.EveningMessages;
using SlateAi.Api.Tests.Features.LessonContents;
using SlateAi.Api.Tests.Features.LessonLinks;
using SlateAi.Api.Tests.Infrastructure;
using SlateAi.Api.Tests.Curriculum;

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
    private readonly FakeLessonLinkStore _links = new();
    private readonly FakeLessonContentStore _contents = new();
    private Guid? _curriculumId;

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
    public async Task A_sum_lesson_is_drafted_with_the_sums_prompt_and_the_books_pair_by_code()
    {
        // D-116, T-128: with the flag on, M3.ADD.CARRY gets the picture "248 + 438" and three choices by code.
        _llm.Json = TodaysMessageTests.Output(
            """{"language":"kn","concept":"ಕೂಡುವಾಗ ಮೊದಲು ಬಿಡಿ ಕಡ್ಡಿಗಳನ್ನು ಕೂಡಿ. ಹತ್ತು ಬಿಡಿ ಆದರೆ ಒಂದು ದಶಕದ ಕಟ್ಟು ಮಾಡಿ.","activity":["466 ಮತ್ತು 176 ಕ್ಕೆ ಕಡ್ಡಿಗಳ ಕಟ್ಟು ಮಾಡಿ.","ಮೊದಲು ಬಿಡಿ ಕಡ್ಡಿಗಳನ್ನು ಒಟ್ಟು ಸೇರಿಸಿ.","ಎಷ್ಟು ಆಯಿತು ಎಂದು ಮಗು ಹೇಳಲಿ."]}""",
            """{"language":"hi","concept":"जोड़ते समय पहले खुली तीलियाँ जोड़िए। दस खुली तीलियाँ हों तो एक गड्डी बनाइए।","activity":["466 और 176 के लिए तीलियों की गड्डियाँ बनाइए।","पहले खुली तीलियाँ एक साथ रखिए।","बच्चा बताए कुल कितनी हुईं।"]}""");
        var id = (await _store.StartAsync(ClassId, Today, [TodaysMessageTests.Carry.Id], DateTimeOffset.UnixEpoch, Ct))!.Value;

        await Generator(arithmetic: true).GenerateAsync(new TodaysJob(id, ["kn", "hi"]), Ct);

        var row = _store.Rows.Single();
        Assert.Equal((TodaysMessageStatus.Ready, "valid"), (row.Status, row.Verdict));
        var picture = row.Sections!.Picture!;
        Assert.Equal(("add", 248, 438, "686"), (picture.Kind, picture.A, picture.B, picture.Answer));
        Assert.Equal([676, 696, 686], picture.Options!);
        Assert.Equal("a9-todays-message-sums", _llm.Requests.Single().Prompt.Id);
    }

    [Fact]
    public async Task A_sum_lesson_without_the_flag_gets_no_picture_and_the_lesson_prompt()
    {
        // T-139: with TodaysMessage:ArithmeticExercises off, an add lesson is like any lesson without exercises.
        _llm.Json = TodaysMessageTests.Output(LessonKannada);
        var id = (await _store.StartAsync(ClassId, Today, [TodaysMessageTests.Carry.Id], DateTimeOffset.UnixEpoch, Ct))!.Value;

        await Generator().GenerateAsync(new TodaysJob(id, ["kn"]), Ct);

        Assert.Equal(TodaysMessageStatus.Ready, _store.Rows.Single().Status);
        Assert.Null(_store.Rows.Single().Sections!.Picture);
        Assert.Equal(TodaysMessageWriter.LessonPromptId, _llm.Requests.Single().Prompt.Id);
    }

    [Fact]
    public async Task A9_is_about_every_family_who_gets_it_and_each_voice_only_about_its_languages_families()
    {
        // T-127, D-115: the demo's Under the hood "For" column; IDs only, the scope ends with the job.
        // A9 writes every language in one call; a voice call is one language, so only that language's families.
        var (manjunath, rohan) = (_evening.Children[0].Id, _evening.Children[1].Id);
        var id = await StartAsync();

        await Generator().GenerateAsync(
            new TodaysJob(id, ["kn", "hi"], [new TodaysFamily(manjunath, "kn"), new TodaysFamily(rohan, "hi")]), Ct);

        Assert.Equal([manjunath, rohan], Assert.Single(_aiCalls.Calls).ChildIds!);
        Assert.Contains("kn-IN", _tts.LanguageCodes);
        Assert.Contains("hi-IN", _tts.LanguageCodes);
        Assert.All(
            _tts.LanguageCodes.Zip(_tts.About),
            call => Assert.Equal(call.First == "kn-IN" ? [manjunath] : [rohan], call.Second!));
        Assert.Null(AiCallScope.Current);
    }

    [Fact]
    public async Task A_voice_in_a_language_no_family_gets_tonight_is_class_level()
    {
        // Kannada is always drafted, for her to read (D-92); with only a Hindi family, the Kannada voice is about no one.
        var rohan = _evening.Children[1].Id;
        var id = await StartAsync();

        await Generator().GenerateAsync(new TodaysJob(id, ["kn", "hi"], [new TodaysFamily(rohan, "hi")]), Ct);

        Assert.All(
            _tts.LanguageCodes.Zip(_tts.About),
            call => Assert.Equal(call.First == "kn-IN" ? null : [rohan], call.Second));
    }

    [Fact]
    public async Task The_lessons_link_from_the_admin_page_becomes_the_video_by_code()
    {
        // T-94, D-106: the AI never chooses it; a later change on the admin page reaches only the next draft.
        _links.Links[TodaysMessageTests.Compare.Id] = "https://diksha.gov.in/play/content/do_1";
        var id = await StartAsync();

        await Generator().GenerateAsync(new TodaysJob(id, ["kn", "hi"]), Ct);

        Assert.Equal(new TodaysVideo("https://diksha.gov.in/play/content/do_1"), _store.Rows.Single().Sections!.Video);
        Assert.DoesNotContain("diksha", _llm.Requests.Single().UserContent);
    }

    [Fact]
    public async Task A_lesson_without_a_link_has_no_video()
    {
        var id = await StartAsync();

        await Generator().GenerateAsync(new TodaysJob(id, ["kn", "hi"]), Ct);

        Assert.Null(_store.Rows.Single().Sections!.Video);
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

    /// <summary>A lesson-prompt answer with no numbers at all (T-139): any lesson's allowed numbers accept it.</summary>
    private const string LessonKannada =
        """{"language":"kn","concept":"ಎರಡು ರಾಶಿಗಳನ್ನು ಸೇರಿಸಿದಾಗ ಎಲ್ಲವನ್ನೂ ಒಟ್ಟಿಗೆ ಎಣಿಸುತ್ತೇವೆ. ಮೊದಲು ಬಿಡಿ ಕಡ್ಡಿಗಳನ್ನು ಎಣಿಸಿ, ನಂತರ ಕಟ್ಟುಗಳನ್ನು ಎಣಿಸಿ.","activity":["ಕಡ್ಡಿಗಳಿಂದ ಎರಡು ಸಣ್ಣ ರಾಶಿ ಮಾಡಿ.","ಮಗು ಅವುಗಳನ್ನು ಸೇರಿಸಿ ಎಣಿಸಿ ಹೇಳಲಿ."]}""";

    [Fact]
    public async Task A_lesson_without_the_books_exercises_gets_concept_and_activity_from_the_lesson_prompt_and_no_picture()
    {
        // T-139, D-121: every confirmed lesson gets today's message; with no exercises there is no picture question.
        _llm.Json = TodaysMessageTests.Output(LessonKannada);
        var id = (await _store.StartAsync(ClassId, Today, [TodaysMessageTests.NoExercises.Id], DateTimeOffset.UnixEpoch, Ct))!.Value;

        await Generator().GenerateAsync(new TodaysJob(id, ["kn"]), Ct);

        var row = _store.Rows.Single();
        Assert.Equal((TodaysMessageStatus.Ready, "valid"), (row.Status, row.Verdict));
        Assert.Null(row.Sections!.Picture);
        Assert.Single(row.Sections.Concept.Clips);
        var request = _llm.Requests.Single();
        Assert.Equal("a9-todays-message-lesson", request.Prompt.Id);
        Assert.Contains("\"pictureQuestion\":null", request.UserContent);
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
    public async Task A_note_she_wrote_in_English_is_written_in_every_family_language_Kannada_included()
    {
        // T-119, D-111: Kannada families read it in Kannada; her Kannada concept and steps stay word for word.
        const string english = "Today we wrote numbers on leaves and compared them.";
        _llm.Json = TodaysMessageTests.Output(
            TodaysMessageTests.Kannada.Replace("\"]}", "\"],\"note\":\"ಇಂದು ನಾವು ಎಲೆಗಳ ಮೇಲೆ ಸಂಖ್ಯೆ ಬರೆದು ಹೋಲಿಸಿದೆವು.\"}"),
            TodaysMessageTests.Hindi.Replace("\"]}", "\"],\"note\":\"हमने पत्तों पर संख्याएँ लिखकर तुलना की।\"}"));
        var id = await EditedAsync("ಮೊದಲು ಕಟ್ಟುಗಳನ್ನು ಎಣಿಸಿ.", note: english, languages: ["kn", "hi"]);

        await Generator().GenerateAsync(new TodaysJob(id, ["kn", "hi"]), Ct);

        var row = _store.Rows.Single();
        Assert.Equal(TodaysMessageStatus.Ready, row.Status);
        Assert.Equal("ಮೊದಲು ಕಟ್ಟುಗಳನ್ನು ಎಣಿಸಿ.", row.Sections!.Concept.Text["kn"]); // hers, not A9's Kannada concept
        Assert.Equal("ಇಂದು ನಾವು ಎಲೆಗಳ ಮೇಲೆ ಸಂಖ್ಯೆ ಬರೆದು ಹೋಲಿಸಿದೆವು.", row.Sections.Note!.Text["kn"]);
        Assert.StartsWith("हमने", row.Sections.Note.Text["hi"]);
        Assert.Equal(english, row.Sections.Note.Original);
        var request = _llm.Requests.Single().UserContent;
        Assert.Contains("\"code\":\"kn\"", request);
        Assert.Contains("\"code\":\"hi\"", request);
        Assert.Contains(english, request);
    }

    [Fact]
    public async Task A_note_in_English_with_only_Kannada_families_still_asks_A9_for_its_Kannada()
    {
        _llm.Json = TodaysMessageTests.Output(
            TodaysMessageTests.Kannada.Replace("\"]}", "\"],\"note\":\"ಇಂದು ನಾವು ಎಲೆಗಳ ಮೇಲೆ ಸಂಖ್ಯೆ ಬರೆದು ಹೋಲಿಸಿದೆವು.\"}"));
        var id = await EditedAsync("ಮೊದಲು ಕಟ್ಟುಗಳನ್ನು ಎಣಿಸಿ.", note: "We compared numbers on leaves.", languages: ["kn"]);

        await Generator().GenerateAsync(new TodaysJob(id, ["kn"]), Ct);

        var row = _store.Rows.Single();
        Assert.Equal(TodaysMessageStatus.Ready, row.Status);
        Assert.Equal("ಇಂದು ನಾವು ಎಲೆಗಳ ಮೇಲೆ ಸಂಖ್ಯೆ ಬರೆದು ಹೋಲಿಸಿದೆವು.", row.Sections!.Note!.Text["kn"]);
        Assert.Single(_llm.Requests);
    }

    [Fact]
    public async Task A9_leaving_her_English_note_in_English_for_a_family_is_refused()
    {
        // The note is checked on its own: inside the whole Kannada text, a short English note would pass.
        _llm.Json = TodaysMessageTests.Output(
            TodaysMessageTests.Kannada.Replace("\"]}", "\"],\"note\":\"We compared numbers on leaves.\"}"));
        var id = await EditedAsync("ಮೊದಲು ಕಟ್ಟುಗಳನ್ನು ಎಣಿಸಿ.", note: "We compared numbers on leaves.", languages: ["kn"]);

        await Generator().GenerateAsync(new TodaysJob(id, ["kn"]), Ct);

        var row = _store.Rows.Single();
        Assert.Equal((TodaysMessageStatus.Failed, "fallback:script"), (row.Status, row.Verdict));
        Assert.Equal(2, _llm.Requests.Count); // one retry with the problem, then it fails
    }

    [Fact]
    public async Task A_new_draft_after_her_edit_keeps_her_picture_pair()
    {
        var id = await EditedAsync("ಮೊದಲು ಕಟ್ಟುಗಳನ್ನು ಎಣಿಸಿ.", note: null, languages: ["kn"], picture: new TodaysPicture("compare", 86, 86, "="));

        await Generator().GenerateAsync(new TodaysJob(id, ["kn"]), Ct);

        Assert.Equal(new TodaysPicture("compare", 86, 86, "="), _store.Rows.Single().Sections!.Picture);
    }

    [Fact]
    public async Task A_fresh_draft_tells_A9_what_she_said_in_class()
    {
        // T-120: A1's summary and examples from her voice note shape the wording; numbers still come from the book.
        _evening.Lessons.Add(new ConfirmedLesson(Guid.NewGuid(), Today, [TodaysMessageTests.Compare.Id], "ಬಳೆಗಳಿಂದ 48 ಮತ್ತು 33 ಹೋಲಿಸಿದೆವು.", ["bangles", "48 and 33"]));
        var id = await StartAsync();

        await Generator().GenerateAsync(new TodaysJob(id, ["kn", "hi"]), Ct);

        var request = _llm.Requests.Single().UserContent;
        Assert.Contains("\"fromClass\":{", request);
        Assert.Contains("ಬಳೆಗಳಿಂದ 48 ಮತ್ತು 33 ಹೋಲಿಸಿದೆವು.", request);
        Assert.Contains("bangles", request);
        Assert.Contains("48 and 33", request);
        Assert.Equal(TodaysMessageStatus.Ready, _store.Rows.Single().Status);
    }

    [Fact]
    public async Task Her_class_words_with_a_name_placeholder_or_a_number_off_the_book_never_reach_A9()
    {
        _evening.Lessons.Add(new ConfirmedLesson(
            Guid.NewGuid(), Today, [TodaysMessageTests.Compare.Id], "{child} ಚೆನ್ನಾಗಿ ಹೇಳಿದ.",
            ["Manjunath counted", "27 + 15", "48 > 33", "bundles of sticks"]));
        var id = await StartAsync();

        await Generator().GenerateAsync(new TodaysJob(id, ["kn", "hi"]), Ct);

        var request = _llm.Requests.Single().UserContent;
        Assert.Contains("bundles of sticks", request);
        Assert.DoesNotContain("Manjunath", request);
        Assert.DoesNotContain("{child}", request);
        Assert.DoesNotContain("27 + 15", request);
        Assert.DoesNotContain("48 > 33", request);
    }

    [Fact]
    public async Task Another_days_lesson_or_another_lesson_says_nothing_about_her_class()
    {
        _evening.Lessons.Add(new ConfirmedLesson(Guid.NewGuid(), Today.AddDays(-1), [TodaysMessageTests.Compare.Id], null, ["bangles"]));
        var id = await StartAsync();

        await Generator().GenerateAsync(new TodaysJob(id, ["kn", "hi"]), Ct);

        Assert.Contains("\"fromClass\":null", _llm.Requests.Single().UserContent);
    }

    [Fact]
    public async Task Her_own_kannada_wins_over_the_class_summary()
    {
        // When she edited, A9 only says her words in other languages (D-92): nothing else may shape them.
        _llm.Json = TodaysMessageTests.Output(TodaysMessageTests.Hindi);
        _evening.Lessons.Add(new ConfirmedLesson(Guid.NewGuid(), Today, [TodaysMessageTests.Compare.Id], null, ["bangles"]));
        var id = await EditedAsync("ಮೊದಲು ಕಟ್ಟುಗಳನ್ನು ಎಣಿಸಿ.", note: null, languages: ["kn", "hi"]);

        await Generator().GenerateAsync(new TodaysJob(id, ["kn", "hi"]), Ct);

        var request = _llm.Requests.Single().UserContent;
        Assert.Contains("teacherKannada", request);
        Assert.DoesNotContain("bangles", request);
    }

    [Fact]
    public async Task Each_languages_activity_steps_are_voiced_too()
    {
        // T-120: for a parent who reads little. One clip per language: the steps read one after the other.
        var id = await StartAsync();

        await Generator().GenerateAsync(new TodaysJob(id, ["kn", "hi"]), Ct);

        var activity = _store.Rows.Single().Sections!.Activity;
        Assert.Equal(["hi", "kn"], activity.Clips!.Keys.Order());
        Assert.Contains(string.Join(' ', activity.Steps["hi"]), _tts.Texts);
        Assert.All(activity.Clips.Values, c => Assert.Contains(_audio.Clips, a => a.Info.Id == c && a.Info.Kind == AudioKind.Lesson));
        Assert.DoesNotContain(activity.Clips.Values, c => _store.Rows.Single().Sections!.Concept.Clips.Values.Contains(c));
    }

    [Fact]
    public async Task Steps_too_long_to_voice_keep_their_text_and_the_draft_is_still_ready()
    {
        _llm.Json = TodaysMessageTests.Output(TodaysMessageTests.Kannada);
        var id = await StartAsync();

        await Generator(new LongStepsTts()).GenerateAsync(new TodaysJob(id, ["kn"]), Ct);

        var row = _store.Rows.Single();
        Assert.Equal((TodaysMessageStatus.Ready, "valid"), (row.Status, row.Verdict));
        Assert.Single(row.Sections!.Concept.Clips);
        Assert.Empty(row.Sections.Activity.Clips!);
        Assert.Equal(3, row.Sections.Activity.Steps["kn"].Count);
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

    /// <summary>The fake voice, but 500 s for the Kannada activity steps (they ask "ಮಗುವನ್ನು ಕೇಳಿ"), so only they are too long.</summary>
    private sealed class LongStepsTts : ITextToSpeech
    {
        private readonly FakeTextToSpeech _fake = new();

        public Task<SpokenAudio> SynthesizeAsync(string text, string languageCode, CancellationToken cancellationToken) =>
            text.Contains("ಮಗುವನ್ನು ಕೇಳಿ")
                ? Task.FromResult(FakeTextToSpeech.Silence(500))
                : _fake.SynthesizeAsync(text, languageCode, cancellationToken);
    }

    [Fact]
    public async Task A9_gets_the_lessons_description_and_may_use_its_worked_examples_numbers()
    {
        // T-141, D-123: the spoken concept walks through one of the stored worked examples, so its numbers pass the check.
        _contents.Contents[TodaysMessageTests.Compare.Id] = new StoredLessonContent(
            "Look at the bundles of ten first.", ["52 and 25: 52 has 5 bundles of ten and 25 has only 2."], "written");
        _llm.Json = TodaysMessageTests.Output(
            TodaysMessageTests.Kannada.Replace("ಹೆಚ್ಚು ಕಟ್ಟು ಇರುವ ಸಂಖ್ಯೆ ಹೆಚ್ಚು.", "52 ರಲ್ಲಿ 5 ಕಟ್ಟು, 25 ರಲ್ಲಿ 2 ಕಟ್ಟು."), TodaysMessageTests.Hindi);
        var id = await StartAsync();

        await Generator().GenerateAsync(new TodaysJob(id, ["kn", "hi"]), Ct);

        Assert.Equal((TodaysMessageStatus.Ready, "valid"), (_store.Rows.Single().Status, _store.Rows.Single().Verdict));
        var request = _llm.Requests.Single().UserContent;
        Assert.Contains("Look at the bundles of ten first.", request);
        Assert.Contains("52 and 25", request);
    }

    [Fact]
    public async Task A_class_following_a_stored_curriculum_gets_that_curriculum_s_description_and_video_not_the_built_in_one_s()
    {
        // T-147: two books can both have a lesson with this ID; each class reads its own.
        var stored = Guid.NewGuid();
        _curriculumId = stored;
        _contents.Contents[TodaysMessageTests.Compare.Id] = new StoredLessonContent("From the built-in book.", ["1 and 2."], "written");
        _links.Links[TodaysMessageTests.Compare.Id] = "https://diksha.gov.in/play/content/built_in";
        await _contents.SetAsync(Guid.NewGuid(), stored, TodaysMessageTests.Compare.Id,
            new LessonContentText("From the class's own book.", ["3 and 4."]), DateTimeOffset.UnixEpoch, Ct);
        await _links.SetAsync(Guid.NewGuid(), stored, TodaysMessageTests.Compare.Id, "https://diksha.gov.in/play/content/own", DateTimeOffset.UnixEpoch, Ct);
        var id = await StartAsync();

        await Generator().GenerateAsync(new TodaysJob(id, ["kn", "hi"]), Ct);

        var request = _llm.Requests.Single().UserContent;
        Assert.Contains("From the class's own book.", request);
        Assert.DoesNotContain("From the built-in book.", request);
        Assert.Equal(new TodaysVideo("https://diksha.gov.in/play/content/own"), _store.Rows.Single().Sections!.Video);
    }

    [Fact]
    public async Task Without_stored_content_the_worked_example_numbers_are_not_allowed()
    {
        _llm.Json = TodaysMessageTests.Output(
            TodaysMessageTests.Kannada.Replace("ಹೆಚ್ಚು ಕಟ್ಟು ಇರುವ ಸಂಖ್ಯೆ ಹೆಚ್ಚು.", "52 ರಲ್ಲಿ 5 ಕಟ್ಟು, 25 ರಲ್ಲಿ 2 ಕಟ್ಟು."), TodaysMessageTests.Hindi);
        var id = await StartAsync();

        await Generator().GenerateAsync(new TodaysJob(id, ["kn", "hi"]), Ct);

        Assert.Equal((TodaysMessageStatus.Failed, "fallback:numbers"), (_store.Rows.Single().Status, _store.Rows.Single().Verdict));
        Assert.Contains("\"content\":null", _llm.Requests[0].UserContent);
    }

    [Fact]
    public async Task A_lesson_changed_while_the_first_draft_was_failing_still_gets_its_own_draft()
    {
        // T-143: she changes the lesson while the first draft is being voiced, and that draft then fails. The failure is
        // about the old lesson: it must not mark the new lesson's draft failed, or the new job skips it.
        var id = await StartAsync();
        var newLesson = "M3.GEO.2D";
        var changing = new ChangeLessonTts(() => _store.StartAsync(ClassId, Today, [newLesson], DateTimeOffset.UnixEpoch, Ct));

        await Generator(changing).GenerateAsync(new TodaysJob(id, ["kn", "hi"]), Ct);

        Assert.Equal(TodaysMessageStatus.Generating, _store.Rows.Single().Status);

        _llm.Json = TodaysMessageTests.Output(LessonKannada);
        await Generator().GenerateAsync(new TodaysJob(id, ["kn"]), Ct);

        var row = _store.Rows.Single();
        Assert.Equal((TodaysMessageStatus.Ready, "valid"), (row.Status, row.Verdict));
        Assert.Equal([newLesson], row.CompetencyIds);
        Assert.Null(row.Sections!.Picture);
    }

    /// <summary>Changes the lesson the first time it is asked to speak, then fails like a voice outage.</summary>
    private sealed class ChangeLessonTts(Func<Task> change) : ITextToSpeech
    {
        public async Task<SpokenAudio> SynthesizeAsync(string text, string languageCode, CancellationToken cancellationToken)
        {
            await change();
            throw new SpeechException(SpeechFailure.Rejected, "down");
        }
    }

    private TodaysMessageGenerator Generator(ITextToSpeech? tts = null, bool arithmetic = false)
    {
        var curriculum = new CurriculumGraph(
            [TodaysMessageTests.Compare, TodaysMessageTests.NoExercises with { Id = "M3.GEO.2D" }, TodaysMessageTests.Carry], []);
        var prompts = PromptLibrary.Load(
            [
                ("a9-todays-message.md", "---\nid: a9-todays-message\nversion: 1\nmodel_key: Script\n---\nPrompt."),
                ("a9-todays-message-sums.md", "---\nid: a9-todays-message-sums\nversion: 1\nmodel_key: Script\n---\nSums."),
                ("a9-todays-message-lesson.md", "---\nid: a9-todays-message-lesson\nversion: 1\nmodel_key: Script\n---\nLesson."),
            ],
            ["Script"]);
        return new TodaysMessageGenerator(
            _store,
            _evening,
            new TodaysMessageWriter(_llm, _aiCalls, prompts),
            tts ?? _tts,
            _audio,
            _links,
            _contents,
            new DisclosureClips(_tts, _audio, Disclosure),
            new FixedCurriculumProvider(curriculum, _curriculumId),
            prompts,
            Options.Create(new VerifierOptions()),
            Options.Create(new TodaysMessageOptions { ArithmeticExercises = arithmetic }),
            new FixedClock(DateTimeOffset.UnixEpoch),
            NullLogger<TodaysMessageGenerator>.Instance);
    }
}
