using System.Text.Json;
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
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    // Arbitrary app-wide key for pg_advisory_xact_lock; serialises seeding across instances.
    private const long SeedLockKey = 0x5EED_0001;

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        var seed = await LoadAsync(cancellationToken);
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

        SetLogin(teacher, seed.Teacher.Phone);

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

            db.Children.Add(new Child
            {
                Id = child.Id,
                ClassId = seed.Class.Id,
                RollNo = child.RollNo,
                FirstName = child.FirstName,
                HomeLanguage = child.HomeLanguage,
                HasPhone = child.HasPhone,
                ParentPhone = child.ParentPhone,
                Consent = child.Consent ?? "no",
            });
            added++;
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        logger.LogInformation("Demo seed applied: {Added} rows added", added);
    }

    private void SetLogin(Teacher teacher, string? phone)
    {
        teacher.Phone ??= PhoneNumber.Normalise(phone);
        if (teacher.PinHash is not null)
        {
            return;
        }

        var pin = demo.Value.TeacherPin;
        if (pin is { Length: >= 4 and <= 6 } && pin.All(char.IsAsciiDigit))
        {
            teacher.PinHash = hasher.HashPassword(teacher, pin);
        }
        else
        {
            logger.LogWarning("Demo:TeacherPin is not set to 4-6 digits; the demo teacher can't sign in");
        }
    }

    private async Task<DemoSeed> LoadAsync(CancellationToken cancellationToken)
    {
        var path = Path.Combine(AppContext.BaseDirectory, options.Value.SeedFile);
        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<DemoSeed>(stream, JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException($"Seed file '{path}' is empty.");
    }

    private sealed record DemoSeed(SeedTeacher Teacher, SeedClass Class, IReadOnlyList<SeedChild> Children);

    private sealed record SeedTeacher(Guid Id, string DisplayName, string? Phone);

    private sealed record SeedClass(Guid Id, int Grade, string Medium);

    private sealed record SeedChild(
        Guid Id, int RollNo, string FirstName, string HomeLanguage, bool HasPhone, string? ParentPhone, string? Consent);
}
