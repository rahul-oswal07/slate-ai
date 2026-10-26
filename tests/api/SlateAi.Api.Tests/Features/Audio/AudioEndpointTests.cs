using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using SlateAi.Api.Features.Audio;
using SlateAi.Api.Speech;
using SlateAi.Api.Tests.Infrastructure;

namespace SlateAi.Api.Tests.Features.Audio;

public sealed class AudioEndpointTests
{
    private static readonly CancellationToken Ct = TestContext.Current.CancellationToken;
    private static readonly Guid ClassId = Guid.NewGuid();
    private readonly FakeAudioStore _store = new();
    private readonly Guid _script;
    private readonly Guid _disclosure;

    public AudioEndpointTests()
    {
        var package = Guid.NewGuid();
        _store.PackageClasses[package] = ClassId;
        var audio = FakeTextToSpeech.Silence(2);
        _script = _store.ReplaceScriptAsync(package, "te", audio, Ct).GetAwaiter().GetResult().Id;
        _disclosure = _store.SaveSharedAsync("disclosure", "te", "hash", audio, Ct).GetAwaiter().GetResult().Id;
    }

    [Fact]
    public async Task Her_class_audio_and_the_shared_disclosure_play_with_range_support()
    {
        await using var factory = Create();
        using var client = factory.CreateTeacherClient(ClassId);

        var script = await client.GetAsync($"/api/audio/{_script}", Ct);
        var disclosure = await client.GetAsync($"/api/audio/{_disclosure}", Ct);
        using var range = new HttpRequestMessage(HttpMethod.Get, $"/api/audio/{_script}") { Headers = { Range = new RangeHeaderValue(0, 99) } };
        var partial = await client.SendAsync(range, Ct);

        Assert.Equal(HttpStatusCode.OK, script.StatusCode);
        Assert.Equal("audio/ogg", script.Content.Headers.ContentType!.MediaType);
        Assert.Equal(HttpStatusCode.OK, disclosure.StatusCode);
        Assert.Equal(HttpStatusCode.PartialContent, partial.StatusCode);
        Assert.Equal(100, (await partial.Content.ReadAsByteArrayAsync(Ct)).Length);
    }

    [Fact]
    public async Task Another_class_gets_404_and_signed_out_gets_401()
    {
        await using var factory = Create();
        using var other = factory.CreateTeacherClient(Guid.NewGuid());
        using var anonymous = factory.CreateHttpsClient();

        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"/api/audio/{_script}", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"/api/audio/{Guid.NewGuid()}", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync($"/api/audio/{_script}", Ct)).StatusCode);
    }

    [Fact]
    public async Task A_greeting_plays_for_the_teachers_class_and_for_the_teacher_who_recorded_it_but_no_one_else()
    {
        var teacher = Guid.NewGuid();
        var greeting = _store.AddGreeting(teacher, ClassId);
        var coTeacherGreeting = _store.AddGreeting(Guid.NewGuid(), Guid.Empty); // a co-teacher: no class of her own
        await using var factory = Create();
        using var classmate = factory.CreateTeacherClient(ClassId);
        using var recorder = factory.CreateTeacherClient(Guid.NewGuid(), coTeacherGreeting.TeacherId);
        using var other = factory.CreateTeacherClient(Guid.NewGuid());

        Assert.Equal(HttpStatusCode.OK, (await classmate.GetAsync($"/api/audio/{greeting.Id}", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await recorder.GetAsync($"/api/audio/{coTeacherGreeting.Id}", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"/api/audio/{greeting.Id}", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"/api/audio/{coTeacherGreeting.Id}", Ct)).StatusCode);
    }

    private ApiFactory Create() => FakeApi.Create(services => services.AddSingleton<IAudioStore>(_store));
}
