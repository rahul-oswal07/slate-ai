namespace SlateAi.Api.Features.Delivery;

/// <summary>
/// "Approve and send now" (T-117, D-109): after an approval was applied, runs its send straight away and delivers what
/// it queued, instead of waiting for the 6 pm job. It never decides what may go: the send it runs keeps its own rules
/// (guardrail 3: only what she approved at its current version), and callers only call it after an approval was applied.
/// </summary>
public sealed class SendNow(DeliveryDispatcher delivery)
{
    public async Task RunAsync(Func<CancellationToken, Task> send, CancellationToken cancellationToken)
    {
        await send(cancellationToken);
        // Families' phones get it now rather than on the next minute's tick (T-40); demo channel: nothing to deliver.
        await delivery.RunOnceAsync(cancellationToken);
    }
}
