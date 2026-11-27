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
/// Seeded teachers' names always follow the file (no screen edits them), so a renamed login reaches existing databases
/// on the next start (T-72: co-teacher Lakshmi became Savitha, apart from child Lakshmi).
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

        teacher.DisplayName = seed.Teacher.DisplayName;

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

            coTeacher.DisplayName = co.DisplayName;

            SetLogin(coTeacher, co.Phone, demo.Value.CoTeacherPin, "Demo:CoTeacherPin");
            coTeacherIds.Add(co.Id);
        }

        added += await SeedClassAsync(
            seed.Class.Id, seed.Teacher.Id, seed.Class.Name, seed.Class.Grade, seed.Class.Medium, seed.Children,
            coTeacherIds.Prepend(seed.Teacher.Id).ToList(), cancellationToken);
        // More fictional classes (T-115), e.g. Monika's own 3B: she then signs in to two classes.
        foreach (var other in seed.OtherClasses ?? [])
        {
            added += await SeedClassAsync(
                other.Id, other.TeacherId, other.Name, other.Grade, other.Medium, other.Children, [other.TeacherId],
                cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        logger.LogInformation("Demo seed applied: {Added} rows added", added);
    }

    /// <summary>
    /// One class, its children and who can sign in to it (<paramref name="members"/>, main teacher first). Returns the
    /// rows added.
    /// </summary>
    private async Task<int> SeedClassAsync(
        Guid classId,
        Guid mainTeacherId,
        string? name,
        int grade,
        string medium,
        IReadOnlyList<DemoSeedFile.SeedChild> children,
        IReadOnlyList<Guid> members,
        CancellationToken cancellationToken)
    {
        var added = 0;

        // Children are added only with a new class: a child the teacher deleted (FR-15) must not come back on the next
        // start or deploy. The demo Reset (T-18) re-seeds on purpose.
        var stored = await db.Classes.SingleOrDefaultAsync(c => c.Id == classId, cancellationToken);
        var newClass = stored is null;
        if (stored is null)
        {
            db.Classes.Add(new Class
            {
                Id = classId,
                TeacherId = mainTeacherId,
                Name = name ?? "",
                Grade = grade,
                Medium = medium,
            });
            added++;
        }
        else if (stored.Name == "" && !string.IsNullOrEmpty(name))
        {
            // A class seeded before names existed (T-115) gets the file's name once; a name set later is kept.
            stored.Name = name;
        }

        var existing = await db.Children
            .Where(c => c.ClassId == classId)
            .ToDictionaryAsync(c => c.Id, cancellationToken);

        foreach (var child in children)
        {
            if (existing.TryGetValue(child.Id, out var storedChild))
            {
                if (storedChild.ConsentChangedAt is null)
                {
                    storedChild.Consent = child.Consent ?? "no";
                }

                continue;
            }

            if (!newClass)
            {
                continue;
            }

            db.Children.Add(child.ToEntity(classId));
            added++;
        }

        // Who can sign in to the class: its main teacher and the co-teachers. Added even when the class already exists.
        var current = await db.ClassTeachers.Where(m => m.ClassId == classId).Select(m => m.TeacherId)
            .ToListAsync(cancellationToken);
        foreach (var teacherId in members.Except(current))
        {
            db.ClassTeachers.Add(new ClassTeacher { ClassId = classId, TeacherId = teacherId });
            added++;
        }

        return added;
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
