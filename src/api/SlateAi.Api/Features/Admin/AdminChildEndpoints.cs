using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;
using SlateAi.Api.Data;
using SlateAi.Api.Data.Seed;

namespace SlateAi.Api.Features.Admin;

/// <param name="Phone">Number 1 saved on the admin page; WhatsApp uses only numbers saved here (D-114).</param>
/// <param name="Consent">"yes", "no" or "nophone": nothing is sent unless "yes", whatever the number.</param>
/// <param name="Stopped">The family tapped "Stop these messages": nothing is sent.</param>
/// <param name="LanguageChanged">The home language was changed on this page (T-122), not the seed's.</param>
/// <param name="Phone2">Number 2 saved on the admin page (T-140, D-122). Every message goes to each saved number; with
/// neither, WhatsApp skips the family.</param>
public sealed record AdminChild(
    Guid Id, int RollNo, string FirstName, string HomeLanguage, string Consent, bool Stopped, string? Phone,
    bool LanguageChanged, string? Phone2 = null);

/// <param name="Label">"Shobha · Grade 3": the main teacher and grade.</param>
/// <param name="Demo">The demo class, with the demo on: each child has a simulated family phone at <c>/demo/family/{id}</c> (T-130).</param>
public sealed record AdminChildrenClass(Guid Id, string Label, IReadOnlyList<AdminChild> Children, bool Demo = false);

public sealed record AdminChildrenResponse(IReadOnlyList<AdminChildrenClass> Classes);

/// <param name="Phone">A 10-digit Indian mobile, or a full number with its country code; blank clears the saved number.</param>
/// <param name="Slot">Which of the family's numbers: 1 (the default) or 2 (T-140, D-122).</param>
public sealed record SetParentPhoneRequest(Guid ClientRequestId, string? Phone, int Slot = 1);

/// <param name="Language">"kn", "en", "te", "ta" or "hi".</param>
public sealed record SetHomeLanguageRequest(Guid ClientRequestId, string? Language);

/// <summary>
/// The admin children page (T-118): every class's children with the number their family's WhatsApp goes to, which can
/// be saved or cleared here and is the only number WhatsApp uses (D-114), and their home language, which can be changed
/// here (T-122, D-113). Open to anyone with the address, like the other admin pages (D-112). The numbers only ever go to
/// WhatsApp's send (guardrail 1); they are never logged.
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
            .WithSummary("Every class's children with the numbers their family's WhatsApp goes to (T-118, T-140)")
            .WithDescription(
                "phone and phone2 are the family's number 1 and 2 saved on this page, the only ones WhatsApp uses (D-114); " +
                "every message goes to each saved number (D-122), and with neither WhatsApp skips the family. Seeded numbers are fictional and never shown. Consent and stopped still decide whether " +
                "anything is sent. demo is true for the demo class when the demo is on: each of its children has a " +
                "simulated family phone at /demo/family/{id} (T-130). Admin page; no key (D-112).");

        app.MapPut("/api/admin/children/{childId:guid}/phone", SetPhoneAsync)
            .WithTags("Admin")
            .WithSummary("Save or clear one of the numbers a family's WhatsApp goes to (T-118, T-140, D-114, D-122)")
            .WithDescription(
                "Body { clientRequestId, phone, slot }: slot 1 (the default) or 2 says which of the family's two numbers; " +
                "the other is left as it is. phone is a 10-digit Indian mobile (+91 is added) or a full number with its " +
                "country code, stored as E.164; blank clears it, and WhatsApp then skips that number. Idempotent per " +
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
        HttpContext http,
        IAdminChildStore store,
        IOptions<DemoOptions> demo,
        IOptions<DatabaseOptions> database,
        CancellationToken cancellationToken)
    {
        var classes = await store.ListAsync(cancellationToken);
        // The demo class is the seed's, as in DemoEndpoints; with the demo off, /demo/family isn't mapped (T-130).
        Guid? demoClassId = demo.Value.Enabled
            ? (await DemoSeedFile.LoadAsync(database.Value.SeedFile, cancellationToken)).Class.Id
            : null;
        AdminEndpoints.NoStore(http);
        return TypedResults.Ok(new AdminChildrenResponse(classes
            .Select(c => new AdminChildrenClass(
                c.Id, $"{c.TeacherName} · Grade {c.Grade}", c.Children.Select(r => View(r)).ToList(), c.Id == demoClassId))
            .ToList()));
    }

    private static async Task<Results<Ok<AdminChild>, NotFound, ValidationProblem>> SetPhoneAsync(
        Guid childId,
        SetParentPhoneRequest request,
        HttpContext http,
        IAdminChildStore store,
        TimeProvider clock,
        CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();
        if (request.ClientRequestId == Guid.Empty)
        {
            errors["clientRequestId"] = ["Required."];
        }

        if (request.Slot is not (1 or 2))
        {
            errors["slot"] = ["1 or 2."];
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

        if (await store.SetPhoneAsync(
                request.ClientRequestId, childId, request.Slot, phone, clock.GetUtcNow(), cancellationToken) is not { } row)
        {
            return TypedResults.NotFound();
        }

        AdminEndpoints.NoStore(http);
        return TypedResults.Ok(View(row));
    }

    private static async Task<Results<Ok<AdminChild>, NotFound, ValidationProblem>> SetLanguageAsync(
        Guid childId,
        SetHomeLanguageRequest request,
        HttpContext http,
        IAdminChildStore store,
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
        return TypedResults.Ok(View(row));
    }

    private static AdminChild View(AdminChildRow row) => new(
        row.Id, row.RollNo, row.FirstName, row.HomeLanguage, row.Consent, row.Stopped, row.SavedPhone, row.LanguageChanged,
        row.SavedPhone2);
}
