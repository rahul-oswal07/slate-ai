using Microsoft.EntityFrameworkCore;
using SlateAi.Api.Data;
using SlateAi.Api.Data.Entities;

namespace SlateAi.Api.Features.LessonLinks;

/// <summary>Database side of the lesson links (T-94), behind an interface so endpoints and the drafter can use a fake.</summary>
public interface ILessonLinkStore
{
    /// <summary>Every lesson of the curriculum that has a link: competency ID → URL.</summary>
    /// <param name="curriculumId">A published curriculum, or <c>BuiltInCurriculum.Id</c> (T-147).</param>
    Task<IReadOnlyDictionary<string, string>> GetAllAsync(Guid curriculumId, CancellationToken cancellationToken);

    /// <summary>The links of these lessons of the curriculum, in the order given; lessons without one are left out.</summary>
    Task<IReadOnlyList<(string CompetencyId, string Url)>> GetForAsync(
        Guid curriculumId, IReadOnlyList<string> competencyIds, CancellationToken cancellationToken);

    /// <summary>
    /// Sets (<paramref name="url"/>) or removes (null) the lesson's link, unless <paramref name="clientRequestId"/> was
    /// already applied. Returns the lesson's link afterwards.
    /// </summary>
    Task<string?> SetAsync(
        Guid clientRequestId, Guid curriculumId, string competencyId, string? url, DateTimeOffset now, CancellationToken cancellationToken);
}

public sealed class LessonLinkStore(AppDbContext db) : ILessonLinkStore
{
    private const string Endpoint = "admin-lesson-link";

    public async Task<IReadOnlyDictionary<string, string>> GetAllAsync(Guid curriculumId, CancellationToken cancellationToken) =>
        await db.LessonLinks.AsNoTracking().Where(l => l.CurriculumId == curriculumId)
            .ToDictionaryAsync(l => l.CompetencyId, l => l.Url, cancellationToken);

    public async Task<IReadOnlyList<(string CompetencyId, string Url)>> GetForAsync(
        Guid curriculumId, IReadOnlyList<string> competencyIds, CancellationToken cancellationToken)
    {
        var ids = competencyIds.ToList();
        var links = await db.LessonLinks.AsNoTracking()
            .Where(l => l.CurriculumId == curriculumId && ids.Contains(l.CompetencyId))
            .ToDictionaryAsync(l => l.CompetencyId, l => l.Url, cancellationToken);
        return [.. competencyIds.Where(links.ContainsKey).Select(id => (id, links[id]))];
    }

    public async Task<string?> SetAsync(
        Guid clientRequestId, Guid curriculumId, string competencyId, string? url, DateTimeOffset now, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.Database.ExecuteSqlAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({$"lesson-link:{curriculumId}:{competencyId}"}, 0))", cancellationToken);
        var link = await db.LessonLinks.SingleOrDefaultAsync(
            l => l.CurriculumId == curriculumId && l.CompetencyId == competencyId, cancellationToken);
        if (await db.ProcessedRequests.AnyAsync(r => r.ClientRequestId == clientRequestId, cancellationToken))
        {
            return link?.Url;
        }

        if (url is null)
        {
            if (link is not null)
            {
                db.LessonLinks.Remove(link);
            }
        }
        else if (link is null)
        {
            db.LessonLinks.Add(new LessonLink { CurriculumId = curriculumId, CompetencyId = competencyId, Url = url, UpdatedAt = now });
        }
        else
        {
            link.Url = url;
            link.UpdatedAt = now;
        }

        db.ProcessedRequests.Add(new ProcessedRequest { ClientRequestId = clientRequestId, Endpoint = Endpoint, CreatedAt = now });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return url;
    }
}
