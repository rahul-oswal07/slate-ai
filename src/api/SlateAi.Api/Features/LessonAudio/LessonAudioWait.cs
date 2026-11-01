namespace SlateAi.Api.Features.LessonAudio;

/// <summary>
/// Whether the 6 pm job holds a class's evening messages for the lesson audio (T-82, FR-23). Once a ticked day is sent
/// the clip can't be approved any more, so the job waits while a clip is still being made or is ready for her to
/// listen to, until <c>EveningMessages:LessonAudioWaitUntil</c>. Approved, failed or unticked means no wait.
/// </summary>
public static class LessonAudioWait
{
    public static bool Holds(IEnumerable<LessonAudioRow> rows, DateTimeOffset now, DateTimeOffset waitUntil) =>
        now < waitUntil && rows.Any(r => r.Status == LessonAudioStatus.Ready || IsGenerating(r, now));

    // Same reading as the card: a row generating for longer than this (app restarted mid-way) has failed.
    private static bool IsGenerating(LessonAudioRow row, DateTimeOffset now) =>
        row.Status == LessonAudioStatus.Generating && now - row.UpdatedAt <= LessonAudioService.StaleGenerating;
}
