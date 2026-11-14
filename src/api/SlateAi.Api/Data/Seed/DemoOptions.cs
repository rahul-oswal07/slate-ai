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
    /// The sample Kannada voice note about Nalikali p 4 (T-93), relative to the app's output directory. The demo
    /// teacher's Today offers "Use the sample voice note", which sends it through the real STT and A1 path. Missing
    /// file: the button is hidden.
    /// </summary>
    public string VoiceNoteFile { get; set; } = "data/seed/demo-voice-note.webm";

    /// <summary>
    /// Approving on this server sends at once instead of at 6 pm (D-94, T-93), so the family phone gets it during the
    /// 3-minute demo. Same approval rules (guardrail 3): only what she approved at its current version goes. Off by
    /// default; on in Development and on staging (<c>Demo__SendOnApprove=true</c>); prod keeps 6 pm.
    /// </summary>
    public bool SendOnApprove { get; set; }
}
