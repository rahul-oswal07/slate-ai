namespace SlateAi.Api.Curriculum;

public sealed class CurriculumOptions
{
    public const string Section = "Curriculum";

    /// <summary>Folder of curriculum JSON files, relative to the app's output directory.</summary>
    public string Directory { get; set; } = "data/curriculum";
}
