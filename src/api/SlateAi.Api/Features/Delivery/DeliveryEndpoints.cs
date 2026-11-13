using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;
using SlateAi.Api.Features.Admin;

namespace SlateAi.Api.Features.Delivery;

/// <param name="ChildId">A child listed in <c>WhatsApp:Recipients</c>: the message goes to that registered phone.</param>
public sealed record WhatsAppTestRequest(Guid ClientRequestId, Guid ChildId);

/// <summary>What WhatsApp said. Never echoes the phone number.</summary>
/// <param name="Outcome">"sent", "skipped", "failed" or "retry".</param>
/// <param name="Error">A short code, e.g. "no-recipient", "graph-190" (bad token), "graph-131030" (number not registered).</param>
public sealed record WhatsAppTestResponse(string Outcome, string? Error, string? ProviderMessageId, bool Replay);

/// <summary>
/// The admin "send a test message" (T-40): Meta's pre-approved <c>hello_world</c> template to one registered test
/// phone, so the token, phone number ID and recipient can be checked before our own templates are approved. Mapped only
/// when <c>Admin:Key</c> is set (staging, not prod); a missing or wrong key gets 404 like the other admin routes.
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
        if (!app.ServiceProvider.GetRequiredService<IOptions<AdminOptions>>().Value.Enabled)
        {
            return app;
        }

        app.MapPost("/api/admin/whatsapp/test", SendTestAsync)
            .WithTags("Admin")
            .WithSummary("Send Meta's hello_world template to one registered WhatsApp test phone")
            .WithDescription(
                $"Needs the {AdminEndpoints.KeyHeader} header; 404 without it. childId must be in WhatsApp:Recipients " +
                "(otherwise outcome skipped, error no-recipient). Works whatever Delivery:Channel is, so the Meta setup " +
                "can be checked before switching it on. A repeated clientRequestId returns the first result without " +
                "sending again. The response never contains the phone number. Admin only.");
        return app;
    }

    private static async Task<Results<Ok<WhatsAppTestResponse>, NotFound, ValidationProblem>> SendTestAsync(
        WhatsAppTestRequest request,
        HttpContext http,
        WhatsAppClient client,
        IOptions<WhatsAppOptions> whatsApp,
        IOptions<AdminOptions> admin,
        CancellationToken cancellationToken)
    {
        if (!AdminEndpoints.HasKey(http, admin.Value))
        {
            return TypedResults.NotFound();
        }

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

        var result = whatsApp.Value.RecipientFor(request.ChildId) is { } number
            ? await client.SendTemplateAsync(number, HelloWorld, HelloWorldLanguage, [], null, cancellationToken)
            : ChannelResult.Skipped("no-recipient");
        var response = new WhatsAppTestResponse(result.Outcome, result.Error, result.ProviderMessageId, Replay: false);
        if (result.Outcome != DeliveryStatus.Skipped)
        {
            Sent.TryAdd(request.ClientRequestId, response);
        }

        return TypedResults.Ok(response);
    }
}
