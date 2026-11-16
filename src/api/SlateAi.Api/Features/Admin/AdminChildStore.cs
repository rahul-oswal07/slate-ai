using Microsoft.EntityFrameworkCore;
using SlateAi.Api.Data;
using SlateAi.Api.Data.Entities;

namespace SlateAi.Api.Features.Admin;

/// <summary>One child as the admin children page sees it (T-118).</summary>
/// <param name="SavedPhone">Number 1 saved on the admin page; null when none (seeded numbers are never shown).</param>
/// <param name="LanguageChanged">The home language was changed on the admin page (T-122), not the seed's.</param>
/// <param name="SavedPhone2">Number 2 saved on the admin page (T-140, D-122); null when none.</param>
public sealed record AdminChildRow(
    Guid Id, int RollNo, string FirstName, string HomeLanguage, string Consent, bool Stopped, string? SavedPhone,
    bool LanguageChanged, string? SavedPhone2 = null);

/// <summary>A number saved on the admin page and which of the family's two it is (T-140, D-122).</summary>
/// <param name="Slot">1 or 2.</param>
public sealed record SavedNumber(int Slot, string? Phone);

/// <summary>One class with its children by roll number.</summary>
/// <param name="TeacherName">The class's main teacher, for the "Shobha · Grade 3" label.</param>
public sealed record AdminChildClass(Guid Id, string TeacherName, int Grade, IReadOnlyList<AdminChildRow> Children);

/// <summary>Database side of the admin children page (T-118), behind an interface so endpoint tests can use a fake.</summary>
public interface IAdminChildStore
{
    /// <summary>Every class, by main teacher then grade, with its children by roll number.</summary>
    Task<IReadOnlyList<AdminChildClass>> ListAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Saves (<paramref name="phone"/>, E.164) or clears (null) the family's number 1 or 2 (<paramref name="slot"/>,
    /// T-140), leaving the other as it is, unless <paramref name="clientRequestId"/> was already applied. Returns the
    /// child afterwards, or null for an unknown child.
    /// </summary>
    Task<AdminChildRow?> SetPhoneAsync(
        Guid clientRequestId, Guid childId, int slot, string? phone, DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>
    /// Changes the child's home language (T-122, D-113), unless <paramref name="clientRequestId"/> was already applied.
    /// Returns the child afterwards, or null for an unknown child.
    /// </summary>
    Task<AdminChildRow?> SetLanguageAsync(
        Guid clientRequestId, Guid childId, string language, DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>
    /// The numbers saved for the child on the admin page, number 1 first; empty when none (seeded numbers never count,
    /// D-114).
    /// </summary>
    Task<IReadOnlyList<SavedNumber>> GetSavedPhonesAsync(Guid childId, CancellationToken cancellationToken);
}

public sealed class AdminChildStore(AppDbContext db) : IAdminChildStore
{
    private const string Endpoint = "admin-parent-phone";
    private const string LanguageEndpoint = "admin-home-language";

    public async Task<IReadOnlyList<AdminChildClass>> ListAsync(CancellationToken cancellationToken)
    {
        var classes = await (
                from cls in db.Classes.AsNoTracking()
                join teacher in db.Teachers on cls.TeacherId equals teacher.Id
                select new { cls.Id, teacher.DisplayName, cls.Grade })
            .ToListAsync(cancellationToken);
        var children = await db.Children.AsNoTracking()
            .Select(c => new
            {
                c.ClassId,
                Row = new AdminChildRow(
                    c.Id, c.RollNo, c.FirstName, c.HomeLanguage, c.Consent, c.ParentStoppedAt != null,
                    c.ParentPhoneSetAt != null ? c.ParentPhone : null, c.HomeLanguageSetAt != null,
                    c.ParentPhone2SetAt != null ? c.ParentPhone2 : null),
            })
            .ToListAsync(cancellationToken);
        var byClass = children.ToLookup(c => c.ClassId, c => c.Row);
        return classes
            .OrderBy(c => c.DisplayName).ThenBy(c => c.Grade)
            .Select(c => new AdminChildClass(c.Id, c.DisplayName, c.Grade, byClass[c.Id].OrderBy(r => r.RollNo).ToList()))
            .ToList();
    }

    public async Task<AdminChildRow?> SetPhoneAsync(
        Guid clientRequestId, Guid childId, int slot, string? phone, DateTimeOffset now, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        // One lock per child for both numbers: a save of each touches the same row.
        await db.Database.ExecuteSqlAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({$"parent-phone:{childId}"}, 0))", cancellationToken);
        if (await db.Children.SingleOrDefaultAsync(c => c.Id == childId, cancellationToken) is not { } child)
        {
            return null;
        }

        if (!await db.ProcessedRequests.AnyAsync(r => r.ClientRequestId == clientRequestId, cancellationToken))
        {
            if (slot == 2)
            {
                child.ParentPhone2 = phone;
                child.ParentPhone2SetAt = phone is null ? null : now;
            }
            else
            {
                child.ParentPhone = phone;
                child.ParentPhoneSetAt = phone is null ? null : now;
            }

            db.ProcessedRequests.Add(new ProcessedRequest { ClientRequestId = clientRequestId, Endpoint = Endpoint, CreatedAt = now });
            await db.SaveChangesAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return Row(child);
    }

    public async Task<AdminChildRow?> SetLanguageAsync(
        Guid clientRequestId, Guid childId, string language, DateTimeOffset now, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.Database.ExecuteSqlAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({$"home-language:{childId}"}, 0))", cancellationToken);
        if (await db.Children.SingleOrDefaultAsync(c => c.Id == childId, cancellationToken) is not { } child)
        {
            return null;
        }

        if (!await db.ProcessedRequests.AnyAsync(r => r.ClientRequestId == clientRequestId, cancellationToken))
        {
            child.HomeLanguage = language;
            child.HomeLanguageSetAt = now;
            db.ProcessedRequests.Add(new ProcessedRequest { ClientRequestId = clientRequestId, Endpoint = LanguageEndpoint, CreatedAt = now });
            await db.SaveChangesAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return Row(child);
    }

    public async Task<IReadOnlyList<SavedNumber>> GetSavedPhonesAsync(Guid childId, CancellationToken cancellationToken)
    {
        var saved = await db.Children.AsNoTracking()
            .Where(c => c.Id == childId)
            .Select(c => new
            {
                One = c.ParentPhoneSetAt != null ? c.ParentPhone : null,
                Two = c.ParentPhone2SetAt != null ? c.ParentPhone2 : null,
            })
            .SingleOrDefaultAsync(cancellationToken);
        return saved is null
            ? []
            : new[] { new SavedNumber(1, saved.One), new SavedNumber(2, saved.Two) }.Where(n => n.Phone is not null).ToList();
    }

    private static AdminChildRow Row(Child child) => new(
        child.Id, child.RollNo, child.FirstName, child.HomeLanguage, child.Consent, child.ParentStoppedAt != null,
        child.ParentPhoneSetAt is null ? null : child.ParentPhone, child.HomeLanguageSetAt is not null,
        child.ParentPhone2SetAt is null ? null : child.ParentPhone2);
}
