using Microsoft.Extensions.Options;
using SlateAi.Api.Ai;
using SlateAi.Api.Data.Entities;
using SlateAi.Api.Features.Audio;
using SlateAi.Api.Features.CatchUps;
using SlateAi.Api.Features.CatchUps.Practice;
using SlateAi.Api.Features.CatchUps.Script;
using SlateAi.Api.Features.CatchUps.Verify;
using SlateAi.Api.Speech;
using SlateAi.Api.Tests.Features.Audio;
using SlateAi.Api.Tests.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using SlateAi.Api.Tests.Curriculum;

namespace SlateAi.Api.Tests.Features.CatchUps;

public sealed class CatchUpWriterTests
{
    private static readonly CancellationToken Ct = TestContext.Current.CancellationToken;
    private static readonly Guid ClassId = Guid.NewGuid();
    private static readonly DateOnly Monday = new(2026, 10, 19);
    private const string Pass = """{"teachesTarget":true,"ageAppropriate":true,"noShaming":true,"glossMatches":true,"reason":"ok"}""";
    private const string Shaming = """{"teachesTarget":true,"ageAppropriate":true,"noShaming":false,"glossMatches":true,"reason":"Blames the child."}""";

    private readonly FakeCatchUpStore _store = new(ClassId);
    private readonly FakeAudioStore _audio = new();
    private readonly RecordingTts _tts = new();
    private readonly FakeAiCallStore _aiCalls = new();
    private readonly SlateAi.Api.Tests.Features.LessonLinks.FakeLessonLinkStore _links = new();
    private readonly SequenceLlm _llm;
    private readonly Guid _packageId;
    private readonly PackageProblems _problems;

    public CatchUpWriterTests()
    {
        _llm = new SequenceLlm(_aiCalls);
        _store.Names.AddRange(["Ravi", "Shobha"]);
        _problems = PackagePractice.For(
            ["M3.SUB.BORROW"], A3ValidatorTests.Book, CatchUpPlanner.SeedKey(ClassId, Monday, "M3.SUB.BORROW", "te"))!;
        _store.SaveGenerationAsync(ClassId, Monday,
            [new PlannedPackage(["M3.SUB.BORROW"], "M3.SUB.BORROW", "te", _problems)], [], Ct).GetAwaiter().GetResult();
        _packageId = _store.Packages.Single().Id;
    }

    private string Good => A3ValidatorTests.Output(
        script: $"ఈ రోజు {_problems.Activity.Numbers[0]} నుండి {_problems.Activity.Numbers[1]} ಕಳೆ ಕಳೆ ದಶಕ ದಶಕ ಹತ್ತು ಹತ್ತು. మీ టీచర్‌కు చూపించండి.");

    private string NoObjects => A3ValidatorTests.Output(
        script: $"ఈ రోజు {_problems.Activity.Numbers[0]} నుండి {_problems.Activity.Numbers[1]} ಕಳೆ ಕಳೆ ದಶಕ ದಶಕ ಹತ್ತು ಹತ್ತು.",
        objects: []);

    [Fact]
    public async Task Passing_first_time_is_ready_after_one_script_call()
    {
        _llm.Script.Enqueue(Good);
        _llm.Check.Enqueue(Pass);

        await Writer().WriteAsync(_packageId, Ct);

        var package = _store.Packages.Single();
        Assert.Equal(PackageStatus.Ready, package.Status);
        Assert.Equal("verified", package.Verdict);
        Assert.Single(_llm.ScriptCalls);
        var outcome = _store.Outcomes.Single();
        Assert.Equal(1, outcome.Review!.Attempts);

        // Audio: disclosure → script (no greeting recorded yet), and an audio check in the review.
        Assert.Equal(["disclosure", "script"], outcome.Audio!.Segments.Select(s => s.Kind));
        Assert.Contains(outcome.Review.Checks, c => c is { Name: "audio", Passed: true });
    }

    [Fact]
    public async Task The_voice_is_never_given_a_name()
    {
        _llm.Script.Enqueue(Good);
        _llm.Check.Enqueue(Pass);

        await Writer().WriteAsync(_packageId, Ct);

        Assert.NotEmpty(_tts.Texts);
        Assert.All(_tts.Texts, text =>
        {
            Assert.DoesNotContain("Ravi", text);
            Assert.DoesNotContain("Shobha", text);
        });
    }

    [Fact]
    public async Task Every_call_that_writes_it_is_tagged_with_the_children_it_is_for()
    {
        // T-73: the Under the hood panel says who A3, A4 and the voice were about; IDs only, never names.
        var ravi = Guid.NewGuid();
        var asha = Guid.NewGuid();
        foreach (var child in new[] { ravi, asha })
        {
            _store.Assignments.Add(new CatchUpAssignment
            {
                Id = Guid.NewGuid(), ClassId = ClassId, ChildId = child, PackageId = _packageId, WeekOf = Monday,
                CompressionRationale = "{}",
            });
        }

        _llm.Script.Enqueue(Good);
        _llm.Check.Enqueue(Pass);

        await Writer().WriteAsync(_packageId, Ct);

        Assert.Equal(2, _aiCalls.Calls.Count);
        Assert.All(_aiCalls.Calls, c => Assert.Equal([ravi, asha], c.ChildIds!));
        Assert.Contains(_tts.About, about => about is not null && about.SequenceEqual([ravi, asha]));
        Assert.Null(AiCallScope.Current);
    }

    [Fact]
    public async Task Audio_too_long_or_not_made_turns_a_verified_catch_up_into_needs_review()
    {
        _llm.Script.Enqueue(Good);
        _llm.Check.Enqueue(Pass);
        _tts.Fail = true;

        await Writer().WriteAsync(_packageId, Ct);

        var package = _store.Packages.Single();
        Assert.Equal(PackageStatus.NeedsReview, package.Status);
        Assert.Equal("verifier:audio", package.Verdict);
        Assert.NotNull(package.Script);
        Assert.Contains(_store.Outcomes.Single().Review!.Checks, c => c is { Name: "audio", Passed: false, Detail: "the audio could not be made" });
    }

    [Fact]
    public async Task A_failed_check_is_retried_once_with_the_reasons_then_ready()
    {
        _llm.Script.Enqueue(NoObjects);
        _llm.Script.Enqueue(Good);
        _llm.Check.Enqueue(Pass);

        await Writer().WriteAsync(_packageId, Ct);

        Assert.Equal(PackageStatus.Ready, _store.Packages.Single().Status);
        Assert.Equal(2, _llm.ScriptCalls.Count);
        Assert.Contains("objects: no household object suggested", _llm.ScriptCalls[1].UserContent);
        Assert.Contains("\"previousAttemptProblems\":[]", _llm.ScriptCalls[0].UserContent);
        Assert.Equal(2, _store.Outcomes.Single().Review!.Attempts);
    }

    [Fact]
    public async Task Failing_the_verifier_twice_is_needs_review_with_the_words_and_reasons_kept()
    {
        _llm.Script.Enqueue(Good);
        _llm.Script.Enqueue(Good);
        _llm.Check.Enqueue(Shaming);
        _llm.Check.Enqueue(Shaming);

        await Writer().WriteAsync(_packageId, Ct);

        var package = _store.Packages.Single();
        Assert.Equal(PackageStatus.NeedsReview, package.Status);
        Assert.Equal("verifier:content", package.Verdict);
        Assert.NotNull(package.Script);
        var review = _store.Outcomes.Single().Review!;
        Assert.False(review.Passed);
        Assert.Contains(review.Checks, c => c is { Name: "content", Passed: false } && c.Detail.StartsWith("speaks negatively"));
    }

    [Fact]
    public async Task Unusable_output_twice_is_failed()
    {
        _llm.Script.Enqueue("not json");
        _llm.Script.Enqueue("not json");

        await Writer().WriteAsync(_packageId, Ct);

        var package = _store.Packages.Single();
        Assert.Equal(PackageStatus.Failed, package.Status);
        Assert.Null(package.Script);
        Assert.Empty(_llm.CheckCalls);
    }

    [Fact]
    public async Task Code_check_failures_skip_the_haiku_call()
    {
        _llm.Script.Enqueue(NoObjects);
        _llm.Script.Enqueue(NoObjects);

        await Writer().WriteAsync(_packageId, Ct);

        Assert.Equal(PackageStatus.NeedsReview, _store.Packages.Single().Status);
        Assert.Empty(_llm.CheckCalls);
    }

    private CatchUpWriter Writer()
    {
        var prompts = PromptLibrary.Load(
            [
                ("a3-script.md", "---\nid: a3-script\nversion: 2\nmodel_key: Script\n---\nPrompt."),
                ("a4-check.md", "---\nid: a4-check\nversion: 1\nmodel_key: Check\n---\nPrompt."),
            ],
            ["Script", "Check"]);
        var book = A3ValidatorTests.Book;
        return new CatchUpWriter(
            _store,
            new ScriptWriter(_llm, _aiCalls, prompts),
            new Verifier(new ContentCheck(_llm, _aiCalls, prompts), Options.Create(new VerifierOptions())),
            new PackageAudio(_tts, _audio, new DisclosureClips(_tts, _audio, Disclosure), Options.Create(new VerifierOptions()), NullLogger<PackageAudio>.Instance),
            new FixedCurriculumProvider(book),
            prompts,
            _links);
    }

    [Fact]
    public async Task The_lesson_video_from_the_admin_page_goes_in_by_code()
    {
        // T-103: the same Video section as today's message, from the admin page's link (D-106), never from the AI.
        _links.Links["M3.SUB.BORROW"] = "https://diksha.gov.in/play/content/do_123";
        _llm.Script.Enqueue(Good);
        _llm.Check.Enqueue(Pass);

        await Writer().WriteAsync(_packageId, Ct);

        Assert.Equal("https://diksha.gov.in/play/content/do_123", _store.Packages.Single().VideoUrl);
    }

    [Fact]
    public async Task Her_edited_script_is_voiced_with_no_ai_call_and_the_version_she_saved_stays()
    {
        // T-103: after her edit the writer only voices her words, through the same audio check (FR-09).
        _llm.Script.Enqueue(Good);
        _llm.Check.Enqueue(Pass);
        await Writer().WriteAsync(_packageId, Ct);
        var package = _store.Packages.Single();
        var view = (await _store.GetViewAsync(_packageId, Ct))!;
        var mine = $"నా మాటలు {_problems.Activity.Numbers[0]} మరియు {_problems.Activity.Numbers[1]}.";
        var review = CatchUpEditCheck.Review(view.Review, CatchUpSectionKind.Concept,
            [new SlateAi.Api.Features.CatchUps.Verify.VerifierCheck("length", true, "about 3 seconds")]);
        Assert.Equal(PackageChange.Applied, await _store.ReviseAsync(
            _packageId, Guid.NewGuid(), package.ScriptVersion, new PackageRevision(CatchUpSectionKind.Concept, mine, Review: review), Ct));
        var version = package.ScriptVersion;
        Assert.Equal((PackageStatus.Generating, null), (package.Status, package.AudioSegments));
        var calls = _llm.ScriptCalls.Count + _llm.CheckCalls.Count;

        await Writer().WriteAsync(_packageId, Ct);

        Assert.Equal(PackageStatus.Ready, package.Status);
        Assert.Equal(version, package.ScriptVersion);
        Assert.Null(package.Hint);
        Assert.Equal(calls, _llm.ScriptCalls.Count + _llm.CheckCalls.Count);
        Assert.Equal(mine, _tts.Texts.Last());
        Assert.NotNull(package.AudioSegments);
        var checks = (await _store.GetViewAsync(_packageId, Ct))!.Review!.Checks;
        Assert.Contains(checks, c => c is { Name: "audio", Passed: true });
        Assert.DoesNotContain(checks, c => c.Name is "content" or "keyWords");
    }

    [Fact]
    public async Task Her_edited_script_that_cannot_be_voiced_needs_review()
    {
        _llm.Script.Enqueue(Good);
        _llm.Check.Enqueue(Pass);
        await Writer().WriteAsync(_packageId, Ct);
        var package = _store.Packages.Single();
        await _store.ReviseAsync(_packageId, Guid.NewGuid(), package.ScriptVersion,
            new PackageRevision(CatchUpSectionKind.Concept, $"నా మాటలు {_problems.Activity.Numbers[0]}."), Ct);
        _tts.Fail = true;

        await Writer().WriteAsync(_packageId, Ct);

        Assert.Equal(PackageStatus.NeedsReview, package.Status);
        Assert.Equal("verifier:audio", package.Verdict);
    }

    internal static readonly DisclosureWording Disclosure = new("1", new Dictionary<string, string>
    {
        ["te"] = "ఇది మీ బిడ్డ టీచర్ పంపిన పాఠం. మిగిలినది కంప్యూటర్ గొంతు చదువుతుంది.",
    });

    /// <summary>The fake voice, recording what it was asked to say; can be made to fail or to run long.</summary>
    internal sealed class RecordingTts : ITextToSpeech
    {
        private readonly FakeTextToSpeech _fake = new();

        public List<string> Texts { get; } = [];

        /// <summary>Who each synthesis was about (<c>AiCallScope</c>), as the logged voice would record it.</summary>
        public List<IReadOnlyList<Guid>?> About { get; } = [];

        /// <summary>The language code of each synthesis, in the same order as <see cref="About"/>.</summary>
        public List<string> LanguageCodes { get; } = [];

        public bool Fail { get; set; }
        public double? Seconds { get; set; }

        public async Task<SpokenAudio> SynthesizeAsync(string text, string languageCode, CancellationToken cancellationToken)
        {
            Texts.Add(text);
            About.Add(AiCallScope.Current);
            LanguageCodes.Add(languageCode);
            if (Fail)
            {
                throw new SpeechException(SpeechFailure.Unavailable, "down");
            }

            return Seconds is { } seconds
                ? FakeTextToSpeech.Silence(seconds)
                : await _fake.SynthesizeAsync(text, languageCode, cancellationToken);
        }
    }

    /// <summary>Answers A3 and A4 from separate queues, in order.</summary>
    private sealed class SequenceLlm(FakeAiCallStore calls) : ILlmClient
    {
        public Queue<string> Script { get; } = new();
        public Queue<string> Check { get; } = new();
        public List<LlmRequest> ScriptCalls { get; } = [];
        public List<LlmRequest> CheckCalls { get; } = [];

        public async Task<LlmResponse> CompleteJsonAsync(LlmRequest request, CancellationToken cancellationToken)
        {
            var isCheck = request.Prompt.Id == ContentCheck.PromptId;
            (isCheck ? CheckCalls : ScriptCalls).Add(request);
            var json = (isCheck ? Check : Script).Dequeue();
            var call = new AiCall
            {
                Id = Guid.NewGuid(), Purpose = request.Prompt.Id, Model = "stub", CreatedAt = DateTimeOffset.UnixEpoch,
                ChildIds = AiCallScope.Current?.ToArray(),
            };
            await calls.AddAsync(call, cancellationToken);
            return new LlmResponse(json, "stub", new LlmUsage(0, 0, 0), call.Id);
        }
    }
}
