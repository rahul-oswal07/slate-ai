using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SlateAi.Api.Data.Entities;
using SlateAi.Api.Features.Auth;

namespace SlateAi.Api.Data.Seed;

/// <summary>
/// Loads the fictional demo class from JSON. Idempotent: rows are keyed by the fixed IDs in the
/// file and only missing rows are inserted. Logs counts only, never names (NFR-05). The demo teacher's
/// phone and PIN are filled in only while empty, so a PIN changed later is never overwritten. A child's consent comes
/// from the file until the teacher records it herself (<c>ConsentChangedAt</c> set), so existing databases get it too.
/// </summary>
public sealed class DemoSeeder(
    AppDbContext db,
    IOptions<DatabaseOptions> options,
    IOptions<DemoOptions> demo,
    IPasswordHasher<Teacher> hasher,
    ILogger<DemoSeeder> logger)
{
    /// <summary>Arbitrary app-wide key for pg_advisory_xact_lock; serialises seeding (and the demo Reset) across instances.</summary>
    public const long SeedLockKey = 0x5EED_0001;

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        var seed = await DemoSeedFile.LoadAsync(options.Value.SeedFile, cancellationToken);
        var added = 0;

        // Two instances can start at once (e.g. a slot swap); the lock makes the existence checks below safe.
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.Database.ExecuteSqlAsync($"SELECT pg_advisory_xact_lock({SeedLockKey})", cancellationToken);

        var teacher = await db.Teachers.SingleOrDefaultAsync(t => t.Id == seed.Teacher.Id, cancellationToken);
        if (teacher is null)
        {
            teacher = new Teacher { Id = seed.Teacher.Id, DisplayName = seed.Teacher.DisplayName };
            db.Teachers.Add(teacher);
            added++;
        }

        SetLogin(teacher, seed.Teacher.Phone, demo.Value.TeacherPin, "Demo:TeacherPin");

        var coTeacherIds = new List<Guid>();
        foreach (var co in seed.CoTeachers ?? [])
        {
            var coTeacher = await db.Teachers.SingleOrDefaultAsync(t => t.Id == co.Id, cancellationToken);
            if (coTeacher is null)
            {
                coTeacher = new Teacher { Id = co.Id, DisplayName = co.DisplayName };
                db.Teachers.Add(coTeacher);
                added++;
            }

            SetLogin(coTeacher, co.Phone, demo.Value.CoTeacherPin, "Demo:CoTeacherPin");
            coTeacherIds.Add(co.Id);
        }

        // Children are added only with a new class: a child the teacher deleted (FR-15) must not come back on the next
        // start or deploy. The demo Reset (T-18) re-seeds on purpose.
        var newClass = !await db.Classes.AnyAsync(c => c.Id == seed.Class.Id, cancellationToken);
        if (newClass)
        {
            db.Classes.Add(new Class
            {
                Id = seed.Class.Id,
                TeacherId = seed.Teacher.Id,
                Grade = seed.Class.Grade,
                Medium = seed.Class.Medium,
            });
            added++;
        }

        var existing = await db.Children
            .Where(c => c.ClassId == seed.Class.Id)
            .ToDictionaryAsync(c => c.Id, cancellationToken);

        foreach (var child in seed.Children)
        {
            if (existing.TryGetValue(child.Id, out var stored))
            {
                if (stored.ConsentChangedAt is null)
                {
                    stored.Consent = child.Consent ?? "no";
                }

                continue;
            }

            if (!newClass)
            {
                continue;
            }

            db.Children.Add(child.ToEntity(seed.Class.Id));
            added++;
        }

        // Who can sign in to the class: its main teacher and the co-teachers. Added even when the class already exists.
        var members = await db.ClassTeachers.Where(m => m.ClassId == seed.Class.Id).Select(m => m.TeacherId)
            .ToListAsync(cancellationToken);
        foreach (var teacherId in coTeacherIds.Prepend(seed.Teacher.Id).Except(members))
        {
            db.ClassTeachers.Add(new ClassTeacher { ClassId = seed.Class.Id, TeacherId = teacherId });
            added++;
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        logger.LogInformation("Demo seed applied: {Added} rows added", added);
    }

    private void SetLogin(Teacher teacher, string? phone, string? pin, string setting)
    {
        teacher.Phone ??= PhoneNumber.Normalise(phone);
        if (teacher.PinHash is not null)
        {
            return;
        }

        if (pin is { Length: >= 4 and <= 6 } && pin.All(char.IsAsciiDigit))
        {
            teacher.PinHash = hasher.HashPassword(teacher, pin);
        }
        else
        {
            logger.LogWarning("{Setting} is not set to 4-6 digits; that demo teacher can't sign in", setting);
        }
    }
}
