using System.Text.Json;

namespace SlateAi.Api.Ai;

/// <summary>
/// The only way the app talks to an LLM. Every call returns JSON matching <see cref="LlmRequest.Schema"/>
/// (structured outputs) and is logged to <c>AiCall</c>. Callers still validate the result (guardrail 2).
/// </summary>
public interface ILlmClient
{
    /// <exception cref="LlmException">Refused, truncated, unavailable or not configured.</exception>
    Task<LlmResponse> CompleteJsonAsync(LlmRequest request, CancellationToken cancellationToken);
}

/// <param name="Prompt">System prompt and model key, from <c>prompts/</c>.</param>
/// <param name="UserContent">IDs and placeholders only, never real names (guardrail 1).</param>
/// <param name="Schema">JSON schema for the response. Every object needs <c>additionalProperties: false</c>.</param>
public sealed record LlmRequest(
    Prompt Prompt,
    string UserContent,
    JsonElement Schema,
    int MaxTokens = 4096,
    LlmEffort? Effort = null);

/// <param name="AiCallId">The <c>AiCall</c> row for this call, so the caller can record its verdict.</param>
public sealed record LlmResponse(string Json, string Model, LlmUsage Usage, Guid AiCallId);

public sealed record LlmUsage(int InputTokens, int OutputTokens, int CacheReadTokens);

public enum LlmEffort
{
    Low,
    Medium,
    High,
}

public enum LlmFailure
{
    /// <summary>No API key or no model configured for the prompt's model key.</summary>
    NotConfigured,

    /// <summary>The provider is down, rate-limited after retries, or returned an error.</summary>
    Unavailable,

    /// <summary>The model declined (stop reason <c>refusal</c>).</summary>
    Refused,

    /// <summary>Output hit <see cref="LlmRequest.MaxTokens"/>, so the JSON is incomplete.</summary>
    Truncated,
}

/// <summary>An LLM call failed. Callers fall back to rules or the teacher (e.g. the FR-04 picker).</summary>
public sealed class LlmException(LlmFailure failure, string message, Exception? inner = null)
    : Exception(message, inner)
{
    public LlmFailure Failure { get; } = failure;
}
