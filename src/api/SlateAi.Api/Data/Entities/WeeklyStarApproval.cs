namespace SlateAi.Api.Data.Entities;

/// <summary>
/// The teacher's one approval of the weekly star wording (T-125, D-118, guardrail 3), per class. It counts only while
/// <see cref="Version"/> matches <c>data/seed/weekly-star.json</c>: a new version asks her again (D-49).
/// </summary>
public sealed class WeeklyStarApproval
{
    public Guid ClassId { get; set; }
    public required string Version { get; set; }
    public DateTimeOffset ApprovedAt { get; set; }
    public Guid ApprovedByTeacherId { get; set; }
}
