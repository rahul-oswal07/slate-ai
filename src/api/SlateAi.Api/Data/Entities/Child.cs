namespace SlateAi.Api.Data.Entities;

public sealed class Child
{
    public Guid Id { get; set; }
    public Guid ClassId { get; set; }
    public int RollNo { get; set; }
    public required string FirstName { get; set; }

    /// <summary>Home language code: "kn", "te", "ta" or "hi" (English is a family language too, D-73).</summary>
    public required string HomeLanguage { get; set; }

    public string? ParentPhone { get; set; }
    public DateTimeOffset? ParentConsentAt { get; set; }
    public bool HasPhone { get; set; }

    /// <summary>
    /// The family's paper consent form as the teacher recorded it: "yes", "no" or "nophone" (D-39). A form that
    /// hasn't come back counts as "no". Nothing is sent unless "yes" (FR-15).
    /// </summary>
    public string Consent { get; set; } = "no";

    public DateTimeOffset? ConsentChangedAt { get; set; }

    /// <summary>
    /// The family tapped "Stop these messages" (D-19). Separate from <see cref="Consent"/>; restarting clears it.
    /// Every send path refuses a stopped child.
    /// </summary>
    public DateTimeOffset? ParentStoppedAt { get; set; }
}
