using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;
using SlateAi.Api.Features.Delivery;

namespace SlateAi.Api.Features.Admin;

/// <summary>Where a child's WhatsApp number comes from (T-118, D-110).</summary>
public static class PhoneFrom
{
    /// <summary>Saved on the admin page: WhatsApp goes there first.</summary>
    public const string Saved = "saved";

    /// <summary>The <c>WhatsApp__Recipients__{childId}</c> server setting, used when nothing is saved.</summary>
    public const string Setting = "setting";
}

/// <param name="Phone">The number WhatsApp would use (saved, else the server setting), or null: WhatsApp skips the family.</param>
/// <param name="PhoneFrom">A <see cref="Admin.PhoneFrom"/> value, or null with no number.</param>
/// <param name="Consent">"yes", "no" or "nophone": nothing is sent unless "yes", whatever the number.</param>
/// <param name="Stopped">The family tapped "Stop these messages": nothing is sent.</param>
/// <param name="LanguageChanged">The home language was changed on this page (T-122), not the seed's.</param>
public sealed record AdminChild(
    Guid Id, int RollNo, string FirstName, string HomeLanguage, string Consent, bool Stopped, string? Phone, string? PhoneFrom,
    bool LanguageChanged);

/// <param name="Label">"Shobha · Grade 3": the main teacher and grade.</param>
public sealed record AdminChildrenClass(Guid Id, string Label, IReadOnlyList<AdminChild> Children);

public sealed record AdminChildrenResponse(IReadOnlyList<AdminChildrenClass> Classes);

/// <param name="Phone">A 10-digit Indian mobile, or a full number with its country code; blank clears the saved number.</param>
public sealed record SetParentPhoneRequest(Guid ClientRequestId, string? Phone);

/// <param name="Language">"kn", "en", "te", "ta" or "hi".</param>
public sealed record SetHomeLanguageRequest(Guid ClientRequestId, string? Language);

/// <summary>
/// The admin children page (T-118, D-110): every class's children with the number their family's WhatsApp goes to, which
/// can be saved or cleared here, and their home language, which can be changed here (T-122, D-113). Open to anyone with the address, like the other admin pages (D-112). The numbers only ever go to WhatsApp's send (guardrail 1); they are never logged.
/// </summary>
public static class AdminChildEndpoints
{
    public static IServiceCollection AddAdminChildren(this IServiceCollection services) =>
        services.AddScoped<IAdminChildStore, AdminChildStore>();

    /// <summary>Mapped by <see cref="AdminEndpoints.MapAdminEndpoints"/>.</summary>
    public static IEndpointRouteBuilder MapAdminChildEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/admin/children", ListAsync)
            .WithTags("Admin")
            .WithSummary("Every class's children with the number their family's WhatsApp goes to (T-118)")
            .WithDescription(
                "phone is the number saved " +
                "on this page (phoneFrom saved), else the WhatsApp__Recipients setting (phoneFrom setting), else null: WhatsApp " +
                "skips the family. Seeded numbers are fictional and never shown. Consent and stopped still decide whether " +
                "anything is sent. Admin page; no key (D-112).");

        app.MapPut("/api/admin/children/{childId:guid}/phone", SetPhoneAsync)
            .WithTags("Admin")
            .WithSummary("Save or clear the number a family's WhatsApp goes to (T-118, D-110)")
            .WithDescription(
                "Body { clientRequestId, phone }: a 10-digit Indian mobile (+91 is added) or a full number with its country " +
                "code, stored as E.164; blank clears it, and WhatsApp falls back to the server setting. Idempotent per " +
                "clientRequestId. 400 for anything else, 404 for an unknown child. Admin page; no key (D-112).");

        app.MapPut("/api/admin/children/{childId:guid}/language", SetLanguageAsync)
            .WithTags("Admin")
            .WithSummary("Change a child's home language (T-122, D-113)")
            .WithDescription(
                "Body { clientRequestId, language }: kn, en, te, ta or hi. Everything made or sent after the change uses " +
                "it (evening message, today's message, lesson audio, catch-ups); what was already made keeps its language. " +
                "Kept across the demo Reset. Sends nothing. Idempotent per clientRequestId. 400 for another language, 404 " +
                "for an unknown child. Admin page; no key (D-112).");
        return app;
    }

    private static async Task<Ok<AdminChildrenResponse>> ListAsync(
        HttpContext http, IAdminChildStore store, IOptions<WhatsAppOptions> whatsApp, CancellationToken cancellationToken)
    {
        var classes = await store.ListAsync(cancellationToken);
        AdminEndpoints.NoStore(http);
        return TypedResults.Ok(new AdminChildrenResponse(classes
            .Select(c => new AdminChildrenClass(
                c.Id, $"{c.TeacherName} · Grade {c.Grade}", c.Children.Select(r => View(r, whatsApp.Value)).ToList()))
            .ToList()));
    }

    private static async Task<Results<Ok<AdminChild>, NotFound, ValidationProblem>> SetPhoneAsync(
        Guid childId,
        SetParentPhoneRequest request,
        HttpContext http,
        IAdminChildStore store,
        IOptions<WhatsAppOptions> whatsApp,
        TimeProvider clock,
        CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();
        if (request.ClientRequestId == Guid.Empty)
        {
            errors["clientRequestId"] = ["Required."];
        }

        var (phone, error) = ParentPhoneRules.Normalise(request.Phone);
        if (error is not null)
        {
            errors["phone"] = [error];
        }

        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        if (await store.SetPhoneAsync(request.ClientRequestId, childId, phone, clock.GetUtcNow(), cancellationToken) is not { } row)
        {
            return TypedResults.NotFound();
        }

        AdminEndpoints.NoStore(http);
        return TypedResults.Ok(View(row, whatsApp.Value));
    }

    private static async Task<Results<Ok<AdminChild>, NotFound, ValidationProblem>> SetLanguageAsync(
        Guid childId,
        SetHomeLanguageRequest request,
        HttpContext http,
        IAdminChildStore store,
        IOptions<WhatsAppOptions> whatsApp,
        TimeProvider clock,
        CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();
        if (request.ClientRequestId == Guid.Empty)
        {
            errors["clientRequestId"] = ["Required."];
        }

        var (language, error) = HomeLanguageRules.Normalise(request.Language);
        if (error is not null)
        {
            errors["language"] = [error];
        }

        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        if (await store.SetLanguageAsync(request.ClientRequestId, childId, language!, clock.GetUtcNow(), cancellationToken) is not { } row)
        {
            return TypedResults.NotFound();
        }

        AdminEndpoints.NoStore(http);
        return TypedResults.Ok(View(row, whatsApp.Value));
    }

    /// <summary>The same order WhatsApp uses (<see cref="WhatsAppChannel"/>): the saved number, else the server setting.</summary>
    private static AdminChild View(AdminChildRow row, WhatsAppOptions whatsApp)
    {
        var (phone, from) = row.SavedPhone is { } saved
            ? (saved, PhoneFrom.Saved)
            : whatsApp.RecipientFor(row.Id) is { } setting ? (setting, PhoneFrom.Setting) : ((string?)null, (string?)null);
        return new AdminChild(
            row.Id, row.RollNo, row.FirstName, row.HomeLanguage, row.Consent, row.Stopped, phone, from, row.LanguageChanged);
    }
}
