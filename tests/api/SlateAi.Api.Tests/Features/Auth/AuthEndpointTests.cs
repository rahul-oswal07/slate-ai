using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using SlateAi.Api.Data.Entities;
using SlateAi.Api.Features.Auth;
using SlateAi.Api.Tests.Infrastructure;

namespace SlateAi.Api.Tests.Features.Auth;

public sealed class AuthEndpointTests
{
    private const string Phone = "9000000000";
    private const string Pin = "2468";

    private static readonly DateTimeOffset Now = new(2026, 10, 21, 4, 0, 0, TimeSpan.Zero);
    private static readonly CancellationToken Ct = TestContext.Current.CancellationToken;

    private readonly FakeAuthStore _store = new(Phone, Pin);
    private readonly MovableClock _clock = new() { Now = Now };

    [Fact]
    public async Task Right_pin_signs_in_and_returns_the_teacher()
    {
        await using var factory = Create();
        using var client = factory.CreateHttpsClient();

        var response = await SignInAsync(client, "+91 90000 00000", Pin);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var me = await response.Content.ReadFromJsonAsync<MeResponse>(Ct);
        Assert.Equal(new MeResponse(_store.TeacherId, "Shobha", _store.ClassId, SetupComplete: false), me);
        var cookie = Assert.Single(response.Headers.GetValues("Set-Cookie"));
        Assert.Contains("slate.session=", cookie);
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task The_session_cookie_opens_the_teachers_own_class()
    {
        await using var factory = Create();
        using var client = factory.CreateHttpsClient();
        await SignInAsync(client, Phone, Pin);

        var me = await client.GetFromJsonAsync<MeResponse>("/api/me", Ct);

        Assert.Equal(_store.TeacherId, me!.TeacherId);
    }

    [Fact]
    public async Task Me_without_a_session_is_401()
    {
        await using var factory = Create();
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync("/api/me", Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Sign_out_ends_the_session()
    {
        await using var factory = Create();
        using var client = factory.CreateHttpsClient();
        await SignInAsync(client, Phone, Pin);

        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync("/api/auth/logout", null, Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/me", Ct)).StatusCode);
    }

    [Fact]
    public async Task Wrong_pin_and_unknown_phone_give_the_same_401()
    {
        await using var factory = Create();
        using var client = factory.CreateHttpsClient();

        var wrongPin = await SignInAsync(client, Phone, "1111");
        var unknownPhone = await SignInAsync(client, "9123456789", Pin);

        Assert.Equal(HttpStatusCode.Unauthorized, wrongPin.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknownPhone.StatusCode);
        Assert.Equal(await Title(wrongPin), await Title(unknownPhone));
        Assert.Equal(1, _store.FailedPinAttempts);
        Assert.False(wrongPin.Headers.Contains("Set-Cookie"));
    }

    [Fact]
    public async Task Fifth_wrong_pin_locks_for_15_minutes_even_for_the_right_pin()
    {
        await using var factory = Create();
        using var client = factory.CreateHttpsClient();
        for (var i = 1; i < AuthEndpoints.MaxPinAttempts; i++)
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await SignInAsync(client, Phone, "1111")).StatusCode);
        }

        var locking = await SignInAsync(client, Phone, "1111");
        var rightPinWhileLocked = await SignInAsync(client, Phone, Pin);

        Assert.Equal(HttpStatusCode.TooManyRequests, locking.StatusCode);
        var body = await locking.Content.ReadFromJsonAsync<PinLockedResponse>(Ct);
        Assert.Equal(Now + AuthEndpoints.PinLockout, body!.LockedUntil);
        Assert.Equal(HttpStatusCode.TooManyRequests, rightPinWhileLocked.StatusCode);
    }

    [Fact]
    public async Task After_the_lock_lapses_the_right_pin_works_and_clears_the_count()
    {
        _store.PinLockedUntil = Now.AddMinutes(-1);
        _store.FailedPinAttempts = 0;
        await using var factory = Create();
        using var client = factory.CreateHttpsClient();

        var response = await SignInAsync(client, Phone, Pin);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Null(_store.PinLockedUntil);
    }

    [Fact]
    public async Task Right_pin_resets_earlier_failures()
    {
        _store.FailedPinAttempts = 3;
        await using var factory = Create();
        using var client = factory.CreateHttpsClient();

        await SignInAsync(client, Phone, Pin);

        Assert.Equal(0, _store.FailedPinAttempts);
    }

    [Fact]
    public async Task A_teacher_without_a_pin_cannot_sign_in()
    {
        _store.PinHash = null;
        await using var factory = Create();
        using var client = factory.CreateHttpsClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await SignInAsync(client, Phone, Pin)).StatusCode);
    }

    [Theory]
    [InlineData("12345", Pin)]
    [InlineData(Phone, "12")]
    [InlineData(Phone, "12ab")]
    [InlineData(Phone, "1234567")]
    public async Task Malformed_phone_or_pin_is_400_and_not_a_failed_attempt(string phone, string pin)
    {
        await using var factory = Create();
        using var client = factory.CreateHttpsClient();

        Assert.Equal(HttpStatusCode.BadRequest, (await SignInAsync(client, phone, pin)).StatusCode);
        Assert.Equal(0, _store.FailedPinAttempts);
    }

    [Fact]
    public async Task Welcome_completes_setup_once_and_a_replay_keeps_the_first_time()
    {
        await using var factory = Create();
        using var client = factory.CreateHttpsClient();
        await SignInAsync(client, Phone, Pin);
        var request = new WelcomeRequest(Guid.NewGuid());

        var first = await client.PostAsJsonAsync("/api/me/welcome", request, Ct);
        var consentAt = _store.ConsentAt;
        _clock.Now = Now.AddHours(1);
        var replay = await client.PostAsJsonAsync("/api/me/welcome", request, Ct);

        Assert.True((await first.Content.ReadFromJsonAsync<MeResponse>(Ct))!.SetupComplete);
        Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        Assert.Equal(Now, consentAt);
        Assert.Equal(Now, _store.ConsentAt);
    }

    [Fact]
    public async Task Welcome_needs_a_session_and_a_client_request_id()
    {
        await using var factory = Create();
        using var anonymous = factory.CreateHttpsClient();
        using var teacher = factory.CreateHttpsClient();
        await SignInAsync(teacher, Phone, Pin);

        var noSession = await anonymous.PostAsJsonAsync("/api/me/welcome", new WelcomeRequest(Guid.NewGuid()), Ct);
        var noRequestId = await teacher.PostAsJsonAsync("/api/me/welcome", new WelcomeRequest(Guid.Empty), Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, noSession.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, noRequestId.StatusCode);
        Assert.Null(_store.ConsentAt);
    }

    private ApiFactory Create() =>
        FakeApi.Create(services =>
        {
            services.AddSingleton<IAuthStore>(_store);
            services.AddSingleton<TimeProvider>(_clock);
        });

    private static Task<HttpResponseMessage> SignInAsync(HttpClient client, string phone, string pin) =>
        client.PostAsJsonAsync("/api/auth/pin", new PinLoginRequest(phone, pin), Ct);

    private static async Task<string?> Title(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<ProblemTitle>(Ct))?.Title;

    private sealed record ProblemTitle(string? Title);

    /// <summary>One teacher, held in memory.</summary>
    private sealed class FakeAuthStore : IAuthStore
    {
        private readonly string _phone;

        public FakeAuthStore(string phone, string pin)
        {
            _phone = phone;
            PinHash = new PasswordHasher<Teacher>().HashPassword(null!, pin);
        }

        public Guid TeacherId { get; } = Guid.NewGuid();
        public Guid ClassId { get; } = Guid.NewGuid();
        public string? PinHash { get; set; }
        public int FailedPinAttempts { get; set; }
        public DateTimeOffset? PinLockedUntil { get; set; }
        public DateTimeOffset? ConsentAt { get; private set; }

        public Task<TeacherLogin?> FindByPhoneAsync(string phone, CancellationToken cancellationToken) =>
            Task.FromResult(phone == _phone
                ? new TeacherLogin(TeacherId, PinHash, FailedPinAttempts, PinLockedUntil)
                : null);

        public Task SetPinFailuresAsync(
            Guid teacherId, int failedAttempts, DateTimeOffset? lockedUntil, CancellationToken cancellationToken)
        {
            FailedPinAttempts = failedAttempts;
            PinLockedUntil = lockedUntil;
            return Task.CompletedTask;
        }

        public Task<MeResponse?> GetMeAsync(Guid teacherId, CancellationToken cancellationToken) =>
            Task.FromResult(teacherId == TeacherId
                ? new MeResponse(TeacherId, "Shobha", ClassId, ConsentAt is not null)
                : null);

        public Task<MeResponse?> MarkWelcomedAsync(Guid teacherId, DateTimeOffset at, CancellationToken cancellationToken)
        {
            ConsentAt ??= at;
            return GetMeAsync(teacherId, cancellationToken);
        }
    }
}
