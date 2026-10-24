namespace SlateAi.Api.Data.Entities;

/// <summary>
/// One evening message sent to one family for one day of absence (FR-20). At most one per child per day.
/// <see cref="Token"/> opens the parent view; listens are not tracked (D-36).
/// </summary>
public sealed class EveningMessage
{
    public Guid Id { get; set; }
    public Guid ClassId { get; set; }
    public Guid ChildId { get; set; }
    public DateOnly Date { get; set; }
    public Guid TemplateId { get; set; }
    public Guid LessonId { get; set; }

    /// <summary>"kn", "te", "ta", "en" or "hi": the child's home language when it was sent.</summary>
    public required string Language { get; set; }

    /// <summary>The code-filled slot values as JSON (lesson names and page; the child's name is never stored here).</summary>
    public required string SlotValues { get; set; }

    /// <summary>128-bit random, base64url. The only key to the parent view.</summary>
    public required string Token { get; set; }

    public DateTimeOffset SentAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
}
