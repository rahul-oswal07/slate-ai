namespace SlateAi.Api.Features.LessonAudio;

/// <summary>
/// Whether the 6 pm job holds a class's evening messages for the lesson audio (T-82, T-85, FR-23). Once a ticked day is
/// sent the clip can't be approved any more, so the job waits while a clip is still being made, and gives a ready clip
/// she hasn't checked <c>EveningMessages:LessonAudioWait</c> (5 minutes) from when it was ready, or from the send time if
/// it was ready earlier. Then the plain message goes. Approved, failed or unticked means no wait.
/// </summary>
public static class LessonAudioWait
{
    public static bool Holds(IEnumerable<LessonAudioRow> rows, DateTimeOffset now, DateTimeOffset sendAt, TimeSpan wait) =>
        rows.Any(r => IsGenerating(r, now) || (r.Status == LessonAudioStatus.Ready && now < Later(sendAt, r.UpdatedAt) + wait));

    // Same reading as the card: a row generating for longer than this (app restarted mid-way) has failed.
    private static bool IsGenerating(LessonAudioRow row, DateTimeOffset now) =>
        row.Status == LessonAudioStatus.Generating && now - row.UpdatedAt <= LessonAudioService.StaleGenerating;

    private static DateTimeOffset Later(DateTimeOffset a, DateTimeOffset b) => a > b ? a : b;
}
