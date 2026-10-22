using SlateAi.Api.Ai;
using SlateAi.Api.Data.Entities;
using SlateAi.Api.Features.CatchUps.Script;
using SlateAi.Api.Tests.Infrastructure;

namespace SlateAi.Api.Tests.Features.CatchUps;

public sealed class ScriptWriterTests
{
    private static readonly CancellationToken Ct = TestContext.Current.CancellationToken;
    private readonly FakeAiCallStore _aiCalls = new();
    private readonly StubLlm _llm;

    public ScriptWriterTests() => _llm = new StubLlm(_aiCalls) { Json = A3ValidatorTests.Output() };

    [Fact]
    public async Task Valid_words_are_returned_and_logged_valid()
    {
        var result = await Writer().WriteAsync(A3ValidatorTests.Input, ["Ravi"], Ct);

        Assert.NotNull(result.Content);
        Assert.Equal("valid", Assert.Single(_aiCalls.Calls).Verdict);
        Assert.Equal(ScriptWriter.PromptId, _llm.Requests.Single().Prompt.Id);
    }

    [Fact]
    public async Task Claude_gets_the_code_made_numbers_and_lesson_data_but_no_names_or_the_correct_option_position()
    {
        await Writer().WriteAsync(A3ValidatorTests.Input with { Hint = "shorter" }, ["Ravi", "Shobha"], Ct);

        var content = _llm.Requests.Single().UserContent;
        var problems = A3ValidatorTests.Input.Problems;
        Assert.Contains($"\"question\":\"{problems.Check.Question}\"", content);
        Assert.Contains("\"languageName\":\"Telugu\"", content);
        Assert.Contains("\"hint\":\"shorter\"", content);
        Assert.Contains("ದಶಕ", content);
        Assert.DoesNotContain("Ravi", content);
        Assert.DoesNotContain("Shobha", content);
        Assert.DoesNotContain("correctIndex", content);
    }

    [Fact]
    public async Task Invalid_output_and_llm_failures_are_refused()
    {
        _llm.Json = A3ValidatorTests.Output(script: "Hello {child}");
        Assert.Equal("fallback:placeholder", (await Writer().WriteAsync(A3ValidatorTests.Input, [], Ct)).Verdict);

        _llm.Failure = LlmFailure.Unavailable;
        var down = await Writer().WriteAsync(A3ValidatorTests.Input, [], Ct);
        Assert.Null(down.Content);
        Assert.Equal("error:Unavailable", down.Verdict);
    }

    private ScriptWriter Writer() => new(
        _llm,
        _aiCalls,
        PromptLibrary.Load([("a3-script.md", "---\nid: a3-script\nversion: 1\nmodel_key: Script\n---\nPrompt.")], ["Script"]),
        A3ValidatorTests.Book);

    internal sealed class StubLlm(FakeAiCallStore calls) : ILlmClient
    {
        public string Json { get; set; } = "{}";
        public LlmFailure? Failure { get; set; }
        public List<LlmRequest> Requests { get; } = [];

        public async Task<LlmResponse> CompleteJsonAsync(LlmRequest request, CancellationToken cancellationToken)
        {
            lock (Requests)
            {
                Requests.Add(request);
            }

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
