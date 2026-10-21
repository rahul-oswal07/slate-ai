namespace SlateAi.Api.Data.Seed;

public sealed class DemoOptions
{
    public const string Section = "Demo";

    /// <summary>
    /// PIN for the seeded demo teacher (4-6 digits). From user-secrets or an App Service setting
    /// (<c>Demo__TeacherPin</c>); hashed into the database by the seeder, never stored in clear.
    /// </summary>
    public string? TeacherPin { get; set; }
}
