using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SlateAi.Api.Data;
using SlateAi.Api.Data.Entities;

namespace SlateAi.Api.Curriculum;

/// <summary>Database side of the curricula a class can follow (T-145), behind an interface so tests can use a fake.</summary>
public interface ICurriculumStore
{
    /// <summary>The stored curriculum the class follows; null for the built-in one, or when there is no such class.</summary>
    Task<Guid?> GetClassCurriculumIdAsync(Guid classId, CancellationToken cancellationToken);

    /// <summary>A published curriculum as parsed, not yet checked; null when there is none with this ID or it is a draft.</summary>
    Task<CurriculumFile?> GetPublishedAsync(Guid curriculumId, CancellationToken cancellationToken);
}

public sealed class CurriculumStore(AppDbContext db) : ICurriculumStore
{
    public Task<Guid?> GetClassCurriculumIdAsync(Guid classId, CancellationToken cancellationToken) =>
        db.Classes.AsNoTracking()
            .Where(c => c.Id == classId)
            .Select(c => c.CurriculumId)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<CurriculumFile?> GetPublishedAsync(Guid curriculumId, CancellationToken cancellationToken)
    {
        var curriculum = await db.Curricula.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == curriculumId && c.Status == CurriculumStatuses.Published, cancellationToken);
        if (curriculum is null)
        {
            return null;
        }

        var lessons = await db.CurriculumLessons.AsNoTracking()
            .Where(l => l.CurriculumId == curriculumId)
            .OrderBy(l => l.Sequence)
            .Select(l => l.Competency)
            .ToListAsync(cancellationToken);

        return new CurriculumFile(
            curriculum.Grade,
            curriculum.Subject,
            Version: null,
            lessons.Select(json => JsonSerializer.Deserialize<Competency>(json, JsonSerializerOptions.Web)!).ToList(),
            JsonSerializer.Deserialize<List<Chapter>>(curriculum.Chapters, JsonSerializerOptions.Web) ?? []);
    }
}
