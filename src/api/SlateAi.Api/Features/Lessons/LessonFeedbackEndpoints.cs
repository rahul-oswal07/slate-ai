using Microsoft.AspNetCore.Http.HttpResults;

namespace SlateAi.Api.Features.Lessons;

/// <summary>
/// One saved eval case (T-102), in the shape of <c>evals/cases/a1-lesson-mapping.json</c> plus what A1 offered.
/// </summary>
/// <param name="Transcript">A1's Kannada summary of her note (English when there is none). It stands in for the
/// transcript, which is never stored.</param>
/// <param name="Expected">The first lesson she confirmed (book order).</param>
/// <param name="AlsoAcceptable">The other lessons she confirmed.</param>
/// <param name="Matched">A1's ranked match, as she saw it.</param>
public sealed record LessonFeedbackCase(
    string Id,
    string? Transcript,
    string Expected,
    IReadOnlyList<string> AlsoAcceptable,
    string Outcome,
    string ConfirmedVia,
    string MatchStatus,
    IReadOnlyList<string> Matched,
    double? Confidence,
    string? SummaryEn,
    IReadOnlyList<string> ExamplesUsed,
    int PromptVersion,
    Guid ClassId,
    DateOnly Date,
    DateTimeOffset SavedAt);

public sealed record LessonFeedbackCases(IReadOnlyList<LessonFeedbackCase> Cases);

/// <summary>Her lesson corrections as A1 eval cases (T-102), on the admin pages (no key, D-112).</summary>
public static class LessonFeedbackEndpoints
{
    public const int MaxCases = 500;

    public static IEndpointRouteBuilder MapLessonFeedbackEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/admin/lesson-feedback", ListAsync)
            .WithTags("Admin")
            .WithSummary("Teachers' lesson confirmations over A1's match, as A1 eval cases")
            .WithDescription(
                "Newest first, at most 500; from=YYYY-MM-DD keeps the lesson days from then on. Each case is what A1 " +
                "matched for a note and what the teacher confirmed (accepted, edited or replaced). `transcript` is A1's " +
                "own summary of the note: the transcript and audio are never stored, and no names are kept. Admin page; " +
                "no key (D-112).");
        return app;
    }

    private static async Task<Ok<LessonFeedbackCases>> ListAsync(
        DateOnly? from, ILessonStore store, HttpContext http, CancellationToken cancellationToken)
    {
        Admin.AdminEndpoints.NoStore(http);
        var saved = await store.ListFeedbackAsync(from, MaxCases, cancellationToken);
        return TypedResults.Ok(new LessonFeedbackCases(saved.Select(Case).ToList()));
    }

    internal static LessonFeedbackCase Case(SavedLessonFeedback saved)
    {
        var f = saved.Feedback;
        return new LessonFeedbackCase(
            $"feedback-{f.Date:yyyy-MM-dd}-{saved.Id.ToString("N")[..8]}",
            f.SummaryKn ?? f.SummaryEn,
            f.ChosenCompetencyIds[0],
            f.ChosenCompetencyIds.Skip(1).ToList(),
            f.Outcome,
            f.ConfirmedVia,
            f.MatchStatus,
            f.MatchedCompetencyIds,
            f.Confidence,
            f.SummaryEn,
            f.ExamplesUsed,
            f.PromptVersion,
            saved.ClassId,
            f.Date,
            saved.CreatedAt);
    }
}
