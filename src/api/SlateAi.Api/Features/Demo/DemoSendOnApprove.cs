using Microsoft.Extensions.Options;
using SlateAi.Api.Data.Seed;
using SlateAi.Api.Features.Delivery;

namespace SlateAi.Api.Features.Demo;

/// <summary>
/// "Approve sends at once" in the demo (D-94, T-93): when <see cref="DemoOptions.SendOnApprove"/> is on, an approval
/// runs its send straight away and delivers what it queued, instead of waiting for the 6 pm job. Catch-up approval
/// uses it now; today's message (T-88) calls it the same way. It never decides what may go: the send it runs keeps its
/// own rules (guardrail 3), and callers only call it after an approval was applied.
/// </summary>
public sealed class DemoSendOnApprove(IOptions<DemoOptions> options, DeliveryDispatcher delivery)
{
    public bool Enabled => options.Value is { Enabled: true, SendOnApprove: true };

    /// <summary>Runs <paramref name="send"/> and then the delivery dispatcher when enabled; otherwise does nothing.</summary>
    public async Task RunAsync(Func<CancellationToken, Task> send, CancellationToken cancellationToken)
    {
        if (!Enabled)
        {
            return;
        }

        await send(cancellationToken);
        // Registered WhatsApp test phones get it now rather than on the next minute's tick (T-40); demo channel: nothing.
        await delivery.RunOnceAsync(cancellationToken);
    }
}
