using System.Text.Json;
using Microsoft.Extensions.Options;
using SlateAi.Api.Ai;
using SlateAi.Api.Tests.Infrastructure;

namespace SlateAi.Api.Tests.Ai;

public sealed class LoggedLlmClientTests
{
    private static readonly Prompt Prompt = new("a1-lesson-mapping", 2, "Mapping", "text");
    private static readonly JsonElement Schema = JsonDocument.Parse("""{ "type": "object" }""").RootElement;
    private static readonly IOptions<AiOptions> Options =
        Microsoft.Extensions.Options.Options.Create(new AiOptions { Models = { ["Mapping"] = "claude-sonnet-5-5" } });

    [Fact]
    public async Task Logs_a_successful_call_and_returns_its_id()
    {
        var store = new FakeAiCallStore();
        var inner = new StubLlm(_ => new LlmResponse("{}", "claude-sonnet-5-5", new LlmUsage(10, 5, 8), Guid.Empty));
        var client = new LoggedLlmClient(inner, store, Options, TimeProvider.System);

        var response = await client.CompleteJsonAsync(new LlmRequest(Prompt, "x", Schema), TestContext.Current.CancellationToken);

        var call = Assert.Single(store.Calls);
        Assert.Equal(call.Id, response.AiCallId);
        Assert.Equal("a1-lesson-mapping", call.Purpose);
        Assert.Equal(2, call.PromptVersion);
        Assert.Equal("claude-sonnet-5-5", call.Model);
        Assert.Equal((10, 5, 8), (call.InputTokens, call.OutputTokens, call.CacheReadTokens));
        Assert.Null(call.Error);
        Assert.Null(call.Verdict);

        await store.SetVerdictAsync(response.AiCallId, "valid", TestContext.Current.CancellationToken);
        Assert.Equal("valid", call.Verdict);
    }

    [Fact]
    public async Task Logs_a_failed_call_and_rethrows()
    {
        var store = new FakeAiCallStore();
        var inner = new StubLlm(_ => throw new LlmException(LlmFailure.Refused, "Claude declined the request."));
        var client = new LoggedLlmClient(inner, store, Options, TimeProvider.System);

        var error = await Assert.ThrowsAsync<LlmException>(
            () => client.CompleteJsonAsync(new LlmRequest(Prompt, "x", Schema), TestContext.Current.CancellationToken));

        Assert.Equal(LlmFailure.Refused, error.Failure);
        var call = Assert.Single(store.Calls);
        Assert.Equal("Refused: Claude declined the request.", call.Error);
        Assert.Equal("claude-sonnet-5-5", call.Model);
    }

    [Fact]
    public async Task Logs_a_cancelled_call_as_an_error()
    {
        var store = new FakeAiCallStore();
        var inner = new StubLlm(_ => throw new OperationCanceledException());
        var client = new LoggedLlmClient(inner, store, Options, TimeProvider.System);

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => client.CompleteJsonAsync(new LlmRequest(Prompt, "x", Schema), TestContext.Current.CancellationToken));

        Assert.Equal("OperationCanceledException", Assert.Single(store.Calls).Error);
    }

    private sealed class StubLlm(Func<LlmRequest, LlmResponse> respond) : ILlmClient
    {
        public Task<LlmResponse> CompleteJsonAsync(LlmRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(respond(request));
    }
}
