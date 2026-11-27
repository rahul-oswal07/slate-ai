using Microsoft.EntityFrameworkCore;
using SlateAi.Api.Data;

namespace SlateAi.Api.Features.Auth;

/// <summary>What sign-in needs to know about a teacher found by phone.</summary>
public sealed record TeacherLogin(Guid TeacherId, string? PinHash, int FailedPinAttempts, DateTimeOffset? PinLockedUntil);

/// <summary>Database side of sign-in, kept behind an interface so endpoint tests can use a fake.</summary>
public interface IAuthStore
{
    /// <summary>The teacher with this normalised phone number, or null.</summary>
    Task<TeacherLogin?> FindByPhoneAsync(string phone, CancellationToken cancellationToken);

    /// <summary>Stores the failed-attempt count and, once the limit is reached, the lock end.</summary>
    Task SetPinFailuresAsync(
        Guid teacherId, int failedAttempts, DateTimeOffset? lockedUntil, CancellationToken cancellationToken);

    /// <summary>The signed-in teacher, or null if they no longer exist.</summary>
    Task<MeResponse?> GetMeAsync(Guid teacherId, CancellationToken cancellationToken);

    /// <summary>Records the first-run welcome as done (sets <c>ConsentAt</c> if it is still empty).</summary>
    Task<MeResponse?> MarkWelcomedAsync(Guid teacherId, DateTimeOffset at, CancellationToken cancellationToken);
}

public sealed class AuthStore(AppDbContext db) : IAuthStore
{
    public Task<TeacherLogin?> FindByPhoneAsync(string phone, CancellationToken cancellationToken) =>
        db.Teachers
            .Where(t => t.Phone == phone)
            .Select(t => new TeacherLogin(t.Id, t.PinHash, t.FailedPinAttempts, t.PinLockedUntil))
            .SingleOrDefaultAsync(cancellationToken);

    public Task SetPinFailuresAsync(
        Guid teacherId, int failedAttempts, DateTimeOffset? lockedUntil, CancellationToken cancellationToken) =>
        db.Teachers
            .Where(t => t.Id == teacherId)
            .ExecuteUpdateAsync(
                s => s.SetProperty(t => t.FailedPinAttempts, failedAttempts)
                    .SetProperty(t => t.PinLockedUntil, lockedUntil),
                cancellationToken);

    public async Task<MeResponse?> GetMeAsync(Guid teacherId, CancellationToken cancellationToken)
    {
        var teacher = await db.Teachers
            .Where(t => t.Id == teacherId)
            .Select(t => new { t.Id, t.DisplayName, t.ConsentAt, t.Phone })
            .SingleOrDefaultAsync(cancellationToken);
        if (teacher is null)
        {
            return null;
        }

        // Every class she belongs to (T-115; a class can have several teachers, ClassTeachers). The class she is the
        // main teacher of comes first: that is her default. The header (T-135) says whose class it is: its main teacher,
        // also for a co-teacher.
        var classes = await (
                from m in db.ClassTeachers
                where m.TeacherId == teacherId
                join c in db.Classes on m.ClassId equals c.Id
                join main in db.Teachers on c.TeacherId equals main.Id
                orderby c.TeacherId == teacherId descending, c.Name, c.Grade, c.Id
                select new MeClass(c.Id, c.Name, c.Grade, c.Medium, main.DisplayName, null))
            .ToListAsync(cancellationToken);
        var group = classes.FirstOrDefault();
        var greeting = await db.AudioClips
            .Where(c => c.TeacherId == teacherId && c.Kind == "greeting")
            .Select(c => new GreetingInfo("/api/audio/" + c.Id, Math.Round(c.Seconds, 1)))
            .FirstOrDefaultAsync(cancellationToken);
        // Profile (T-109): only the last 4 digits of her phone.
        return new MeResponse(
            teacher.Id,
            teacher.DisplayName,
            group?.Id,
            teacher.ConsentAt is not null,
            greeting,
            PhoneLast4: PhoneMask.Last4(teacher.Phone),
            Grade: group?.Grade,
            Medium: group?.Medium,
            ClassTeacherName: group?.ClassTeacherName,
            Classes: classes);
    }

    public async Task<MeResponse?> MarkWelcomedAsync(Guid teacherId, DateTimeOffset at, CancellationToken cancellationToken)
    {
        // Only the first call sets it, so a replay keeps the original time.
        await db.Teachers
            .Where(t => t.Id == teacherId && t.ConsentAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.ConsentAt, at), cancellationToken);
        return await GetMeAsync(teacherId, cancellationToken);
    }
}
