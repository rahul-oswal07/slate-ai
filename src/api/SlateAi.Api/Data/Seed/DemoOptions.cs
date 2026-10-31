namespace SlateAi.Api.Data.Seed;

public sealed class DemoOptions
{
    public const string Section = "Demo";

    /// <summary>
    /// PIN for the seeded demo teacher (4-6 digits). From user-secrets or an App Service setting
    /// (<c>Demo__TeacherPin</c>); hashed into the database by the seeder, never stored in clear.
    /// </summary>
    public string? TeacherPin { get; set; }

    /// <summary>
    /// PIN for the seeded co-teachers who share the demo class (4-6 digits). From user-secrets or an App Service
    /// setting (<c>Demo__CoTeacherPin</c>); hashed by the seeder, never stored in clear.
    /// </summary>
    public string? CoTeacherPin { get; set; }

    /// <summary>
    /// Maps the demo-shell endpoints (e.g. <c>POST /api/demo/sats-feed</c>). On by default: judges use the
    /// demo shell (FR-14). Set <c>Demo__Enabled=false</c> for a real school.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// The pre-made catch-up the demo Reset restores as Ready (T-18, risk R5), relative to the app's output directory.
    /// Missing file: Reset skips it and every catch-up is generated live.
    /// </summary>
    public string SnapshotFile { get; set; } = "data/seed/demo-catchup.json";

    /// <summary>
    /// Which story the Reset writes: <see cref="JudgesStory"/> (the default, the guide on /demo) or
    /// <see cref="VideoStory"/> for recording the demo video (T-70, D-85; local and staging only, never prod).
    /// </summary>
    public string Story { get; set; } = JudgesStory;

    /// <summary>
    /// Fixes the school date the app treats as today (T-75, D-88; staging only, never prod): with the video story on
    /// Sat 31 Oct 2026 the whole week is behind on any day. Null (the default) is the real date.
    /// </summary>
    public DateOnly? Today { get; set; }

    /// <summary>The fixed date, only while the demo is enabled.</summary>
    public DateOnly? FixedToday => Enabled ? Today : null;

    public const string JudgesStory = "judges";
    public const string VideoStory = "video";

    public bool IsVideo => Story == VideoStory;
}
