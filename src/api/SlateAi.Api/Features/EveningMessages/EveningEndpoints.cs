using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;
using SlateAi.Api.Curriculum;
using SlateAi.Api.Data.Seed;
using SlateAi.Api.Features.Attendance;
using SlateAi.Api.Features.Auth;

namespace SlateAi.Api.Features.EveningMessages;

public sealed record ApproveTemplateRequest(Guid ClientRequestId, string Version);

public sealed record SendEveningRequest(Guid ClientRequestId);

/// <summary>One language of the template, filled with an example so she never sees <c>{child}</c> syntax (T0).</summary>
public sealed record TemplateLanguageView(string Language, IReadOnlyList<TextPart> Example);

/// <param name="Status">"approved" when every language is approved, else "draft".</param>
public sealed record MessageTemplateResponse(
    string Version, string Status, DateTimeOffset? ApprovedAt, IReadOnlyList<TemplateLanguageView> Languages);

/// <summary>Her greeting as the family fetches it: a token-scoped URL, since a parent has no teacher session.</summary>
public sealed record ParentGreetingView(string Url, double Seconds);

/// <summary>
/// What the family sees (demo chat, later WhatsApp): her greeting, then the written message. <c>Reason</c> is the
/// category they already gave for that day (FR-21), so a reopened link shows it.
/// </summary>
public sealed record ParentEveningResponse(
    DateOnly Date,
    string Language,
    string ChildName,
    string TeacherName,
    string Text,
    ParentGreetingView? Greeting,
    DateTimeOffset SentAt,
    bool Stopped,
    string? Reason = null)
{
    /// <summary>The parent link serves catch-ups too (<c>Features/Parent</c>); this tells the page which it got.</summary>
    public string Kind => "evening";
}

/// <summary>
/// Evening message on each day of absence (FR-20, D-21, guardrail 3): the template she approves once, the 6 pm
/// send and Today's tonight's card. The family's view by link token is in <c>Features/Parent</c>. No LLM anywhere on
/// this path.
/// </summary>
public static class EveningEndpoints
{
    public static IServiceCollection AddEveningMessages(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<EveningOptions>(configuration.GetSection(EveningOptions.Section));
        services.AddSingleton(sp => EveningWording.Load(
            Path.Combine(AppContext.BaseDirectory, sp.GetRequiredService<IOptions<EveningOptions>>().Value.WordingFile)));
        services.AddScoped<IEveningStore, EveningStore>();
        services.AddScoped<EveningSender>();
        services.AddHostedService<EveningSendWorker>();
        return services;
    }

    public static IEndpointRouteBuilder MapEveningMessageEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/classes/{classId:guid}/message-template", GetTemplateAsync)
            .RequireClassOwner()
            .WithTags("Evening message")
            .WithSummary("The evening-message template, filled with an example (T0 step 2)")
            .WithDescription(
                "One entry per language, as text parts with the code-filled slots marked (child, lesson, page). " +
                "The example uses a Telugu-speaking child from the class and the latest confirmed lesson.");

        app.MapPost("/api/classes/{classId:guid}/message-template/approve", ApproveAsync)
            .RequireClassOwner()
            .WithTags("Evening message")
            .WithSummary("Approve the evening message once (FR-20, D-21)")
            .WithDescription(
                "Approves every language at the given version. Replaying the same clientRequestId is a no-op. " +
                "409 with the latest template if the wording changed since she saw it (D-49).");

        app.MapGet("/api/classes/{classId:guid}/evening-messages/{date}", GetDayAsync)
            .RequireClassOwner()
            .WithTags("Evening message")
            .WithSummary("Tonight's evening messages: who gets one and why not (T1 tonight's card)");

        app.MapPost("/api/classes/{classId:guid}/evening-messages/send", SendAsync)
            .RequireClassOwner()
            .WithTags("Evening message")
            .WithSummary("Send today's evening messages now")
            .WithDescription(
                "The 6 pm job does this by itself. 409 before the send time (unless Demo:Enabled), when the template " +
                "isn't approved, when today's lesson isn't confirmed or when attendance isn't in. Families without " +
                "consent Yes, or who stopped messages, are refused in the per-child result. Safe to repeat: one " +
                "message per child per day.");

        return app;
    }

    private static async Task<Results<Ok<MessageTemplateResponse>, NotFound>> GetTemplateAsync(
        Guid classId,
        IEveningStore store,
        EveningWording wording,
        CurriculumGraph curriculum,
        TimeProvider clock,
        CancellationToken cancellationToken)
    {
        return await TemplateAsync(classId, store, wording, curriculum, clock, cancellationToken) is { } template
            ? TypedResults.Ok(template)
            : TypedResults.NotFound();
    }

    private static async Task<Results<Ok<MessageTemplateResponse>, Conflict<MessageTemplateResponse>, NotFound, ValidationProblem>>
        ApproveAsync(
            Guid classId,
            ApproveTemplateRequest request,
            HttpContext http,
            IEveningStore store,
            EveningWording wording,
            CurriculumGraph curriculum,
            TimeProvider clock,
            CancellationToken cancellationToken)
    {
        if (request.ClientRequestId == Guid.Empty || string.IsNullOrWhiteSpace(request.Version))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                [request.ClientRequestId == Guid.Empty ? "clientRequestId" : "version"] = ["Required."],
            });
        }

        // Makes sure the rows exist at the current wording before approving.
        if (await TemplateAsync(classId, store, wording, curriculum, clock, cancellationToken) is null)
        {
            return TypedResults.NotFound();
        }

        var result = await store.ApproveAsync(
            classId, request.ClientRequestId, request.Version, http.User.GetTeacherId()!.Value, cancellationToken);
        var latest = (await TemplateAsync(classId, store, wording, curriculum, clock, cancellationToken))!;
        return result == ApproveResult.Stale ? TypedResults.Conflict(latest) : TypedResults.Ok(latest);
    }

    private static async Task<Results<Ok<EveningDayResponse>, NotFound, ValidationProblem>> GetDayAsync(
        Guid classId,
        DateOnly date,
        EveningSender sender,
        TimeProvider clock,
        CancellationToken cancellationToken)
    {
        if (date > SchoolClock.Today(clock))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["date"] = ["Can't use a future date."],
            });
        }

        return await sender.PlanAsync(classId, date, cancellationToken) is { } day
            ? TypedResults.Ok(day)
            : TypedResults.NotFound();
    }

    private static async Task<Results<Ok<EveningDayResponse>, Conflict<EveningDayResponse>, NotFound, ValidationProblem>>
        SendAsync(
            Guid classId,
            SendEveningRequest request,
            EveningSender sender,
            IOptions<EveningOptions> options,
            IOptions<DemoOptions> demo,
            TimeProvider clock,
            CancellationToken cancellationToken)
    {
        if (request.ClientRequestId == Guid.Empty)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["clientRequestId"] = ["Required."],
            });
        }

        var today = SchoolClock.Today(clock);
        var early = clock.GetUtcNow() < SchoolClock.At(today, options.Value.SendAt);
        if (early && !demo.Value.Enabled)
        {
            return await sender.PlanAsync(classId, today, cancellationToken) is { } plan
                ? TypedResults.Conflict(plan)
                : TypedResults.NotFound();
        }

        if (await sender.SendAsync(classId, today, cancellationToken) is not { } day)
        {
            return TypedResults.NotFound();
        }

        return day.Blocked is null ? TypedResults.Ok(day) : TypedResults.Conflict(day);
    }

    private static async Task<MessageTemplateResponse?> TemplateAsync(
        Guid classId,
        IEveningStore store,
        EveningWording wording,
        CurriculumGraph curriculum,
        TimeProvider clock,
        CancellationToken cancellationToken)
    {
        if (await store.GetClassAsync(classId, cancellationToken) is not { } group)
        {
            return null;
        }

        var templates = await store.GetTemplatesAsync(classId, wording, cancellationToken);

        // T0 shows "Ravi, Tuesday": a Telugu-speaking child (the parent-language sample, A-21) and a real lesson.
        var child = group.Children.FirstOrDefault(c => c.HomeLanguage == "te") ?? group.Children.FirstOrDefault();
        var lesson = await store.LatestConfirmedLessonAsync(classId, SchoolClock.Today(clock), cancellationToken);
        IReadOnlyList<string> lessonIds = lesson?.CompetencyIds.Where(curriculum.Contains).ToList() is { Count: > 0 } ids
            ? ids
            : curriculum.All.Where(c => c.Chapter > 0).OrderBy(c => c.Sequence).Take(1).Select(c => c.Id).ToList();

        var languages = templates
            .OrderBy(t => t.Language, StringComparer.Ordinal)
            .Select(t => new TemplateLanguageView(
                t.Language,
                (EveningSlots.Fill(t.Wording, t.Language, child?.FirstName ?? "", lessonIds, curriculum)
                    ?? EveningSlots.Render(t.Text, child?.FirstName ?? "", new SlotValues("", ""))).Parts))
            .ToList();
        var approved = templates.Count > 0 && templates.All(t => t.Status == TemplateStatus.Approved);
        return new MessageTemplateResponse(
            wording.Version,
            approved ? TemplateStatus.Approved : TemplateStatus.Draft,
            approved ? templates.Max(t => t.ApprovedAt) : null,
            languages);
    }
}
