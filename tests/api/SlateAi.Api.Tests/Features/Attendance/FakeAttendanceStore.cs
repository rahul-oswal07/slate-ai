using SlateAi.Api.Features.Attendance;
using SlateAi.Api.Features.Attendance.Source;

namespace SlateAi.Api.Tests.Features.Attendance;

public sealed record SaveCall(Guid ClientRequestId, IReadOnlyCollection<Guid> ChildIds, IReadOnlySet<Guid> Absent);

/// <summary>One class in memory, using the real merge rules, so endpoint tests need no database.</summary>
public sealed class FakeAttendanceStore(Guid classId, IReadOnlyList<Guid> children, TimeProvider? clock = null)
    : IAttendanceStore
{
    private readonly Dictionary<DateOnly, IReadOnlyList<AttendanceRow>> _days = [];
    private readonly HashSet<Guid> _processed = [];

    public IReadOnlyList<RosterChild> Roster { get; } =
        children.Select((id, i) => new RosterChild(id, i + 1, $"Child {i + 1}")).ToList();

    public List<SaveCall> Saved { get; } = [];

    public int SourceSaves { get; private set; }

    public IReadOnlyList<AttendanceRow> Day(DateOnly date) => _days.GetValueOrDefault(date) ?? [];

    public void SetDay(DateOnly date, IReadOnlyList<AttendanceRow> rows) => _days[date] = rows;

    public Task<IReadOnlyList<RosterChild>?> GetRosterAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(id == classId ? Roster : null);

    public Task<IReadOnlyList<AttendanceRow>> GetDayAsync(Guid id, DateOnly date, CancellationToken cancellationToken) =>
        Task.FromResult(Day(date));

    public Task<MarkAttendanceResponse> SaveRegisterAsync(
        Guid id,
        Guid clientRequestId,
        DateOnly date,
        IReadOnlyCollection<Guid> childIds,
        IReadOnlySet<Guid> absent,
        CancellationToken cancellationToken)
    {
        Saved.Add(new SaveCall(clientRequestId, childIds, absent));
        if (_processed.Add(clientRequestId))
        {
            _days[date] = RegisterMerge.FromTeacher(Day(date), childIds, absent, (clock ?? TimeProvider.System).GetUtcNow());
        }

        var rows = Day(date);
        return Task.FromResult(new MarkAttendanceResponse(date, rows.Count(r => r.Present), rows.Count(r => !r.Present)));
    }

    public Task<IReadOnlyList<AttendanceRow>> SaveSourceRegisterAsync(
        Guid id,
        DateOnly date,
        IReadOnlyCollection<Guid> childIds,
        SourceResult.Arrived register,
        CancellationToken cancellationToken)
    {
        if (Day(date).All(r => r.SourcePresent is null))
        {
            SourceSaves++;
            _days[date] = RegisterMerge.FromSource(Day(date), childIds, register.AbsentChildIds, register.ReceivedAt);
        }

        return Task.FromResult(Day(date));
    }

    public Task RemoveDayAsync(Guid id, DateOnly date, CancellationToken cancellationToken)
    {
        _days.Remove(date);
        return Task.CompletedTask;
    }
}
