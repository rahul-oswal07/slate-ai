using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http.HttpResults;
using SlateAi.Api.Features.Admin;

namespace SlateAi.Api.Features.Delivery;

/// <param name="ChildId">A child with a number saved on <c>/admin/children</c>: the message goes to each saved number (D-114, D-122).</param>
public sealed record WhatsAppTestRequest(Guid ClientRequestId, Guid ChildId);

/// <summary>What WhatsApp said. Never echoes the phone number.</summary>
/// <param name="ProviderMessageId">The SHA-256 of WhatsApp's message ID ("sha256:…"), not the wamid itself, which encodes the number.</param>
/// <param name="Outcome">"sent", "skipped", "failed" or "retry": number 1's, or the only saved number's.</param>
/// <param name="Error">A short code, e.g. "no-recipient", "graph-190" (bad token), "graph-131030" (number not registered).</param>
/// <param name="Numbers">One result per saved number, number 1 first (T-140); empty when none is saved.</param>
public sealed record WhatsAppTestResponse(
    string Outcome, string? Error, string? ProviderMessageId, bool Replay, IReadOnlyList<WhatsAppTestNumber> Numbers);

/// <summary>What WhatsApp said for one of the family's numbers (T-140). Never the number itself.</summary>
/// <param name="Slot">1 or 2.</param>
public sealed record WhatsAppTestNumber(int Slot, string Outcome, string? Error, string? ProviderMessageId);

/// <summary>
/// The admin "send a test message" (T-40): Meta's pre-approved <c>hello_world</c> template to each number of one child's family (T-140), so the
/// token, phone number ID and number can be checked before our own templates are approved. Open to anyone with the
/// address, like the admin pages (D-112); it only reaches numbers saved on <c>/admin/children</c> (D-114).
/// Carries no names or school data.
/// </summary>
public static class DeliveryEndpoints
{
    public const string HelloWorld = "hello_world";
    public const string HelloWorldLanguage = "en_US";

    /// <summary>Replays of a test send by clientRequestId, in memory: a diagnostic on one instance needs no table.</summary>
    private static readonly ConcurrentDictionary<Guid, WhatsAppTestResponse> Sent = new();

    public static IEndpointRouteBuilder MapDeliveryEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/admin/whatsapp/test", SendTestAsync)
            .WithTags("Admin")
            .WithSummary("Send Meta's hello_world template to each number saved for one child")
            .WithDescription(
                "No key (D-112). childId must have a number saved on /admin/children (D-114) " +
                "(otherwise outcome skipped, error no-recipient). With two saved numbers both get it (D-122): numbers " +
                "has one result per number, and outcome, error and providerMessageId are number 1's. Works whatever Delivery:Channel is, so the Meta setup " +
                "can be checked before switching it on. A repeated clientRequestId returns the first result without " +
                "sending again. The response never contains the phone number, and the message ID is hashed. Admin only.");
        return app;
    }

    private static async Task<Results<Ok<WhatsAppTestResponse>, ValidationProblem>> SendTestAsync(
        WhatsAppTestRequest request,
        HttpContext http,
        WhatsAppClient client,
        IAdminChildStore children,
        CancellationToken cancellationToken)
    {
        AdminEndpoints.NoStore(http);
        if (request.ClientRequestId == Guid.Empty)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["clientRequestId"] = ["Required."],
            });
        }

        if (Sent.TryGetValue(request.ClientRequestId, out var earlier))
        {
            return TypedResults.Ok(earlier with { Replay = true });
        }

        var numbers = new List<WhatsAppTestNumber>();
        foreach (var saved in await children.GetSavedPhonesAsync(request.ChildId, cancellationToken))
        {
            var sent = await client.SendTemplateAsync(saved.Phone!, HelloWorld, HelloWorldLanguage, [], null, cancellationToken);
            numbers.Add(new WhatsAppTestNumber(saved.Slot, sent.Outcome, sent.Error, sent.ProviderMessageId));
        }

        var first = numbers.FirstOrDefault();
        var response = first is null
            ? new WhatsAppTestResponse(DeliveryStatus.Skipped, "no-recipient", null, Replay: false, [])
            : new WhatsAppTestResponse(first.Outcome, first.Error, first.ProviderMessageId, Replay: false, numbers);
        if (response.Outcome != DeliveryStatus.Skipped)
        {
            Sent.TryAdd(request.ClientRequestId, response);
        }

        return TypedResults.Ok(response);
    }
}
