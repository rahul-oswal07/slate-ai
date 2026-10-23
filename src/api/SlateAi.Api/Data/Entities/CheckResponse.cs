namespace SlateAi.Api.Data.Entities;

/// <summary>
/// The family's picture tap on P3, scored by code (FR-11): one per assignment, and the first answer counts (D-62).
/// The family never sees the outcome (D-20); the teacher's ledger does.
/// </summary>
public sealed class CheckResponse
{
    public Guid AssignmentId { get; set; }

    /// <summary>The position of the picture tapped (0–2), as the parent page showed them.</summary>
    public int ChoiceId { get; set; }

    public bool Correct { get; set; }

    /// <summary>"caughtUp" or "needsBuddy".</summary>
    public required string Outcome { get; set; }

    public Guid ClientRequestId { get; set; }
    public DateTimeOffset AnsweredAt { get; set; }
}
