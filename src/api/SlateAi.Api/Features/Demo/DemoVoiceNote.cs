using Microsoft.Extensions.Options;
using SlateAi.Api.Data;
using SlateAi.Api.Data.Seed;

namespace SlateAi.Api.Features.Demo;

/// <summary>What the teacher app does differently for the demo teacher (T-93). Null for everyone else.</summary>
/// <param name="SendOnApprove">Approve sends at once (D-94), so the app says "sent", not "goes after 6 pm".</param>
/// <param name="SampleVoiceNote">Today offers "Use the sample voice note" (<c>GET /api/demo/voice-note</c>).</param>
public sealed record DemoFlags(bool SendOnApprove, bool SampleVoiceNote);

public static class DemoVoiceNote
{
    /// <summary>The sample clip's full path, or null when the file isn't there.</summary>
    public static string? Path(DemoOptions demo)
    {
        var path = System.IO.Path.Combine(AppContext.BaseDirectory, demo.VoiceNoteFile);
        return File.Exists(path) ? path : null;
    }

    /// <summary>The demo flags when <paramref name="classId"/> is the seeded demo class and the demo is on; else null.</summary>
    public static async Task<DemoFlags?> FlagsAsync(
        Guid? classId, DemoOptions demo, DatabaseOptions database, DemoSendOnApprove sendOnApprove, CancellationToken cancellationToken)
    {
        if (!demo.Enabled || classId is null)
        {
            return null;
        }

        var seed = await DemoSeedFile.LoadAsync(database.SeedFile, cancellationToken);
        return seed.Class.Id == classId ? new DemoFlags(sendOnApprove.Enabled, Path(demo) is not null) : null;
    }
}
