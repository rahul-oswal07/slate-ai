using Microsoft.AspNetCore.Http.HttpResults;
using SlateAi.Api.Curriculum;
using SlateAi.Api.Data.Entities;
using SlateAi.Api.Features.Audio;
using SlateAi.Api.Features.EveningMessages;

namespace SlateAi.Api.Features.Parent;

/// <summary>
/// The family's side (P1–P4, FR-10, FR-11, FR-19, FR-21): the page behind a link token, the picture answer, the
/// reason for absence and stop / restart. No sign-in: the 128-bit token is the key. Nothing here calls an LLM; a lesson is shown only from a
/// catch-up she approved (guardrail 3).
/// </summary>
public static class ParentEndpoints
{
    public static IServiceCollection AddParent(this IServiceCollection services) =>
        services.AddScoped<IParentStore, ParentStore>();

    public static IEndpointRouteBuilder MapParentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/p/{token}").AllowAnonymous().WithTags("Parent");

        group.MapGet("", GetAsync)
            .WithSummary("What a family's link opens: the catch-up (P1–P4) or the evening message")
            .WithDescription(
                "kind \"catchUp\": the lesson only for an approved catch-up at the approved version (404 otherwise); " +
                "no lesson fields when the family stopped messages; the check's options without the correct answer. " +
                "kind \"evening\": the evening message. 404 if unknown; 410 once expired (14 days), with the " +
                "teacher's name and homework page for a catch-up.");

        group.MapGet("/audio/{clipId:guid}", GetAudioAsync)
            .WithSummary("One audio segment of the family's catch-up (Ogg Opus)")
            .WithDescription("Only the segments of this link's catch-up, while the link works. Supports range requests.");

        group.MapGet("/greeting", GetGreetingAsync)
            .WithSummary("The teacher's own greeting on an evening message (Ogg Opus)")
            .WithDescription(
                "Only for an evening-message link that is still valid and not stopped; 404 when she has not recorded " +
                "one. Supports range requests.");

        group.MapPost("/answer", AnswerAsync)
            .WithSummary("The picture the child tapped (P3), scored by code (FR-11)")
            .WithDescription(
                "correct → caught up, wrong → needs buddy, on the teacher's ledger. The first answer counts (D-62): " +
                "later answers and replays return 200 and change nothing. The response never says right or wrong. " +
                "409 when the family stopped messages, the link expired or the catch-up isn't approved.");

        group.MapPost("/reason", ReasonAsync)
            .WithSummary("Why the child was away, from the evening message (FR-21, D-23)")
            .WithDescription(
                "Evening-message links only (a reason belongs to one absent day); 404 for a catch-up link. One of " +
                "unwell, travelOrWork, festival, neededAtHome, other; anything else is 400. Category only. A later " +
                "tap replaces it; replaying the same clientRequestId changes nothing. 409 once the link expired or " +
                "the family stopped messages. Skipping sends nothing.");

        group.MapPost("/stop", StopAsync)
            .WithSummary("Stop these messages (FR-19, D-19)")
            .WithDescription(
                "Works with a catch-up or an evening-message link, also after it expired. Every send path then " +
                "refuses this child. Repeating it changes nothing.");

        group.MapPost("/restart", RestartAsync)
            .WithSummary("Start the messages again (FR-19, D-30)")
            .WithDescription("Undoes a stop. Repeating it changes nothing.");
        return app;
    }

    private static async Task<Results<Ok<ParentCatchUpResponse>, Ok<ParentEveningResponse>, JsonHttpResult<ParentExpiredResponse>, NotFound, StatusCodeHttpResult>>
        GetAsync(
            string token,
            IParentStore parents,
            IEveningStore evenings,
            CurriculumGraph curriculum,
            TimeProvider clock,
            CancellationToken cancellationToken)
    {
        if (!LooksLikeToken(token))
        {
            return TypedResults.NotFound();
        }

        if (await parents.FindCatchUpAsync(token, cancellationToken) is { } record)
        {
            if (!ParentRules.CanServe(record.Status, record.ApprovedScriptVersion, record.ScriptVersion))
            {
                return TypedResults.NotFound();
            }

            var homework = Homework(record, curriculum);
            if (clock.GetUtcNow() >= record.ExpiresAt)
            {
                return TypedResults.Json(
                    new ParentExpiredResponse(record.Language, record.ChildName, record.TeacherName, homework),
                    statusCode: StatusCodes.Status410Gone);
            }

            return TypedResults.Ok(record.Stopped
                ? new ParentCatchUpResponse(
                    record.Language, record.ChildName, record.TeacherName, true, record.Answered, record.ExpiresAt,
                    homework, null, null, null, null)
                : CatchUp(token, record, homework));
        }

        if (await evenings.FindByTokenAsync(token, cancellationToken) is not { } message)
        {
            return TypedResults.NotFound();
        }

        if (clock.GetUtcNow() >= message.ExpiresAt)
        {
            return TypedResults.StatusCode(StatusCodes.Status410Gone);
        }

        var text = EveningSlots.Render(message.TemplateText, message.ChildName, message.Slots).Text;
        var reason = (await parents.FindEveningAsync(token, cancellationToken))?.Reason;
        return TypedResults.Ok(new ParentEveningResponse(
            message.Date, message.Language, message.ChildName, message.TeacherName, text,
            message.Greeting is { } greeting && !message.Stopped
                ? new ParentGreetingView($"/api/p/{token}/greeting", Math.Round(greeting.Seconds, 1))
                : null,
            message.SentAt, message.Stopped, reason));
    }

    private static async Task<Results<FileContentHttpResult, NotFound>> GetAudioAsync(
        string token,
        Guid clipId,
        HttpContext http,
        IParentStore parents,
        IAudioStore audio,
        TimeProvider clock,
        CancellationToken cancellationToken)
    {
        if (!LooksLikeToken(token)
            || await parents.FindCatchUpAsync(token, cancellationToken) is not { } record
            || !Usable(record, clock)
            || record.Audio?.Segments.Any(s => ClipId(s.Url) == clipId) != true
            || await audio.GetBytesAsync(clipId, cancellationToken) is not { } bytes)
        {
            return TypedResults.NotFound();
        }

        var contentType = await audio.GetContentTypeAsync(clipId, cancellationToken) ?? "application/octet-stream";
        http.Response.Headers.CacheControl = "private, max-age=86400";
        return TypedResults.File(bytes, contentType, enableRangeProcessing: true);
    }

    private static async Task<Results<FileContentHttpResult, NotFound>> GetGreetingAsync(
        string token,
        HttpContext http,
        IEveningStore evenings,
        IAudioStore audio,
        TimeProvider clock,
        CancellationToken cancellationToken)
    {
        if (!LooksLikeToken(token)
            || await evenings.FindByTokenAsync(token, cancellationToken) is not { Greeting: { } greeting } message
            || message.Stopped
            || clock.GetUtcNow() >= message.ExpiresAt
            || await audio.GetBytesAsync(greeting.ClipId, cancellationToken) is not { } bytes)
        {
            return TypedResults.NotFound();
        }

        var contentType = await audio.GetContentTypeAsync(greeting.ClipId, cancellationToken) ?? "application/octet-stream";
        http.Response.Headers.CacheControl = "private, max-age=86400";
        return TypedResults.File(bytes, contentType, enableRangeProcessing: true);
    }

    private static async Task<Results<Ok<AnswerResponse>, Conflict, NotFound, ValidationProblem>> AnswerAsync(
        string token,
        AnswerRequest request,
        IParentStore parents,
        TimeProvider clock,
        ILoggerFactory loggers,
        CancellationToken cancellationToken)
    {
        if (request.ClientRequestId == Guid.Empty)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["clientRequestId"] = ["Required."],
            });
        }

        if (!LooksLikeToken(token) || await parents.FindCatchUpAsync(token, cancellationToken) is not { } record)
        {
            return TypedResults.NotFound();
        }

        if (!Usable(record, clock))
        {
            return TypedResults.Conflict();
        }

        if (record.Answered)
        {
            return TypedResults.Ok(new AnswerResponse(true));
        }

        if (AnswerScoring.Score(record.Check, request.ChoiceId) is not { } score)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["choiceId"] = [$"Must be 0 to {record.Check.Options.Count - 1}."],
            });
        }

        var saved = await parents.SaveAnswerAsync(
            new CheckResponse
            {
                AssignmentId = record.AssignmentId,
                ChoiceId = request.ChoiceId,
                Correct = score.Correct,
                Outcome = score.Outcome,
                ClientRequestId = request.ClientRequestId,
                AnsweredAt = clock.GetUtcNow(),
            },
            cancellationToken);
        if (saved)
        {
            loggers.CreateLogger(typeof(ParentEndpoints)).LogInformation(
                "Check answered for assignment {AssignmentId}: {Outcome}", record.AssignmentId, score.Outcome);
        }

        return TypedResults.Ok(new AnswerResponse(true));
    }

    private static async Task<Results<Ok<ReasonResponse>, Conflict, NotFound, ValidationProblem>> ReasonAsync(
        string token,
        ReasonRequest request,
        IParentStore parents,
        TimeProvider clock,
        CancellationToken cancellationToken)
    {
        if (request.ClientRequestId == Guid.Empty || !ReasonCategory.All.Contains(request.Category ?? ""))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                [request.ClientRequestId == Guid.Empty ? "clientRequestId" : "category"] =
                    [request.ClientRequestId == Guid.Empty ? "Required." : $"Must be one of {string.Join(", ", ReasonCategory.All)}."],
            });
        }

        if (!LooksLikeToken(token) || await parents.FindEveningAsync(token, cancellationToken) is not { } link)
        {
            return TypedResults.NotFound();
        }

        var now = clock.GetUtcNow();
        if (now >= link.ExpiresAt || link.Stopped)
        {
            return TypedResults.Conflict();
        }

        await parents.SaveReasonAsync(
            new AbsenceReason
            {
                Id = Guid.NewGuid(),
                ClassId = link.ClassId,
                ChildId = link.ChildId,
                Date = link.Date,
                Category = request.Category!,
                CreatedAt = now,
                UpdatedAt = now,
            },
            request.ClientRequestId,
            cancellationToken);
        return TypedResults.Ok(new ReasonResponse(request.Category!));
    }

    private static Task<Results<Ok<StopResponse>, NotFound, ValidationProblem>> StopAsync(
        string token, StopRequest request, IParentStore parents, TimeProvider clock, CancellationToken cancellationToken) =>
        SetStoppedAsync(token, request, stopped: true, parents, clock, cancellationToken);

    private static Task<Results<Ok<StopResponse>, NotFound, ValidationProblem>> RestartAsync(
        string token, StopRequest request, IParentStore parents, TimeProvider clock, CancellationToken cancellationToken) =>
        SetStoppedAsync(token, request, stopped: false, parents, clock, cancellationToken);

    private static async Task<Results<Ok<StopResponse>, NotFound, ValidationProblem>> SetStoppedAsync(
        string token, StopRequest request, bool stopped, IParentStore parents, TimeProvider clock, CancellationToken cancellationToken)
    {
        if (request.ClientRequestId == Guid.Empty)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["clientRequestId"] = ["Required."],
            });
        }

        // Expired links still work: a family must always be able to stop (and undo it).
        if (!LooksLikeToken(token) || await parents.FindChildAsync(token, cancellationToken) is not { } childId)
        {
            return TypedResults.NotFound();
        }

        await parents.SetStoppedAsync(childId, stopped, clock.GetUtcNow(), cancellationToken);
        return TypedResults.Ok(new StopResponse(stopped));
    }

    private static ParentCatchUpResponse CatchUp(string token, ParentCatchUpRecord record, TextbookPage? homework)
    {
        var audio = record.Audio is { } segments
            ? new ParentAudio(
                segments.Segments
                    .Select(s => new ParentAudioSegment(s.Kind, $"/api/p/{token}/audio/{ClipId(s.Url)}", s.Seconds))
                    .ToList(),
                segments.TotalSeconds)
            : null;
        var activity = record.Activity;
        var check = record.Check;
        return new ParentCatchUpResponse(
            record.Language,
            record.ChildName,
            record.TeacherName,
            false,
            record.Answered,
            record.ExpiresAt,
            homework,
            audio,
            new ParentTranscript(record.Audio?.DisclosureText ?? "", record.Script ?? "", record.KeyWordsKn),
            new ParentActivity(activity.Question, activity.Numbers.FirstOrDefault(), activity.Instruction, activity.ObjectOption),
            new ParentCheck(check.Question, check.Prompt, check.Options.Select((n, i) => new ParentOption(i, n)).ToList()));
    }

    /// <summary>The link may show its lesson and take an answer: approved, not expired, not stopped.</summary>
    private static bool Usable(ParentCatchUpRecord record, TimeProvider clock) =>
        ParentRules.CanServe(record.Status, record.ApprovedScriptVersion, record.ScriptVersion)
        && clock.GetUtcNow() < record.ExpiresAt
        && !record.Stopped;

    /// <summary>Homework is the first target's textbook page: the one the check tests (D-22).</summary>
    private static TextbookPage? Homework(ParentCatchUpRecord record, CurriculumGraph curriculum) =>
        record.TargetIds.Count > 0 ? curriculum.Get(record.TargetIds[0])?.TextbookPage : null;

    /// <summary>Segment URLs are <c>/api/audio/{id}</c>.</summary>
    private static Guid? ClipId(string url) =>
        Guid.TryParse(url.AsSpan(url.LastIndexOf('/') + 1), out var id) ? id : null;

    /// <summary>Tokens are 22 URL-safe characters; anything far off is not worth a query.</summary>
    private static bool LooksLikeToken(string token) => token.Length is >= 16 and <= 30;
}
