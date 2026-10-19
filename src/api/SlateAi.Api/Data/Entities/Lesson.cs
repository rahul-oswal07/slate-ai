namespace SlateAi.Api.Data.Entities;

/// <summary>
/// What was taught on a day. Minimal for T-06 so the ledger can join on it; T-07 adds the
/// transcript, confidence and the capture endpoint.
/// </summary>
public sealed class Lesson
{
    public Guid Id { get; set; }
    public Guid ClassId { get; set; }
    public DateOnly Date { get; set; }

    /// <summary>"voice", "photo" or "picked".</summary>
    public required string Source { get; set; }

    /// <summary>Curriculum IDs such as "M3.ADD.CARRY".</summary>
    public List<string> CompetencyIds { get; set; } = [];

    public string? SummaryKn { get; set; }

    /// <summary>"pending", "mapped" or "confirmed".</summary>
    public required string Status { get; set; }
}
