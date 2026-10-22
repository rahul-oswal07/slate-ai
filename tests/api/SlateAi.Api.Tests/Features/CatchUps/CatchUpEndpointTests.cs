using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using SlateAi.Api.Ai;
using SlateAi.Api.Curriculum;
using SlateAi.Api.Data.Entities;
using SlateAi.Api.Features.Attendance;
using SlateAi.Api.Features.CatchUps;
using SlateAi.Api.Features.Ledger;
using SlateAi.Api.Tests.Infrastructure;

namespace SlateAi.Api.Tests.Features.CatchUps;

public sealed class CatchUpEndpointTests
{
    private static readonly Guid ClassId = Guid.NewGuid();
    private static readonly Guid Ravi = Guid.NewGuid();
    private static readonly Guid Asha = Guid.NewGuid();
    private static readonly Guid Kiran = Guid.NewGuid();
    private static readonly DateOnly Monday = new(2026, 10, 19);
    private static readonly DateOnly Friday = Monday.AddDays(4);
    private static readonly CancellationToken Ct = TestContext.Current.CancellationToken;
    private static readonly string GenerateUrl = $"/api/classes/{ClassId}/catchups/generate";
    private static readonly string ListUrl = $"/api/classes/{ClassId}/packages";

    private static readonly CurriculumGraph Book = new(
    [
        Item("M3.SUB.NOBORROW", 13, "none"),
        Item("M3.SUB.CHECK", 14, "none", "M3.SUB.NOBORROW"),
        Item("M3.SUB.BORROW", 15, "tens", "M3.SUB.NOBORROW"),
        Item("M3.SUB.WORD", 16, "tens", "M3.SUB.BORROW"),
    ]);

    private readonly FakeCatchUpStore _store = new(ClassId);
    private readonly FakeAiCallStore _aiCalls = new();
    private readonly SchemaLlm _llm;

    public CatchUpEndpointTests()
    {
        _llm = new SchemaLlm(_aiCalls);
        _store.Languages[Ravi] = "te";
        _store.Languages[Asha] = "kn";
        _store.Languages[Kiran] = "te";
        _store.Names.AddRange(["Ravi", "Asha", "Kiran", "Shobha"]);
    }

    [Fact]
    public async Task Generate_groups_children_by_targets_and_language_and_writes_the_words_in_the_background()
    {
        await using var factory = Create(Ledger(kiranAway: true));
        using var client = factory.CreateTeacherClient(ClassId);

        var response = await client.PostAsJsonAsync(GenerateUrl, new GenerateCatchUpsRequest(Guid.NewGuid()), Ct);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var week = await WaitUntilWrittenAsync(client);
        Assert.Equal(Monday, week.WeekOf);
        Assert.Equal(2, week.Packages.Count);

        // Ravi and Kiran (both Telugu, same targets) share one catch-up; Asha (Kannada) has her own.
        var telugu = week.Packages.Single(p => p.Language == "te");
        Assert.Equal(["M3.SUB.BORROW"], telugu.TargetCompetencyIds);
        Assert.Equal(new HashSet<Guid> { Ravi, Kiran }, telugu.Children.Select(c => c.ChildId).ToHashSet());
        Assert.Equal(PackageStatus.Ready, telugu.Status);
        Assert.Equal(1, telugu.ScriptVersion);
        Assert.NotNull(telugu.Script);
        Assert.Equal(3, telugu.KeyWordsKn.Count);

        // Numbers from code: the check tests the first target, with its answer at the stored index.
        Assert.Equal("M3.SUB.BORROW", telugu.Check.CompetencyId);
        Assert.Equal(telugu.Check.Answer, telugu.Check.Options[telugu.Check.CorrectIndex]);
        Assert.NotNull(telugu.Check.Prompt);

        var ravi = telugu.Children.Single(c => c.ChildId == Ravi);
        Assert.Equal(["M3.SUB.BORROW"], ravi.Rationale.Targets.Select(t => t.Id));
        Assert.Equal(new HashSet<string> { "M3.SUB.NOBORROW", "M3.SUB.CHECK", "M3.SUB.BORROW" }, ravi.MissedCompetencyIds.ToHashSet());

        Assert.Equal("kn", week.Packages.Single(p => p.Children.Any(c => c.ChildId == Asha)).Language);
    }

    [Fact]
    public async Task Generating_again_keeps_the_catch_ups_and_asks_claude_nothing_new()
    {
        await using var factory = Create(Ledger(kiranAway: false));
        using var client = factory.CreateTeacherClient(ClassId);
        await client.PostAsJsonAsync(GenerateUrl, new GenerateCatchUpsRequest(Guid.NewGuid()), Ct);
        await WaitUntilWrittenAsync(client);
        var calls = _llm.Requests.Count;

        var again = await client.PostAsJsonAsync(GenerateUrl, new GenerateCatchUpsRequest(Guid.NewGuid()), Ct);

        Assert.Equal(HttpStatusCode.Accepted, again.StatusCode);
        Assert.Equal(2, _store.Packages.Count);
        Assert.Equal(2, _store.Assignments.Count);
        Assert.Equal(calls, _llm.Requests.Count);
    }

    [Fact]
    public async Task Invalid_words_leave_the_catch_up_failed_not_half_written()
    {
        _llm.ScriptJson = """{"script":"Hello {child}","keyWordsKn":["ಕಳೆ","ದಶಕ","ಹತ್ತು"],"activity":{"instruction":"a","objectOption":"b"},"checkPrompt":"c","glossKn":"d","glossEn":"e"}""";
        await using var factory = Create(Ledger(kiranAway: false));
        using var client = factory.CreateTeacherClient(ClassId);

        await client.PostAsJsonAsync(GenerateUrl, new GenerateCatchUpsRequest(Guid.NewGuid()), Ct);
        var week = await WaitUntilWrittenAsync(client);

        Assert.All(week.Packages, p =>
        {
            Assert.Equal(PackageStatus.Failed, p.Status);
            Assert.Equal("fallback:placeholder", p.Verdict);
            Assert.Null(p.Script);
        });
    }

    [Fact]
    public async Task Signed_out_is_401_another_class_is_404_and_a_request_id_is_required()
    {
        await using var factory = Create(Ledger(kiranAway: false));
        using var anonymous = factory.CreateHttpsClient();
        using var other = factory.CreateTeacherClient(Guid.NewGuid());
        using var client = factory.CreateTeacherClient(ClassId);

        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(ListUrl, Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await other.PostAsJsonAsync(GenerateUrl, new GenerateCatchUpsRequest(Guid.NewGuid()), Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.PostAsJsonAsync(GenerateUrl, new GenerateCatchUpsRequest(Guid.Empty), Ct)).StatusCode);
    }

    private static async Task<CatchUpWeekResponse> WaitUntilWrittenAsync(HttpClient client)
    {
        for (var i = 0; i < 100; i++)
        {
            var week = (await client.GetFromJsonAsync<CatchUpWeekResponse>(ListUrl, Ct))!;
            if (week.Packages.Count > 0 && week.Packages.All(p => p.Status != PackageStatus.Generating))
            {
                return week;
            }

            await Task.Delay(50, Ct);
        }

        throw new TimeoutException("Catch-ups were not written.");
    }

    /// <summary>Ravi away Tue–Thu, Asha Tue only, Kiran Tue–Thu when asked; Thursday's lesson is the latest.</summary>
    private static LedgerData Ledger(bool kiranAway)
    {
        var days = new (DateOnly Date, string Id)[]
        {
            (Monday.AddDays(1), "M3.SUB.NOBORROW"), (Monday.AddDays(2), "M3.SUB.CHECK"), (Monday.AddDays(3), "M3.SUB.BORROW"),
        };
        bool Away(Guid child, DateOnly date) =>
            child == Ravi || (child == Kiran && kiranAway) || (child == Asha && date == Monday.AddDays(1));
        var children = new[] { new LedgerChildRow(Ravi, 4, "Ravi"), new LedgerChildRow(Asha, 1, "Asha"), new LedgerChildRow(Kiran, 9, "Kiran") };
        return new LedgerData(
            children,
            days.SelectMany(d => children.Select(c => new RegisterRow(c.Id, d.Date, !Away(c.Id, d.Date)))).ToList(),
            days.Select(d => new LessonRow(Guid.NewGuid(), d.Date, [d.Id])).ToList());
    }

    private ApiFactory Create(LedgerData ledger) =>
        FakeApi.Create(services =>
        {
            services.AddSingleton<ICatchUpStore>(_store);
            services.AddSingleton<ILedgerStore>(new StubLedger(ledger));
            services.AddSingleton<IAiCallStore>(_aiCalls);
            services.AddSingleton<ILlmClient>(_llm);
            services.AddSingleton(Book);
            services.AddSingleton<TimeProvider>(new FixedClock(SchoolClock.At(Friday, new TimeOnly(15, 0))));
        });

    private static Competency Item(string id, int sequence, string borrow, params string[] prerequisites) =>
        new(id, $"ಕ {id}", $"Name of {id}", prerequisites, null, 5, sequence, [], new TextbookPage("textbook", 2, sequence),
            KeyWordsKn: ["ಕಳೆ", "ದಶಕ", "ಹತ್ತು", "ಬಿಡಿ"], Practice: new PracticeRule("sub", 3, Borrow: borrow));

    private sealed class StubLedger(LedgerData data) : ILedgerStore
    {
        public Task<LedgerData?> LoadAsync(Guid classId, CancellationToken cancellationToken) => Task.FromResult<LedgerData?>(data);
    }

    /// <summary>Answers A2 with the first allowed target and A3 with valid words, both read from the request schema.</summary>
    private sealed class SchemaLlm(FakeAiCallStore calls) : ILlmClient
    {
        public string? ScriptJson { get; set; }
        public List<LlmRequest> Requests { get; } = [];

        public async Task<LlmResponse> CompleteJsonAsync(LlmRequest request, CancellationToken cancellationToken)
        {
            lock (Requests)
            {
                Requests.Add(request);
            }

            string Enum(params string[] path) =>
                path.Aggregate(request.Schema, (e, p) => e.GetProperty(p)).EnumerateArray().First().GetString()!;

            var json = request.Prompt.Id == "a2-compression"
                ? JsonSerializer.Serialize(new
                {
                    targets = new[] { new { competencyId = Enum("properties", "targets", "items", "properties", "competencyId", "enum"), reason = "Next week needs it." } },
                    dropped = Array.Empty<object>(),
                })
                : ScriptJson ?? A3ValidatorTests.Output();
            var call = new AiCall { Id = Guid.NewGuid(), Purpose = request.Prompt.Id, Model = "stub", CreatedAt = DateTimeOffset.UnixEpoch };
            await calls.AddAsync(call, cancellationToken);
            return new LlmResponse(json, "stub", new LlmUsage(0, 0, 0), call.Id);
        }
    }
}
