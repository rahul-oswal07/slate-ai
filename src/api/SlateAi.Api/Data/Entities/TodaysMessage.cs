namespace SlateAi.Api.Data.Entities;

/// <summary>
/// Today's message for one class and day (D-91, T-88): the concept, the parent-led activity, the picture question and
/// later the video and her note, in Kannada for her review and in each absent family's language. Drafted by A9 when she
/// confirms the lesson; nothing in it reaches a family until she approves this exact <see cref="Version"/> (guardrail 3).
/// The evening message carries it (<see cref="EveningMessage.TodaysMessageId"/>); unapproved means the plain message.
/// </summary>
public sealed class TodaysMessage
{
    public Guid Id { get; set; }
    public Guid ClassId { get; set; }
    public DateOnly Date { get; set; }

    /// <summary>The confirmed lesson it was written from.</summary>
    public List<string> CompetencyIds { get; set; } = [];

    /// <summary>generating, ready, approved or failed (<c>TodaysMessageStatus</c>).</summary>
    public required string Status { get; set; }

    /// <summary>Goes up on every new draft and every edit (remove, restore); approval is bound to it.</summary>
    public int Version { get; set; }

    /// <summary>The version she approved. Only an approved row whose <see cref="ApprovedVersion"/> equals <see cref="Version"/> is sent.</summary>
    public int? ApprovedVersion { get; set; }

    public DateTimeOffset? ApprovedAt { get; set; }
    public Guid? ApprovedByTeacherId { get; set; }

    /// <summary>valid, fallback:* or error:* (validator or voice verdict, no text).</summary>
    public string? Verdict { get; set; }

    /// <summary>The sections as JSON (<c>TodaysSections</c>): text and clip IDs per language, numbers by code. No names.</summary>
    public required string Sections { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
