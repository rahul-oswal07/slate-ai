namespace SlateAi.Api.Data.Entities;

/// <summary>
/// One day's optional lesson audio for one language of one class (T-62): created when the teacher ticks "Also send
/// today's lesson as audio", approved by her after she has listened (guardrail 3), and attached to that day's
/// evening messages by the child's language. The clip itself is shared: made once per (lesson, language).
/// </summary>
public sealed class LessonAudio
{
    public Guid Id { get; set; }
    public Guid ClassId { get; set; }
    public DateOnly Date { get; set; }

    /// <summary>"kn", "te", "ta", "en" or "hi".</summary>
    public required string Language { get; set; }

    /// <summary>The <c>lesson</c> AudioClip she hears and approves; null while generating or failed.</summary>
    public Guid? ClipId { get; set; }

    /// <summary>generating, ready, approved or failed (<c>LessonAudioStatus</c>).</summary>
    public required string Status { get; set; }

    /// <summary>Why it failed (validator or voice verdict, no text), or "valid".</summary>
    public string? Verdict { get; set; }

    public DateTimeOffset? ApprovedAt { get; set; }
    public Guid? ApprovedByTeacherId { get; set; }

    /// <summary>The clip she approved. Only an approved row whose clip is still <see cref="ClipId"/> is ever sent.</summary>
    public Guid? ApprovedClipId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
