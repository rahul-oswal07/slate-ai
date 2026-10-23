using Microsoft.AspNetCore.Http.HttpResults;
using SlateAi.Api.Curriculum;
using SlateAi.Api.Features.Attendance;
using SlateAi.Api.Features.Auth;
using SlateAi.Api.Features.Ledger;

namespace SlateAi.Api.Features.Buddy;

/// <param name="HomeworkReturned">"Brought homework" (D-22); null leaves it.</param>
/// <param name="Done">"Mark buddy time done"; false is Undo; null leaves it.</param>
public sealed record SaveBuddyTicksRequest(Guid ClientRequestId, bool? HomeworkReturned, bool? Done);

/// <summary>
/// T5 Back today (FR-12, FR-19, D-22, D-59): a buddy card for every child who came back, computed by code from
/// attendance and her confirmed lessons, and her two ticks. No AI.
/// </summary>
public static class BuddyEndpoints
{
    public static IServiceCollection AddBuddyCards(this IServiceCollection services) =>
        services.AddScoped<IBuddyStore, BuddyStore>();

    public static IEndpointRouteBuilder MapBuddyEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/classes/{classId:guid}/buddy-cards/{date}", GetAsync)
            .RequireClassOwner()
            .WithTags("Buddy cards")
            .WithSummary("Buddy cards for the children who came back on a day (T5)")
            .WithDescription(
                "One card per child present that day whose absence ended that day, phone or not, stopped or not. " +
                "Buddy = a classmate in class on the most missed days (null: teacher-led, D-59); 3 sums made by code " +
                "from the catch-up's first target (else the latest missed lesson); textbook Kannada words and page. " +
                "Never the family's channel or the picture-check result (B-20).");

        app.MapPut("/api/classes/{classId:guid}/buddy-cards/{date}/{childId:guid}", SaveAsync)
            .RequireClassOwner()
            .WithTags("Buddy cards")
            .WithSummary("Tick \"Brought homework\" or \"Buddy time done\" on a card (D-22)")
            .WithDescription(
                "Either or both; false undoes. Replaying the same clientRequestId changes nothing. 404 when the child " +
                "didn't come back that day. Returns the card.");
        return app;
    }

    private static async Task<Results<Ok<BuddyCardsResponse>, NotFound, ValidationProblem>> GetAsync(
        Guid classId,
        DateOnly date,
        ILedgerStore ledger,
        IBuddyStore store,
        CurriculumGraph curriculum,
        TimeProvider clock,
        CancellationToken cancellationToken)
    {
        if (date > SchoolClock.Today(clock))
        {
            return FutureDate();
        }

        return await CardsAsync(classId, date, ledger, store, curriculum, cancellationToken) is { } cards
            ? TypedResults.Ok(new BuddyCardsResponse(date, cards))
            : TypedResults.NotFound();
    }

    private static async Task<Results<Ok<BuddyCardView>, NotFound, ValidationProblem>> SaveAsync(
        Guid classId,
        DateOnly date,
        Guid childId,
        SaveBuddyTicksRequest request,
        ILedgerStore ledger,
        IBuddyStore store,
        CurriculumGraph curriculum,
        TimeProvider clock,
        CancellationToken cancellationToken)
    {
        if (request.ClientRequestId == Guid.Empty)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["clientRequestId"] = ["Required."] });
        }

        if (date > SchoolClock.Today(clock))
        {
            return FutureDate();
        }

        var cards = await CardsAsync(classId, date, ledger, store, curriculum, cancellationToken);
        if (cards?.Any(c => c.ChildId == childId) != true)
        {
            return TypedResults.NotFound();
        }

        await store.SaveTicksAsync(
            classId, childId, date, new BuddyTickChange(request.HomeworkReturned, request.Done), request.ClientRequestId,
            clock.GetUtcNow(), cancellationToken);
        var card = (await CardsAsync(classId, date, ledger, store, curriculum, cancellationToken))!.Single(c => c.ChildId == childId);
        return TypedResults.Ok(card);
    }

    private static async Task<IReadOnlyList<BuddyCardView>?> CardsAsync(
        Guid classId, DateOnly date, ILedgerStore ledger, IBuddyStore store, CurriculumGraph curriculum,
        CancellationToken cancellationToken)
    {
        if (await ledger.LoadAsync(classId, cancellationToken) is not { } data)
        {
            return null;
        }

        var targets = await store.GetTargetsAsync(classId, cancellationToken);
        var ticks = await store.GetTicksAsync(classId, date, cancellationToken);
        return BuddyCardBuilder.Build(data, targets, ticks, curriculum, date);
    }

    private static ValidationProblem FutureDate() =>
        TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["date"] = ["Can't use a future date."] });
}
