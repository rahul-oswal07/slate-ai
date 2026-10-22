using Microsoft.AspNetCore.Http.HttpResults;
using SlateAi.Api.Curriculum;
using SlateAi.Api.Features.Attendance;
using SlateAi.Api.Features.Auth;

namespace SlateAi.Api.Features.Lessons;

public sealed record SaveLessonRequest(Guid ClientRequestId, IReadOnlyList<string> CompetencyIds);

public sealed record LessonView(IReadOnlyList<string> CompetencyIds, string Source, string Status);

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

    public static IServiceCollection AddLessons(this IServiceCollection services) =>
        services.AddScoped<ILessonStore, LessonStore>();

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

        return TypedResults.Ok(await DayAsync(classId, date, store, curriculum, cancellationToken));
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

        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        // Teaching order, so the done card and missed lessons read in book order.
        var ordered = ids.OrderBy(id => curriculum.Get(id)!.Sequence).ToList();
        await store.SaveConfirmedAsync(classId, request.ClientRequestId, date, ordered, LessonValues.Picked, cancellationToken);
        return TypedResults.Ok(await DayAsync(classId, date, store, curriculum, cancellationToken));
    }

    private static async Task<LessonDayResponse> DayAsync(
        Guid classId, DateOnly date, ILessonStore store, CurriculumGraph curriculum, CancellationToken cancellationToken)
    {
        var lesson = await store.GetDayAsync(classId, date, cancellationToken);
        var last = (await store.LastConfirmedBeforeAsync(classId, date, cancellationToken))
            .Where(curriculum.Contains)
            .MaxBy(id => curriculum.Get(id)!.Sequence);
        return new LessonDayResponse(
            date,
            lesson is null ? null : new LessonView(lesson.CompetencyIds, lesson.Source, lesson.Status),
            curriculum.NextInBook(last)?.Id);
    }

    private static ValidationProblem FutureDate() =>
        TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["date"] = ["Can't use a future date."] });
}
