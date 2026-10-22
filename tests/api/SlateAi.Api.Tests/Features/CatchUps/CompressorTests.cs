using SlateAi.Api.Ai;
using SlateAi.Api.Data.Entities;
using SlateAi.Api.Features.CatchUps.Compression;
using SlateAi.Api.Features.Ledger;
using SlateAi.Api.Tests.Infrastructure;

namespace SlateAi.Api.Tests.Features.CatchUps;

public sealed class CompressorTests
{
    private static readonly CancellationToken Ct = TestContext.Current.CancellationToken;
    private static readonly Guid ClassId = Guid.NewGuid();
    private static readonly Guid Ravi = Guid.NewGuid();
    private static readonly DateOnly Tuesday = new(2026, 10, 20);
    private static readonly DateOnly Friday = new(2026, 10, 23);

    private static readonly CompressionInput Input = new(
        Ravi,
        [Missed("M3.SUB.NOBORROW"), Missed("M3.SUB.CHECK"), Missed("M3.SUB.BORROW")],
        ["M3.SUB.WORD"],
        ["M3.SUB.BORROW", "M3.SUB.NOBORROW"]);

    private readonly FakeAiCallStore _aiCalls = new();
    private readonly StubLedger _ledger = new();
    private readonly StubLlm _llm;

    public CompressorTests() => _llm = new StubLlm(_aiCalls);

    [Fact]
    public async Task A_valid_choice_passes_through_as_the_ais_and_is_logged_valid()
    {
        var result = await Create().CompressAsync(Input, Ct);

        Assert.Equal(CompressionSource.Ai, result.Source);
        Assert.Equal(["M3.SUB.BORROW"], result.Targets.Select(t => t.Id));
        Assert.Equal("valid", Assert.Single(_aiCalls.Calls).Verdict);
        Assert.Equal(Compressor.PromptId, _llm.Requests.Single().Prompt.Id);
    }

    [Fact]
    public async Task A_target_outside_the_candidates_falls_back_to_the_rule_and_says_so_in_the_log()
    {
        _llm.Json = """{"targets":[{"competencyId":"M3.SUB.CHECK","reason":"x"}],"dropped":[]}""";

        var result = await Create().CompressAsync(Input, Ct);

        Assert.Equal(CompressionSource.Rule, result.Source);
        Assert.Equal("fallback:target-outside-candidates", Assert.Single(_aiCalls.Calls).Verdict);
    }

    [Fact]
    public async Task When_claude_is_down_the_rule_still_gives_a_target()
    {
        _llm.Failure = LlmFailure.Unavailable;

        var result = await Create().CompressAsync(Input, Ct);

        Assert.Equal(CompressionSource.Rule, result.Source);
        Assert.Equal("error:Unavailable", result.Verdict);
        Assert.Equal(["M3.SUB.BORROW"], result.Targets.Select(t => t.Id));
    }

    [Fact]
    public async Task No_candidates_means_no_llm_call()
    {
        var result = await Create().CompressAsync(Input with { Candidates = [] }, Ct);

        Assert.Equal("rule:no-candidates", result.Verdict);
        Assert.Empty(_llm.Requests);
    }

    [Fact]
    public async Task Claude_gets_lesson_ids_and_names_but_no_child_id_or_names_and_the_schema_enum_is_the_candidates()
    {
        await Create().CompressAsync(Input, Ct);

        var request = _llm.Requests.Single();
        Assert.Contains("\"candidates\":[\"M3.SUB.BORROW\",\"M3.SUB.NOBORROW\"]", request.UserContent);
        Assert.Contains("Name of M3.SUB.CHECK", request.UserContent);
        Assert.DoesNotContain(Ravi.ToString(), request.UserContent);
        Assert.DoesNotContain("Ravi", request.UserContent);
        var targets = request.Schema.GetProperty("properties").GetProperty("targets").GetProperty("items")
            .GetProperty("properties").GetProperty("competencyId").GetProperty("enum");
        Assert.Equal(["M3.SUB.BORROW", "M3.SUB.NOBORROW"], targets.EnumerateArray().Select(e => e.GetString()));
    }

    [Fact]
    public async Task A_class_gives_one_result_per_child_who_missed_a_lesson_this_week()
    {
        var asha = Guid.NewGuid();
        _ledger.Data = new LedgerData(
            [new LedgerChildRow(Ravi, 4, "Ravi"), new LedgerChildRow(asha, 1, "Asha")],
            [new RegisterRow(Ravi, Tuesday, false), new RegisterRow(asha, Tuesday, true)],
            [new LessonRow(Guid.NewGuid(), Tuesday, ["M3.SUB.NOBORROW"])]);

        var results = await Create().CompressClassAsync(ClassId, Friday, Ct);

        Assert.Equal(Ravi, Assert.Single(results).ChildId);
    }

    [Fact]
    public async Task Children_who_missed_the_same_lessons_on_the_same_days_share_one_claude_call()
    {
        var asha = Guid.NewGuid();
        var kiran = Guid.NewGuid();
        var wednesday = Tuesday.AddDays(1);
        _ledger.Data = new LedgerData(
            [new LedgerChildRow(Ravi, 4, "Ravi"), new LedgerChildRow(asha, 1, "Asha"), new LedgerChildRow(kiran, 9, "Kiran")],
            [
                new RegisterRow(Ravi, Tuesday, false), new RegisterRow(asha, Tuesday, false), new RegisterRow(kiran, Tuesday, false),
                new RegisterRow(Ravi, wednesday, true), new RegisterRow(asha, wednesday, true), new RegisterRow(kiran, wednesday, false),
            ],
            [new LessonRow(Guid.NewGuid(), Tuesday, ["M3.SUB.NOBORROW"]), new LessonRow(Guid.NewGuid(), wednesday, ["M3.SUB.CHECK"])]);

        var results = await Create().CompressClassAsync(ClassId, Friday, Ct);

        // Ravi and Asha missed Tuesday only (one call); Kiran also missed Wednesday (a second call).
        Assert.Equal(2, _llm.Requests.Count);
        Assert.Equal(new HashSet<Guid> { Ravi, asha, kiran }, results.Select(r => r.ChildId).ToHashSet());
        Assert.Equal(
            results.Single(r => r.ChildId == Ravi).Targets.Select(t => t.Id),
            results.Single(r => r.ChildId == asha).Targets.Select(t => t.Id));
    }

    private Compressor Create() => new(
        _llm,
        _aiCalls,
        PromptLibrary.Load([("a2-compression.md", "---\nid: a2-compression\nversion: 1\nmodel_key: Compression\n---\nPrompt.")], ["Compression"]),
        CompressionBook.Graph,
        _ledger);

    private static MissedCompetency Missed(string id) => new(id, [Tuesday]);

    private sealed class StubLedger : ILedgerStore
    {
        public LedgerData? Data { get; set; }

        public Task<LedgerData?> LoadAsync(Guid classId, CancellationToken cancellationToken) => Task.FromResult(Data);
    }

    private sealed class StubLlm(FakeAiCallStore calls) : ILlmClient
    {
        public string Json { get; set; } =
            """{"targets":[{"competencyId":"M3.SUB.BORROW","reason":"Next week needs it."}],"dropped":[]}""";
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
