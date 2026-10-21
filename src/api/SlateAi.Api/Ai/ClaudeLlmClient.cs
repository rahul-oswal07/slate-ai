using System.Text.Json;
using Anthropic;
using Anthropic.Exceptions;
using Anthropic.Models.Messages;
using Microsoft.Extensions.Options;

namespace SlateAi.Api.Ai;

/// <summary>
/// Claude via the official SDK (D-11). JSON comes back through structured outputs, because Sonnet 5.5 and
/// Opus 5.5 reject forced tool use. The system prompt is cached; per-request content goes after it.
/// </summary>
public sealed class ClaudeLlmClient(AnthropicClient client, IOptions<AiOptions> ai, IOptions<AnthropicOptions> anthropic)
    : ILlmClient
{
    public async Task<LlmResponse> CompleteJsonAsync(LlmRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(anthropic.Value.ApiKey))
        {
            throw new LlmException(LlmFailure.NotConfigured, "Anthropic:ApiKey is not set.");
        }

        var model = ai.Value.ModelFor(request.Prompt.ModelKey);
        Message response;
        try
        {
            response = await client.Messages.Create(BuildParams(request, model), cancellationToken);
        }
        catch (Exception e) when (e is AnthropicException or HttpRequestException
                                      || (e is TaskCanceledException && !cancellationToken.IsCancellationRequested))
        {
            throw new LlmException(LlmFailure.Unavailable, $"Claude call failed: {e.GetType().Name}", e);
        }

        // Raw() is the wire value ("refusal", "max_tokens", …); ToString() is not.
        var stopReason = response.StopReason?.Raw();
        if (stopReason == "refusal")
        {
            throw new LlmException(LlmFailure.Refused, "Claude declined the request.");
        }

        if (stopReason == "max_tokens")
        {
            throw new LlmException(LlmFailure.Truncated, $"Output reached {request.MaxTokens} tokens.");
        }

        var json = string.Concat(response.Content.Select(b => b.Value).OfType<TextBlock>().Select(t => t.Text));
        var usage = response.Usage;
        return new LlmResponse(
            json,
            model,
            new LlmUsage(
                (int)usage.InputTokens,
                (int)usage.OutputTokens,
                (int)(usage.CacheReadInputTokens ?? 0)),
            Guid.Empty);
    }

    internal static MessageCreateParams BuildParams(LlmRequest request, string model)
    {
        var schema = request.Schema.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.Clone());
        var outputConfig = request.Effort is { } effort
            ? new OutputConfig { Format = new JsonOutputFormat { Schema = schema }, Effort = ToSdk(effort) }
            : new OutputConfig { Format = new JsonOutputFormat { Schema = schema } };

        return new MessageCreateParams
        {
            Model = model,
            MaxTokens = request.MaxTokens,
            System = new List<TextBlockParam>
            {
                new() { Text = request.Prompt.Text, CacheControl = new CacheControlEphemeral() },
            },
            Messages = [new() { Role = Role.User, Content = request.UserContent }],
            OutputConfig = outputConfig,
        };
    }

    private static Effort ToSdk(LlmEffort effort) => effort switch
    {
        LlmEffort.Low => Effort.Low,
        LlmEffort.Medium => Effort.Medium,
        _ => Effort.High,
    };
}
