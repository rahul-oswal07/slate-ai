using Microsoft.AspNetCore.Http.HttpResults;
using SlateAi.Api.Ai;
using SlateAi.Api.Curriculum;
using SlateAi.Api.Features.Attendance;
using SlateAi.Api.Features.Auth;
using SlateAi.Api.Features.EveningMessages;
using SlateAi.Api.Features.Lessons.Voice;
using SlateAi.Api.Features.TodaysMessage;

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
/// <param name="SentToFamilies">Tonight's message for this day has gone to at least one family, so the confirmed lesson
/// can no longer be changed (T-143, D-124).</param>
public sealed record LessonDayResponse(DateOnly Date, LessonView? Lesson, string? NextInBook, bool SentToFamilies = false);

/// <param name="HasExercises">The lesson has the book's exercise numbers (D-93, T-128): the badge in the picker, on the
/// confirm screen and on Today's lesson card.</param>
public sealed record CompetencyView(string Id, string NameKn, string NameEn, int Sequence, TextbookPage? TextbookPage, bool HasExercises);

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
        app.MapGet("/api/classes/{classId:guid}/curriculum", GetCurriculumAsync)
            .RequireClassOwner()
            .WithTags("Lessons")
            .WithSummary("The class's chapters and their lessons, for the lesson picker")
            .WithDescription(
                "The curriculum the class follows (T-145, D-125): the built-in one, or a published one made from a " +
                "textbook. `hasExercises`: the lesson has the book's own exercise numbers (D-93, T-128).");

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
                "Replaying the same clientRequestId is a no-op (offline outbox). 409 with the day as it is when tonight's " +
                "message has already gone to families and the lesson would change (T-143, D-124); the same lesson is fine. " +
                "Confirming over a voice or photo match also saves what A1 offered and what she chose as an eval case " +
                "(T-102; IDs and A1's summary only, see `/api/admin/lesson-feedback`).");

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

    private static async Task<Ok<CurriculumResponse>> GetCurriculumAsync(
        Guid classId, ICurriculumProvider curricula, CancellationToken cancellationToken)
    {
        var curriculum = await curricula.ForClassAsync(classId, cancellationToken);
        var titles = curriculum.Chapters.ToDictionary(c => c.Number);
        var chapters = curriculum.All
            .GroupBy(c => c.Chapter)
            .OrderBy(g => g.Key)
            .Select(g => new ChapterView(
                g.Key,
                titles.GetValueOrDefault(g.Key)?.NameKn ?? "",
                titles.GetValueOrDefault(g.Key)?.NameEn ?? "",
                g.Select(c => new CompetencyView(c.Id, c.NameKn, c.NameEn, c.Sequence, c.TextbookPage, c.Exercises?.HasAny == true)).ToList()))
            .ToList();
        return TypedResults.Ok(new CurriculumResponse(chapters));
    }

    private static async Task<Results<Ok<LessonDayResponse>, ValidationProblem>> GetDayAsync(
        Guid classId,
        DateOnly date,
        ILessonStore store,
        IEveningStore evening,
        ICurriculumProvider curricula,
        TimeProvider clock,
        CancellationToken cancellationToken)
    {
        if (date > SchoolClock.Today(clock))
        {
            return FutureDate();
        }

        return TypedResults.Ok(await DayAsync(classId, date, store, evening, curricula, clock, cancellationToken));
    }

    private static async Task<Results<Accepted<LessonDayResponse>, ValidationProblem, Conflict, StatusCodeHttpResult>>
        UploadAudioAsync(
            Guid classId,
            DateOnly date,
            IFormFile? file,
            IFormFile? photo,
            [Microsoft.AspNetCore.Mvc.FromForm] Guid clientRequestId,
            ILessonStore store,
            ITodaysMessageStore todays,
            IEveningStore evening,
            LessonMappingQueue queue,
            ICurriculumProvider curricula,
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
        // D-104: a note may still replace a confirmed lesson until today's message is approved or tonight's has gone.
        var replaceConfirmed = LessonWindow.CanReplaceConfirmed(
            await todays.GetAsync(classId, date, cancellationToken),
            (await evening.GetSentChildIdsAsync(classId, date, cancellationToken)).Count > 0);
        switch (await store.StartVoiceAsync(classId, clientRequestId, date, startedAt, replaceConfirmed, cancellationToken))
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

                // The reply describes the note just accepted ("processing"), so it is read before the job is queued:
                // read afterwards, the background mapper may already have moved the lesson on (T-124).
                var accepted = await DayAsync(classId, date, store, evening, curricula, clock, cancellationToken);
                if (queue.TryEnqueue(job))
                {
                    return TypedResults.Accepted((string?)null, accepted);
                }

                await store.SaveMappingAsync(classId, date, startedAt, A1Validator.Unclear("error:queue-full"), cancellationToken);
                break;
        }

        return TypedResults.Accepted(
            (string?)null, await DayAsync(classId, date, store, evening, curricula, clock, cancellationToken));
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

    private static async Task<Results<Ok<LessonDayResponse>, ValidationProblem, Conflict<LessonDayResponse>>> SaveAsync(
        Guid classId,
        DateOnly date,
        SaveLessonRequest request,
        ILessonStore store,
        IEveningStore evening,
        ICurriculumProvider curricula,
        TodaysMessageService todays,
        PromptLibrary prompts,
        TimeProvider clock,
        CancellationToken cancellationToken)
    {
        var curriculum = await curricula.ForClassAsync(classId, cancellationToken);
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

        // T-143, D-124: once tonight's message has gone, families have the message about the confirmed lesson; another
        // lesson would make Missed lessons disagree with it, and the message is never sent twice. The same lesson is fine.
        var current = await store.GetDayAsync(classId, date, cancellationToken);
        if (current is { Status: LessonValues.Confirmed }
            && !current.CompetencyIds.Order(StringComparer.Ordinal).SequenceEqual(ordered.Order(StringComparer.Ordinal))
            && await SentToFamiliesAsync(evening, classId, date, cancellationToken))
        {
            return TypedResults.Conflict(await DayAsync(classId, date, store, evening, curricula, clock, cancellationToken));
        }

        // T-102: confirming over A1's match keeps what A1 offered and what she chose, as an eval case.
        var source = request.Source ?? LessonValues.Picked;
        var feedback = LessonFeedbackBuilder.From(
            date, current, ordered, source, prompts.Get(LessonMapper.PromptId).Version);
        var saved = await store.SaveConfirmedAsync(
            classId, request.ClientRequestId, date, ordered, source, feedback, cancellationToken);
        // D-91: confirming the lesson starts today's message for absent families (a replay of the same lesson doesn't).
        await todays.StartAsync(classId, date, saved.CompetencyIds, force: false, cancellationToken);
        return TypedResults.Ok(await DayAsync(classId, date, store, evening, curricula, clock, cancellationToken));
    }

    private static async Task<LessonDayResponse> DayAsync(
        Guid classId,
        DateOnly date,
        ILessonStore store,
        IEveningStore evening,
        ICurriculumProvider curricula,
        TimeProvider clock,
        CancellationToken cancellationToken)
    {
        var curriculum = await curricula.ForClassAsync(classId, cancellationToken);
        var lesson = await store.GetDayAsync(classId, date, cancellationToken);
        var last = (await store.LastConfirmedBeforeAsync(classId, date, cancellationToken))
            .Where(curriculum.Contains)
            .MaxBy(id => curriculum.Get(id)!.Sequence);
        return new LessonDayResponse(
            date,
            lesson is null ? null : View(lesson, clock.GetUtcNow()),
            curriculum.NextInBook(last)?.Id,
            await SentToFamiliesAsync(evening, classId, date, cancellationToken));
    }

    /// <summary>Tonight's message for the day has gone to at least one family (T-143, D-124).</summary>
    private static async Task<bool> SentToFamiliesAsync(
        IEveningStore evening, Guid classId, DateOnly date, CancellationToken cancellationToken) =>
        (await evening.GetSentChildIdsAsync(classId, date, cancellationToken)).Count > 0;

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
