using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SlateAi.Api.Features.Delivery;
using SlateAi.Api.Tests.Infrastructure;

namespace SlateAi.Api.Tests.Features.Delivery;

public sealed class DeliveryDispatcherTests
{
    private static readonly CancellationToken Ct = TestContext.Current.CancellationToken;
    private static readonly DateTimeOffset Now = new(2026, 11, 13, 12, 35, 0, TimeSpan.Zero);

    private readonly FakeDeliveryStore _store = new();
    private readonly RecordingChannel _channel = new();

    private DeliveryDispatcher Dispatcher(int maxAttempts = 3) => new(
        _store, _channel, Options.Create(new DeliveryOptions { MaxAttempts = maxAttempts }), new FixedClock(Now),
        NullLogger<DeliveryDispatcher>.Instance);

    private PendingDelivery Queue(string consent = "yes", bool stopped = false, DateTimeOffset? expiresAt = null, int attempts = 0)
    {
        var id = Guid.NewGuid();
        var pending = new PendingDelivery(id, attempts, consent, stopped, expiresAt ?? Now.AddDays(14), new DeliveryMessage(
            id, Guid.NewGuid(), DeliveryKinds.Evening, "kn", ["Asha", "ವ್ಯವಕಲನ", "ಪುಟ 15"], "tok"));
        _store.Pending.Add(pending);
        return pending;
    }

    [Fact]
    public async Task Sends_each_pending_delivery_once_and_records_the_provider_id()
    {
        var delivery = Queue();

        Assert.Equal(1, await Dispatcher().RunOnceAsync(Ct));
        Assert.Equal(0, await Dispatcher().RunOnceAsync(Ct));

        Assert.Equal(delivery.Message, Assert.Single(_channel.Sent));
        var update = Assert.Single(_store.Updates);
        Assert.Equal(new DeliveryUpdate(delivery.Id, DeliveryStatus.Sent, "recording", "wamid.1", null, Now), update);
    }

    [Theory]
    [InlineData("yes", true, "stopped")]
    [InlineData("no", false, "no-consent")]
    [InlineData("nophone", false, "no-consent")]
    public async Task A_family_who_stopped_or_lost_consent_after_queueing_gets_nothing(string consent, bool stopped, string reason)
    {
        // Guardrail 3 is checked again at send time: a Stop tapped between 6:00 and the send still wins.
        var delivery = Queue(consent, stopped);

        await Dispatcher().RunOnceAsync(Ct);

        Assert.Empty(_channel.Sent);
        Assert.Equal(new DeliveryUpdate(delivery.Id, DeliveryStatus.Skipped, "recording", null, reason, null), Assert.Single(_store.Updates));
    }

    [Fact]
    public async Task An_expired_link_is_not_sent()
    {
        Queue(expiresAt: Now);

        await Dispatcher().RunOnceAsync(Ct);

        Assert.Empty(_channel.Sent);
        Assert.Equal("expired", Assert.Single(_store.Updates).Error);
    }

    [Fact]
    public async Task A_temporary_failure_stays_pending_until_the_last_attempt_then_fails()
    {
        var delivery = Queue();
        _channel.Next = ChannelResult.Retry("http-502");

        await Dispatcher(maxAttempts: 2).RunOnceAsync(Ct);
        Assert.Equal(DeliveryStatus.Pending, _store.Updates[^1].Status);
        Assert.Single(_store.Pending);

        await Dispatcher(maxAttempts: 2).RunOnceAsync(Ct);
        Assert.Equal(new DeliveryUpdate(delivery.Id, DeliveryStatus.Failed, "recording", null, "http-502", null), _store.Updates[^1]);
        Assert.Empty(_store.Pending);
        Assert.Equal(2, _channel.Sent.Count);
    }

    [Fact]
    public async Task A_channel_exception_is_retried_and_does_not_stop_the_others()
    {
        Queue();
        Queue();
        _channel.ThrowOnce = true;

        Assert.Equal(2, await Dispatcher().RunOnceAsync(Ct));

        Assert.Equal([DeliveryStatus.Pending, DeliveryStatus.Sent], _store.Updates.Select(u => u.Status));
        Assert.Equal("exception", _store.Updates[0].Error);
    }

    [Fact]
    public async Task A_delivery_claimed_by_another_dispatcher_is_left_alone()
    {
        var delivery = Queue();
        _store.ClaimedElsewhere.Add(delivery.Id);

        Assert.Equal(0, await Dispatcher().RunOnceAsync(Ct));
        Assert.Empty(_channel.Sent);
        Assert.Empty(_store.Updates);
    }

    [Fact]
    public async Task The_demo_channel_skips_everything()
    {
        Queue();
        var dispatcher = new DeliveryDispatcher(
            _store, new DemoChannel(), Options.Create(new DeliveryOptions()), new FixedClock(Now), NullLogger<DeliveryDispatcher>.Instance);

        await dispatcher.RunOnceAsync(Ct);

        var update = Assert.Single(_store.Updates);
        Assert.Equal((DeliveryStatus.Skipped, DeliveryChannels.Demo), (update.Status, update.Channel));
    }

    private sealed class RecordingChannel : IParentChannel
    {
        public List<DeliveryMessage> Sent { get; } = [];
        public ChannelResult? Next { get; set; }
        public bool ThrowOnce { get; set; }

        public string Name => "recording";

        public Task<ChannelResult> SendAsync(DeliveryMessage message, CancellationToken cancellationToken)
        {
            if (ThrowOnce)
            {
                ThrowOnce = false;
                throw new InvalidOperationException("boom");
            }

            Sent.Add(message);
            return Task.FromResult(Next ?? ChannelResult.Sent($"wamid.{Sent.Count}"));
        }
    }
}
