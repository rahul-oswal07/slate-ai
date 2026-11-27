using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SlateAi.Api.Features.Delivery;

namespace SlateAi.Api.Features.WhatsAppChat;

/// <summary>
/// Step 2 of WhatsApp (T-106): Meta's webhook for today's message inside the chat. Answers 404 unless
/// <c>WhatsApp:ChatLesson:Enabled</c> is true. The URL to give Meta is <c>https://{host}/api/whatsapp/webhook</c>,
/// subscribed to <c>messages</c>.
/// </summary>
public static class WhatsAppChatEndpoints
{
    public const string Path = "/api/whatsapp/webhook";

    /// <summary>Meta's events are a few KB; anything far bigger isn't one.</summary>
    private const int MaxBodyBytes = 512 * 1024;

    public static IServiceCollection AddWhatsAppChat(this IServiceCollection services)
    {
        services.AddSingleton(_ => ChatWording.Load(System.IO.Path.Combine(AppContext.BaseDirectory, ChatWording.File)));
        services.AddSingleton<WhatsAppMediaCache>();
        services.AddScoped<IWhatsAppChatStore, WhatsAppChatStore>();
        services.AddScoped<ChatHandler>();
        return services;
    }

    public static IEndpointRouteBuilder MapWhatsAppChatEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Path).AllowAnonymous().WithTags("WhatsApp");

        group.MapGet("", Verify)
            .WithSummary("Meta's webhook verification: echoes hub.challenge for our verify token (T-106)")
            .WithDescription(
                "hub.mode must be \"subscribe\" and hub.verify_token must equal WhatsApp:VerifyToken (403 otherwise). " +
                "404 while WhatsApp:ChatLesson:Enabled is false.");

        group.MapPost("", ReceiveAsync)
            .WithSummary("Meta's webhook: the family's taps and replies in the chat (T-106)")
            .WithDescription(
                "X-Hub-Signature-256 must be the HMAC-SHA256 of the body with WhatsApp:AppSecret (401 otherwise). Each " +
                "message is handled once by its ID. \"Send it here\" on todays_lesson_chat_v1 sends the greeting and " +
                "lesson voice notes, one text, the picture question with reply buttons and the reason list, only from an " +
                "approved today's message and within 24 hours of the tap; a reply button or list tap is saved as on the " +
                "family page; STOP stops the family's messages. Unknown tokens and numbers are ignored with 200. 404 while " +
                "WhatsApp:ChatLesson:Enabled is false.")
            .DisableAntiforgery();
        return app;
    }

    private static Results<ContentHttpResult, NotFound, StatusCodeHttpResult> Verify(
        [FromQuery(Name = "hub.mode")] string? mode,
        [FromQuery(Name = "hub.verify_token")] string? verifyToken,
        [FromQuery(Name = "hub.challenge")] string? challenge,
        IOptions<WhatsAppOptions> options)
    {
        var settings = options.Value;
        if (!settings.ChatLesson.Enabled)
        {
            return TypedResults.NotFound();
        }

        return mode == "subscribe" && challenge is not null && SameSecret(verifyToken, settings.VerifyToken)
            ? TypedResults.Text(challenge, "text/plain")
            : TypedResults.StatusCode(StatusCodes.Status403Forbidden);
    }

    private static async Task<Results<Ok, UnauthorizedHttpResult, NotFound, BadRequest, StatusCodeHttpResult>> ReceiveAsync(
        HttpRequest request,
        IOptions<WhatsAppOptions> options,
        ChatHandler handler,
        ILoggerFactory loggers,
        CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (!settings.ChatLesson.Enabled)
        {
            return TypedResults.NotFound();
        }

        if (request.ContentLength > MaxBodyBytes)
        {
            return TypedResults.BadRequest();
        }

        using var buffer = new MemoryStream();
        await request.Body.CopyToAsync(buffer, cancellationToken);
        if (buffer.Length > MaxBodyBytes)
        {
            return TypedResults.BadRequest();
        }

        var logger = loggers.CreateLogger(typeof(WhatsAppChatEndpoints));
        var body = buffer.ToArray();
        if (!WebhookPayload.SignatureValid(body, request.Headers[WebhookPayload.SignatureHeader], settings.AppSecret))
        {
            logger.LogWarning("WhatsApp webhook: bad or missing signature");
            return TypedResults.Unauthorized();
        }

        IReadOnlyList<InboundMessage> messages;
        try
        {
            messages = WebhookPayload.Messages(Encoding.UTF8.GetString(body), settings.PhoneNumberId);
        }
        catch (JsonException)
        {
            return TypedResults.BadRequest();
        }

        // One bad message doesn't stop the others. If any threw, 500 makes Meta send the event again: the ones already
        // handled are then skipped as duplicates.
        var failed = false;
        foreach (var message in messages)
        {
            try
            {
                await handler.HandleAsync(message, cancellationToken);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                logger.LogError(e, "WhatsApp event {EventId} threw", ProviderMessageIds.Hash(message.Id));
                failed = true;
            }
        }

        return failed ? TypedResults.StatusCode(StatusCodes.Status500InternalServerError) : TypedResults.Ok();
    }

    private static bool SameSecret(string? given, string? expected) =>
        !string.IsNullOrEmpty(expected) && given is not null
        && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(given), Encoding.UTF8.GetBytes(expected));
}
