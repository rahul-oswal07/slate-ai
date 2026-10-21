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
            .Select(t => new { t.Id, t.DisplayName, t.ConsentAt })
            .SingleOrDefaultAsync(cancellationToken);
        if (teacher is null)
        {
            return null;
        }

        // One class per teacher in the MVP.
        var classId = await db.Classes
            .Where(c => c.TeacherId == teacherId)
            .Select(c => (Guid?)c.Id)
            .FirstOrDefaultAsync(cancellationToken);
        return new MeResponse(teacher.Id, teacher.DisplayName, classId, teacher.ConsentAt is not null);
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
