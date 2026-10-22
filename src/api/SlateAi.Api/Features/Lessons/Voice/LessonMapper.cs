using System.Text.Json;
using SlateAi.Api.Ai;
using SlateAi.Api.Curriculum;
using SlateAi.Api.Speech;

namespace SlateAi.Api.Features.Lessons.Voice;

/// <summary>A queued voice note. The audio lives only here, in memory, and is dropped after transcription.</summary>
public sealed record MappingJob(
    Guid ClassId, DateOnly Date, DateTimeOffset StartedAt, byte[] Audio, string FileName, string ContentType);

/// <summary>
/// Voice note → Sarvam speech-to-text → names scrubbed → A1 (Claude) → validated lessons (FR-03, T2).
/// The transcript is never stored; the roster never leaves the server.
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
        string transcript;
        try
        {
            using var audio = new MemoryStream(job.Audio, writable: false);
            transcript = (await speech.TranscribeAsync(
                audio, job.FileName, job.ContentType, "kn-IN", KeyTerms(), cancellationToken)).Text;
        }
        catch (SpeechException)
        {
            return A1Validator.Unclear("fallback:stt-failed");
        }

        if (string.IsNullOrWhiteSpace(transcript))
        {
            return A1Validator.Unclear("fallback:no-speech");
        }

        return await MapTranscriptAsync(transcript, await store.GetNamesAsync(job.ClassId, cancellationToken), cancellationToken);
    }

    /// <summary>Scrub → A1 → validate. Also what the eval suite runs, without speech-to-text.</summary>
    public async Task<A1Result> MapTranscriptAsync(string transcript, ClassNames names, CancellationToken cancellationToken)
    {
        var request = new LlmRequest(
            prompts.Get(PromptId),
            UserContent(NameScrubber.Scrub(transcript, names.Children, names.Teacher)),
            A1Validator.Schema(curriculum),
            MaxTokens: 1024);

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

        var result = A1Validator.Validate(response.Json, curriculum);
        if (response.AiCallId != Guid.Empty)
        {
            await aiCalls.SetVerdictAsync(response.AiCallId, result.Verdict, cancellationToken);
        }

        return result;
    }

    /// <summary>The scrubbed transcript and the textbook's lessons; no names, no IDs of people.</summary>
    internal string UserContent(string scrubbedTranscript) =>
        JsonSerializer.Serialize(new
        {
            transcript = scrubbedTranscript,
            lessons = curriculum.All.Select(c => new { c.Id, c.NameKn, c.NameEn, c.Chapter, c.Examples }),
        }, Json);

    /// <summary>Lesson names help Sarvam with maths vocabulary. Never child or teacher names.</summary>
    internal IReadOnlyList<string> KeyTerms() =>
        curriculum.All.SelectMany(c => new[] { c.NameKn, c.NameEn }).Distinct().Take(MaxKeyTerms).ToList();
}
