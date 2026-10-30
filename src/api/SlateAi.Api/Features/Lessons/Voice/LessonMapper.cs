using System.Text.Json;
using SlateAi.Api.Ai;
using SlateAi.Api.Curriculum;
using SlateAi.Api.Speech;

namespace SlateAi.Api.Features.Lessons.Voice;

/// <summary>
/// A queued note: a voice recording, a photo of the page or board (T-64), or both. Both live only here, in
/// memory, and are dropped after matching.
/// </summary>
public sealed record MappingJob(
    Guid ClassId,
    DateOnly Date,
    DateTimeOffset StartedAt,
    byte[]? Audio,
    string FileName,
    string ContentType,
    LlmImage? Photo = null);

/// <summary>
/// Voice note → Sarvam speech-to-text → names scrubbed → A1 (Claude, with her photo if she sent one) →
/// validated lessons (FR-03, T2). The transcript and photo are never stored; the roster never leaves the server.
/// </summary>
public sealed class LessonMapper(
    ISpeechToText speech,
    ILlmClient llm,
    IAiCallStore aiCalls,
    PromptLibrary prompts,
    CurriculumGraph curriculum,
    ILessonStore store)
{
    public const string PromptId = "a1-lesson-mapping";
    private const int MaxKeyTerms = 50;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>Matches the note and stores the result on the day's lesson.</summary>
    public async Task<A1Result> MapAsync(MappingJob job, CancellationToken cancellationToken)
    {
        var result = await MatchAsync(job, cancellationToken);
        await store.SaveMappingAsync(job.ClassId, job.Date, job.StartedAt, result, cancellationToken);
        return result;
    }

    private async Task<A1Result> MatchAsync(MappingJob job, CancellationToken cancellationToken)
    {
        // With a photo, a note that can't be heard still gets matched from the photo alone.
        var transcript = "";
        string? speechFailure = null;
        if (job.Audio is { } recording)
        {
            try
            {
                using var audio = new MemoryStream(recording, writable: false);
                transcript = (await speech.TranscribeAsync(
                    audio, job.FileName, job.ContentType, "kn-IN", KeyTerms(), cancellationToken)).Text;
                speechFailure = string.IsNullOrWhiteSpace(transcript) ? "fallback:no-speech" : null;
            }
            catch (SpeechException)
            {
                speechFailure = "fallback:stt-failed";
            }
        }

        if (job.Photo is null && (speechFailure is not null || string.IsNullOrWhiteSpace(transcript)))
        {
            return A1Validator.Unclear(speechFailure ?? "fallback:no-speech");
        }

        return await MapTranscriptAsync(
            transcript, await store.GetNamesAsync(job.ClassId, cancellationToken), cancellationToken, job.Photo);
    }

    /// <summary>
    /// Scrub → A1 → validate → scrub A1's wording again (a photo of the board can show a child's name, and Claude
    /// may copy it). Also what the eval suite runs, without speech-to-text.
    /// </summary>
    public async Task<A1Result> MapTranscriptAsync(
        string transcript, ClassNames names, CancellationToken cancellationToken, LlmImage? photo = null)
    {
        var request = new LlmRequest(
            prompts.Get(PromptId),
            UserContent(NameScrubber.Scrub(transcript, names.Children, names.Teacher), photo is not null),
            A1Validator.Schema(curriculum),
            MaxTokens: 1024,
            Images: photo is null ? null : [photo]);

        LlmResponse response;
        try
        {
            response = await llm.CompleteJsonAsync(request, cancellationToken);
        }
        catch (LlmException e)
        {
            // The AiCall row already records the failure.
            return A1Validator.Unclear($"error:{e.Failure}");
        }

        var result = ScrubNames(A1Validator.Validate(response.Json, curriculum), names);
        if (response.AiCallId != Guid.Empty)
        {
            await aiCalls.SetVerdictAsync(response.AiCallId, result.Verdict, cancellationToken);
        }

        return result;
    }

    private static A1Result ScrubNames(A1Result result, ClassNames names)
    {
        string? Scrub(string? text) => text is null ? null : NameScrubber.Scrub(text, names.Children, names.Teacher);
        return result with
        {
            SummaryKn = Scrub(result.SummaryKn),
            SummaryEn = Scrub(result.SummaryEn),
            ExamplesUsed = result.ExamplesUsed.Select(e => Scrub(e)!).ToList(),
        };
    }

    /// <summary>The scrubbed transcript (empty with a photo only) and the textbook's lessons; no names, no IDs of people.</summary>
    internal string UserContent(string scrubbedTranscript, bool hasPhoto = false) =>
        JsonSerializer.Serialize(new
        {
            transcript = scrubbedTranscript,
            photo = hasPhoto,
            lessons = curriculum.All.Select(c => new { c.Id, c.NameKn, c.NameEn, c.Chapter, c.Examples }),
        }, Json);

    /// <summary>Lesson names help Sarvam with maths vocabulary. Never child or teacher names.</summary>
    internal IReadOnlyList<string> KeyTerms() =>
        curriculum.All.SelectMany(c => new[] { c.NameKn, c.NameEn }).Distinct().Take(MaxKeyTerms).ToList();
}
