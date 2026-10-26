using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using SlateAi.Api.Features.Auth;
using SlateAi.Api.Features.Greeting;
using SlateAi.Api.Tests.Infrastructure;

namespace SlateAi.Api.Tests.Features.Greeting;

public sealed class GreetingEndpointTests
{
    private static readonly CancellationToken Ct = TestContext.Current.CancellationToken;
    private readonly FakeGreeting _store = new();

    [Fact]
    public async Task A_recording_with_consent_is_stored_as_opus_and_comes_back_on_me()
    {
        await using var factory = Create();
        using var client = factory.CreateTeacherClient(Guid.NewGuid(), _store.TeacherId);

        var response = await Post(client, Wav(10));
        var me = (await response.Content.ReadFromJsonAsync<MeResponse>(Ct))!;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(me.SetupComplete); // the consent tick finishes the first-run welcome
        Assert.Equal(10, me.Greeting!.Seconds);
        Assert.StartsWith("/api/audio/", me.Greeting.Url);
        Assert.Equal("OggS", System.Text.Encoding.ASCII.GetString(_store.Ogg![..4]));
        Assert.True(_store.Ogg.Length < 60_000, $"a 10 s greeting should be about 30 KB, was {_store.Ogg.Length}");
    }

    [Fact]
    public async Task Without_the_consent_tick_nothing_is_stored()
    {
        await using var factory = Create();
        using var client = factory.CreateTeacherClient(Guid.NewGuid(), _store.TeacherId);

        var response = await Post(client, Wav(10), consent: false);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("consent", await response.Content.ReadAsStringAsync(Ct));
        Assert.Null(_store.Ogg);
        Assert.Null(_store.ConsentAt);
    }

    [Theory]
    [InlineData(0.4)]
    [InlineData(13)]
    public async Task A_greeting_that_is_too_short_or_too_long_is_refused(double seconds)
    {
        await using var factory = Create();
        using var client = factory.CreateTeacherClient(Guid.NewGuid(), _store.TeacherId);

        var response = await Post(client, Wav(seconds));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Null(_store.Ogg);
    }

    [Fact]
    public async Task Only_wav_is_accepted_and_the_bytes_must_be_a_wav()
    {
        await using var factory = Create();
        using var client = factory.CreateTeacherClient(Guid.NewGuid(), _store.TeacherId);

        var webm = await Post(client, Wav(5), contentType: "audio/webm");
        var garbage = await Post(client, new byte[2000]);

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, webm.StatusCode);
        Assert.Equal(HttpStatusCode.UnsupportedMediaType, garbage.StatusCode);
        Assert.Null(_store.Ogg);
    }

    [Fact]
    public async Task A_file_over_one_megabyte_is_413()
    {
        await using var factory = Create();
        using var client = factory.CreateTeacherClient(Guid.NewGuid(), _store.TeacherId);

        var response = await Post(client, new byte[GreetingEndpoints.MaxBytes + 1]);

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
    }

    [Fact]
    public async Task A_missing_request_id_or_file_is_a_validation_error_and_signed_out_is_401()
    {
        await using var factory = Create();
        using var client = factory.CreateTeacherClient(Guid.NewGuid(), _store.TeacherId);
        using var anonymous = factory.CreateHttpsClient();

        var noId = await Post(client, Wav(5), requestId: Guid.Empty);
        using var noFile = new MultipartFormDataContent
        {
            { new StringContent(Guid.NewGuid().ToString()), "clientRequestId" },
            { new StringContent("true"), "consent" },
        };
        var missingFile = await client.PostAsync("/api/me/greeting", noFile, Ct);
        var signedOut = await Post(anonymous, Wav(5));

        Assert.Equal(HttpStatusCode.BadRequest, noId.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, missingFile.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, signedOut.StatusCode);
    }

    [Fact]
    public async Task The_request_id_reaches_the_store_so_a_replay_can_be_ignored()
    {
        await using var factory = Create();
        using var client = factory.CreateTeacherClient(Guid.NewGuid(), _store.TeacherId);
        var requestId = Guid.NewGuid();

        await Post(client, Wav(5), requestId: requestId);

        Assert.Equal(requestId, _store.RequestId);
    }

    private static Task<HttpResponseMessage> Post(
        HttpClient client, byte[] bytes, bool consent = true, string contentType = "audio/wav", Guid? requestId = null)
    {
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        var form = new MultipartFormDataContent
        {
            { file, "file", "greeting.wav" },
            { new StringContent((requestId ?? Guid.NewGuid()).ToString()), "clientRequestId" },
            { new StringContent(consent ? "true" : "false"), "consent" },
        };
        return client.PostAsync("/api/me/greeting", form, Ct);
    }

    /// <summary>16 kHz mono 16-bit PCM, as the phone sends it.</summary>
    private static byte[] Wav(double seconds)
    {
        const int rate = 16000;
        var samples = (int)(seconds * rate);
        using var stream = new MemoryStream();
        using var w = new BinaryWriter(stream);
        w.Write("RIFF"u8);
        w.Write(36 + samples * 2);
        w.Write("WAVEfmt "u8);
        w.Write(16);
        w.Write((short)1);
        w.Write((short)1);
        w.Write(rate);
        w.Write(rate * 2);
        w.Write((short)2);
        w.Write((short)16);
        w.Write("data"u8);
        w.Write(samples * 2);
        for (var i = 0; i < samples; i++)
        {
            w.Write((short)(Math.Sin(i * 2 * Math.PI * 220 / rate) * 3000));
        }

        return stream.ToArray();
    }

    private ApiFactory Create() => FakeApi.Create(services =>
    {
        services.AddSingleton<IGreetingStore>(_store);
        services.AddSingleton<IAuthStore>(_store);
        services.AddSingleton<TimeProvider>(new FixedClock(new DateTimeOffset(2026, 10, 26, 9, 0, 0, TimeSpan.Zero)));
    });

    /// <summary>One teacher, in memory: the greeting store and the part of the auth store the endpoint reads.</summary>
    private sealed class FakeGreeting : IGreetingStore, IAuthStore
    {
        public Guid TeacherId { get; } = Guid.NewGuid();
        public byte[]? Ogg { get; private set; }
        public double Seconds { get; private set; }
        public DateTimeOffset? ConsentAt { get; private set; }
        public Guid? RequestId { get; private set; }

        public Task SaveAsync(
            Guid teacherId, Guid clientRequestId, byte[] ogg, double seconds, DateTimeOffset at, CancellationToken cancellationToken)
        {
            Ogg = ogg;
            Seconds = seconds;
            ConsentAt ??= at;
            RequestId = clientRequestId;
            return Task.CompletedTask;
        }

        public Task<MeResponse?> GetMeAsync(Guid teacherId, CancellationToken cancellationToken) =>
            Task.FromResult<MeResponse?>(teacherId == TeacherId
                ? new MeResponse(
                    TeacherId, "Shobha", null, ConsentAt is not null,
                    Ogg is null ? null : new GreetingInfo("/api/audio/" + Guid.NewGuid(), Math.Round(Seconds, 1)))
                : null);

        public Task<TeacherLogin?> FindByPhoneAsync(string phone, CancellationToken cancellationToken) =>
            Task.FromResult<TeacherLogin?>(null);

        public Task SetPinFailuresAsync(Guid teacherId, int failedAttempts, DateTimeOffset? lockedUntil, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<MeResponse?> MarkWelcomedAsync(Guid teacherId, DateTimeOffset at, CancellationToken cancellationToken) =>
            GetMeAsync(teacherId, cancellationToken);
    }
}
