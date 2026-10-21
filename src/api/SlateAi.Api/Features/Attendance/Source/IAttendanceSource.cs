namespace SlateAi.Api.Features.Attendance.Source;

public sealed record RosterChild(Guid Id, int RollNo, string FirstName);

/// <summary>What the school's attendance system says about one class and date.</summary>
public abstract record SourceResult
{
    /// <summary>The register is in: these children are absent, everyone else present.</summary>
    public sealed record Arrived(IReadOnlySet<Guid> AbsentChildIds, DateTimeOffset ReceivedAt) : SourceResult;

    /// <summary>A school day, but nothing has come in yet.</summary>
    public sealed record NotYet : SourceResult;

    /// <summary>Sunday, second Saturday or a holiday: no register, and never an absence.</summary>
    public sealed record NoSchool : SourceResult;
}

/// <summary>
/// The school's attendance system (D-27, D-41): the simulated SATS feed now; a file import or state API later.
/// Read only. Corrections stay in our database and are never written back.
/// </summary>
public interface IAttendanceSource
{
    Task<SourceResult> FetchAsync(
        Guid classId, DateOnly date, IReadOnlyList<RosterChild> roster, CancellationToken cancellationToken);
}
