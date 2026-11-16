namespace SlateAi.Api.Data.Entities;

/// <summary>
/// One weekly star message to one family for one finished week (T-125, D-118): the child was present every school day.
/// At most one per child per week. Holds the approved texts with <c>{child}</c> still in them (the name is filled in
/// when the page is opened, never stored). <see cref="Token"/> opens the page.
/// </summary>
public sealed class WeeklyStarMessage
{
    public Guid Id { get; set; }
    public Guid ClassId { get; set; }
    public Guid ChildId { get; set; }

    /// <summary>Monday of the finished week.</summary>
    public DateOnly WeekOf { get; set; }

    /// <summary>"kn", "te", "ta", "en" or "hi": the child's home language when it was sent.</summary>
    public required string Language { get; set; }

    /// <summary>The version of <c>data/seed/weekly-star.json</c> she approved, which these texts come from.</summary>
    public required string Version { get; set; }

    /// <summary>Screen 1 of the page, with <c>{child}</c>.</summary>
    public required string MessageText { get; set; }

    /// <summary>The badge card, with <c>{child}</c>.</summary>
    public required string BadgeText { get; set; }

    /// <summary>128-bit random, base64url. The only key to the page.</summary>
    public required string Token { get; set; }

    public DateTimeOffset SentAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
}
