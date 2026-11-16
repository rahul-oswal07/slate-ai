using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;
using SlateAi.Api.Features.Auth;
using SlateAi.Api.Features.EveningMessages;
using SlateAi.Api.Features.Ledger;

namespace SlateAi.Api.Features.WeeklyStar;

/// <param name="WhatsApp">The WhatsApp message (template <c>weekly_star_v1</c>).</param>
/// <param name="Message">Screen 1 of the family's page.</param>
/// <param name="Badge">The badge card.</param>
public sealed record StarWordingView(string Language, IReadOnlyList<TextPart> WhatsApp, IReadOnlyList<TextPart> Message, IReadOnlyList<TextPart> Badge);

/// <param name="Approved">She approved this version; an older approval doesn't count (D-49).</param>
public sealed record WeeklyStarWordingResponse(string Version, bool Approved, IReadOnlyList<StarWordingView> Languages);

public sealed record ApproveWeeklyStarRequest(Guid ClientRequestId, string Version);

/// <summary>
/// The weekly star for families (T-125, D-118, guardrail 3): the wording she approves once, filled with an example child.
/// The send is the 6 pm job's (<see cref="WeeklyStarSender"/>); the family's page is in <c>Features/Parent</c>. No LLM.
/// </summary>
public static class WeeklyStarEndpoints
{
    public static IServiceCollection AddWeeklyStar(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<WeeklyStarOptions>(configuration.GetSection(WeeklyStarOptions.Section));
        services.AddSingleton(sp => WeeklyStarWording.Load(
            Path.Combine(AppContext.BaseDirectory, sp.GetRequiredService<IOptions<WeeklyStarOptions>>().Value.WordingFile)));
        services.AddScoped<IWeeklyStarStore, WeeklyStarStore>();
        services.AddScoped<WeeklyStarSender>();
        return services;
    }

    public static IEndpointRouteBuilder MapWeeklyStarEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/classes/{classId:guid}/weekly-star", GetAsync)
            .RequireClassOwner()
            .WithTags("Weekly star")
            .WithSummary("The weekly star wording, filled with an example child (T-125)")
            .WithDescription(
                "One entry per language: the WhatsApp text, the page's message and its badge card, as text parts with " +
                "the child slot marked. approved is true only for the current version.");

        app.MapPost("/api/classes/{classId:guid}/weekly-star/approve", ApproveAsync)
            .RequireClassOwner()
            .WithTags("Weekly star")
            .WithSummary("Approve the weekly star wording once (T-125, D-118)")
            .WithDescription(
                "Approves every language at the given version; approving again is a no-op. 409 with the latest wording " +
                "if it changed since she saw it (D-49). Nothing goes to a family until this is done (guardrail 3).");

        return app;
    }

    private static async Task<Results<Ok<WeeklyStarWordingResponse>, NotFound>> GetAsync(
        Guid classId, ILedgerStore ledger, IWeeklyStarStore store, WeeklyStarWording wording, CancellationToken cancellationToken) =>
        await WordingAsync(classId, ledger, store, wording, cancellationToken) is { } response
            ? TypedResults.Ok(response)
            : TypedResults.NotFound();

    private static async Task<Results<Ok<WeeklyStarWordingResponse>, Conflict<WeeklyStarWordingResponse>, NotFound, ValidationProblem>>
        ApproveAsync(
            Guid classId,
            ApproveWeeklyStarRequest request,
            HttpContext http,
            ILedgerStore ledger,
            IWeeklyStarStore store,
            WeeklyStarWording wording,
            CancellationToken cancellationToken)
    {
        if (request.ClientRequestId == Guid.Empty || string.IsNullOrWhiteSpace(request.Version))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                [request.ClientRequestId == Guid.Empty ? "clientRequestId" : "version"] = ["Required."],
            });
        }

        if (await WordingAsync(classId, ledger, store, wording, cancellationToken) is not { } current)
        {
            return TypedResults.NotFound();
        }

        if (request.Version != wording.Version)
        {
            return TypedResults.Conflict(current);
        }

        await store.ApproveAsync(classId, request.Version, http.User.GetTeacherId()!.Value, cancellationToken);
        return TypedResults.Ok(current with { Approved = true });
    }

    private static async Task<WeeklyStarWordingResponse?> WordingAsync(
        Guid classId, ILedgerStore ledger, IWeeklyStarStore store, WeeklyStarWording wording, CancellationToken cancellationToken)
    {
        if (await ledger.LoadAsync(classId, cancellationToken) is not { } data)
        {
            return null;
        }

        // A real child from her class, as on the evening message's example (A-20: never "{child}").
        var example = data.Children.OrderBy(c => c.RollNo).FirstOrDefault()?.FirstName ?? "";
        var languages = wording.Languages
            .OrderBy(l => l.Key, StringComparer.Ordinal)
            .Select(l => new StarWordingView(l.Key, Parts(l.Value.WhatsApp, example), Parts(l.Value.Message, example), Parts(l.Value.Badge, example)))
            .ToList();
        var approved = await store.GetApprovedVersionAsync(classId, cancellationToken) == wording.Version;
        return new WeeklyStarWordingResponse(wording.Version, approved, languages);
    }

    /// <summary>The text split around {child}, the name marked as the slot code fills.</summary>
    internal static IReadOnlyList<TextPart> Parts(string text, string child)
    {
        var parts = new List<TextPart>();
        var pieces = text.Split(WeeklyStarWording.ChildSlot);
        for (var i = 0; i < pieces.Length; i++)
        {
            if (i > 0)
            {
                parts.Add(new TextPart(child, "child"));
            }

            if (pieces[i].Length > 0)
            {
                parts.Add(new TextPart(pieces[i], null));
            }
        }

        return parts;
    }
}
