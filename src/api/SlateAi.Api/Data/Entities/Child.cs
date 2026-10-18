namespace SlateAi.Api.Data.Entities;

public sealed class Child
{
    public Guid Id { get; set; }
    public Guid ClassId { get; set; }
    public int RollNo { get; set; }
    public required string FirstName { get; set; }

    /// <summary>Home language code: "kn", "te" or "ta".</summary>
    public required string HomeLanguage { get; set; }

    public string? ParentPhone { get; set; }
    public DateTimeOffset? ParentConsentAt { get; set; }
    public bool HasPhone { get; set; }
}
