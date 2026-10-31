using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using SlateAi.Api.Data;
using SlateAi.Api.Data.Entities;

namespace SlateAi.Api.Features.Auth;

public sealed record PinLoginRequest(string Phone, string Pin);

public sealed record WelcomeRequest(Guid ClientRequestId);

/// <summary>Her recorded greeting: where to play it (<c>/api/audio/{id}</c>) and how long it is.</summary>
public sealed record GreetingInfo(string Url, double Seconds);

/// <summary>
/// The signed-in teacher. <c>SetupComplete</c> is false until the first-run welcome is done; <c>Greeting</c> is null
/// until she has recorded one (Today then offers it).
/// </summary>
public sealed record MeResponse(Guid TeacherId, string DisplayName, Guid? ClassId, bool SetupComplete, GreetingInfo? Greeting = null);

public sealed record PinLockedResponse(DateTimeOffset LockedUntil);

/// <summary>
/// Teacher sign-in for seeded teachers: phone + PIN, then a long-lived session cookie (T-47, D-40).
/// There is no registration.
/// </summary>
public static class AuthEndpoints
{
    public const int MaxPinAttempts = 5;
    public static readonly TimeSpan PinLockout = TimeSpan.FromMinutes(15);

    // A shared Android phone with patchy data: stay signed in for a month of use.
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromDays(30);

    private const string WrongPinTitle = "Phone or PIN is wrong";

    // Verified when the phone is unknown, so both failures take the same time.
    private static readonly string DummyHash = new PasswordHasher<Teacher>().HashPassword(null!, "000000");

    public static IServiceCollection AddAuth(this IServiceCollection services)
    {
        services.AddScoped<IAuthStore, AuthStore>();
        services.AddSingleton<IPasswordHasher<Teacher>, PasswordHasher<Teacher>>();

        // Keys in Postgres so sessions survive restarts and deploys.
        services.AddDataProtection().SetApplicationName("slate-ai").PersistKeysToDbContext<AppDbContext>();

        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.Name = "slate.session";
                // The real clock even when a demo date is fixed (T-75): the cookie's expiry is for the browser.
                options.TimeProvider = TimeProvider.System;
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.ExpireTimeSpan = SessionLifetime;
                options.SlidingExpiration = true;
                // An API: answer with status codes, never redirect to a login page.
                options.Events.OnRedirectToLogin = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                };
                options.Events.OnRedirectToAccessDenied = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                };
            });
        services.AddAuthorization();
        return services;
    }

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/pin", SignInAsync)
            .WithTags("Auth")
            .WithSummary("Sign in with phone and PIN")
            .WithDescription(
                $"Sets the session cookie. Wrong phone and wrong PIN give the same 401. After {MaxPinAttempts} " +
                $"wrong PINs the teacher is locked out for {PinLockout.TotalMinutes} minutes (429).");

        app.MapPost("/api/auth/logout", SignOutAsync)
            .WithTags("Auth")
            .WithSummary("Sign out");

        app.MapGet("/api/me", GetMeAsync)
            .RequireAuthorization()
            .WithTags("Auth")
            .WithSummary("The signed-in teacher")
            .WithDescription(
                "setupComplete is false until the first-run welcome is done (Teacher.ConsentAt). greeting is her " +
                "recorded greeting, or null.");

        app.MapPost("/api/me/welcome", WelcomeAsync)
            .RequireAuthorization()
            .WithTags("Auth")
            .WithSummary("Finish the first-run welcome without a greeting")
            .WithDescription(
                "For a teacher who continues without recording (microphone blocked, D-34). Sets Teacher.ConsentAt on " +
                "the first call; replays keep the original time. Saving a greeting sets it too (POST /api/me/greeting).");
        return app;
    }

    private static async Task<Results<Ok<MeResponse>, ValidationProblem, ProblemHttpResult, JsonHttpResult<PinLockedResponse>>>
        SignInAsync(
            PinLoginRequest request,
            HttpContext http,
            IAuthStore store,
            IPasswordHasher<Teacher> hasher,
            TimeProvider clock,
            CancellationToken cancellationToken)
    {
        var phone = PhoneNumber.Normalise(request.Phone);
        var errors = new Dictionary<string, string[]>();
        if (phone is null)
        {
            errors["phone"] = ["Enter a 10-digit mobile number."];
        }

        if (request.Pin is not { Length: >= 4 and <= 6 } || !request.Pin.All(char.IsAsciiDigit))
        {
            errors["pin"] = ["The PIN is 4 to 6 digits."];
        }

        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var now = clock.GetUtcNow();
        var teacher = await store.FindByPhoneAsync(phone!, cancellationToken);
        if (teacher?.PinLockedUntil is { } lockedUntil && lockedUntil > now)
        {
            return Locked(lockedUntil);
        }

        var verified = hasher.VerifyHashedPassword(null!, teacher?.PinHash ?? DummyHash, request.Pin)
            != PasswordVerificationResult.Failed;
        if (teacher is null)
        {
            return WrongPin();
        }

        if (teacher.PinHash is null || !verified)
        {
            var failures = teacher.FailedPinAttempts + 1;
            if (failures >= MaxPinAttempts)
            {
                // The count restarts at zero, so a lapsed lock gives a fresh set of attempts.
                var until = now + PinLockout;
                await store.SetPinFailuresAsync(teacher.TeacherId, 0, until, cancellationToken);
                return Locked(until);
            }

            await store.SetPinFailuresAsync(teacher.TeacherId, failures, null, cancellationToken);
            return WrongPin();
        }

        if (teacher.FailedPinAttempts > 0 || teacher.PinLockedUntil is not null)
        {
            await store.SetPinFailuresAsync(teacher.TeacherId, 0, null, cancellationToken);
        }

        var me = await store.GetMeAsync(teacher.TeacherId, cancellationToken);
        if (me is null)
        {
            return WrongPin();
        }

        await http.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            AuthClaims.Teacher(me.TeacherId, me.ClassId, CookieAuthenticationDefaults.AuthenticationScheme),
            new AuthenticationProperties { IsPersistent = true });
        return TypedResults.Ok(me);
    }

    private static async Task<NoContent> SignOutAsync(HttpContext http, CancellationToken cancellationToken)
    {
        await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return TypedResults.NoContent();
    }

    private static async Task<Results<Ok<MeResponse>, UnauthorizedHttpResult>> GetMeAsync(
        HttpContext http, IAuthStore store, CancellationToken cancellationToken)
    {
        var me = http.User.GetTeacherId() is { } teacherId
            ? await store.GetMeAsync(teacherId, cancellationToken)
            : null;
        if (me is null)
        {
            // The teacher was deleted after the cookie was issued.
            await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return TypedResults.Unauthorized();
        }

        return TypedResults.Ok(me);
    }

    private static async Task<Results<Ok<MeResponse>, ValidationProblem, UnauthorizedHttpResult>> WelcomeAsync(
        WelcomeRequest request,
        HttpContext http,
        IAuthStore store,
        TimeProvider clock,
        CancellationToken cancellationToken)
    {
        if (request.ClientRequestId == Guid.Empty)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["clientRequestId"] = ["Required."],
            });
        }

        var me = http.User.GetTeacherId() is { } teacherId
            ? await store.MarkWelcomedAsync(teacherId, clock.GetUtcNow(), cancellationToken)
            : null;
        return me is null ? TypedResults.Unauthorized() : TypedResults.Ok(me);
    }

    private static ProblemHttpResult WrongPin() =>
        TypedResults.Problem(title: WrongPinTitle, statusCode: StatusCodes.Status401Unauthorized);

    private static JsonHttpResult<PinLockedResponse> Locked(DateTimeOffset until) =>
        TypedResults.Json(new PinLockedResponse(until), statusCode: StatusCodes.Status429TooManyRequests);
}
