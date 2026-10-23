using SlateAi.Api.Ai;
using SlateAi.Api.Features.CatchUps.Verify;
using SlateAi.Api.Tests.Infrastructure;

namespace SlateAi.Api.Tests.Features.CatchUps;

public sealed class ContentCheckTests
{
    private static readonly CancellationToken Ct = TestContext.Current.CancellationToken;
    private readonly FakeAiCallStore _aiCalls = new();
    private readonly ScriptWriterTests.StubLlm _llm;

    public ContentCheckTests() => _llm = new ScriptWriterTests.StubLlm(_aiCalls)
    {
        Json = """{"teachesTarget":true,"ageAppropriate":true,"noShaming":true,"glossMatches":true,"reason":"ok"}""",
    };

    [Fact]
    public async Task All_four_true_passes_and_is_logged()
    {
        var check = await Check().RunAsync(Input(), Ct);

        Assert.True(check.Passed);
        Assert.Equal("pass", Assert.Single(_aiCalls.Calls).Verdict);
        Assert.Equal(ContentCheck.PromptId, _llm.Requests.Single().Prompt.Id);
    }

    [Fact]
    public async Task One_false_fails_with_its_plain_reason()
    {
        _llm.Json = """{"teachesTarget":true,"ageAppropriate":true,"noShaming":false,"glossMatches":true,"reason":"It says missing school was bad."}""";

        var check = await Check().RunAsync(Input(), Ct);

        Assert.False(check.Passed);
        Assert.Equal("speaks negatively about the absence (It says missing school was bad.)", check.Detail);
    }

    [Fact]
    public async Task When_haiku_is_down_the_check_does_not_pass()
    {
        _llm.Failure = LlmFailure.Unavailable;

        Assert.False((await Check().RunAsync(Input(), Ct)).Passed);
    }

    [Fact]
    public async Task Haiku_gets_the_words_but_no_names()
    {
        await Check().RunAsync(Input(), Ct);

        var content = _llm.Requests.Single().UserContent;
        Assert.Contains("ದಶಕ", content);
        Assert.DoesNotContain("Ravi", content);
    }

    private ContentCheck Check() => new(
        _llm,
        _aiCalls,
        PromptLibrary.Load([("a4-check.md", "---\nid: a4-check\nversion: 1\nmodel_key: Check\n---\nPrompt.")], ["Check"]),
        A3ValidatorTests.Book);

    private static VerifyInput Input() =>
        new("te", ["M3.SUB.BORROW"],
            new SlateAi.Api.Features.CatchUps.Script.ScriptContent("ఈ రోజు ಕಳೆ ದಶಕ", ["ಕಳೆ", "ದಶಕ", "ಬಿಡಿ"], "a", "b", "c", "ದಶಕ", "borrowing", ["stones"]),
            new SlateAi.Api.Features.CatchUps.ActivityData("M3.SUB.BORROW", "sub", "523 − 168", [523, 168], 355, null, null),
            new SlateAi.Api.Features.CatchUps.CheckData("M3.SUB.BORROW", "sub", "434 − 171", [363, 263, 273], 1, 263, null),
            ["Ravi"]);
}
