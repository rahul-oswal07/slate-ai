using Microsoft.AspNetCore.Http.HttpResults;
using SlateAi.Api.Features.Auth;

namespace SlateAi.Api.Features.Children;

/// <summary>A child's data (FR-15, T4 "Child's data"): the teacher deletes it when a family asks.</summary>
public static class ChildEndpoints
{
    public static IServiceCollection AddChildren(this IServiceCollection services) =>
        services.AddScoped<IChildStore, ChildStore>();

    public static IEndpointRouteBuilder MapChildEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/classes/{classId:guid}/children/{childId:guid}", DeleteAsync)
            .RequireClassOwner()
            .WithTags("Children")
            .WithSummary("Delete all of a child's data (FR-15)")
            .WithDescription(
                "Removes the child with their attendance, evening messages, catch-up links and answers, and reasons; " +
                "unsent catch-ups no longer include them. Can't be undone. Replaying the same clientRequestId " +
                "returns 204 again; an unknown child is 404.");
        return app;
    }

    private static async Task<Results<NoContent, NotFound, ValidationProblem>> DeleteAsync(
        Guid classId, Guid childId, Guid? clientRequestId, IChildStore store, ILoggerFactory loggers, CancellationToken cancellationToken)
    {
        if (clientRequestId is not { } requestId || requestId == Guid.Empty)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["clientRequestId"] = ["Required."],
            });
        }

        var result = await store.DeleteAsync(classId, childId, requestId, cancellationToken);
        if (result == ChildDelete.Deleted)
        {
            // The ID only: no name in the logs.
            loggers.CreateLogger(typeof(ChildEndpoints)).LogInformation("Child {ChildId} deleted (FR-15)", childId);
        }

        return result == ChildDelete.NotFound ? TypedResults.NotFound() : TypedResults.NoContent();
    }
}
