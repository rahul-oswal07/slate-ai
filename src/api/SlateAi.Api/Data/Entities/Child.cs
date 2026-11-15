namespace SlateAi.Api.Data.Entities;

public sealed class Child
{
    public Guid Id { get; set; }
    public Guid ClassId { get; set; }
    public int RollNo { get; set; }
    public required string FirstName { get; set; }

    /// <summary>Home language code: "kn", "en", "te", "ta" or "hi" (D-73). Can be changed on the admin page (T-122, D-113).</summary>
    public required string HomeLanguage { get; set; }

    /// <summary>When <see cref="HomeLanguage"/> was changed on the admin page; null while it is the seed's (T-122, D-113).</summary>
    public DateTimeOffset? HomeLanguageSetAt { get; set; }

    /// <summary>
    /// The family's number (E.164). Seeded values are fictional and never dialled; only a number saved on the admin page
    /// (<see cref="ParentPhoneSetAt"/> set) gets WhatsApp (T-118, D-110).
    /// </summary>
    public string? ParentPhone { get; set; }

    /// <summary>When <see cref="ParentPhone"/> was saved on the admin page; null for a seeded or cleared number.</summary>
    public DateTimeOffset? ParentPhoneSetAt { get; set; }

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
