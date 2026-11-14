using Microsoft.AspNetCore.Http.HttpResults;
using SlateAi.Api.Features.Auth;

namespace SlateAi.Api.Features.TodaysMessage;

public sealed record ApproveTodaysMessageRequest(Guid ClientRequestId, int Version);

public sealed record SetTodaysSectionRequest(Guid ClientRequestId, bool Removed);

public sealed record RetryTodaysMessageRequest(Guid ClientRequestId);

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
