using Microsoft.AspNetCore.Http.HttpResults;
using SlateAi.Api.Features.Auth;

namespace SlateAi.Api.Features.TodaysMessage;

public sealed record ApproveTodaysMessageRequest(Guid ClientRequestId, int Version);

public sealed record SetTodaysSectionRequest(Guid ClientRequestId, bool Removed);

public sealed record RetryTodaysMessageRequest(Guid ClientRequestId);

/// <param name="Version">The version she was editing; a newer one means 409 with the latest.</param>
/// <param name="Text">Concept or note, in Kannada.</param>
/// <param name="Steps">Activity steps, in Kannada (1 to 3).</param>
public sealed record EditTodaysSectionRequest(Guid ClientRequestId, int Version, string? Text = null, IReadOnlyList<string>? Steps = null);

/// <param name="Index">Which pair of the book's list (the view's <c>pairs</c>).</param>
public sealed record SetTodaysPictureRequest(Guid ClientRequestId, int Version, int Index);

/// <summary>Today's message (D-91, T-88): read, try again, remove or restore a section, approve.</summary>
public static class TodaysMessageEndpoints
{
    private const string Day = "/api/classes/{classId:guid}/todays-message/{date}";

    public static IServiceCollection AddTodaysMessage(this IServiceCollection services)
    {
        services.AddScoped<ITodaysMessageStore, TodaysMessageStore>();
        services.AddScoped<TodaysMessageWriter>();
        services.AddScoped<TodaysMessageGenerator>();
        services.AddScoped<TodaysMessageService>();
        services.AddSingleton<TodaysMessageQueue>();
        services.AddHostedService<TodaysMessageWorker>();
        return services;
    }

    public static IEndpointRouteBuilder MapTodaysMessageEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(Day, GetAsync)
            .RequireClassOwner()
            .WithTags("Today's message")
            .WithSummary("Today's message: concept, activity, picture question, per language, with its status and version")
            .WithDescription(
                "404 until a draft exists. A draft starts when she confirms a lesson with the book's exercises (D-93) and " +
                "someone is due tonight's message. generating → ready (check, then approve this version) → approved. " +
                "sent: families have it, read only. The picture numbers and answer come from code, never the AI.");

        app.MapGet($"{Day}/answers", GetAnswersAsync)
            .RequireClassOwner()
            .WithTags("Today's message")
            .WithSummary("The children's answers to today's picture question, with their level (T-92, D-95)")
            .WithDescription(
                "sent: families who got today's message; answered: questions finished (right, or two taps). Each row has " +
                "the child's last tap (>, < or =, or null) and the level worked out by code: gotIt (right first time), " +
                "gettingThere (right on the second try), needsHelp (wrong twice), notAnswered. 404 without a today's message.");

        app.MapPost($"{Day}/generate", RetryAsync)
            .RequireClassOwner()
            .WithTags("Today's message")
            .WithSummary("Try again: write a new draft from today's confirmed lesson")
            .WithDescription(
                "202 and back to generating, approval cleared. 409 when the lesson isn't confirmed, has no exercise " +
                "list, nobody is due tonight's message, or it was already sent.");

        app.MapPatch($"{Day}/sections/{{kind}}", SetSectionAsync)
            .RequireClassOwner()
            .WithTags("Today's message")
            .WithSummary("Remove or restore one section (concept, activity, picture, video, note)")
            .WithDescription(
                "A removed section is not sent. Each change is a new version and clears her approval, so she ticks again " +
                "(D-92). 409 while generating, once sent, or when it would remove the last section. Replaying the same " +
                "clientRequestId is a no-op.");

        app.MapPut($"{Day}/sections/{{kind}}", EditSectionAsync)
            .RequireClassOwner()
            .WithTags("Today's message")
            .WithSummary("Save her Kannada for the concept, the activity or her note (D-92)")
            .WithDescription(
                "Her text goes through the same checks as A9's: no child's name, only the book's numbers (their tens and ones, " +
                "the page and 10), her language's script, the length limits. 400 with problem, detail and the allowed numbers " +
                "when a check fails; nothing is saved. 202 otherwise: back to generating, the other languages are written " +
                "from her Kannada and the concepts voiced, then ready at the next version (she ticks again). 409 with the " +
                "latest for an older version, while generating, or once sent.");

        app.MapPut($"{Day}/picture", SetPictureAsync)
            .RequireClassOwner()
            .WithTags("Today's message")
            .WithSummary("Choose another pair from the book's list for the picture question (D-92)")
            .WithDescription(
                "Code takes pair `index` of the lesson's exercise list and works out the answer (guardrail 4): a new version, " +
                "her approval cleared. 409 for an older version, an index outside the list, while generating, or once sent.");

        app.MapPost($"{Day}/approve", ApproveAsync)
            .RequireClassOwner()
            .WithTags("Today's message")
            .WithSummary("Approve the version she checked (guardrail 3)")
            .WithDescription(
                "Only a ready draft at its current version; 409 with the latest otherwise. It goes with tonight's evening " +
                "message after 6 pm; with Demo:SendOnApprove (staging's demo, D-94) the evening send runs at once. Without " +
                "an approval families get the plain evening message. Replaying the same clientRequestId is a no-op.");
        return app;
    }

    private static async Task<Results<Ok<TodaysMessageView>, NotFound>> GetAsync(
        Guid classId, DateOnly date, TodaysMessageService service, CancellationToken cancellationToken) =>
        await service.GetAsync(classId, date, cancellationToken) is { } view ? TypedResults.Ok(view) : TypedResults.NotFound();

    private static async Task<Results<Ok<TodaysAnswersView>, NotFound>> GetAnswersAsync(
        Guid classId, DateOnly date, TodaysMessageService service, CancellationToken cancellationToken) =>
        await service.GetAnswersAsync(classId, date, cancellationToken) is { } answers ? TypedResults.Ok(answers) : TypedResults.NotFound();

    private static async Task<Results<Accepted<TodaysMessageView>, Conflict, ValidationProblem>> RetryAsync(
        Guid classId, DateOnly date, RetryTodaysMessageRequest request, TodaysMessageService service, CancellationToken cancellationToken)
    {
        if (request.ClientRequestId == Guid.Empty)
        {
            return Required();
        }

        // Each tap is one new draft; a replayed request would only redo the same work, so it isn't recorded.
        return await service.RetryAsync(classId, date, cancellationToken)
            ? TypedResults.Accepted((string?)null, (await service.GetAsync(classId, date, cancellationToken))!)
            : TypedResults.Conflict();
    }

    private static async Task<Results<Ok<TodaysMessageView>, Conflict<TodaysMessageView>, NotFound, ValidationProblem>> SetSectionAsync(
        Guid classId,
        DateOnly date,
        string kind,
        SetTodaysSectionRequest request,
        TodaysMessageService service,
        CancellationToken cancellationToken)
    {
        if (request.ClientRequestId == Guid.Empty)
        {
            return Required();
        }

        if (!TodaysSectionKind.All.Contains(kind))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["kind"] = [$"Use one of: {string.Join(", ", TodaysSectionKind.All)}."],
            });
        }

        var result = await service.SetRemovedAsync(classId, date, request.ClientRequestId, kind, request.Removed, cancellationToken);
        return await Reply(result, classId, date, service, cancellationToken);
    }

    private static async Task<Results<Accepted<TodaysMessageView>, Conflict<TodaysMessageView>, NotFound, ValidationProblem>> EditSectionAsync(
        Guid classId,
        DateOnly date,
        string kind,
        EditTodaysSectionRequest request,
        TodaysMessageService service,
        CancellationToken cancellationToken)
    {
        if (request.ClientRequestId == Guid.Empty)
        {
            return Required();
        }

        if (kind is not (TodaysSectionKind.Concept or TodaysSectionKind.Activity or TodaysSectionKind.Note))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["kind"] = ["Use concept, activity or note."] });
        }

        var (change, problem) = await service.EditAsync(
            classId, date, request.ClientRequestId, request.Version, kind, request.Text, request.Steps, cancellationToken);
        if (problem is not null)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["problem"] = [problem.Problem],
                ["detail"] = [problem.Detail ?? ""],
                ["allowed"] = [.. problem.Allowed],
            });
        }

        if (change == TodaysChange.NotFound || await service.GetAsync(classId, date, cancellationToken) is not { } latest)
        {
            return TypedResults.NotFound();
        }

        return change == TodaysChange.Conflict ? TypedResults.Conflict(latest) : TypedResults.Accepted((string?)null, latest);
    }

    private static async Task<Results<Ok<TodaysMessageView>, Conflict<TodaysMessageView>, NotFound, ValidationProblem>> SetPictureAsync(
        Guid classId,
        DateOnly date,
        SetTodaysPictureRequest request,
        TodaysMessageService service,
        CancellationToken cancellationToken)
    {
        if (request.ClientRequestId == Guid.Empty)
        {
            return Required();
        }

        var result = await service.SetPictureAsync(classId, date, request.ClientRequestId, request.Version, request.Index, cancellationToken);
        return await Reply(result, classId, date, service, cancellationToken);
    }

    private static async Task<Results<Ok<TodaysMessageView>, Conflict<TodaysMessageView>, NotFound, ValidationProblem>> ApproveAsync(
        Guid classId,
        DateOnly date,
        ApproveTodaysMessageRequest request,
        HttpContext http,
        TodaysMessageService service,
        CancellationToken cancellationToken)
    {
        if (request.ClientRequestId == Guid.Empty)
        {
            return Required();
        }

        var result = await service.ApproveAsync(
            classId, date, request.ClientRequestId, request.Version, http.User.GetTeacherId()!.Value, cancellationToken);
        return await Reply(result, classId, date, service, cancellationToken);
    }

    private static async Task<Results<Ok<TodaysMessageView>, Conflict<TodaysMessageView>, NotFound, ValidationProblem>> Reply(
        TodaysChange result, Guid classId, DateOnly date, TodaysMessageService service, CancellationToken cancellationToken)
    {
        if (result == TodaysChange.NotFound || await service.GetAsync(classId, date, cancellationToken) is not { } latest)
        {
            return TypedResults.NotFound();
        }

        return result == TodaysChange.Conflict ? TypedResults.Conflict(latest) : TypedResults.Ok(latest);
    }

    private static ValidationProblem Required() =>
        TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["clientRequestId"] = ["Required."] });
}
