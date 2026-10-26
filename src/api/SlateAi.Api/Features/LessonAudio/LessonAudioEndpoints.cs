using Microsoft.AspNetCore.Http.HttpResults;
using SlateAi.Api.Features.Auth;

namespace SlateAi.Api.Features.LessonAudio;

public sealed record SetLessonAudioRequest(Guid ClientRequestId, bool Enabled);

/// <param name="ClipId">The clip she listened to; approval is for exactly this one.</param>
public sealed record ApproveLessonAudioRequest(Guid ClientRequestId, Guid ClipId);

public sealed record RegenerateLessonAudioRequest(Guid ClientRequestId);

/// <summary>
/// The optional "also send today's lesson as audio" (T-62). Ticking makes one clip per language of tonight's families
/// (shared across classes); she listens and approves each (guardrail 3); the evening send attaches only approved ones.
/// </summary>
public static class LessonAudioEndpoints
{
    private const string Day = "/api/classes/{classId:guid}/lesson-audio/{date}";

    public static IServiceCollection AddLessonAudio(this IServiceCollection services)
    {
        services.AddScoped<ILessonAudioStore, LessonAudioStore>();
        services.AddScoped<LessonScriptWriter>();
        services.AddScoped<LessonAudioGenerator>();
        services.AddScoped<LessonAudioService>();
        services.AddSingleton<LessonAudioQueue>();
        services.AddHostedService<LessonAudioWorker>();
        return services;
    }

    public static IEndpointRouteBuilder MapLessonAudioEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(Day, GetAsync)
            .RequireClassOwner()
            .WithTags("Lesson audio")
            .WithSummary("The day's optional lesson audio: one row per language, with its clip and status")
            .WithDescription(
                "available: she can tick it. enabled: ticked. sent: tonight's messages have gone, so it is read-only. " +
                "Each item is generating, ready (listen, then approve), approved or failed.");

        app.MapPut(Day, SetAsync)
            .RequireClassOwner()
            .WithTags("Lesson audio")
            .WithSummary("Tick or untick \"Also send today's lesson as audio\"")
            .WithDescription(
                "Ticking starts making one clip per language of tonight's families (202). 409 unless the lesson is " +
                "confirmed, the template approved and someone will get the message, and 409 once the messages are sent. " +
                "Unticking removes the day's clips and approvals.");

        app.MapPost($"{Day}/{{language}}/approve", ApproveAsync)
            .RequireClassOwner()
            .WithTags("Lesson audio")
            .WithSummary("Approve the clip she listened to")
            .WithDescription(
                "Guardrail 3: nothing reaches a family without this. 409 unless the clip is ready and is the clipId she " +
                "was shown (a regenerated clip needs approving again), and 409 once the messages are sent.");

        app.MapPost($"{Day}/{{language}}/regenerate", RegenerateAsync)
            .RequireClassOwner()
            .WithTags("Lesson audio")
            .WithSummary("Try again: make a new clip for this language")
            .WithDescription("Back to generating with no approval (202). 409 once the messages are sent.");
        return app;
    }

    private static async Task<Results<Ok<LessonAudioDayResponse>, NotFound>> GetAsync(
        Guid classId, DateOnly date, LessonAudioService service, CancellationToken cancellationToken) =>
        await service.GetAsync(classId, date, cancellationToken) is { } day ? TypedResults.Ok(day) : TypedResults.NotFound();

    private static async Task<Results<Accepted<LessonAudioDayResponse>, ValidationProblem, Conflict<LessonAudioDayResponse>, NotFound>> SetAsync(
        Guid classId, DateOnly date, SetLessonAudioRequest request, LessonAudioService service, CancellationToken cancellationToken)
    {
        if (request.ClientRequestId == Guid.Empty)
        {
            return Required();
        }

        var outcome = await service.SetAsync(classId, date, request.Enabled, cancellationToken);
        return await Reply(outcome, classId, date, service, cancellationToken) switch
        {
            (LessonAudioOutcome.Ok, { } day) => TypedResults.Accepted((string?)null, day),
            (LessonAudioOutcome.Conflict, { } day) => TypedResults.Conflict(day),
            _ => TypedResults.NotFound(),
        };
    }

    private static async Task<Results<Ok<LessonAudioDayResponse>, ValidationProblem, Conflict<LessonAudioDayResponse>, NotFound>> ApproveAsync(
        Guid classId,
        DateOnly date,
        string language,
        ApproveLessonAudioRequest request,
        HttpContext http,
        LessonAudioService service,
        CancellationToken cancellationToken)
    {
        if (request.ClientRequestId == Guid.Empty || request.ClipId == Guid.Empty)
        {
            return Required();
        }

        var outcome = http.User.GetTeacherId() is { } teacherId
            ? await service.ApproveAsync(classId, date, language, request.ClipId, request.ClientRequestId, teacherId, cancellationToken)
            : LessonAudioOutcome.NotFound;
        return await Reply(outcome, classId, date, service, cancellationToken) switch
        {
            (LessonAudioOutcome.Ok, { } day) => TypedResults.Ok(day),
            (LessonAudioOutcome.Conflict, { } day) => TypedResults.Conflict(day),
            _ => TypedResults.NotFound(),
        };
    }

    private static async Task<Results<Accepted<LessonAudioDayResponse>, ValidationProblem, Conflict<LessonAudioDayResponse>, NotFound>> RegenerateAsync(
        Guid classId,
        DateOnly date,
        string language,
        RegenerateLessonAudioRequest request,
        LessonAudioService service,
        CancellationToken cancellationToken)
    {
        if (request.ClientRequestId == Guid.Empty)
        {
            return Required();
        }

        var outcome = await service.RegenerateAsync(classId, date, language, cancellationToken);
        return await Reply(outcome, classId, date, service, cancellationToken) switch
        {
            (LessonAudioOutcome.Ok, { } day) => TypedResults.Accepted((string?)null, day),
            (LessonAudioOutcome.Conflict, { } day) => TypedResults.Conflict(day),
            _ => TypedResults.NotFound(),
        };
    }

    private static async Task<(LessonAudioOutcome, LessonAudioDayResponse?)> Reply(
        LessonAudioOutcome outcome, Guid classId, DateOnly date, LessonAudioService service, CancellationToken cancellationToken) =>
        outcome == LessonAudioOutcome.NotFound ? (outcome, null) : (outcome, await service.GetAsync(classId, date, cancellationToken));

    private static ValidationProblem Required() =>
        TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["clientRequestId"] = ["Required."] });
}
