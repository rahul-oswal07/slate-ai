namespace SlateAi.Api.Data.Entities;

public sealed class Class
{
    public Guid Id { get; set; }
    public Guid TeacherId { get; set; }
    public int Grade { get; set; }

    /// <summary>Medium of instruction as a language code, e.g. "kn".</summary>
    public required string Medium { get; set; }
}
