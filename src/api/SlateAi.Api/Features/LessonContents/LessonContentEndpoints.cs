using Microsoft.AspNetCore.Http.HttpResults;
using SlateAi.Api.Curriculum;
using SlateAi.Api.Features.Admin;

namespace SlateAi.Api.Features.LessonContents;

/// <summary>A lesson's description and examples as the admin page shows them.</summary>
/// <param name="Source"><c>textbook</c>, <c>written</c> or <c>edited</c>.</param>
public sealed record AdminLessonContent(string Description, IReadOnlyList<string> Examples, string Source)
{
    public static AdminLessonContent? From(StoredLessonContent? stored) =>
        stored is null ? null : new(stored.Description, stored.Examples, stored.Source);
}

/// <param name="Description">Blank, with no examples, removes the lesson's content.</param>
/// <param name="Examples">1–4 worked examples, one sentence each; blank lines are dropped.</param>
public sealed record SetLessonContentRequest(Guid ClientRequestId, string? Description, IReadOnlyList<string?>? Examples);

/// <summary>
/// Edits a lesson's description and examples on <c>/admin/lesson-links</c> (T-141, D-123). The list itself comes with
/// the lesson links (<c>GET /api/admin/lesson-links</c>). Open to anyone with the address, like the other admin pages (D-112).
/// </summary>
public static class LessonContentEndpoints
{
    public static IServiceCollection AddLessonContents(this IServiceCollection services) =>
        services.AddScoped<ILessonContentStore, LessonContentStore>().AddScoped<LessonContentLoader>();

    /// <summary>Mapped by <see cref="AdminEndpoints.MapAdminEndpoints"/>.</summary>
    public static IEndpointRouteBuilder MapLessonContentEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPut("/api/admin/lesson-content/{competencyId}", SetAsync)
            .WithTags("Admin")
            .WithSummary("Set or remove one lesson's description and examples (T-141)")
            .WithDescription(
                "Body { clientRequestId, description, examples[] }: a description of at most 700 characters and 1–4 " +
                "examples of at most 200, no braces; all blank removes it. Idempotent per clientRequestId. 400 for text " +
                "that breaks the rules, 404 for a lesson not in the curriculum. A new today's message for the lesson " +
                "builds its spoken concept on it; the startup load never overwrites an edit. Admin page; no key (D-112).");
        return app;
    }

    private static async Task<Results<Ok<AdminLessonContent>, NoContent, NotFound, ValidationProblem>> SetAsync(
        string competencyId,
        SetLessonContentRequest request,
        HttpContext http,
        ILessonContentStore store,
        BuiltInCurriculum builtIn,
        TimeProvider clock,
        CancellationToken cancellationToken)
    {
        // The file and this page describe the built-in curriculum (T-145).
        if (builtIn.Graph.Get(competencyId) is not { } lesson)
        {
            return TypedResults.NotFound();
        }

        var (content, errors) = LessonContentRules.Normalise(request.Description, request.Examples);
        if (request.ClientRequestId == Guid.Empty)
        {
            errors["clientRequestId"] = ["Required."];
        }

        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var stored = await store.SetAsync(request.ClientRequestId, lesson.Id, content, clock.GetUtcNow(), cancellationToken);
        AdminEndpoints.NoStore(http);
        return AdminLessonContent.From(stored) is { } result ? TypedResults.Ok(result) : TypedResults.NoContent();
    }
}
