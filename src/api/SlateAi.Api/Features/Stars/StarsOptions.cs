using System.ComponentModel.DataAnnotations;

namespace SlateAi.Api.Features.Stars;

/// <summary>Attendance stars (FR-24, D-81).</summary>
public sealed class StarsOptions
{
    public const string Section = "Stars";

    /// <summary>A school week earns a star with at most this many days away (and at least one day present).</summary>
    [Range(0, 5)]
    public int MaxDaysAwayPerWeek { get; set; } = 1;
}
