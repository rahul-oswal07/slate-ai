using System.Collections.Concurrent;

namespace SlateAi.Api.Features.Attendance.Source;

/// <summary>
/// Class/date pairs the demo endpoint is holding back as "SATS not in yet". In memory, so it lasts until
/// the app restarts; demo and staging only.
/// </summary>
public sealed class SatsDemoHolds
{
    private readonly ConcurrentDictionary<(Guid ClassId, DateOnly Date), byte> _held = new();

    public void Hold(Guid classId, DateOnly date) => _held[(classId, date)] = 0;

    public void Release(Guid classId, DateOnly date) => _held.TryRemove((classId, date), out _);

    public bool IsHeld(Guid classId, DateOnly date) => _held.ContainsKey((classId, date));
}
