using SlateAi.Api.Features.TodaysMessage;

namespace SlateAi.Api.Features.Lessons;

/// <summary>
/// When a new voice note may replace the day's confirmed lesson (D-104, changing D-55): until today's message is
/// approved, or, on a day without one, until tonight's message has gone. After that she uses Change. Pure.
/// </summary>
public static class LessonWindow
{
    /// <param name="todays">The day's today's message, or null when there is none.</param>
    /// <param name="eveningSent">Tonight's message has gone to at least one family.</param>
    public static bool CanReplaceConfirmed(TodaysMessageRow? todays, bool eveningSent)
    {
        if (eveningSent || todays is { Sent: true })
        {
            return false;
        }

        // Approved counts only at its current version: an edit clears it (D-91).
        return todays is not { Status: TodaysMessageStatus.Approved } row || row.ApprovedVersion != row.Version;
    }
}
