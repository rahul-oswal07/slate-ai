using Microsoft.AspNetCore.Http.HttpResults;
using SlateAi.Api.Ai;
using SlateAi.Api.Curriculum;
using SlateAi.Api.Features.Attendance;
using SlateAi.Api.Features.Auth;
using SlateAi.Api.Features.Lessons.Voice;

namespace SlateAi.Api.Features.Lessons;

/// <param name="Source">"voice" when she confirms a voice match (T2); otherwise picked from the list.</param>
public sealed record SaveLessonRequest(Guid ClientRequestId, IReadOnlyList<string> CompetencyIds, string? Source = null);

/// <summary>The day's lesson. Before she confirms a voice note, <c>CompetencyIds</c> is A1's ranked match.</summary>
/// <param name="Status">processing | mapped | low | unclear | confirmed. A confidence number is never sent.</param>
public sealed record LessonView(
    IReadOnlyList<string> CompetencyIds,
    string Source,
    string Status,
    string? SummaryKn,
    string? SummaryEn,
    IReadOnlyList<string> ExamplesUsed);

/// <summary>A day's lesson for T1/T2, plus the picker's suggestion.</summary>
/// <param name="NextInBook">Competency the picker lists first (T2 "Next in the book"); null at the end of the book.</param>
public sealed record LessonDayResponse(DateOnly Date, LessonView? Lesson, string? NextInBook);

public sealed record CompetencyView(string Id, string NameKn, string NameEn, int Sequence, TextbookPage? TextbookPage);

public sealed record ChapterView(int Number, string NameKn, string NameEn, IReadOnlyList<CompetencyView> Competencies);

public sealed record CurriculumResponse(IReadOnlyList<ChapterView> Chapters);

/// <summary>
/// Today's lesson (FR-03, FR-04). This slice is the no-AI path: she picks from the chapter list. The voice
/// path (T-07 PR B) fills the same record. Only confirmed lessons count as missed (ledger).
/// </summary>
public static class LessonEndpoints
{
    public const int MaxLessonsPerDay = 3;
    public const long MaxAudioBytes = 1024 * 1024;

    /// <summary>The phone downsizes her photo to about 200 KB first; this only stops an unresized original.</summary>
    public const long MaxPhotoBytes = 2 * 1024 * 1024;

    /// <summary>A note still "processing" after this (app restarted mid-way) reads as unclear.</summary>
    public static readonly TimeSpan StaleProcessing = TimeSpan.FromMinutes(2);

    private static readonly string[] AudioTypes = ["audio/webm", "audio/ogg", "audio/mp4", "audio/mpeg", "audio/wav", "audio/x-wav"];

    private static readonly string[] PhotoTypes = ["image/jpeg", "image/png", "image/webp"];

    public static IServiceCollection AddLessons(this IServiceCollection services)
    {
        services.AddScoped<ILessonStore, LessonStore>();
        services.AddScoped<LessonMapper>();
        services.AddSingleton<LessonMappingQueue>();
        services.AddHostedService<LessonMappingWorker>();
        return services;
    }

    public static IEndpointRouteBuilder MapLessonEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/curriculum", GetCurriculum)
            .RequireAuthorization()
            .WithTags("Lessons")
            .WithSummary("Chapters and their lessons, for the lesson picker");

        app.MapGet("/api/classes/{classId:guid}/lessons/{date}", GetDayAsync)
            .RequireClassOwner()
            .WithTags("Lessons")
            .WithSummary("The day's lesson and the picker's \"Next in the book\"");

        app.MapPut("/api/classes/{classId:guid}/lessons/{date}", SaveAsync)
            .RequireClassOwner()
            .WithTags("Lessons")
            .WithSummary("Confirm the day's lesson(s) from the list")
            .WithDescription(
                "Creates or replaces the day's lesson (one per day, D-55) as picked and confirmed. 1–3 curriculum IDs. " +
                "Replaying the same clientRequestId is a no-op (offline outbox).");

        app.MapPost("/api/classes/{classId:guid}/lessons/{date}/audio", UploadAudioAsync)
            .RequireClassOwner()
            .DisableAntiforgery() // JSON-only app; the session cookie is SameSite=Strict
            .WithTags("Lessons")
            .WithSummary("Send today's voice note and/or a photo of the page to be matched to lessons (T2)")
            .WithDescription(
                "multipart: file (≤ 1 MB audio) and/or photo (≤ 2 MB jpeg/png/webp of the textbook page or board, " +
                "T-64), clientRequestId. Returns 202 at once; speech-to-text and A1 run in the background and the " +
                "day's lesson moves from processing to mapped, low or unclear. Neither the audio nor the photo is " +
                "stored. 409 if the day's lesson is already confirmed (D-55).");
        return app;
    }

    private static Ok<CurriculumResponse> GetCurriculum(CurriculumGraph curriculum)
    {
        var titles = curriculum.Chapters.ToDictionary(c => c.Number);
        var chapters = curriculum.All
            .GroupBy(c => c.Chapter)
            .OrderBy(g => g.Key)
            .Select(g => new ChapterView(
                g.Key,
                titles.GetValueOrDefault(g.Key)?.NameKn ?? "",
                titles.GetValueOrDefault(g.Key)?.NameEn ?? "",
                g.Select(c => new CompetencyView(c.Id, c.NameKn, c.NameEn, c.Sequence, c.TextbookPage)).ToList()))
            .ToList();
        return TypedResults.Ok(new CurriculumResponse(chapters));
    }

    private static async Task<Results<Ok<LessonDayResponse>, ValidationProblem>> GetDayAsync(
        Guid classId,
        DateOnly date,
        ILessonStore store,
        CurriculumGraph curriculum,
        TimeProvider clock,
        CancellationToken cancellationToken)
    {
        if (date > SchoolClock.Today(clock))
        {
            return FutureDate();
        }

        return TypedResults.Ok(await DayAsync(classId, date, store, curriculum, clock, cancellationToken));
    }

    private static async Task<Results<Accepted<LessonDayResponse>, ValidationProblem, Conflict, StatusCodeHttpResult>>
        UploadAudioAsync(
            Guid classId,
            DateOnly date,
            IFormFile? file,
            IFormFile? photo,
            [Microsoft.AspNetCore.Mvc.FromForm] Guid clientRequestId,
            ILessonStore store,
            LessonMappingQueue queue,
            CurriculumGraph curriculum,
            TimeProvider clock,
            CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();
        if (clientRequestId == Guid.Empty)
        {
            errors["clientRequestId"] = ["Required."];
        }

        if (file is { Length: 0 } || photo is { Length: 0 } || (file is null && photo is null))
        {
            errors["file"] = ["Send the recording or the photo."];
        }

        if (date > SchoolClock.Today(clock))
        {
            errors["date"] = ["Can't use a future date."];
        }

        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        if (file?.Length > MaxAudioBytes || photo?.Length > MaxPhotoBytes)
        {
            return TypedResults.StatusCode(StatusCodes.Status413PayloadTooLarge);
        }

        var contentType = MediaType(file);
        var photoType = MediaType(photo);
        if ((file is not null && !AudioTypes.Contains(contentType)) || (photo is not null && !PhotoTypes.Contains(photoType)))
        {
            return TypedResults.StatusCode(StatusCodes.Status415UnsupportedMediaType);
        }

        var startedAt = clock.GetUtcNow();
        switch (await store.StartVoiceAsync(classId, clientRequestId, date, startedAt, cancellationToken))
        {
            case VoiceStart.AlreadyConfirmed:
                return TypedResults.Conflict();
            case VoiceStart.Started:
                var job = new MappingJob(
                    classId,
                    date,
                    startedAt,
                    await BytesAsync(file, cancellationToken),
                    file?.FileName ?? "note",
                    contentType,
                    photo is null ? null : new LlmImage((await BytesAsync(photo, cancellationToken))!, photoType));
                if (!queue.TryEnqueue(job))
                {
                    await store.SaveMappingAsync(classId, date, startedAt, A1Validator.Unclear("error:queue-full"), cancellationToken);
                }

                break;
        }

        return TypedResults.Accepted(
            (string?)null, await DayAsync(classId, date, store, curriculum, clock, cancellationToken));
    }

    private static string MediaType(IFormFile? file) =>
        (file?.ContentType ?? "").Split(';')[0].Trim().ToLowerInvariant();

    private static async Task<byte[]?> BytesAsync(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null)
        {
            return null;
        }

        using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer, cancellationToken);
        return buffer.ToArray();
    }

    private static async Task<Results<Ok<LessonDayResponse>, ValidationProblem>> SaveAsync(
        Guid classId,
        DateOnly date,
        SaveLessonRequest request,
        ILessonStore store,
        CurriculumGraph curriculum,
        TimeProvider clock,
        CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();
        if (request.ClientRequestId == Guid.Empty)
        {
            errors["clientRequestId"] = ["Required."];
        }

        var ids = (request.CompetencyIds ?? []).Distinct(StringComparer.Ordinal).ToList();
        if (ids.Count is 0 or > MaxLessonsPerDay)
        {
            errors["competencyIds"] = [$"Choose 1 to {MaxLessonsPerDay} lessons."];
        }
        else if (ids.FirstOrDefault(id => !curriculum.Contains(id)) is { } unknown)
        {
            errors["competencyIds"] = [$"Unknown lesson {unknown}."];
        }

        if (date > SchoolClock.Today(clock))
        {
            errors["date"] = ["Can't use a future date."];
        }

        if (request.Source is not (null or LessonValues.Picked or LessonValues.Voice))
        {
            errors["source"] = ["Use \"voice\" or \"picked\"."];
        }

        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        // Teaching order, so the done card and missed lessons read in book order.
        var ordered = ids.OrderBy(id => curriculum.Get(id)!.Sequence).ToList();
        await store.SaveConfirmedAsync(
            classId, request.ClientRequestId, date, ordered, request.Source ?? LessonValues.Picked, cancellationToken);
        return TypedResults.Ok(await DayAsync(classId, date, store, curriculum, clock, cancellationToken));
    }

    private static async Task<LessonDayResponse> DayAsync(
        Guid classId,
        DateOnly date,
        ILessonStore store,
        CurriculumGraph curriculum,
        TimeProvider clock,
        CancellationToken cancellationToken)
    {
        var lesson = await store.GetDayAsync(classId, date, cancellationToken);
        var last = (await store.LastConfirmedBeforeAsync(classId, date, cancellationToken))
            .Where(curriculum.Contains)
            .MaxBy(id => curriculum.Get(id)!.Sequence);
        return new LessonDayResponse(
            date,
            lesson is null ? null : View(lesson, clock.GetUtcNow()),
            curriculum.NextInBook(last)?.Id);
    }

    private static LessonView View(LessonRecord lesson, DateTimeOffset now)
    {
        var stale = lesson.Status == LessonValues.Processing
            && lesson.MappingStartedAt is { } started && now - started > StaleProcessing;
        return new LessonView(
            lesson.CompetencyIds,
            lesson.Source,
            stale ? LessonValues.Unclear : lesson.Status,
            lesson.SummaryKn,
            lesson.SummaryEn,
            lesson.ExamplesUsed ?? []);
    }

    private static ValidationProblem FutureDate() =>
        TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["date"] = ["Can't use a future date."] });
}
