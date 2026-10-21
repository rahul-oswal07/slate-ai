using System.Net;
using System.Text.Json;
using Anthropic;
using Microsoft.Extensions.Options;
using SlateAi.Api.Ai;
using SlateAi.Api.Tests.Infrastructure;

namespace SlateAi.Api.Tests.Ai;

public sealed class ClaudeLlmClientTests
{
    private static readonly Prompt Prompt = new("a1-lesson-mapping", 3, "Mapping", "You map lessons.");

    private static readonly JsonElement Schema = JsonDocument.Parse("""
        { "type": "object", "additionalProperties": false, "required": ["ids"],
          "properties": { "ids": { "type": "array", "items": { "type": "string", "enum": ["M3.ADD.CARRY"] } } } }
        """).RootElement;

    [Fact]
    public async Task Sends_the_configured_model_a_cached_system_prompt_and_the_output_schema()
    {
        var handler = new StubHttpHandler(() => StubHttpHandler.Json(Message("""{"ids":["M3.ADD.CARRY"]}""")));

        var response = await Client(handler).CompleteJsonAsync(
            new LlmRequest(Prompt, "Transcript: …", Schema, MaxTokens: 1000, Effort: LlmEffort.Low),
            TestContext.Current.CancellationToken);

        Assert.Equal("""{"ids":["M3.ADD.CARRY"]}""", response.Json);
        Assert.Equal("claude-sonnet-5-5", response.Model);
        Assert.Equal(new LlmUsage(120, 30, 100), response.Usage);

        var (request, body) = Assert.Single(handler.Requests);
        Assert.EndsWith("/v1/messages", request.RequestUri!.AbsolutePath);
        var sent = JsonDocument.Parse(body).RootElement;
        Assert.Equal("claude-sonnet-5-5", sent.GetProperty("model").GetString());
        Assert.Equal(1000, sent.GetProperty("max_tokens").GetInt32());
        var system = sent.GetProperty("system")[0];
        Assert.Equal("You map lessons.", system.GetProperty("text").GetString());
        Assert.Equal("ephemeral", system.GetProperty("cache_control").GetProperty("type").GetString());
        Assert.Equal("Transcript: …", sent.GetProperty("messages")[0].GetProperty("content").GetString());
        var output = sent.GetProperty("output_config");
        Assert.Equal("low", output.GetProperty("effort").GetString());
        Assert.Equal("json_schema", output.GetProperty("format").GetProperty("type").GetString());
        Assert.Equal(
            "M3.ADD.CARRY",
            output.GetProperty("format").GetProperty("schema").GetProperty("properties").GetProperty("ids")
                .GetProperty("items").GetProperty("enum")[0].GetString());
        // Sonnet 5.5 / Opus 5.5 reject forced tool use; JSON comes from structured outputs instead.
        Assert.False(sent.TryGetProperty("tool_choice", out _));
    }

    [Theory]
    [InlineData("refusal", LlmFailure.Refused)]
    [InlineData("max_tokens", LlmFailure.Truncated)]
    public async Task A_refusal_or_truncated_output_is_a_failure(string stopReason, LlmFailure expected)
    {
        var handler = new StubHttpHandler(() => StubHttpHandler.Json(Message("""{"ids":[""", stopReason)));

        var error = await Assert.ThrowsAsync<LlmException>(() => Client(handler).CompleteJsonAsync(
            new LlmRequest(Prompt, "x", Schema), TestContext.Current.CancellationToken));

        Assert.Equal(expected, error.Failure);
    }

    [Fact]
    public async Task A_server_error_means_unavailable()
    {
        var handler = new StubHttpHandler(() => StubHttpHandler.Json(
            """{"type":"error","error":{"type":"api_error","message":"boom"}}""", HttpStatusCode.InternalServerError));

        var error = await Assert.ThrowsAsync<LlmException>(() => Client(handler).CompleteJsonAsync(
            new LlmRequest(Prompt, "x", Schema), TestContext.Current.CancellationToken));

        Assert.Equal(LlmFailure.Unavailable, error.Failure);
    }

    [Fact]
    public async Task Without_an_api_key_nothing_is_sent()
    {
        var handler = new StubHttpHandler(() => StubHttpHandler.Json(Message("{}")));

        var error = await Assert.ThrowsAsync<LlmException>(() => Client(handler, apiKey: null).CompleteJsonAsync(
            new LlmRequest(Prompt, "x", Schema), TestContext.Current.CancellationToken));

        Assert.Equal(LlmFailure.NotConfigured, error.Failure);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task A_prompt_whose_model_key_has_no_model_is_not_configured()
    {
        var handler = new StubHttpHandler(() => StubHttpHandler.Json(Message("{}")));

        var error = await Assert.ThrowsAsync<LlmException>(() => Client(handler).CompleteJsonAsync(
            new LlmRequest(Prompt with { ModelKey = "Script" }, "x", Schema), TestContext.Current.CancellationToken));

        Assert.Equal(LlmFailure.NotConfigured, error.Failure);
        Assert.Empty(handler.Requests);
    }

    private static ClaudeLlmClient Client(StubHttpHandler handler, string? apiKey = "test-key") => new(
        new AnthropicClient { ApiKey = "test-key", HttpClient = new HttpClient(handler), MaxRetries = 0 },
        Options.Create(new AiOptions { Models = { ["Mapping"] = "claude-sonnet-5-5" } }),
        Options.Create(new AnthropicOptions { ApiKey = apiKey }));

    private static string Message(string text, string stopReason = "end_turn") => JsonSerializer.Serialize(new
    {
        id = "msg_test",
        type = "message",
        role = "assistant",
        model = "claude-sonnet-5-5",
        content = new[] { new { type = "text", text } },
        stop_reason = stopReason,
        stop_sequence = (string?)null,
        usage = new { input_tokens = 120, output_tokens = 30, cache_read_input_tokens = 100, cache_creation_input_tokens = 0 },
    });
}
