using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using SlateAi.Api.Ai;
using SlateAi.Api.Curriculum;
using SlateAi.Api.Features.Attendance;
using SlateAi.Api.Features.Lessons;
using SlateAi.Api.Tests.Infrastructure;

namespace SlateAi.Api.Tests.Features.Lessons.Voice;

/// <summary>Upload → background worker → fake Sarvam + fake Claude (Ai:Provider=Fake) → stored match.</summary>
public sealed class VoiceUploadEndpointTests
{
    private static readonly Guid ClassId = Guid.NewGuid();
    private static readonly DateOnly Today = new(2026, 10, 22);
    private static readonly CancellationToken Ct = TestContext.Current.CancellationToken;
    private static readonly string DayUrl = $"/api/classes/{ClassId}/lessons/{Today:yyyy-MM-dd}";

    // The fake Claude picks the first ID in the enum, with confidence 0.9.
    private static readonly CurriculumGraph Book = new(
        new[] { "M3.ADD.CARRY", "M3.SUB.NOBORROW" }.Select((id, i) => new Competency(id, "ಕ", id, [], null, 5, i, [], null)));

    private readonly FakeLessonStore _store = new();
    private readonly FakeAiCallStore _aiCalls = new();
    private readonly MovableClock _clock = new() { Now = SchoolClock.At(Today, new TimeOnly(11, 0)) };

    [Fact]
    public async Task Accepts_at_once_then_the_worker_maps_the_note()
    {
        await using var factory = Create();
        using var client = factory.CreateTeacherClient(ClassId);

        var response = await UploadAsync(client, Guid.NewGuid());

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal("processing", (await response.Content.ReadFromJsonAsync<LessonDayResponse>(Ct))!.Lesson!.Status);
        var day = await WaitForMatchAsync(client);
        Assert.Equal("mapped", day.Lesson!.Status);
        Assert.Equal(["M3.ADD.CARRY"], day.Lesson.CompetencyIds);
        Assert.Equal("voice", day.Lesson.Source);
        Assert.Contains(_aiCalls.Calls, c => c.Purpose == "a1-lesson-mapping" && c.Verdict == "valid");
        Assert.Contains(_aiCalls.Calls, c => c.Purpose == "stt");
    }

    [Fact]
    public async Task A_replay_is_not_processed_twice()
    {
        await using var factory = Create();
        using var client = factory.CreateTeacherClient(ClassId);
        var requestId = Guid.NewGuid();

        await UploadAsync(client, requestId);
        await WaitForMatchAsync(client);
        var replay = await UploadAsync(client, requestId);

        Assert.Equal(HttpStatusCode.Accepted, replay.StatusCode);
        Assert.Equal(1, _store.Starts);
        Assert.Equal("mapped", (await replay.Content.ReadFromJsonAsync<LessonDayResponse>(Ct))!.Lesson!.Status);
    }

    [Fact]
    public async Task No_second_note_once_the_lesson_is_confirmed()
    {
        await _store.SaveConfirmedAsync(ClassId, Guid.NewGuid(), Today, ["M3.SUB.NOBORROW"], "picked", Ct);
        await using var factory = Create();
        using var client = factory.CreateTeacherClient(ClassId);

        Assert.Equal(HttpStatusCode.Conflict, (await UploadAsync(client, Guid.NewGuid())).StatusCode);
    }

    [Fact]
    public async Task Confirming_a_voice_match_keeps_source_voice()
    {
        await using var factory = Create();
        using var client = factory.CreateTeacherClient(ClassId);
        await UploadAsync(client, Guid.NewGuid());
        await WaitForMatchAsync(client);

        var confirmed = await client.PutAsJsonAsync(
            DayUrl, new SaveLessonRequest(Guid.NewGuid(), ["M3.ADD.CARRY"], "voice"), Ct);

        var day = await confirmed.Content.ReadFromJsonAsync<LessonDayResponse>(Ct);
        Assert.Equal("confirmed", day!.Lesson!.Status);
        Assert.Equal("voice", day.Lesson.Source);
    }

    [Fact]
    public async Task A_note_stuck_in_processing_reads_as_unclear()
    {
        await _store.StartVoiceAsync(ClassId, Guid.NewGuid(), Today, _clock.Now.AddMinutes(-3), Ct);
        await using var factory = Create();
        using var client = factory.CreateTeacherClient(ClassId);

        var day = await client.GetFromJsonAsync<LessonDayResponse>(DayUrl, Ct);

        Assert.Equal("unclear", day!.Lesson!.Status);
    }

    [Fact]
    public async Task Rejects_bad_uploads()
    {
        await using var factory = Create();
        using var client = factory.CreateTeacherClient(ClassId);
        using var other = factory.CreateTeacherClient(Guid.NewGuid());

        Assert.Equal(HttpStatusCode.BadRequest, (await UploadAsync(client, Guid.Empty)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await UploadAsync(client, Guid.NewGuid(), bytes: 0)).StatusCode);
        Assert.Equal(HttpStatusCode.RequestEntityTooLarge,
            (await UploadAsync(client, Guid.NewGuid(), bytes: 1024 * 1024 + 1)).StatusCode);
        Assert.Equal(HttpStatusCode.UnsupportedMediaType,
            (await UploadAsync(client, Guid.NewGuid(), contentType: "text/plain")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await UploadAsync(other, Guid.NewGuid())).StatusCode);
        Assert.Equal(0, _store.Starts);
    }

    private ApiFactory Create() =>
        FakeApi.Create(services =>
        {
            services.AddSingleton<ILessonStore>(_store);
            services.AddSingleton<IAiCallStore>(_aiCalls);
            services.AddSingleton(Book);
            services.AddSingleton<TimeProvider>(_clock);
        });

    private static async Task<HttpResponseMessage> UploadAsync(
        HttpClient client, Guid requestId, int bytes = 2048, string contentType = "audio/webm")
    {
        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(new byte[bytes]);
        file.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
        form.Add(file, "file", "note.webm");
        form.Add(new StringContent(requestId.ToString()), "clientRequestId");
        return await client.PostAsync($"{DayUrl}/audio", form, Ct);
    }

    private static async Task<LessonDayResponse> WaitForMatchAsync(HttpClient client)
    {
        for (var i = 0; i < 100; i++)
        {
            var day = await client.GetFromJsonAsync<LessonDayResponse>(DayUrl, Ct);
            if (day!.Lesson?.Status is not (null or "processing"))
            {
                return day;
            }

            await Task.Delay(50, Ct);
        }

        throw new TimeoutException("The worker never finished the note.");
    }
}
