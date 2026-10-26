using SlateAi.Api.Features.LessonAudio;

namespace SlateAi.Api.Tests.Features.LessonAudio;

/// <summary>In-memory <see cref="ILessonAudioStore"/> with the same rules as the EF one (approve only a ready row's current clip).</summary>
public sealed class FakeLessonAudioStore : ILessonAudioStore
{
    private readonly Lock _gate = new();

    public List<Row> Rows { get; } = [];

    public sealed class Row
    {
        public required Guid Id { get; init; }
        public required Guid ClassId { get; init; }
        public required DateOnly Date { get; init; }
        public required string Language { get; init; }
        public Guid? ClipId { get; set; }
        public string Status { get; set; } = LessonAudioStatus.Generating;
        public string? Verdict { get; set; }
        public DateTimeOffset? ApprovedAt { get; set; }
        public Guid? ApprovedClipId { get; set; }
        public Guid? ApprovedBy { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }

        public LessonAudioRow ToRow() => new(Id, ClassId, Date, Language, ClipId, Status, Verdict, ApprovedAt, UpdatedAt);
    }

    /// <summary>What <c>EveningStore.GetApprovedLessonAudioAsync</c> returns: approved rows whose clip is still the approved one.</summary>
    public IReadOnlyDictionary<string, Guid> ApprovedFor(DateOnly date)
    {
        lock (_gate)
        {
            return Rows.Where(r => r.Date == date && r.Status == LessonAudioStatus.Approved && r.ApprovedClipId == r.ClipId)
                .ToDictionary(r => r.Language, r => r.Id);
        }
    }

    public Task<IReadOnlyList<LessonAudioRow>> GetDayAsync(Guid classId, DateOnly date, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            return Task.FromResult<IReadOnlyList<LessonAudioRow>>(
                Rows.Where(r => r.ClassId == classId && r.Date == date).OrderBy(r => r.Language).Select(r => r.ToRow()).ToList());
        }
    }

    public Task<LessonAudioRow?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            return Task.FromResult(Rows.FirstOrDefault(r => r.Id == id)?.ToRow());
        }
    }

    public Task<IReadOnlyList<Guid>> StartAsync(
        Guid classId, DateOnly date, IReadOnlyCollection<string> languages, DateTimeOffset at, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            var start = new List<Guid>();
            foreach (var language in languages)
            {
                var row = Rows.FirstOrDefault(r => r.ClassId == classId && r.Date == date && r.Language == language);
                if (row is null)
                {
                    row = new Row { Id = Guid.NewGuid(), ClassId = classId, Date = date, Language = language, UpdatedAt = at };
                    Rows.Add(row);
                    start.Add(row.Id);
                }
                else if (row.Status == LessonAudioStatus.Failed)
                {
                    Reset(row, at);
                    start.Add(row.Id);
                }
            }

            return Task.FromResult<IReadOnlyList<Guid>>(start);
        }
    }

    public Task RemoveAsync(Guid classId, DateOnly date, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            Rows.RemoveAll(r => r.ClassId == classId && r.Date == date);
            return Task.CompletedTask;
        }
    }

    public Task<Guid?> RestartAsync(Guid classId, DateOnly date, string language, DateTimeOffset at, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            var row = Rows.FirstOrDefault(r => r.ClassId == classId && r.Date == date && r.Language == language);
            if (row is not null)
            {
                Reset(row, at);
            }

            return Task.FromResult(row?.Id);
        }
    }

    public Task SetResultAsync(Guid id, Guid? clipId, string status, string? verdict, DateTimeOffset at, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (Rows.FirstOrDefault(r => r.Id == id) is { Status: LessonAudioStatus.Generating } row)
            {
                (row.ClipId, row.Status, row.Verdict, row.UpdatedAt) = (clipId, status, verdict, at);
            }

            return Task.CompletedTask;
        }
    }

    public List<Guid> Replays { get; } = [];

    public Task<LessonAudioApproval> ApproveAsync(
        Guid classId, DateOnly date, string language, Guid clipId, Guid clientRequestId, Guid teacherId, DateTimeOffset at,
        CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (Replays.Contains(clientRequestId))
            {
                return Task.FromResult(LessonAudioApproval.Replay);
            }

            var row = Rows.FirstOrDefault(r => r.ClassId == classId && r.Date == date && r.Language == language);
            if (row is null)
            {
                return Task.FromResult(LessonAudioApproval.NotFound);
            }

            if (row.Status != LessonAudioStatus.Ready || row.ClipId != clipId)
            {
                return Task.FromResult(LessonAudioApproval.Stale);
            }

            (row.Status, row.ApprovedAt, row.ApprovedClipId, row.ApprovedBy, row.UpdatedAt) =
                (LessonAudioStatus.Approved, at, clipId, teacherId, at);
            Replays.Add(clientRequestId);
            return Task.FromResult(LessonAudioApproval.Approved);
        }
    }

    private static void Reset(Row row, DateTimeOffset at) =>
        (row.Status, row.ClipId, row.Verdict, row.ApprovedAt, row.ApprovedClipId, row.ApprovedBy, row.UpdatedAt) =
            (LessonAudioStatus.Generating, null, null, null, null, null, at);
}
