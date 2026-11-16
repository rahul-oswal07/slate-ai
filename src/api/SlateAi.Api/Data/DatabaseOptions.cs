namespace SlateAi.Api.Data;

public sealed class DatabaseOptions
{
    public const string Section = "Database";

    /// <summary>Apply EF migrations at startup. Seeding only runs when this is on.</summary>
    public bool MigrateOnStartup { get; set; } = true;

    public bool SeedOnStartup { get; set; } = true;

    /// <summary>Seed file path, relative to the app's output directory.</summary>
    public string SeedFile { get; set; } = "data/seed/demo-class.json";

    /// <summary>Lesson descriptions and examples loaded at startup (T-141), relative to the app's output directory.</summary>
    public string LessonContentFile { get; set; } = "data/lesson-content/grade3-maths.json";
}
