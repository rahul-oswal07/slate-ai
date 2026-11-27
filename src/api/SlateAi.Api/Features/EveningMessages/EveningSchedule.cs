using SlateAi.Api.Features.LessonAudio;
using SlateAi.Api.Features.TodaysMessage;

namespace SlateAi.Api.Features.EveningMessages;

/// <summary>What the 6 pm job is waiting for (<see cref="EveningDayResponse.Waiting"/>).</summary>
public static class EveningWaiting
{
    /// <summary>Today's message (or a ticked lesson clip) is being written: no known end yet.</summary>
    public const string Writing = "writing";

    /// <summary>A ready today's message (or clip) waits for her approval until <see cref="EveningNext.GoesAt"/>.</summary>
    public const string ForYou = "forYou";
}

/// <param name="GoesAt">When the job sends if she does nothing; null while something is still being written.</param>
/// <param name="Waiting">What holds it (<see cref="EveningWaiting"/>), or null.</param>
public sealed record EveningNext(DateTimeOffset? GoesAt, string? Waiting)
{
    /// <summary>The job holds this class now (it never sends before the send time either).</summary>
    public bool Holds(DateTimeOffset now) => Waiting == EveningWaiting.Writing || now < GoesAt;
}

/// <summary>
/// When a class's evening messages go (T-86): the send time, held while today's message is written and then
/// <see cref="EveningOptions.TodaysMessageWait"/> for her approval (T-111, D-100), and the same for a ticked lesson clip
/// (T-82, T-85). The 6 pm job and Today's card both read this, so the countdown she sees is the job's hold.
/// </summary>
public static class EveningSchedule
{
    public static EveningNext Next(
        IEnumerable<LessonAudioRow> clips, TodaysMessageRow? todays, DateTimeOffset now, DateTimeOffset sendAt, EveningOptions options)
    {
        var clipList = clips as IReadOnlyCollection<LessonAudioRow> ?? clips.ToList();
        if (LessonAudioWait.IsWriting(clipList, now) || TodaysMessageWait.IsWriting(todays, now))
        {
            return new EveningNext(null, EveningWaiting.Writing);
        }

        var until = Later(
            LessonAudioWait.ReadyUntil(clipList, sendAt, options.LessonAudioWait),
            TodaysMessageWait.ReadyUntil(todays, sendAt, options.TodaysMessageWait));
        return until is { } held && now < held
            ? new EveningNext(held, EveningWaiting.ForYou)
            : new EveningNext(sendAt, null);
    }

    private static DateTimeOffset? Later(DateTimeOffset? a, DateTimeOffset? b) =>
        a is null ? b : b is null ? a : a > b ? a : b;
}
