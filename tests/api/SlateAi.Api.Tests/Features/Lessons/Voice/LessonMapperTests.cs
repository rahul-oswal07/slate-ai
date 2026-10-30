using System.Text.Json;
using SlateAi.Api.Ai;
using SlateAi.Api.Curriculum;
using SlateAi.Api.Data.Entities;
using SlateAi.Api.Features.Lessons;
using SlateAi.Api.Features.Lessons.Voice;
using SlateAi.Api.Speech;
using SlateAi.Api.Tests.Infrastructure;

namespace SlateAi.Api.Tests.Features.Lessons.Voice;

public sealed class LessonMapperTests
{
    private static readonly Guid ClassId = Guid.NewGuid();
    private static readonly DateOnly Day = new(2026, 10, 22);
    private static readonly DateTimeOffset Started = new(2026, 10, 22, 6, 0, 0, TimeSpan.Zero);
    private static readonly CancellationToken Ct = TestContext.Current.CancellationToken;

    private static readonly CurriculumGraph Book = new(
        new[] { "M3.ADD.CARRY", "M3.SUB.NOBORROW", "M3.SUB.BORROW" }
            .Select((id, i) => new Competency(id, $"ಹೆಸರು {i}", id, [], null, 5, i, [], null)));

    private readonly FakeLessonStore _store = new();
    private readonly FakeAiCallStore _aiCalls = new();
    private readonly StubSpeech _speech = new();
    private readonly StubLlm _llm;

    public LessonMapperTests()
    {
        _llm = new StubLlm(_aiCalls);
        _store.StartVoiceAsync(ClassId, Guid.NewGuid(), Day, Started, Ct).GetAwaiter().GetResult();
    }

    private LessonMapper Mapper() => new(
        _speech, _llm, _aiCalls, PromptLibrary.Load([("a1-lesson-mapping.md", "---\nid: a1-lesson-mapping\nversion: 1\nmodel_key: Mapping\n---\nPrompt.")], ["Mapping"]),
        Book, _store);

    private static MappingJob Job() => new(ClassId, Day, Started, [1, 2, 3], "note.webm", "audio/webm");

    [Fact]
    public async Task A_clear_note_is_mapped_stored_and_logged_valid()
    {
        _speech.Text = "ಇಂದು ರವಿ ಮತ್ತು ಅನನ್ಯ ಜೊತೆ ದಶಕ ವರ್ಗಾವಣೆ ಸಂಕಲನ";
        _llm.Json = """{"competencyIds":["M3.ADD.CARRY"],"confidence":0.9,"summaryKn":"ಸಂಕಲನ","summaryEn":"Addition with carrying","examplesUsed":["27 + 15"]}""";

        var result = await Mapper().MapAsync(Job(), Ct);

        Assert.Equal("mapped", result.Status);
        var day = _store.Day(Day)!;
        Assert.Equal("mapped", day.Status);
        Assert.Equal(["M3.ADD.CARRY"], day.CompetencyIds);
        Assert.Equal("valid", Assert.Single(_aiCalls.Calls).Verdict);
    }

    [Fact]
    public async Task Claude_never_sees_the_childrens_or_teachers_names_and_sarvam_gets_no_names_as_key_terms()
    {
        _speech.Text = "ಶೋಭಾ ಟೀಚರ್ ತರಗತಿ: ರವಿ ಮತ್ತು Ananya ದಶಕ ವರ್ಗಾವಣೆ ಸಂಕಲನ ಮಾಡಿದರು";

        await Mapper().MapAsync(Job(), Ct);

        var sent = _llm.Requests.Single().UserContent;
        Assert.DoesNotContain("ರವಿ", sent);
        Assert.DoesNotContain("Ananya", sent);
        Assert.DoesNotContain("ಶೋಭಾ", sent);
        Assert.Contains("{child}", sent);
        Assert.Contains("{teacher}", sent);
        Assert.DoesNotContain(_speech.KeyTerms, term => term.Contains("Ravi") || term.Contains("Shobha"));
        Assert.Equal("kn-IN", _speech.Language);
    }

    [Fact]
    public async Task Low_confidence_is_stored_as_low()
    {
        _llm.Json = """{"competencyIds":["M3.SUB.NOBORROW","M3.SUB.BORROW"],"confidence":0.4,"summaryKn":"","summaryEn":"","examplesUsed":[]}""";

        Assert.Equal("low", (await Mapper().MapAsync(Job(), Ct)).Status);
        Assert.Equal("fallback:low-confidence", Assert.Single(_aiCalls.Calls).Verdict);
    }

    [Fact]
    public async Task Speech_failure_or_silence_is_unclear_without_calling_claude()
    {
        _speech.Fail = true;
        Assert.Equal("unclear", (await Mapper().MapAsync(Job(), Ct)).Status);

        _speech.Fail = false;
        _speech.Text = "   ";
        Assert.Equal("unclear", (await Mapper().MapAsync(Job(), Ct)).Status);

        Assert.Empty(_llm.Requests);
    }

    [Fact]
    public async Task A_photo_alone_goes_to_claude_as_an_image_without_speech_to_text()
    {
        var photo = new LlmImage([9, 9, 9], "image/jpeg");

        var result = await Mapper().MapAsync(Job() with { Audio = null, Photo = photo }, Ct);

        Assert.Equal("mapped", result.Status);
        Assert.Null(_speech.Language);
        var request = Assert.Single(_llm.Requests);
        Assert.Same(photo, Assert.Single(request.Images!));
        var content = JsonDocument.Parse(request.UserContent).RootElement;
        Assert.Equal("", content.GetProperty("transcript").GetString());
        Assert.True(content.GetProperty("photo").GetBoolean());
    }

    [Fact]
    public async Task With_a_photo_a_note_that_cant_be_heard_is_still_matched_from_the_photo()
    {
        _speech.Fail = true;

        var result = await Mapper().MapAsync(Job() with { Photo = new LlmImage([1], "image/png") }, Ct);

        Assert.Equal("mapped", result.Status);
        Assert.Single(_llm.Requests[0].Images!);
    }

    [Fact]
    public async Task A_voice_note_alone_sends_no_image()
    {
        await Mapper().MapAsync(Job(), Ct);

        Assert.Null(Assert.Single(_llm.Requests).Images);
        Assert.False(JsonDocument.Parse(_llm.Requests[0].UserContent).RootElement.GetProperty("photo").GetBoolean());
    }

    [Fact]
    public async Task A_name_claude_copies_off_the_board_is_scrubbed_before_it_is_stored()
    {
        _store.Names = new ClassNames(["Ravi"], "Shobha");
        _llm.Json = """
            {"competencyIds":["M3.ADD.CARRY"],"confidence":0.9,"summaryKn":"Ravi ಸಂಕಲನ","summaryEn":"Shobha taught Ravi carrying","examplesUsed":["Ravi: 27 + 15"]}
            """;

        await Mapper().MapAsync(Job() with { Photo = new LlmImage([1], "image/jpeg") }, Ct);

        var day = _store.Day(Day)!;
        Assert.Equal("{child} ಸಂಕಲನ", day.SummaryKn);
        Assert.Equal("{teacher} taught {child} carrying", day.SummaryEn);
        Assert.Equal(["{child}: 27 + 15"], day.ExamplesUsed);
    }

    [Fact]
    public async Task Claude_failure_is_unclear()
    {
        _llm.Failure = LlmFailure.Unavailable;

        var result = await Mapper().MapAsync(Job(), Ct);

        Assert.Equal("unclear", result.Status);
        Assert.Equal("error:Unavailable", result.Verdict);
    }

    [Fact]
    public async Task A_result_is_ignored_if_she_picked_from_the_list_meanwhile()
    {
        await _store.SaveConfirmedAsync(ClassId, Guid.NewGuid(), Day, ["M3.SUB.BORROW"], "picked", Ct);

        await Mapper().MapAsync(Job(), Ct);

        Assert.Equal(["M3.SUB.BORROW"], _store.Day(Day)!.CompetencyIds);
        Assert.Equal("confirmed", _store.Day(Day)!.Status);
    }

    private sealed class StubSpeech : ISpeechToText
    {
        public string Text { get; set; } = "ದಶಕ ವರ್ಗಾವಣೆ ಸಂಕಲನ ಮಾಡಿದೆವು";
        public bool Fail { get; set; }
        public IReadOnlyList<string> KeyTerms { get; private set; } = [];
        public string? Language { get; private set; }

        public Task<Transcription> TranscribeAsync(
            Stream audio, string fileName, string contentType, string languageCode, IReadOnlyList<string> keyTerms,
            CancellationToken cancellationToken)
        {
            KeyTerms = keyTerms;
            Language = languageCode;
            return Fail
                ? throw new SpeechException(SpeechFailure.Rejected, "bad audio")
                : Task.FromResult(new Transcription(Text, "kn-IN", null, Guid.NewGuid()));
        }
    }

    /// <summary>Records requests and logs each call like LoggedLlmClient, so verdicts can be checked.</summary>
    private sealed class StubLlm(FakeAiCallStore calls) : ILlmClient
    {
        public string Json { get; set; } =
            """{"competencyIds":["M3.ADD.CARRY"],"confidence":0.9,"summaryKn":"","summaryEn":"","examplesUsed":[]}""";
        public LlmFailure? Failure { get; set; }
        public List<LlmRequest> Requests { get; } = [];

        public async Task<LlmResponse> CompleteJsonAsync(LlmRequest request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            if (Failure is { } failure)
            {
                throw new LlmException(failure, "down");
            }

            var call = new AiCall { Id = Guid.NewGuid(), Purpose = request.Prompt.Id, Model = "stub", CreatedAt = DateTimeOffset.UnixEpoch };
            await calls.AddAsync(call, cancellationToken);
            return new LlmResponse(Json, "stub", new LlmUsage(0, 0, 0), call.Id);
        }
    }
}
