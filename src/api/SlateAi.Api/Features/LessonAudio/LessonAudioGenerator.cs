using System.Security.Cryptography;
using System.Text;
using System.Threading.Channels;
using Microsoft.Extensions.Options;
using SlateAi.Api.Ai;
using SlateAi.Api.Curriculum;
using SlateAi.Api.Features.Audio;
using SlateAi.Api.Features.CatchUps.Verify;
using SlateAi.Api.Features.EveningMessages;
using SlateAi.Api.Speech;

namespace SlateAi.Api.Features.LessonAudio;

/// <summary>One row waiting for its clip. <paramref name="Fresh"/>: "Try again", so a new clip is made, not the shared one.</summary>
public sealed record LessonAudioJob(Guid Id, bool Fresh);

/// <summary>
/// Makes the clip for one <see cref="Data.Entities.LessonAudio"/> row: the day's lesson, in the row's language. The clip
/// is made once per (lesson, language) and shared by every family and class (a second class that learned the same lesson
/// gets the same clip at no cost). Only ever started by her ticking the box, never by the evening send.
/// </summary>
public sealed class LessonAudioGenerator(
    ILessonAudioStore store,
    IEveningStore evening,
    LessonScriptWriter writer,
    ITextToSpeech tts,
    IAudioStore audio,
    DisclosureClips disclosure,
    CurriculumGraph curriculum,
    PromptLibrary prompts,
    IOptions<VerifierOptions> options,
    TimeProvider clock,
    ILogger<LessonAudioGenerator> logger)
{
    private const int Attempts = 2;

    public async Task GenerateAsync(LessonAudioJob job, CancellationToken cancellationToken)
    {
        if (await store.GetAsync(job.Id, cancellationToken) is not { } row)
        {
            return;
        }

        var ids = (await evening.LatestConfirmedLessonAsync(row.ClassId, row.Date, cancellationToken)) is { } lesson && lesson.Date == row.Date
            ? lesson.CompetencyIds
            : null;
        var lessons = ids?.Select(curriculum.Get).OfType<Competency>().ToList();
        if (ids is null || lessons is null || lessons.Count != ids.Count || disclosure.Text(row.Language) is null)
        {
            await Fail(row.Id, "fallback:no-lesson", cancellationToken);
            return;
        }

        var hash = Hash(row.Language, ids, job.Fresh);
        if (!job.Fresh && await audio.FindSharedAsync(AudioKind.Lesson, row.Language, hash, cancellationToken) is { } existing)
        {
            await store.SetResultAsync(row.Id, existing.Id, LessonAudioStatus.Ready, "reused", clock.GetUtcNow(), cancellationToken);
            return;
        }

        try
        {
            var names = (await evening.GetClassAsync(row.ClassId, cancellationToken))?.Children.Select(c => c.FirstName) ?? [];
            LessonScriptResult result = LessonScriptValidator.Refused("fallback:invalid-output");
            IReadOnlyList<string>? feedback = null;
            for (var attempt = 1; attempt <= Attempts && result.Script is null; attempt++)
            {
                result = await writer.WriteAsync(row.Language, lessons, feedback, names, cancellationToken);
                feedback = [$"output: {result.Verdict}"];
            }

            if (result.Script is not { } script)
            {
                await Fail(row.Id, result.Verdict, cancellationToken);
                return;
            }

            var disclosureClip = await disclosure.GetOrMakeAsync(row.Language, cancellationToken);
            var spoken = await tts.SynthesizeAsync(script, DisclosureClips.LanguageCode(row.Language), cancellationToken);
            var seconds = disclosureClip.Seconds + spoken.Seconds;
            var kb = (disclosureClip.SizeBytes + spoken.Audio.Length) / 1024.0;
            var limits = options.Value;
            if (seconds > limits.MaxAudioSeconds || kb > limits.MaxAudioKb)
            {
                await Fail(row.Id, "fallback:audio-too-long", cancellationToken);
                return;
            }

            var clip = await audio.SaveSharedAsync(AudioKind.Lesson, row.Language, hash, spoken, cancellationToken, transcript: script);
            await store.SetResultAsync(row.Id, clip.Id, LessonAudioStatus.Ready, result.Verdict, clock.GetUtcNow(), cancellationToken);
        }
        catch (SpeechException e)
        {
            // The AiCall row records the failure; she can try again.
            logger.LogWarning("Lesson audio {Id} voice failed: {Failure}", row.Id, e.Failure);
            await Fail(row.Id, "error:voice", cancellationToken);
        }
    }

    private Task Fail(Guid id, string verdict, CancellationToken cancellationToken) =>
        store.SetResultAsync(id, null, LessonAudioStatus.Failed, verdict, clock.GetUtcNow(), cancellationToken);

    /// <summary>One key per prompt version, language and set of lessons; "Try again" gets its own so it never replaces a shared clip.</summary>
    private string Hash(string language, IReadOnlyList<string> ids, bool fresh)
    {
        var key = $"v{prompts.Get(LessonScriptWriter.PromptId).Version}|{language}|{string.Join(',', ids.Order(StringComparer.Ordinal))}";
        if (fresh)
        {
            key += $"|{Guid.NewGuid():N}";
        }

        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(key)));
    }
}

/// <summary>Rows waiting for their clip. In memory: after a restart a row stays "generating" and reads as failed (see <see cref="LessonAudioService.StaleGenerating"/>), so she can try again.</summary>
public sealed class LessonAudioQueue
{
    private readonly Channel<LessonAudioJob> _jobs = Channel.CreateBounded<LessonAudioJob>(
        new BoundedChannelOptions(50) { FullMode = BoundedChannelFullMode.DropWrite });

    public bool TryEnqueue(LessonAudioJob job) => _jobs.Writer.TryWrite(job);

    public IAsyncEnumerable<LessonAudioJob> ReadAllAsync(CancellationToken cancellationToken) => _jobs.Reader.ReadAllAsync(cancellationToken);
}

/// <summary>Makes queued lesson clips one at a time, each in its own scope, outside the request.</summary>
public sealed class LessonAudioWorker(LessonAudioQueue queue, IServiceScopeFactory scopes, TimeProvider clock, ILogger<LessonAudioWorker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var job in queue.ReadAllAsync(stoppingToken))
        {
            await using var scope = scopes.CreateAsyncScope();
            try
            {
                await scope.ServiceProvider.GetRequiredService<LessonAudioGenerator>().GenerateAsync(job, stoppingToken);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                // Never let one clip stop the worker; the row shows "could not make it" and she can try again.
                logger.LogError(e, "Making lesson audio {Id} failed", job.Id);
                await scope.ServiceProvider.GetRequiredService<ILessonAudioStore>().SetResultAsync(
                    job.Id, null, LessonAudioStatus.Failed, "error:exception", clock.GetUtcNow(), stoppingToken);
            }
        }
    }
}
