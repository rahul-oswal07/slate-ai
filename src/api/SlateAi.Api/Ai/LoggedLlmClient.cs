using Microsoft.Extensions.Options;
using SlateAi.Api.Data.Entities;

namespace SlateAi.Api.Ai;

/// <summary>Records every LLM call, successful or not, in <c>AiCall</c> (CLAUDE.md: every call is logged).</summary>
public sealed class LoggedLlmClient(ILlmClient inner, IAiCallStore store, IOptions<AiOptions> options, TimeProvider clock)
    : ILlmClient
{
    public async Task<LlmResponse> CompleteJsonAsync(LlmRequest request, CancellationToken cancellationToken)
    {
        var call = new AiCall
        {
            Id = Guid.NewGuid(),
            CreatedAt = clock.GetUtcNow(),
            Purpose = request.Prompt.Id,
            ChildIds = AiCallScope.Current?.ToArray(),
            PromptVersion = request.Prompt.Version,
            Model = options.Value.Models.GetValueOrDefault(request.Prompt.ModelKey) ?? "(not configured)",
        };
        var started = clock.GetTimestamp();
        try
        {
            var response = await inner.CompleteJsonAsync(request, cancellationToken);
            call.Model = response.Model;
            call.InputTokens = response.Usage.InputTokens;
            call.OutputTokens = response.Usage.OutputTokens;
            call.CacheReadTokens = response.Usage.CacheReadTokens;
            return response with { AiCallId = call.Id };
        }
        catch (LlmException e)
        {
            call.Error = AiCallText.Error($"{e.Failure}: {e.InnerException?.GetType().Name ?? e.Message}");
            throw;
        }
        catch (Exception e)
        {
            call.Error = e.GetType().Name;
            throw;
        }
        finally
        {
            call.LatencyMs = (int)clock.GetElapsedTime(started).TotalMilliseconds;
            // A cancelled request still gets logged; the log write itself must not be cancelled.
            await store.AddAsync(call, CancellationToken.None);
        }
    }
}
