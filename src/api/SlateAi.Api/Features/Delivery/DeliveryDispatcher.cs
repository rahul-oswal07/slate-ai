using Microsoft.Extensions.Options;

namespace SlateAi.Api.Features.Delivery;

/// <summary>The send rules checked again at delivery time. Pure.</summary>
public static class DeliveryRules
{
    /// <summary>
    /// Why a queued message must not go now, or null. A family who stopped (D-19) or whose consent changed after the
    /// message was queued gets nothing (guardrail 3); an expired link is not worth sending.
    /// </summary>
    public static string? Block(string consent, bool stopped, DateTimeOffset? expiresAt, DateTimeOffset now) =>
        stopped ? "stopped"
        : consent != "yes" ? "no-consent"
        : expiresAt is { } expiry && expiry <= now ? "expired"
        : null;
}

/// <summary>
/// Sends queued deliveries over the configured channel (T-40). Run by the 6 pm job every minute and by the demo's
/// "Send messages now". Each delivery is claimed before sending, so it goes at most once; a temporary failure is
/// retried on the next run up to <see cref="DeliveryOptions.MaxAttempts"/>. Never throws for one bad message.
/// </summary>
public sealed class DeliveryDispatcher(
    IDeliveryStore store,
    IParentChannel channel,
    IOptions<DeliveryOptions> options,
    TimeProvider clock,
    ILogger<DeliveryDispatcher> logger)
{
    /// <summary>Returns how many deliveries were settled or retried in this run.</summary>
    public async Task<int> RunOnceAsync(CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var handled = 0;
        foreach (var pending in await store.GetPendingAsync(settings.BatchSize, cancellationToken))
        {
            if (!await store.TryClaimAsync(pending.Id, pending.Attempts, cancellationToken))
            {
                continue;
            }

            var attempts = pending.Attempts + 1;
            var now = clock.GetUtcNow();
            ChannelResult result;
            if (DeliveryRules.Block(pending.Consent, pending.Stopped, pending.ExpiresAt, now) is { } reason)
            {
                result = ChannelResult.Skipped(reason);
            }
            else
            {
                try
                {
                    result = await channel.SendAsync(pending.Message, cancellationToken);
                }
                catch (Exception e) when (e is not OperationCanceledException)
                {
                    logger.LogError(e, "Delivery {DeliveryId} threw", pending.Id);
                    result = ChannelResult.Retry("exception");
                }
            }

            var status = result.Outcome == ChannelOutcome.Retry
                ? attempts >= settings.MaxAttempts ? DeliveryStatus.Failed : DeliveryStatus.Pending
                : result.Outcome;
            await store.MarkAsync(
                new DeliveryUpdate(
                    pending.Id, status, channel.Name, result.ProviderMessageId, result.Error,
                    status == DeliveryStatus.Sent ? now : null),
                cancellationToken);

            // IDs and codes only (NFR-05): never the name, number or text.
            logger.LogInformation(
                "Delivery {DeliveryId} ({Kind}, child {ChildId}) over {Channel}: {Status} {Error} (attempt {Attempt})",
                pending.Id, pending.Message.Kind, pending.Message.ChildId, channel.Name, status, result.Error, attempts);
            handled++;
        }

        return handled;
    }
}
