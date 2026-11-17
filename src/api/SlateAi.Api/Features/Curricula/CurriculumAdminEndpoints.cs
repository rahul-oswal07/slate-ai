using Microsoft.AspNetCore.Http.HttpResults;
using SlateAi.Api.Curriculum;
using SlateAi.Api.Data.Entities;
using SlateAi.Api.Features.Admin;
using SlateAi.Api.Features.TextbookImport;

namespace SlateAi.Api.Features.Curricula;

/// <param name="NeedsReview">Lessons an admin hasn't checked yet.</param>
/// <param name="Classes">How many classes follow it.</param>
public sealed record AdminCurriculumSummary(
    Guid Id,
    string Title,
    int Grade,
    string Subject,
    string Medium,
    string Status,
    int Lessons,
    int NeedsReview,
    int Flagged,
    int Classes,
    Guid? ImportId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PublishedAt);

/// <summary>The curricula page: every stored curriculum, every class and what it follows, and the built-in one.</summary>
public sealed record AdminCurricula(
    IReadOnlyList<AdminCurriculumSummary> Curricula, IReadOnlyList<AdminClassChoice> Classes, int BuiltInLessons);

/// <summary>One lesson as the review page shows it: the lesson as in a curriculum file, code's answers, the importer's finds.</summary>
public sealed record AdminCurriculumLesson(
    Competency Lesson, ExerciseAnswerList Answers, LessonExtras? Extras, string ReviewStatus, IReadOnlyList<string> Flags);

/// <param name="PublishProblems">Why a draft can't be published yet; empty when it can (or it is published).</param>
public sealed record AdminCurriculumView(
    AdminCurriculumSummary Summary,
    string Book,
    int? Part,
    IReadOnlyList<string> Notes,
    IReadOnlyList<Chapter> Chapters,
    IReadOnlyList<AdminCurriculumLesson> Lessons,
    IReadOnlyList<string> PublishProblems);

public sealed record PublishCurriculumRequest(Guid ClientRequestId);

/// <param name="CurriculumId">A published curriculum, or null for the built-in one.</param>
public sealed record SetClassCurriculumRequest(Guid ClientRequestId, Guid? CurriculumId);

public sealed record CurriculumProblems(IReadOnlyList<string> Problems);

/// <summary>The class has used these lessons and the curriculum doesn't have them.</summary>
public sealed record ClassCurriculumConflict(IReadOnlyList<string> Missing);

/// <summary>
/// Review, publish and give out curricula on <c>/admin/curriculum</c> (T-147, D-125). Open to anyone with the address, like
/// the other admin pages (D-112).
/// </summary>
public static class CurriculumAdminEndpoints
{
    public static IServiceCollection AddCurriculumAdmin(this IServiceCollection services) =>
        services.AddScoped<ICurriculumAdminStore, CurriculumAdminStore>();

    /// <summary>Mapped by <see cref="AdminEndpoints.MapAdminEndpoints"/>.</summary>
    public static IEndpointRouteBuilder MapCurriculumAdminEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/admin/curricula", ListAsync)
            .WithTags("Admin")
            .WithSummary("Every stored curriculum, and which curriculum each class follows (T-147)")
            .WithDescription("A class with no stored curriculum follows the built-in one (data/curriculum). Admin page; no key (D-112).");

        app.MapGet("/api/admin/curricula/{id:guid}", GetAsync)
            .WithTags("Admin")
            .WithSummary("One curriculum with its lessons, for review (T-147)")
            .WithDescription(
                "Each lesson as in a curriculum file, with the answers code works out for its exercises, what the importer " +
                "found (pages, description, other exercises, pictures described), its flags and whether it was reviewed. " +
                "publishProblems says why a draft can't be published yet. Admin page; no key (D-112).");

        app.MapPut("/api/admin/curricula/{id:guid}/lessons/{competencyId}", EditLessonAsync)
            .WithTags("Admin")
            .WithSummary("Edit one lesson of a draft, or mark it reviewed (T-147)")
            .WithDescription(
                "The lesson must keep passing the curriculum rules; prerequisites must be earlier lessons. Idempotent per " +
                "clientRequestId. 400 with errors by field, 404 unknown, 409 published (it never changes). Returns the " +
                "curriculum. Admin page; no key (D-112).");

        app.MapDelete("/api/admin/curricula/{id:guid}/lessons/{competencyId}", RemoveLessonAsync)
            .WithTags("Admin")
            .WithSummary("Remove a lesson from a draft (T-147)")
            .WithDescription(
                "Also removes it from other lessons' prerequisites. ?clientRequestId= makes it idempotent. 404 unknown, 409 " +
                "published. Returns the curriculum. Admin page; no key (D-112).");

        app.MapPost("/api/admin/curricula/{id:guid}/publish", PublishAsync)
            .WithTags("Admin")
            .WithSummary("Publish a reviewed draft (T-147)")
            .WithDescription(
                "Every lesson reviewed and the whole curriculum passing the curriculum file's rules; then it never changes " +
                "again and classes can follow it. Its lessons' descriptions become what today's message is built on. 409 " +
                "with the problems otherwise. Admin page; no key (D-112).");

        app.MapPut("/api/admin/classes/{classId:guid}/curriculum", SetClassCurriculumAsync)
            .WithTags("Admin")
            .WithSummary("Choose the curriculum a class follows (T-147)")
            .WithDescription(
                "curriculumId: a published curriculum, or null for the built-in one. 409 with the lesson IDs when the " +
                "class has already used lessons the curriculum doesn't have (its days, today's messages and catch-ups " +
                "point at them). 400 not a published curriculum, 404 unknown class. Admin page; no key (D-112).");
        return app;
    }

    private static async Task<Ok<AdminCurricula>> ListAsync(
        HttpContext http, ICurriculumAdminStore store, BuiltInCurriculum builtIn, CancellationToken cancellationToken)
    {
        var curricula = await store.ListAsync(cancellationToken);
        var classes = await store.ListClassesAsync(cancellationToken);
        AdminEndpoints.NoStore(http);
        return TypedResults.Ok(new AdminCurricula(curricula.Select(Summary).ToList(), classes, builtIn.Graph.All.Count));
    }

    private static async Task<Results<Ok<AdminCurriculumView>, NotFound>> GetAsync(
        Guid id, HttpContext http, ICurriculumAdminStore store, CancellationToken cancellationToken)
    {
        AdminEndpoints.NoStore(http);
        return await store.GetAsync(id, cancellationToken) is { } draft ? TypedResults.Ok(View(draft)) : TypedResults.NotFound();
    }

    private static async Task<Results<Ok<AdminCurriculumView>, NotFound, Conflict, ValidationProblem>> EditLessonAsync(
        Guid id,
        string competencyId,
        EditDraftLessonRequest request,
        HttpContext http,
        ICurriculumAdminStore store,
        TimeProvider clock,
        CancellationToken cancellationToken)
    {
        if (request.ClientRequestId == Guid.Empty)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["clientRequestId"] = ["Required."] });
        }

        var result = await store.EditLessonAsync(id, competencyId, request, clock.GetUtcNow(), cancellationToken);
        AdminEndpoints.NoStore(http);
        return result.Change switch
        {
            DraftChange.NotFound => TypedResults.NotFound(),
            DraftChange.NotDraft => TypedResults.Conflict(),
            DraftChange.Invalid => TypedResults.ValidationProblem(result.Errors!),
            _ => TypedResults.Ok(View((await store.GetAsync(id, cancellationToken))!)),
        };
    }

    private static async Task<Results<Ok<AdminCurriculumView>, NotFound, Conflict, ValidationProblem>> RemoveLessonAsync(
        Guid id,
        string competencyId,
        Guid? clientRequestId,
        HttpContext http,
        ICurriculumAdminStore store,
        TimeProvider clock,
        CancellationToken cancellationToken)
    {
        if (clientRequestId is not { } requestId || requestId == Guid.Empty)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["clientRequestId"] = ["Required."] });
        }

        var change = await store.RemoveLessonAsync(id, competencyId, requestId, clock.GetUtcNow(), cancellationToken);
        AdminEndpoints.NoStore(http);
        return change switch
        {
            DraftChange.NotFound => TypedResults.NotFound(),
            DraftChange.NotDraft => TypedResults.Conflict(),
            _ => TypedResults.Ok(View((await store.GetAsync(id, cancellationToken))!)),
        };
    }

    private static async Task<Results<Ok<AdminCurriculumView>, NotFound, Conflict<CurriculumProblems>, ValidationProblem>> PublishAsync(
        Guid id,
        PublishCurriculumRequest request,
        HttpContext http,
        ICurriculumAdminStore store,
        TimeProvider clock,
        CancellationToken cancellationToken)
    {
        if (request.ClientRequestId == Guid.Empty)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["clientRequestId"] = ["Required."] });
        }

        var result = await store.PublishAsync(id, request.ClientRequestId, clock.GetUtcNow(), cancellationToken);
        AdminEndpoints.NoStore(http);
        return result.Change switch
        {
            DraftChange.NotFound => TypedResults.NotFound(),
            DraftChange.NotDraft => TypedResults.Conflict(new CurriculumProblems(["It is already published."])),
            DraftChange.Invalid => TypedResults.Conflict(new CurriculumProblems(result.Problems!)),
            _ => TypedResults.Ok(View((await store.GetAsync(id, cancellationToken))!)),
        };
    }

    private static async Task<Results<Ok<AdminClassChoice>, NotFound, Conflict<ClassCurriculumConflict>, ValidationProblem>>
        SetClassCurriculumAsync(
            Guid classId,
            SetClassCurriculumRequest request,
            HttpContext http,
            ICurriculumAdminStore store,
            BuiltInCurriculum builtIn,
            TimeProvider clock,
            CancellationToken cancellationToken)
    {
        if (request.ClientRequestId == Guid.Empty)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["clientRequestId"] = ["Required."] });
        }

        var builtInIds = builtIn.Graph.All.Select(c => c.Id).ToHashSet(StringComparer.Ordinal);
        var (result, missing) = await store.AssignAsync(
            classId, request.CurriculumId, builtInIds, request.ClientRequestId, clock.GetUtcNow(), cancellationToken);
        AdminEndpoints.NoStore(http);
        return result switch
        {
            ClassAssign.ClassNotFound => TypedResults.NotFound(),
            ClassAssign.CurriculumNotPublished => TypedResults.ValidationProblem(
                new Dictionary<string, string[]> { ["curriculumId"] = ["Not a published curriculum."] }),
            ClassAssign.WouldLoseLessons => TypedResults.Conflict(new ClassCurriculumConflict(missing)),
            _ => TypedResults.Ok((await store.ListClassesAsync(cancellationToken)).Single(c => c.Id == classId)),
        };
    }

    internal static AdminCurriculumSummary Summary(CurriculumDraft draft) => new(
        draft.Meta.Id,
        draft.Meta.Title,
        draft.Meta.Grade,
        draft.Meta.Subject,
        draft.Meta.Medium,
        draft.Meta.Status,
        draft.Lessons.Count,
        draft.Lessons.Count(l => l.ReviewStatus != CurriculumReviewStatuses.Reviewed),
        draft.Lessons.Count(l => l.Flags.Count > 0),
        draft.ClassIds.Count,
        draft.Meta.ImportId,
        draft.Meta.CreatedAt,
        draft.Meta.PublishedAt);

    internal static AdminCurriculumView View(CurriculumDraft draft) => new(
        Summary(draft),
        draft.Book,
        draft.Part,
        draft.Notes,
        draft.Chapters,
        draft.Lessons.Select(l => new AdminCurriculumLesson(
            l.Competency, DraftRules.Answers(l.Competency.Exercises), l.Extras, l.ReviewStatus, l.Flags)).ToList(),
        draft.Meta.Status == CurriculumStatuses.Draft ? DraftRules.PublishProblems(draft) : []);
}
