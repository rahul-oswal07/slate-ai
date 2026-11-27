using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;
using SlateAi.Api.Data.Seed;
using SlateAi.Api.Features.Attendance;
using SlateAi.Api.Features.Auth;
using SlateAi.Api.Features.Delivery;
using SlateAi.Api.Features.EveningMessages;

namespace SlateAi.Api.Features.CatchUps;

/// <param name="Send">"Approve and send now" (T-117): sent to the families at once. False: the 6 pm job sends it.</param>
public sealed record ApprovePackageRequest(Guid ClientRequestId, int ScriptVersion, bool Send = false);

/// <param name="Hint">"simpler", "shorter", or null for "Try again".</param>
public sealed record RegeneratePackageRequest(Guid ClientRequestId, string? Hint);

public sealed record SendPackageRequest(Guid ClientRequestId);

/// <param name="ScriptVersion">The version she was editing; a newer one means 409 with the latest.</param>
/// <param name="Text">The script (concept), the activity's instruction, or her note.</param>
/// <param name="ObjectOption">The activity's other-objects line; empty to leave it out.</param>
public sealed record EditPackageSectionRequest(Guid ClientRequestId, int ScriptVersion, string? Text = null, string? ObjectOption = null);

public sealed record SetPackageSectionRequest(Guid ClientRequestId, bool Removed);

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
            .WithSummary("Approve one catch-up at the version she checked (guardrail 3), and send it now or after 6 pm")
            .WithDescription(
                "Only a ready catch-up whose scriptVersion is current. 409 with the latest otherwise (needs review, " +
                "generating, a newer version after Simpler/Shorter, already approved or sent). Replaying the same " +
                "clientRequestId is a no-op. send: true (\"Approve and send now\", T-117) sends it at once, whatever the " +
                "time, and it comes back as sent; send: false leaves it approved for the 6 pm job.");

        app.MapPost("/api/packages/{id:guid}/regenerate", RegenerateAsync)
            .RequireAuthorization()
            .WithTags("Catch-ups")
            .WithSummary("Write a new version: Simpler, Shorter, or Try again (hint null)")
            .WithDescription(
                "From ready, needs review or failed; 409 once approved, sent or while generating. 202: the package goes " +
                "to generating and comes back with scriptVersion + 1, so any earlier tick no longer counts. Same numbers.");

        app.MapPut("/api/packages/{id:guid}/sections/{kind}", EditSectionAsync)
            .RequireAuthorization()
            .WithTags("Catch-ups")
            .WithSummary("Save her words for the concept (the script), the activity or her note (T-103)")
            .WithDescription(
                "The same sections as today's message. Her words go through code checks, never an AI: not empty, the length " +
                "limits, no name, braces or signs, the family's script (her note may be in any language), every sum right, " +
                "only numbers the catch-up already uses, and the script keeps the activity's numbers. 400 with problem, " +
                "detail and the allowed numbers when a check fails; nothing is saved. Otherwise a new scriptVersion and her " +
                "approval cleared, so she ticks again (guardrail 3): 202 and generating for the script, which is voiced " +
                "again (her words, no AI) and comes back ready or needs review; 200 and ready for the activity or the note. " +
                "409 with the latest for an older version, while generating, once sent or failed. Replaying the same " +
                "clientRequestId is a no-op.");

        app.MapPatch("/api/packages/{id:guid}/sections/{kind}", SetSectionAsync)
            .RequireAuthorization()
            .WithTags("Catch-ups")
            .WithSummary("Remove or put back the video or her note (T-103)")
            .WithDescription(
                "A removed section is not sent. A new scriptVersion with her approval cleared. The concept, the activity " +
                "and the picture question stay: the family's page needs them (400). 409 while generating, once sent, or " +
                "when there is no such section. Replaying the same clientRequestId is a no-op.");

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
        Guid id, HttpContext http, ICatchUpStore store, TimeProvider clock, CancellationToken cancellationToken) =>
        await OwnedAsync(id, http, store, cancellationToken) is { } view
            ? TypedResults.Ok(StaleGenerating.Shown(view, clock.GetUtcNow())) // a cut-off one shows as failed (T-136)
            : TypedResults.NotFound();

    private static async Task<Results<Ok<PackageView>, Conflict<PackageView>, NotFound, ValidationProblem>> ApproveAsync(
        Guid id,
        ApprovePackageRequest request,
        HttpContext http,
        ICatchUpStore store,
        CatchUpSender sender,
        SendNow sendNow,
        CancellationToken cancellationToken)
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
        if (result == PackageChange.Applied && request.Send)
        {
            // Her choice skips the 6 pm wait; the send keeps its own check that this exact version is approved (guardrail 3).
            await sendNow.RunAsync(ct => sender.SendAsync(id, ct), cancellationToken);
        }

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

    private static async Task<Results<Ok<PackageView>, Accepted<PackageView>, Conflict<PackageView>, NotFound, ValidationProblem>> EditSectionAsync(
        Guid id,
        string kind,
        EditPackageSectionRequest request,
        HttpContext http,
        ICatchUpStore store,
        CatchUpEditor editor,
        CancellationToken cancellationToken)
    {
        if (request.ClientRequestId == Guid.Empty)
        {
            return Required();
        }

        if (!CatchUpSectionKind.Editable.Contains(kind))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["kind"] = ["Use concept, activity or note."] });
        }

        if (await OwnedAsync(id, http, store, cancellationToken) is not { } view)
        {
            return TypedResults.NotFound();
        }

        var (change, problem) = await editor.EditAsync(
            view, request.ClientRequestId, request.ScriptVersion, new PackageEdit(kind, request.Text, request.ObjectOption), cancellationToken);
        if (problem is not null)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["problem"] = [problem.Problem],
                ["detail"] = [problem.Detail ?? ""],
                ["allowed"] = [.. problem.Allowed],
            });
        }

        var latest = (await store.GetViewAsync(id, cancellationToken))!;
        return change switch
        {
            PackageChange.Conflict => TypedResults.Conflict(latest),
            _ when latest.Status == PackageStatus.Generating => TypedResults.Accepted((string?)null, latest),
            _ => TypedResults.Ok(latest),
        };
    }

    private static async Task<Results<Ok<PackageView>, Conflict<PackageView>, NotFound, ValidationProblem>> SetSectionAsync(
        Guid id,
        string kind,
        SetPackageSectionRequest request,
        HttpContext http,
        ICatchUpStore store,
        CatchUpEditor editor,
        CancellationToken cancellationToken)
    {
        if (request.ClientRequestId == Guid.Empty)
        {
            return Required();
        }

        if (!CatchUpSectionKind.All.Contains(kind))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["kind"] = [$"Use one of: {string.Join(", ", CatchUpSectionKind.All)}."],
            });
        }

        if (!CatchUpSectionKind.Removable.Contains(kind))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["kind"] = ["The family's page needs this section (listen, activity, answer), so it stays. Edit it instead."],
            });
        }

        if (await OwnedAsync(id, http, store, cancellationToken) is null)
        {
            return TypedResults.NotFound();
        }

        var change = await editor.SetRemovedAsync(id, request.ClientRequestId, kind, request.Removed, cancellationToken);
        var latest = (await store.GetViewAsync(id, cancellationToken))!;
        return change == PackageChange.Conflict ? TypedResults.Conflict(latest) : TypedResults.Ok(latest);
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
