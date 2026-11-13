using SlateAi.Api.Features.Delivery;

namespace SlateAi.Api.Tests.Features.Delivery;

/// <summary>An in-memory outbox. <see cref="FakeApi"/> registers an empty one so no default test reaches Postgres.</summary>
public sealed class FakeDeliveryStore : IDeliveryStore
{
    private readonly Lock _gate = new();

    public List<PendingDelivery> Pending { get; } = [];
    public List<DeliveryUpdate> Updates { get; } = [];
    public int Runs { get; private set; }

    /// <summary>Simulates another dispatcher taking the delivery first.</summary>
    public HashSet<Guid> ClaimedElsewhere { get; } = [];

    public Task<IReadOnlyList<PendingDelivery>> GetPendingAsync(int limit, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            Runs++;
            return Task.FromResult<IReadOnlyList<PendingDelivery>>(Pending.Take(limit).ToList());
        }
    }

    public Task<bool> TryClaimAsync(Guid id, int attempts, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            var index = Pending.FindIndex(p => p.Id == id && p.Attempts == attempts);
            if (index < 0 || ClaimedElsewhere.Contains(id))
            {
                return Task.FromResult(false);
            }

            Pending[index] = Pending[index] with { Attempts = attempts + 1 };
            return Task.FromResult(true);
        }
    }

    public Task MarkAsync(DeliveryUpdate update, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            Updates.Add(update);
            if (update.Status != DeliveryStatus.Pending)
            {
                Pending.RemoveAll(p => p.Id == update.Id);
            }

            return Task.CompletedTask;
        }
    }
}
