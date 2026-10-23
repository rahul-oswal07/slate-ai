using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;
using SlateAi.Api.Data.Seed;
using SlateAi.Api.Features.Attendance;
using SlateAi.Api.Features.Auth;
using SlateAi.Api.Features.EveningMessages;

namespace SlateAi.Api.Features.CatchUps;

public sealed record ApprovePackageRequest(Guid ClientRequestId, int ScriptVersion);

/// <param name="Hint">"simpler", "shorter", or null for "Try again".</param>
public sealed record RegeneratePackageRequest(Guid ClientRequestId, string? Hint);

public sealed record SendPackageRequest(Guid ClientRequestId);

/// <summary>
/// T3 Approve (FR-08, guardrail 3): one catch-up at a time, bound to the script version she ticked; Simpler /
/// Shorter / Try again; and the send step that refuses anything not approved. Bulk approve and Withdraw are roadmap (D-70).
/// </summary>
public static class CatchUpApprovalEndpoints
{
    private static readonly string?[] Hints = [null, "simpler", "shorter"];

    public static IEndpointRouteBuilder MapCatchUpApprovalEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/packages/{id:guid}", GetAsync)
            .RequireAuthorization()
            .WithTags("Catch-ups")
            .WithSummary("One catch-up with its children, reasons, review and audio (T3 detail)");

        app.MapPost("/api/packages/{id:guid}/approve", ApproveAsync)
            .RequireAuthorization()
            .WithTags("Catch-ups")
            .WithSummary("Approve one catch-up at the version she checked (guardrail 3)")
            .WithDescription(
                "Only a ready catch-up whose scriptVersion is current. 409 with the latest otherwise (needs review, " +
                "generating, a newer version after Simpler/Shorter, already approved or sent). Replaying the same " +
                "clientRequestId is a no-op.");

        app.MapPost("/api/packages/{id:guid}/regenerate", RegenerateAsync)
            .RequireAuthorization()
            .WithTags("Catch-ups")
            .WithSummary("Write a new version: Simpler, Shorter, or Try again (hint null)")
            .WithDescription(
                "From ready, needs review or failed; 409 once approved, sent or while generating. 202: the package goes " +
                "to generating and comes back with scriptVersion + 1, so any earlier tick no longer counts. Same numbers.");

        app.MapPost("/api/packages/{id:guid}/send", SendAsync)
            .RequireAuthorization()
            .WithTags("Catch-ups")
            .WithSummary("Send an approved catch-up to the families (the 6 pm job does this by itself)")
            .WithDescription(
                "409 unless the catch-up is approved at its current version (guardrail 3), and before 18:00 IST unless " +
                "Demo:Enabled. Families with consent Yes who haven't stopped get a link; others stay on the buddy card.");
        return app;
    }

    private static async Task<Results<Ok<PackageView>, NotFound>> GetAsync(
        Guid id, HttpContext http, ICatchUpStore store, CancellationToken cancellationToken) =>
        await OwnedAsync(id, http, store, cancellationToken) is { } view ? TypedResults.Ok(view) : TypedResults.NotFound();

    private static async Task<Results<Ok<PackageView>, Conflict<PackageView>, NotFound, ValidationProblem>> ApproveAsync(
        Guid id, ApprovePackageRequest request, HttpContext http, ICatchUpStore store, CancellationToken cancellationToken)
    {
        if (request.ClientRequestId == Guid.Empty)
        {
            return Required();
        }

        if (await OwnedAsync(id, http, store, cancellationToken) is null)
        {
            return TypedResults.NotFound();
        }

        var result = await store.ApproveAsync(
            id, request.ClientRequestId, request.ScriptVersion, http.User.GetTeacherId()!.Value, cancellationToken);
        var latest = (await store.GetViewAsync(id, cancellationToken))!;
        return result == PackageChange.Conflict ? TypedResults.Conflict(latest) : TypedResults.Ok(latest);
    }

    private static async Task<Results<Accepted<PackageView>, Conflict<PackageView>, NotFound, ValidationProblem>> RegenerateAsync(
        Guid id,
        RegeneratePackageRequest request,
        HttpContext http,
        ICatchUpStore store,
        CatchUpQueue queue,
        CancellationToken cancellationToken)
    {
        if (request.ClientRequestId == Guid.Empty)
        {
            return Required();
        }

        if (!Hints.Contains(request.Hint))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["hint"] = ["Use \"simpler\", \"shorter\" or null."] });
        }

        if (await OwnedAsync(id, http, store, cancellationToken) is null)
        {
            return TypedResults.NotFound();
        }

        var result = await store.StartRegenerateAsync(id, request.ClientRequestId, request.Hint, cancellationToken);
        if (result == PackageChange.Applied)
        {
            queue.TryEnqueue(id);
        }

        var latest = (await store.GetViewAsync(id, cancellationToken))!;
        return result == PackageChange.Conflict ? TypedResults.Conflict(latest) : TypedResults.Accepted((string?)null, latest);
    }

    private static async Task<Results<Ok<PackageView>, Conflict<PackageView>, NotFound, ValidationProblem>> SendAsync(
        Guid id,
        SendPackageRequest request,
        HttpContext http,
        ICatchUpStore store,
        CatchUpSender sender,
        IOptions<EveningOptions> evening,
        IOptions<DemoOptions> demo,
        TimeProvider clock,
        CancellationToken cancellationToken)
    {
        if (request.ClientRequestId == Guid.Empty)
        {
            return Required();
        }

        if (await OwnedAsync(id, http, store, cancellationToken) is not { } view)
        {
            return TypedResults.NotFound();
        }

        var early = clock.GetUtcNow() < SchoolClock.At(SchoolClock.Today(clock), evening.Value.SendAt);
        if (early && !demo.Value.Enabled)
        {
            return TypedResults.Conflict(view);
        }

        var result = await sender.SendAsync(id, cancellationToken);
        var latest = (await store.GetViewAsync(id, cancellationToken))!;
        return result == PackageChange.Conflict ? TypedResults.Conflict(latest) : TypedResults.Ok(latest);
    }

    /// <summary>The package if it belongs to the signed-in teacher's class; 404 otherwise.</summary>
    private static async Task<PackageView?> OwnedAsync(Guid id, HttpContext http, ICatchUpStore store, CancellationToken cancellationToken) =>
        await store.GetViewAsync(id, cancellationToken) is { } view && view.ClassId == http.User.GetClassId() ? view : null;

    private static ValidationProblem Required() =>
        TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["clientRequestId"] = ["Required."] });
}
