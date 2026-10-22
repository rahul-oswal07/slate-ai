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

    public Task<LessonRecord?> GetDayAsync(Guid classId, DateOnly date, CancellationToken cancellationToken) =>
        Task.FromResult(Day(date));

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
        CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (_processed.Add(clientRequestId))
            {
                Lessons[date] = new LessonRecord(competencyIds, source, LessonValues.Confirmed);
            }

            return Task.FromResult(Lessons[date]);
        }
    }

    public Task<VoiceStart> StartVoiceAsync(
        Guid classId, Guid clientRequestId, DateOnly date, DateTimeOffset startedAt, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (_processed.Contains(clientRequestId))
            {
                return Task.FromResult(VoiceStart.Replay);
            }

            if (Lessons.GetValueOrDefault(date)?.Status == LessonValues.Confirmed)
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
                };
            }

            return Task.CompletedTask;
        }
    }

    public Task<ClassNames> GetNamesAsync(Guid classId, CancellationToken cancellationToken) => Task.FromResult(Names);
}
