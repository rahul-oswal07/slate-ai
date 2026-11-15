using System.Security.Cryptography;
using System.Text;
using System.Threading.Channels;
using Microsoft.Extensions.Options;
using SlateAi.Api.Ai;
using SlateAi.Api.Curriculum;
using SlateAi.Api.Features.Audio;
using SlateAi.Api.Features.CatchUps.Verify;
using SlateAi.Api.Features.EveningMessages;
using SlateAi.Api.Features.LessonAudio;
using SlateAi.Api.Speech;
using SlateAi.Api.Features.LessonLinks;

namespace SlateAi.Api.Features.TodaysMessage;

/// <summary>One message to draft, in Kannada and the absent families' languages.</summary>
public sealed record TodaysJob(Guid Id, IReadOnlyList<string> Languages);

/// <summary>
/// Drafts one today's message (D-91): code picks the picture pair from the book (D-93), A9 writes the concept and the
/// activity in every language from the book and what she said in class (T-120), code checks them (one retry with the
/// problems), and each concept and each language's activity steps are voiced. The video is
/// the first of the day's lessons that has a link on the admin page (T-94, D-106), chosen by code. Nothing here sends
/// anything: the draft waits for her approval (guardrail 3).
/// </summary>
public sealed class TodaysMessageGenerator(
    ITodaysMessageStore store,
    IEveningStore evening,
    TodaysMessageWriter writer,
    ITextToSpeech tts,
    IAudioStore audio,
    ILessonLinkStore links,
    DisclosureClips disclosure,
    CurriculumGraph curriculum,
    PromptLibrary prompts,
    IOptions<VerifierOptions> options,
    TimeProvider clock,
    ILogger<TodaysMessageGenerator> logger)
{
    private const int Attempts = 2;

    /// <summary>The first lesson's link, in the order she confirmed them, or null: no video (T-94).</summary>
    private async Task<TodaysVideo?> VideoAsync(IReadOnlyList<string> competencyIds, CancellationToken cancellationToken) =>
        (await links.GetForAsync(competencyIds, cancellationToken)) is [var (_, url), ..] ? new TodaysVideo(url) : null;

    public async Task GenerateAsync(TodaysJob job, CancellationToken cancellationToken)
    {
        if (await store.GetByIdAsync(job.Id, cancellationToken) is not { Status: TodaysMessageStatus.Generating } row)
        {
            return;
        }

        var lessons = row.CompetencyIds.Select(curriculum.Get).OfType<Competency>().ToList();
        // A draft she edited keeps her Kannada and her picture pair (D-92); a fresh one starts from the book's first pair.
        var teacher = row.Sections is { Teacher: true } edited ? edited : null;
        if (lessons.Count != row.CompetencyIds.Count || (teacher?.Picture ?? TodaysPicturePicker.Pick(lessons)) is not { } picture)
        {
            await Fail(row.Id, "fallback:no-exercises", cancellationToken);
            return;
        }

        // Each concept is voiced behind the AI disclosure, which every family language needs (spec §5.3).
        if (job.Languages.Any(l => disclosure.Text(l) is null))
        {
            await Fail(row.Id, "fallback:no-disclosure", cancellationToken);
            return;
        }

        try
        {
            var names = (await evening.GetClassAsync(row.ClassId, cancellationToken))?.Children.Select(c => c.FirstName).ToList() ?? [];
            var kannada = teacher?.Kannada;
            // Her Kannada is never rewritten; A9 is asked only for the languages she can't write, if any. A note she wrote
            // in another language is written in Kannada too, so Kannada families read it in Kannada (T-119, D-111).
            var noteInKannada = kannada?.Note is not { } typed || TodaysMessageValidator.InScript(typed, "kn");
            var toWrite = kannada is null ? job.Languages : job.Languages.Where(l => l != "kn" || !noteInKannada).ToList();
            // A fresh draft starts from what she said in class (T-120); code keeps only safe phrases with allowed numbers.
            var fromClass = kannada is null
                ? TodaysFromClassFilter.From(
                    await evening.LatestConfirmedLessonAsync(row.ClassId, row.Date, cancellationToken),
                    row.Date,
                    row.CompetencyIds,
                    TodaysMessageValidator.AllowedNumbers(lessons, picture),
                    names)
                : null;
            var texts = new Dictionary<string, TodaysText>();
            var verdict = "valid";
            if (toWrite.Count > 0)
            {
                TodaysWriteResult result = TodaysMessageValidator.Refused("fallback:invalid-output");
                IReadOnlyList<string>? feedback = null;
                for (var attempt = 1; attempt <= Attempts && result.Texts is null; attempt++)
                {
                    result = await writer.WriteAsync(toWrite, lessons, picture, feedback, names, cancellationToken, kannada, fromClass);
                    feedback = [$"output: {result.Verdict}" + (result.Detail is { } detail ? $" ({detail})" : "")];
                }

                if (result.Texts is null)
                {
                    await Fail(row.Id, result.Verdict, cancellationToken);
                    return;
                }

                texts = result.Texts.ToDictionary();
                verdict = result.Verdict;
            }

            if (kannada is not null)
            {
                // Her concept and steps word for word; the note as she wrote it, or A9's Kannada of it.
                texts["kn"] = new TodaysText(kannada.Concept, kannada.Steps, noteInKannada ? kannada.Note : texts["kn"].Note);
            }

            var clips = new Dictionary<string, Guid>();
            var stepClips = new Dictionary<string, Guid>();
            foreach (var (language, text) in texts)
            {
                if (await VoiceAsync(ConceptKind, language, text.Concept, cancellationToken) is not { } clipId)
                {
                    await Fail(row.Id, "fallback:audio-too-long", cancellationToken);
                    return;
                }

                clips[language] = clipId;
                // The steps are voiced too (T-120). Too long to voice is not a failure: the family still reads the steps.
                if (await VoiceAsync(ActivityKind, language, string.Join(' ', text.Steps), cancellationToken) is { } stepsId)
                {
                    stepClips[language] = stepsId;
                }
                else
                {
                    logger.LogInformation("Today's message {Id}: activity in {Language} too long to voice; text only", row.Id, language);
                }
            }

            var concept = new TodaysConcept(texts.ToDictionary(t => t.Key, t => t.Value.Concept), clips, teacher?.Concept.Removed ?? false);
            var activity = new TodaysActivity(
                texts.ToDictionary(t => t.Key, t => t.Value.Steps), teacher?.Activity.Removed ?? false, stepClips);
            var note = kannada?.Note is null
                ? null
                : new TodaysNote(texts.ToDictionary(t => t.Key, t => t.Value.Note!), teacher!.Note!.Removed, kannada.Note);
            // A fresh draft takes the lesson's link as it is now; a draft she edited keeps its video and her Remove.
            var sections = teacher is null
                ? new TodaysSections(concept, activity, picture, await VideoAsync(row.CompetencyIds, cancellationToken))
                : teacher with { Concept = concept, Activity = activity, Note = note };
            // A newer confirm may have restarted it meanwhile; that run writes its own draft.
            if (await store.GetByIdAsync(row.Id, cancellationToken) is { Status: TodaysMessageStatus.Generating } current
                && current.CompetencyIds.SequenceEqual(row.CompetencyIds))
            {
                await store.SetReadyAsync(row.Id, sections, verdict, clock.GetUtcNow(), cancellationToken);
            }
        }
        catch (SpeechException e)
        {
            // The AiCall row records the failure; she can try again.
            logger.LogWarning("Today's message {Id} voice failed: {Failure}", row.Id, e.Failure);
            await Fail(row.Id, "error:voice", cancellationToken);
        }
    }

    private const string ConceptKind = "todays";
    private const string ActivityKind = "todays-activity";

    /// <summary>A section's clip, shared by every class that gets the same words; null when it is too long.</summary>
    private async Task<Guid?> VoiceAsync(string kind, string language, string text, CancellationToken cancellationToken)
    {
        var hash = Hash(kind, language, text);
        if (await audio.FindSharedAsync(AudioKind.Lesson, language, hash, cancellationToken) is { } existing)
        {
            return existing.Id;
        }

        // The disclosure plays first on the family page, as for the lesson clip (T-62).
        var intro = await disclosure.GetOrMakeAsync(language, cancellationToken);
        var spoken = await tts.SynthesizeAsync(text, DisclosureClips.LanguageCode(language), cancellationToken);
        var limits = options.Value;
        if (intro.Seconds + spoken.Seconds > limits.MaxAudioSeconds || (intro.SizeBytes + spoken.Audio.Length) / 1024.0 > limits.MaxAudioKb)
        {
            return null;
        }

        return (await audio.SaveSharedAsync(AudioKind.Lesson, language, hash, spoken, cancellationToken, transcript: text)).Id;
    }

    private Task Fail(Guid id, string verdict, CancellationToken cancellationToken) =>
        store.SetFailedAsync(id, verdict, clock.GetUtcNow(), cancellationToken);

    /// <summary>One clip per section kind, prompt version, language and wording (the concept's key is unchanged since T-88).</summary>
    private string Hash(string kind, string language, string text) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(
            $"{kind}|v{prompts.Get(TodaysMessageWriter.PromptId).Version}|{language}|{text}")));
}

/// <summary>Messages waiting for their draft. In memory: after a restart a row stays "generating" and reads as failed, so she can try again.</summary>
public sealed class TodaysMessageQueue
{
    private readonly Channel<TodaysJob> _jobs = Channel.CreateBounded<TodaysJob>(
        new BoundedChannelOptions(50) { FullMode = BoundedChannelFullMode.DropWrite });

    public bool TryEnqueue(TodaysJob job) => _jobs.Writer.TryWrite(job);

    public IAsyncEnumerable<TodaysJob> ReadAllAsync(CancellationToken cancellationToken) => _jobs.Reader.ReadAllAsync(cancellationToken);
}

/// <summary>Drafts queued messages one at a time, each in its own scope, outside the request.</summary>
public sealed class TodaysMessageWorker(TodaysMessageQueue queue, IServiceScopeFactory scopes, TimeProvider clock, ILogger<TodaysMessageWorker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var job in queue.ReadAllAsync(stoppingToken))
        {
            await using var scope = scopes.CreateAsyncScope();
            try
            {
                await scope.ServiceProvider.GetRequiredService<TodaysMessageGenerator>().GenerateAsync(job, stoppingToken);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                // Never let one message stop the worker; it shows "could not make it" and she can try again.
                logger.LogError(e, "Drafting today's message {Id} failed", job.Id);
                await scope.ServiceProvider.GetRequiredService<ITodaysMessageStore>().SetFailedAsync(
                    job.Id, "error:exception", clock.GetUtcNow(), stoppingToken);
            }
        }
    }
}
