namespace SlateAi.Api.Data.Entities;

public sealed class Class
{
    public Guid Id { get; set; }
    public Guid TeacherId { get; set; }
    public int Grade { get; set; }

    /// <summary>
    /// The section as the school says it, e.g. "3A" (T-115): two sections of one grade and medium would look the same
    /// without it. Empty for a class that has none yet.
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>Medium of instruction as a language code, e.g. "kn".</summary>
    public required string Medium { get; set; }

    /// <summary>
    /// The published <see cref="StoredCurriculum"/> the class follows (T-145, D-125); null for the built-in curriculum from
    /// <c>data/curriculum/</c>.
    /// </summary>
    public Guid? CurriculumId { get; set; }
}
