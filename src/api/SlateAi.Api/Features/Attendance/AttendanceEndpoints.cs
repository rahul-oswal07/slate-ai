using Microsoft.AspNetCore.Http.HttpResults;
using SlateAi.Api.Features.Auth;

namespace SlateAi.Api.Features.Attendance;

/// <summary>The full register for one school day: listed children are absent, everyone else present (FR-01).</summary>
public sealed record MarkAttendanceRequest(Guid ClientRequestId, DateOnly Date, IReadOnlyList<Guid> AbsentChildIds);

public sealed record MarkAttendanceResponse(DateOnly Date, int Present, int Absent);

public static class AttendanceEndpoints
{
    // India has no daylight saving, so a fixed offset avoids depending on tzdata in the chiseled image.
    private static readonly TimeSpan Ist = TimeSpan.FromHours(5.5);

    public static IServiceCollection AddAttendance(this IServiceCollection services) =>
        services.AddScoped<IAttendanceStore, AttendanceStore>();

    public static IEndpointRouteBuilder MapAttendanceEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/classes/{classId:guid}/attendance", MarkAsync)
            .RequireClassOwner()
            .WithTags("Attendance")
            .WithSummary("Mark today's register")
            .WithDescription(
                "Replaces the register for the class and date. Replaying the same clientRequestId is a no-op " +
                "(offline outbox). A new clientRequestId for an already-marked date is a correction.");
        return app;
    }

    private static async Task<Results<Ok<MarkAttendanceResponse>, NotFound, ValidationProblem>> MarkAsync(
        Guid classId,
        MarkAttendanceRequest request,
        IAttendanceStore store,
        TimeProvider clock,
        CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();
        if (request.ClientRequestId == Guid.Empty)
        {
            errors["clientRequestId"] = ["Required."];
        }

        if (request.Date > DateOnly.FromDateTime(clock.GetUtcNow().ToOffset(Ist).DateTime))
        {
            errors["date"] = ["Can't mark attendance for a future date."];
        }

        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var childIds = await store.GetChildIdsAsync(classId, cancellationToken);
        if (childIds is null)
        {
            return TypedResults.NotFound();
        }

        var absent = (request.AbsentChildIds ?? []).ToHashSet();
        if (!absent.IsSubsetOf(childIds))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["absentChildIds"] = ["Every child must belong to this class."],
            });
        }

        return TypedResults.Ok(await store.SaveRegisterAsync(
            classId, request.ClientRequestId, request.Date, childIds, absent, cancellationToken));
    }
}
