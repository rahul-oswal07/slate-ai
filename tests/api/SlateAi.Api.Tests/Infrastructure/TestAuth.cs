using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SlateAi.Api.Features.Auth;

namespace SlateAi.Api.Tests.Infrastructure;

/// <summary>
/// Signs a request in as a teacher from test headers, so endpoint tests don't go through the PIN flow.
/// Requests without the headers fall through to the real session cookie.
/// </summary>
public static class TestAuth
{
    public const string TeacherHeader = "X-Test-Teacher";
    public const string ClassHeader = "X-Test-Class";

    private const string HeaderScheme = "TestHeaders";
    private const string SelectorScheme = "TestHeadersOrCookie";

    public static void Add(IServiceCollection services) =>
        services
            .AddAuthentication(options => options.DefaultScheme = SelectorScheme)
            .AddScheme<AuthenticationSchemeOptions, HeaderHandler>(HeaderScheme, _ => { })
            .AddPolicyScheme(SelectorScheme, null, options => options.ForwardDefaultSelector = context =>
                context.Request.Headers.ContainsKey(TeacherHeader)
                    ? HeaderScheme
                    : CookieAuthenticationDefaults.AuthenticationScheme);

    /// <summary>A client over HTTPS (the session cookie is Secure) with no one signed in.</summary>
    public static HttpClient CreateHttpsClient(this ApiFactory factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });

    /// <summary>A client signed in as the teacher of <paramref name="classId"/>.</summary>
    public static HttpClient CreateTeacherClient(this ApiFactory factory, Guid classId, Guid? teacherId = null)
    {
        var client = factory.CreateHttpsClient();
        client.DefaultRequestHeaders.Add(TeacherHeader, (teacherId ?? Guid.NewGuid()).ToString());
        client.DefaultRequestHeaders.Add(ClassHeader, classId.ToString());
        return client;
    }

    private sealed class HeaderHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Guid.TryParse(Request.Headers[TeacherHeader], out var teacherId))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            Guid? classId = Guid.TryParse(Request.Headers[ClassHeader], out var id) ? id : null;
            ClaimsPrincipal user = AuthClaims.Teacher(teacherId, classId, HeaderScheme);
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(user, HeaderScheme)));
        }
    }
}
