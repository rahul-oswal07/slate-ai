using Microsoft.Extensions.Options;
using SlateAi.Api.Data;
using SlateAi.Api.Data.Seed;

namespace SlateAi.Api.Features.Demo;

/// <summary>What the teacher app does differently for the demo teacher (T-93). Null for everyone else.</summary>
/// <param name="SampleVoiceNote">Today offers "Use the sample voice note" (<c>GET /api/demo/voice-note</c>).</param>
/// <param name="SchoolDate">The admin switch lets her act as a past school day (T-134, D-120): the web shows its bar.</param>
public sealed record DemoFlags(bool SampleVoiceNote, bool SchoolDate = false);

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
        Guid? classId,
        DemoOptions demo,
        DatabaseOptions database,
        Func<CancellationToken, Task<bool>> schoolDateOn,
        CancellationToken cancellationToken)
    {
        if (!demo.Enabled || classId is null)
        {
            return null;
        }

        var seed = await DemoSeedFile.LoadAsync(database.SeedFile, cancellationToken);
        return seed.Class.Id == classId
            ? new DemoFlags(Path(demo) is not null, await schoolDateOn(cancellationToken))
            : null;
    }
}
