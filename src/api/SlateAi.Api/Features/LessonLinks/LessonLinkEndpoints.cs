using Microsoft.AspNetCore.Http.HttpResults;
using SlateAi.Api.Curriculum;
using SlateAi.Api.Features.Admin;
using SlateAi.Api.Features.LessonContents;

namespace SlateAi.Api.Features.LessonLinks;

/// <summary>One lesson on the admin page, with its video link or null, and its description and examples or null (T-141).</summary>
public sealed record AdminLessonLink(
    string Id, string NameKn, string NameEn, TextbookPage? TextbookPage, string? Url, AdminLessonContent? Content = null);

public sealed record AdminLessonLinkChapter(int Number, string NameKn, string NameEn, IReadOnlyList<AdminLessonLink> Lessons);

public sealed record AdminLessonLinksResponse(IReadOnlyList<AdminLessonLinkChapter> Chapters);

/// <param name="Url">Any absolute https link; blank or null removes it (D-106).</param>
public sealed record SetLessonLinkRequest(Guid ClientRequestId, string? Url);

/// <summary>
/// The admin page for lesson links (T-94, D-106): every chapter and lesson of the curriculum with its video link. Whoever
/// sets a link here has checked it; code then copies it into a new today's message for that lesson. Open to anyone with
/// the address, like the other admin pages (D-112).
/// </summary>
public static class LessonLinkEndpoints
{
    public static IServiceCollection AddLessonLinks(this IServiceCollection services) =>
        services.AddScoped<ILessonLinkStore, LessonLinkStore>();

    /// <summary>Mapped by <see cref="AdminEndpoints.MapAdminEndpoints"/>.</summary>
    public static IEndpointRouteBuilder MapLessonLinkEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/admin/lesson-links", GetAsync)
            .WithTags("Admin")
            .WithSummary("Every chapter and lesson with its video link (T-94) and its description and examples (T-141)")
            .WithDescription("Admin page; no key (D-112).");

        app.MapPut("/api/admin/lesson-links/{competencyId}", SetAsync)
            .WithTags("Admin")
            .WithSummary("Set or remove one lesson's video link (T-94)")
            .WithDescription(
                $"Body {{ clientRequestId, url }}: any absolute https URL, or blank to remove it; idempotent per clientRequestId. " +
                "400 for another kind of URL, 404 for a lesson not in the curriculum. A new today's message for the lesson " +
                "copies the link. Admin page; no key (D-112).");
        return app;
    }

    private static async Task<Ok<AdminLessonLinksResponse>> GetAsync(
        HttpContext http,
        ILessonLinkStore store,
        ILessonContentStore contents,
        CurriculumGraph curriculum,
        CancellationToken cancellationToken)
    {
        var links = await store.GetAllAsync(cancellationToken);
        var content = await contents.GetAllAsync(cancellationToken);
        var titles = curriculum.Chapters.ToDictionary(c => c.Number);
        var chapters = curriculum.All
            .GroupBy(c => c.Chapter)
            .OrderBy(g => g.Key)
            .Select(g => new AdminLessonLinkChapter(
                g.Key,
                titles.GetValueOrDefault(g.Key)?.NameKn ?? "",
                titles.GetValueOrDefault(g.Key)?.NameEn ?? "",
                g.Select(c => new AdminLessonLink(
                    c.Id, c.NameKn, c.NameEn, c.TextbookPage, links.GetValueOrDefault(c.Id),
                    AdminLessonContent.From(content.GetValueOrDefault(c.Id)))).ToList()))
            .ToList();
        AdminEndpoints.NoStore(http);
        return TypedResults.Ok(new AdminLessonLinksResponse(chapters));
    }

    private static async Task<Results<Ok<AdminLessonLink>, NotFound, ValidationProblem>> SetAsync(
        string competencyId,
        SetLessonLinkRequest request,
        HttpContext http,
        ILessonLinkStore store,
        CurriculumGraph curriculum,
        TimeProvider clock,
        CancellationToken cancellationToken)
    {
        if (curriculum.Get(competencyId) is not { } lesson)
        {
            return TypedResults.NotFound();
        }

        var errors = new Dictionary<string, string[]>();
        if (request.ClientRequestId == Guid.Empty)
        {
            errors["clientRequestId"] = ["Required."];
        }

        var (url, error) = LessonLinkRules.Normalise(request.Url);
        if (error is not null)
        {
            errors["url"] = [error];
        }

        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var stored = await store.SetAsync(request.ClientRequestId, lesson.Id, url, clock.GetUtcNow(), cancellationToken);
        AdminEndpoints.NoStore(http);
        return TypedResults.Ok(new AdminLessonLink(lesson.Id, lesson.NameKn, lesson.NameEn, lesson.TextbookPage, stored));
    }
}
