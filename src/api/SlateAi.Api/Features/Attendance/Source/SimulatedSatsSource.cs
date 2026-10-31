using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace SlateAi.Api.Features.Attendance.Source;

/// <summary>
/// Stand-in for the SATS feed (D-69). The same class and date always give the same register: a stable hash
/// per child marks about <see cref="SatsOptions.AbsenceRate"/> absent, plus the scripted weekly absences
/// for the demo story. Labelled "SATS (simulated)" in the app.
/// </summary>
public sealed class SimulatedSatsSource : IAttendanceSource
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly SatsOptions _options;
    private readonly Lazy<IReadOnlyList<WeeklyAbsence>> _scenario;

    public SimulatedSatsSource(IOptions<SatsOptions> options)
    {
        _options = options.Value;
        _scenario = new Lazy<IReadOnlyList<WeeklyAbsence>>(LoadScenario);
    }

    public Task<SourceResult> FetchAsync(
        Guid classId, DateOnly date, IReadOnlyList<RosterChild> roster, CancellationToken cancellationToken)
    {
        SourceResult result = IsSchoolDay(date) || _options.EveryDayClassIds.Contains(classId)
            ? new SourceResult.Arrived(Simulate(classId, date, roster), SchoolClock.At(date, _options.ArrivesAt))
            : new SourceResult.NoSchool();
        return Task.FromResult(result);
    }

    /// <summary>The school calendar; <see cref="SatsOptions.EveryDayClassIds"/> still get a register on other days.</summary>
    public bool IsSchoolDay(DateOnly date) => SchoolCalendar.IsSchoolDay(date, _options.Holidays);

    /// <summary>The simulated absentees, ignoring the school calendar (the demo endpoint uses it directly).</summary>
    public IReadOnlySet<Guid> Simulate(Guid classId, DateOnly date, IReadOnlyList<RosterChild> roster)
    {
        var scripted = _scenario.Value
            .Where(w => w.Days.Contains(date.DayOfWeek))
            .Select(w => w.RollNo)
            .ToHashSet();
        return roster
            .Where(child => scripted.Contains(child.RollNo) || Draw(classId, date, child.Id) < _options.AbsenceRate)
            .Select(child => child.Id)
            .ToHashSet();
    }

    /// <summary>A number in [0, 1) that depends only on its inputs.</summary>
    private static double Draw(Guid classId, DateOnly date, Guid childId)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"{classId}:{date:yyyy-MM-dd}:{childId}"));
        return BitConverter.ToUInt64(hash, 0) / (ulong.MaxValue + 1.0);
    }

    private IReadOnlyList<WeeklyAbsence> LoadScenario()
    {
        var path = Path.Combine(AppContext.BaseDirectory, _options.ScenarioFile);
        if (!File.Exists(path))
        {
            return [];
        }

        using var stream = File.OpenRead(path);
        return JsonSerializer.Deserialize<Scenario>(stream, JsonOptions)?.Weekly ?? [];
    }

    private sealed record Scenario(IReadOnlyList<WeeklyAbsence> Weekly);

    private sealed record WeeklyAbsence(int RollNo, IReadOnlyList<DayOfWeek> Days);
}
