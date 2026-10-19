namespace SlateAi.Api.Data.Entities;

/// <summary>
/// A write request that has already been applied, keyed by the client-generated ID, so offline
/// replays from the outbox are ignored.
/// </summary>
public sealed class ProcessedRequest
{
    public Guid ClientRequestId { get; set; }
    public required string Endpoint { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
