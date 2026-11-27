using SlateAi.Api.Features.Lessons;
using SlateAi.Api.Features.Lessons.Voice;

namespace SlateAi.Api.Tests.Features.Lessons;

/// <summary>One class's lessons in memory. Locked: the mapping worker writes from a background thread.</summary>
public sealed class FakeLessonStore : ILessonStore
{
    private readonly object _gate = new();
    private readonly HashSet<Guid> _processed = [];

    public Dictionary<DateOnly, LessonRecord> Lessons { get; } = [];

    public ClassNames Names { get; set; } = new(["Ravi", "Ananya"], "Shobha");

    public int Starts { get; private set; }

    public LessonRecord? Day(DateOnly date)
    {
        lock (_gate)
        {
            return Lessons.GetValueOrDefault(date);
        }
    }

    /// <summary>Runs before each <see cref="GetDayAsync"/>: lets a test hold a read back until the worker has written.</summary>
    public Func<Task>? BeforeGetDay { get; set; }

    public async Task<LessonRecord?> GetDayAsync(Guid classId, DateOnly date, CancellationToken cancellationToken)
    {
        if (BeforeGetDay is { } wait)
        {
            await wait();
        }

        return Day(date);
    }

    public Task<IReadOnlyList<string>> LastConfirmedBeforeAsync(Guid classId, DateOnly date, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            return Task.FromResult(Lessons.Where(l => l.Key < date && l.Value.Status == LessonValues.Confirmed)
                .OrderByDescending(l => l.Key).Select(l => l.Value.CompetencyIds).FirstOrDefault() ?? []);
        }
    }

    public Task<LessonRecord> SaveConfirmedAsync(
        Guid classId, Guid clientRequestId, DateOnly date, IReadOnlyList<string> competencyIds, string source,
        LessonFeedbackRecord? feedback, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (_processed.Add(clientRequestId))
            {
                if (feedback is not null
                    && Lessons.GetValueOrDefault(date) is { Status: LessonValues.Mapped or LessonValues.Low } lesson
                    && lesson.MappingStartedAt == feedback.MappingStartedAt)
                {
                    Feedback.Add(new SavedLessonFeedback(Guid.NewGuid(), classId, DateTimeOffset.UnixEpoch, feedback));
                }

                Lessons[date] = new LessonRecord(competencyIds, source, LessonValues.Confirmed);
            }

            return Task.FromResult(Lessons[date]);
        }
    }

    /// <summary>Eval cases saved by confirming over a match (T-102), oldest first.</summary>
    public List<SavedLessonFeedback> Feedback { get; } = [];

    public Task<IReadOnlyList<SavedLessonFeedback>> ListFeedbackAsync(DateOnly? from, int limit, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            return Task.FromResult<IReadOnlyList<SavedLessonFeedback>>(
                Feedback.Where(f => from is null || f.Feedback.Date >= from).Reverse().Take(limit).ToList());
        }
    }

    public Task<VoiceStart> StartVoiceAsync(
        Guid classId, Guid clientRequestId, DateOnly date, DateTimeOffset startedAt, bool replaceConfirmed, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (_processed.Contains(clientRequestId))
            {
                return Task.FromResult(VoiceStart.Replay);
            }

            if (Lessons.GetValueOrDefault(date)?.Status == LessonValues.Confirmed && !replaceConfirmed)
            {
                return Task.FromResult(VoiceStart.AlreadyConfirmed);
            }

            _processed.Add(clientRequestId);
            Starts++;
            Lessons[date] = new LessonRecord([], LessonValues.Voice, LessonValues.Processing, MappingStartedAt: startedAt);
            return Task.FromResult(VoiceStart.Started);
        }
    }

    public Task SaveMappingAsync(
        Guid classId, DateOnly date, DateTimeOffset startedAt, A1Result result, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (Lessons.GetValueOrDefault(date) is { Status: LessonValues.Processing } lesson && lesson.MappingStartedAt == startedAt)
            {
                Lessons[date] = lesson with
                {
                    CompetencyIds = result.CompetencyIds,
                    Status = result.Status,
                    SummaryKn = result.SummaryKn,
                    SummaryEn = result.SummaryEn,
                    ExamplesUsed = result.ExamplesUsed,
                    Confidence = result.Confidence,
                };
            }

            return Task.CompletedTask;
        }
    }

    public Task<ClassNames> GetNamesAsync(Guid classId, CancellationToken cancellationToken) => Task.FromResult(Names);
}
