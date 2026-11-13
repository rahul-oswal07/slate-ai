namespace SlateAi.Api.Data.Entities;

/// <summary>
/// The outbox for a message to a family's phone (T-40, FR-16). Written in the same transaction as the evening message
/// or the catch-up token it carries, so only what already passed guardrail 3 is ever queued; the 6 pm job then sends
/// it over the configured channel. Exactly one of <see cref="EveningMessageId"/> and <see cref="CatchUpAssignmentId"/>
/// is set. Holds no names, numbers or message text: those are looked up at send time.
/// </summary>
public sealed class ParentDelivery
{
    public Guid Id { get; set; }
    public Guid ClassId { get; set; }
    public Guid ChildId { get; set; }
    public Guid? EveningMessageId { get; set; }
    public Guid? CatchUpAssignmentId { get; set; }

    /// <summary>"pending", "sent", "skipped" (demo channel, or no registered number) or "failed".</summary>
    public required string Status { get; set; }

    /// <summary>"demo" or "whatsapp": the channel that settled it; null while pending.</summary>
    public string? Channel { get; set; }

    /// <summary>WhatsApp's message ID (wamid) once accepted.</summary>
    public string? ProviderMessageId { get; set; }

    public int Attempts { get; set; }

    /// <summary>A short error code from the last failed attempt (e.g. "http-500", "graph-131026"); never message text.</summary>
    public string? LastError { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? SentAt { get; set; }
}
