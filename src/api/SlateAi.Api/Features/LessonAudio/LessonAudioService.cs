using SlateAi.Api.Features.Audio;
using SlateAi.Api.Features.CatchUps;
using SlateAi.Api.Features.EveningMessages;

namespace SlateAi.Api.Features.LessonAudio;

/// <param name="Children">Families who get this language's clip tonight.</param>
/// <param name="Segments">Disclosure then lesson, as the teacher's own player plays them (<c>/api/audio/{id}</c>).</param>
public sealed record LessonAudioItem(
    string Language,
    string Status,
    int Children,
    Guid? ClipId,
    IReadOnlyList<AudioSegment> Segments,
    double Seconds,
    string? Transcript,
    DateTimeOffset? ApprovedAt);

/// <param name="Available">She can tick it: the lesson is confirmed, the template approved, someone to message, nothing sent yet.</param>
/// <param name="Enabled">Ticked (the day has rows).</param>
/// <param name="Sent">Tonight's messages have gone: the day is read-only.</param>
public sealed record LessonAudioDayResponse(
    DateOnly Date, bool Available, bool Enabled, bool Sent, IReadOnlyList<LessonAudioItem> Items);

public enum LessonAudioOutcome
{
    Ok,

    /// <summary>Not allowed now: nothing to send, already sent, or the clip she was shown is not the current one.</summary>
    Conflict,

    NotFound,
}

/// <summary>
/// The optional lesson audio of the evening message (T-62): tick, wait for the clip, listen, approve. Nothing here sends
/// anything: <see cref="EveningSender"/> attaches only an approved clip, code only (guardrail 3).
/// </summary>
public sealed class LessonAudioService(
    EveningSender sender,
    IEveningStore evening,
    ILessonAudioStore store,
    IAudioStore audio,
    DisclosureClips disclosure,
    LessonAudioQueue queue,
    TimeProvider clock)
{
    /// <summary>A row still "generating" after this (app restarted mid-way) reads as failed.</summary>
    public static readonly TimeSpan StaleGenerating = CatchUpEndpoints.StaleGenerating;

    public async Task<LessonAudioDayResponse?> GetAsync(Guid classId, DateOnly date, CancellationToken cancellationToken)
    {
        if (await sender.PlanAsync(classId, date, cancellationToken) is not { } plan
            || await evening.GetClassAsync(classId, cancellationToken) is not { } group)
        {
            return null;
        }

        var languages = Recipients(plan, group).GroupBy(l => l).ToDictionary(g => g.Key, g => g.Count());
        var now = clock.GetUtcNow();
        var items = new List<LessonAudioItem>();
        foreach (var row in await store.GetDayAsync(classId, date, cancellationToken))
        {
            items.Add(await ItemAsync(row, languages.GetValueOrDefault(row.Language), now, cancellationToken));
        }

        var sent = plan.Sent > 0;
        return new LessonAudioDayResponse(date, !sent && plan.Blocked is null && plan.Families > 0, items.Count > 0, sent, items);
    }

    /// <summary>Ticks (creates a row per language of tonight's families and starts making the clips) or unticks.</summary>
    public async Task<LessonAudioOutcome> SetAsync(Guid classId, DateOnly date, bool enabled, CancellationToken cancellationToken)
    {
        if (await sender.PlanAsync(classId, date, cancellationToken) is not { } plan
            || await evening.GetClassAsync(classId, cancellationToken) is not { } group)
        {
            return LessonAudioOutcome.NotFound;
        }

        if (plan.Sent > 0)
        {
            return LessonAudioOutcome.Conflict;
        }

        if (!enabled)
        {
            await store.RemoveAsync(classId, date, cancellationToken);
            return LessonAudioOutcome.Ok;
        }

        var languages = Recipients(plan, group).Distinct().ToList();
        if (plan.Blocked is not null || languages.Count == 0)
        {
            return LessonAudioOutcome.Conflict;
        }

        foreach (var id in await store.StartAsync(classId, date, languages, clock.GetUtcNow(), cancellationToken))
        {
            queue.TryEnqueue(new LessonAudioJob(id, Fresh: false));
        }

        return LessonAudioOutcome.Ok;
    }

    public async Task<LessonAudioOutcome> ApproveAsync(
        Guid classId, DateOnly date, string language, Guid clipId, Guid clientRequestId, Guid teacherId, CancellationToken cancellationToken)
    {
        if (await sender.PlanAsync(classId, date, cancellationToken) is not { } plan)
        {
            return LessonAudioOutcome.NotFound;
        }

        if (plan.Sent > 0)
        {
            return LessonAudioOutcome.Conflict;
        }

        return await store.ApproveAsync(classId, date, language, clipId, clientRequestId, teacherId, clock.GetUtcNow(), cancellationToken) switch
        {
            LessonAudioApproval.Approved or LessonAudioApproval.Replay => LessonAudioOutcome.Ok,
            LessonAudioApproval.NotFound => LessonAudioOutcome.NotFound,
            _ => LessonAudioOutcome.Conflict,
        };
    }

    public async Task<LessonAudioOutcome> RegenerateAsync(Guid classId, DateOnly date, string language, CancellationToken cancellationToken)
    {
        if (await sender.PlanAsync(classId, date, cancellationToken) is not { } plan)
        {
            return LessonAudioOutcome.NotFound;
        }

        if (plan.Sent > 0)
        {
            return LessonAudioOutcome.Conflict;
        }

        if (await store.RestartAsync(classId, date, language, clock.GetUtcNow(), cancellationToken) is not { } id)
        {
            return LessonAudioOutcome.NotFound;
        }

        queue.TryEnqueue(new LessonAudioJob(id, Fresh: true));
        return LessonAudioOutcome.Ok;
    }

    /// <summary>The home language of every child who gets (or got) tonight's evening message.</summary>
    private static IEnumerable<string> Recipients(EveningDayResponse plan, EveningClass group)
    {
        var chosen = plan.Children.Where(c => c.Outcome is EveningOutcome.WillSend or EveningOutcome.Sent).Select(c => c.ChildId).ToHashSet();
        return group.Children.Where(c => chosen.Contains(c.Id)).Select(c => c.HomeLanguage);
    }

    private async Task<LessonAudioItem> ItemAsync(LessonAudioRow row, int children, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var status = row.Status == LessonAudioStatus.Generating && now - row.UpdatedAt > StaleGenerating ? LessonAudioStatus.Failed : row.Status;
        if (row.ClipId is not { } clipId || await audio.GetInfoAsync(clipId, cancellationToken) is not { } clip)
        {
            return new LessonAudioItem(row.Language, status, children, null, [], 0, null, row.ApprovedAt);
        }

        var segments = new List<AudioSegment>();
        if (await disclosure.FindAsync(row.Language, cancellationToken) is { } intro)
        {
            segments.Add(new AudioSegment(AudioKind.Disclosure, AudioSegments.Url(intro.Id), Math.Round(intro.Seconds, 1)));
        }

        // "script" is what the players call the lesson part; the clip's own kind is "lesson".
        segments.Add(new AudioSegment(AudioKind.Script, AudioSegments.Url(clip.Id), Math.Round(clip.Seconds, 1)));
        return new LessonAudioItem(
            row.Language, status, children, clip.Id, segments, Math.Round(segments.Sum(s => s.Seconds), 1),
            await audio.GetTranscriptAsync(clip.Id, cancellationToken), row.ApprovedAt);
    }
}
