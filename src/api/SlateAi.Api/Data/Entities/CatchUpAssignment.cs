namespace SlateAi.Api.Data.Entities;

/// <summary>One child's catch-up for a week: which package, which lessons were missed and why these targets (A2).</summary>
public sealed class CatchUpAssignment
{
    public Guid Id { get; set; }
    public Guid ClassId { get; set; }
    public Guid ChildId { get; set; }
    public Guid PackageId { get; set; }
    public DateOnly WeekOf { get; set; }

    /// <summary>Curriculum IDs the child missed that week (A2 works on competencies, not lesson rows).</summary>
    public List<string> MissedCompetencyIds { get; set; } = [];

    /// <summary>JSON: A2's targets and dropped lessons with reasons, source (ai/rule) and verdict ("Why this", T3).</summary>
    public required string CompressionRationale { get; set; }

    /// <summary>Parent link token, set when sent (T-14/T-15).</summary>
    public string? Token { get; set; }

    public DateTimeOffset? ScheduledAt { get; set; }
    public DateTimeOffset? SentAt { get; set; }
    public DateTimeOffset? OpenedAt { get; set; }

    /// <summary>The parent link stops working after this (spec §4.4: 14 days).</summary>
    public DateTimeOffset? ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
