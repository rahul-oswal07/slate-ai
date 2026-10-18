using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SlateAi.Api.Data.Entities;

namespace SlateAi.Api.Data.Seed;

/// <summary>
/// Loads the fictional demo class from JSON. Idempotent: rows are keyed by the fixed IDs in the
/// file and only missing rows are inserted. Logs counts only, never names (NFR-05).
/// </summary>
public sealed class DemoSeeder(AppDbContext db, IOptions<DatabaseOptions> options, ILogger<DemoSeeder> logger)
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

        if (!await db.Teachers.AnyAsync(t => t.Id == seed.Teacher.Id, cancellationToken))
        {
            db.Teachers.Add(new Teacher { Id = seed.Teacher.Id, DisplayName = seed.Teacher.DisplayName });
            added++;
        }

        if (!await db.Classes.AnyAsync(c => c.Id == seed.Class.Id, cancellationToken))
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

        var existing = (await db.Children
                .Where(c => c.ClassId == seed.Class.Id)
                .Select(c => c.Id)
                .ToListAsync(cancellationToken))
            .ToHashSet();

        foreach (var child in seed.Children.Where(c => !existing.Contains(c.Id)))
        {
            db.Children.Add(new Child
            {
                Id = child.Id,
                ClassId = seed.Class.Id,
                RollNo = child.RollNo,
                FirstName = child.FirstName,
                HomeLanguage = child.HomeLanguage,
                HasPhone = child.HasPhone,
                ParentPhone = child.ParentPhone,
            });
            added++;
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        logger.LogInformation("Demo seed applied: {Added} rows added", added);
    }

    private async Task<DemoSeed> LoadAsync(CancellationToken cancellationToken)
    {
        var path = Path.Combine(AppContext.BaseDirectory, options.Value.SeedFile);
        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<DemoSeed>(stream, JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException($"Seed file '{path}' is empty.");
    }

    private sealed record DemoSeed(SeedTeacher Teacher, SeedClass Class, IReadOnlyList<SeedChild> Children);

    private sealed record SeedTeacher(Guid Id, string DisplayName);

    private sealed record SeedClass(Guid Id, int Grade, string Medium);

    private sealed record SeedChild(
        Guid Id, int RollNo, string FirstName, string HomeLanguage, bool HasPhone, string? ParentPhone);
}
