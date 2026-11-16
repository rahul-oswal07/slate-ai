using Microsoft.AspNetCore.Http.HttpResults;

namespace SlateAi.Api.Features.Admin;

/// <param name="ClassLabel">"Shobha · Grade 3": her class's main teacher and grade; null when she has no class.</param>
/// <param name="ConsentAt">Her voice consent; null: at her next visit she sees the Welcome and records her greeting.</param>
/// <param name="GreetingSeconds">Her greeting's length; null when she has none.</param>
public sealed record AdminTeacher(Guid Id, string Name, string? ClassLabel, DateTimeOffset? ConsentAt, double? GreetingSeconds);

public sealed record ClearConsentRequest(Guid ClientRequestId);

/// <summary>
/// The admin teachers page (T-133): each teacher's voice consent and greeting, and Clear consent. Clearing deletes her
/// greeting (D-44) and sends her back to the first-run Welcome (T0) to record it with her consent again. Open to anyone
/// with the address, like the other admin pages (D-112).
/// </summary>
public static class AdminTeacherEndpoints
{
    public static IServiceCollection AddAdminTeachers(this IServiceCollection services) =>
        services.AddScoped<IAdminTeacherStore, AdminTeacherStore>();

    /// <summary>Mapped by <see cref="AdminEndpoints.MapAdminEndpoints"/>.</summary>
    public static IEndpointRouteBuilder MapAdminTeacherEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/admin/teachers", ListAsync)
            .WithTags("Admin")
            .WithSummary("Every teacher with her voice consent and greeting (T-133)")
            .WithDescription(
                "consentAt is Teacher.ConsentAt, the first-run gate: null means she sees the Welcome at her next visit. " +
                "greetingSeconds is her recorded greeting's length, null when she has none. Admin page; no key (D-112).");

        app.MapPost("/api/admin/teachers/{teacherId:guid}/clear-consent", ClearConsentAsync)
            .WithTags("Admin")
            .WithSummary("Clear a teacher's voice consent and delete her greeting (T-133, D-44)")
            .WithDescription(
                "Body { clientRequestId }. Sets Teacher.ConsentAt to null and deletes her greeting clips (D-44): at her " +
                "next visit she sees the first-run Welcome and records her greeting with her consent again. The " +
                "evening message's approval is untouched. Idempotent per clientRequestId. 400 without it, 404 for an " +
                "unknown teacher. Admin page; no key (D-112).");
        return app;
    }

    private static async Task<Ok<List<AdminTeacher>>> ListAsync(
        HttpContext http, IAdminTeacherStore store, CancellationToken cancellationToken)
    {
        var teachers = await store.ListAsync(cancellationToken);
        AdminEndpoints.NoStore(http);
        return TypedResults.Ok(teachers.Select(View).ToList());
    }

    private static async Task<Results<Ok<AdminTeacher>, NotFound, ValidationProblem>> ClearConsentAsync(
        Guid teacherId,
        ClearConsentRequest request,
        HttpContext http,
        IAdminTeacherStore store,
        TimeProvider clock,
        CancellationToken cancellationToken)
    {
        if (request.ClientRequestId == Guid.Empty)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["clientRequestId"] = ["Required."] });
        }

        if (await store.ClearConsentAsync(request.ClientRequestId, teacherId, clock.GetUtcNow(), cancellationToken) is not { } row)
        {
            return TypedResults.NotFound();
        }

        AdminEndpoints.NoStore(http);
        return TypedResults.Ok(View(row));
    }

    private static AdminTeacher View(AdminTeacherRow row) => new(
        row.Id,
        row.Name,
        row.ClassTeacherName is { } main && row.Grade is { } grade ? $"{main} · Grade {grade}" : null,
        row.ConsentAt,
        row.GreetingSeconds);
}
