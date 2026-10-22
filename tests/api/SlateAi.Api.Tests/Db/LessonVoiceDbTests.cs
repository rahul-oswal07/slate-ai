using Microsoft.Extensions.DependencyInjection;
using SlateAi.Api.Features.Lessons;
using SlateAi.Api.Features.Lessons.Voice;

namespace SlateAi.Api.Tests.Db;

[Trait("Category", "Db")]
[Collection(DbCollection.Name)]
public sealed class LessonVoiceDbTests(PostgresFixture postgres)
{
    private static readonly DateOnly Day = new(2026, 10, 14);

    [Fact]
    public async Task A_voice_result_is_stored_against_the_note_that_started_it()
    {
        // Proves the processing → mapped update matches on MappingStartedAt after a Postgres round trip
        // (timestamptz keeps microseconds; .NET times have more precision) and the text[] columns update.
        await using var api = await DemoClassApi.StartAsync(postgres);
        // 100 ns precision, as Linux clocks give (macOS clocks happen to stop at microseconds).
        var startedAt = new DateTimeOffset(2026, 10, 14, 6, 0, 0, TimeSpan.Zero).AddTicks(1_234_567);
        var result = new A1Result("mapped", ["M3.SUB.NOBORROW", "M3.SUB.CHECK"], "ಕಳೆಯುವಿಕೆ", "Subtraction", ["865 - 432"], 0.86, "valid");

        await api.ScopedAsync<ILessonStore>(async store =>
        {
            Assert.Equal(VoiceStart.Started, await store.StartVoiceAsync(api.ClassId, Guid.NewGuid(), Day, startedAt, DemoClassApi.Ct));
            await store.SaveMappingAsync(api.ClassId, Day, startedAt, result, DemoClassApi.Ct);
        });

        var day = await api.ScopedAsync<ILessonStore, LessonRecord?>(store => store.GetDayAsync(api.ClassId, Day, DemoClassApi.Ct));
        Assert.Equal("mapped", day!.Status);
        Assert.Equal(["M3.SUB.NOBORROW", "M3.SUB.CHECK"], day.CompetencyIds);
        Assert.Equal(["865 - 432"], day.ExamplesUsed);
        Assert.Equal("Subtraction", day.SummaryEn);
    }
}
