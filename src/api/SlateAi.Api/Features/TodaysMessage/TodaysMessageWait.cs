using SlateAi.Api.Features.CatchUps;

namespace SlateAi.Api.Features.TodaysMessage;

/// <summary>
/// Whether the 6 pm job should hold a class's evening messages for today's message (T-111, D-100): while a draft is
/// being written, and for a draft she hasn't approved <c>EveningMessages:TodaysMessageWait</c> (5 minutes) from when it
/// was ready, or from the send time if it was ready earlier. Every edit is a new version with a new
/// <see cref="TodaysMessageRow.UpdatedAt"/>, so the wait starts again. Approved, failed, stuck or already sent: no hold.
/// The same rule as <see cref="LessonAudio.LessonAudioWait"/> for a lesson clip.
/// </summary>
public static class TodaysMessageWait
{
    public static bool Holds(TodaysMessageRow? row, DateTimeOffset now, DateTimeOffset sendAt, TimeSpan wait) =>
        IsWriting(row, now) || now < ReadyUntil(row, sendAt, wait);

    /// <summary>Being written, and not stuck: the hold has no known end yet.</summary>
    public static bool IsWriting(TodaysMessageRow? row, DateTimeOffset now) =>
        // Same reading as the service: a draft generating for longer than this (app restarted mid-way) has failed.
        row is { Sent: false, Status: TodaysMessageStatus.Generating } && now - row.UpdatedAt <= StaleGenerating.After;

    /// <summary>
    /// A ready draft she hasn't approved: the job holds until this time, then the plain message goes. Null when there is
    /// nothing to wait for (no draft, approved, failed, writing or sent).
    /// </summary>
    public static DateTimeOffset? ReadyUntil(TodaysMessageRow? row, DateTimeOffset sendAt, TimeSpan wait) =>
        row is { Sent: false, Status: TodaysMessageStatus.Ready } ? Later(sendAt, row.UpdatedAt) + wait : null;

    private static DateTimeOffset Later(DateTimeOffset a, DateTimeOffset b) => a > b ? a : b;
}
