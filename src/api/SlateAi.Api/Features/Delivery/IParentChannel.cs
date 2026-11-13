namespace SlateAi.Api.Features.Delivery;

public static class DeliveryKinds
{
    public const string Evening = "evening";
    public const string CatchUp = "catchup";
}

public static class DeliveryStatus
{
    public const string Pending = "pending";
    public const string Sent = "sent";
    public const string Skipped = "skipped";
    public const string Failed = "failed";
}

/// <summary>
/// One message to one family, as the channel needs it. Built by code from stored, teacher-approved records
/// (guardrail 3); names are allowed here because this is the final send (guardrail 1).
/// </summary>
/// <param name="Parameters">The template's body parameters in order: evening {child, lesson, page}; catch-up {child, teacher}.</param>
/// <param name="Token">The parent link token: the URL button opens /p/{token}.</param>
/// <param name="WordingVersion">Evening only: the version of the wording the teacher approved (MessageTemplates.Version).</param>
public sealed record DeliveryMessage(
    Guid DeliveryId,
    Guid ChildId,
    string Kind,
    string Language,
    IReadOnlyList<string> Parameters,
    string Token,
    string? WordingVersion = null);

/// <param name="Outcome">A <see cref="DeliveryStatus"/> value, or <see cref="ChannelOutcome.Retry"/>.</param>
/// <param name="Error">A short code, never message text or a number.</param>
public sealed record ChannelResult(string Outcome, string? ProviderMessageId = null, string? Error = null)
{
    public static ChannelResult Sent(string id) => new(DeliveryStatus.Sent, id);
    public static ChannelResult Skipped(string reason) => new(DeliveryStatus.Skipped, Error: reason);
    public static ChannelResult Failed(string error) => new(DeliveryStatus.Failed, Error: error);
    public static ChannelResult Retry(string error) => new(ChannelOutcome.Retry, Error: error);
}

public static class ChannelOutcome
{
    /// <summary>A failure worth trying again on the next tick (network, 5xx, rate limit).</summary>
    public const string Retry = "retry";
}

/// <summary>Takes a message to a family's phone. <see cref="Name"/> is stored with the delivery.</summary>
public interface IParentChannel
{
    string Name { get; }

    Task<ChannelResult> SendAsync(DeliveryMessage message, CancellationToken cancellationToken);
}

/// <summary>The demo chat reads sent messages by token, so there's nothing to send: every delivery is skipped.</summary>
public sealed class DemoChannel : IParentChannel
{
    public string Name => DeliveryChannels.Demo;

    public Task<ChannelResult> SendAsync(DeliveryMessage message, CancellationToken cancellationToken) =>
        Task.FromResult(ChannelResult.Skipped("demo"));
}
