namespace SlateAi.Api.Data.Entities;

/// <summary>
/// One catch-up per (targets, language) per week (FR-07): A3's words around code-made numbers. Nothing reaches a
/// parent unless <see cref="Status"/> is "approved" (guardrail 3, T-14).
/// </summary>
public sealed class CatchUpPackage
{
    public Guid Id { get; set; }
    public Guid ClassId { get; set; }

    /// <summary>The Monday of the school week.</summary>
    public DateOnly WeekOf { get; set; }

    /// <summary>A2's targets, most important first; the learning check tests the first.</summary>
    public List<string> TargetCompetencyIds { get; set; } = [];

    /// <summary>The targets joined with "+": with <see cref="Language"/>, unique per class and week.</summary>
    public required string TargetKey { get; set; }

    /// <summary>"kn", "te", "ta" or "en".</summary>
    public required string Language { get; set; }

    /// <summary>"generating", "ready", "needsReview", "approved", "sent" or "failed" (T3).</summary>
    public required string Status { get; set; }

    /// <summary>+1 on every generation; an approval is bound to the version she ticked (T3, D-28).</summary>
    public int ScriptVersion { get; set; }

    public int? PromptVersion { get; set; }

    /// <summary>"simpler" or "shorter" when she asked to regenerate (FR-08).</summary>
    public string? Hint { get; set; }

    /// <summary>A3's validation verdict (and the verifier's, T-10).</summary>
    public string? Verdict { get; set; }

    public string? Script { get; set; }
    public List<string> KeyWordsKn { get; set; } = [];
    public string? GlossKn { get; set; }
    public string? GlossEn { get; set; }

    /// <summary>JSON: the code-made activity numbers plus A3's instruction and household-object option (P2).</summary>
    public required string Activity { get; set; }

    /// <summary>JSON: the code-made check with its options and correct index, plus A3's prompt (P3).</summary>
    public required string CheckItem { get; set; }

    /// <summary>JSON: the verifier's checks on the stored attempt, with numbers ("Needs review" reason on T3, A4).</summary>
    public string? Review { get; set; }

    /// <summary>The script's audio, <c>/api/audio/{id}</c> (T-11).</summary>
    public string? AudioUrl { get; set; }

    /// <summary>JSON: the segments in playing order (disclosure → script; greeting first once recorded) with seconds and total size.</summary>
    public string? AudioSegments { get; set; }

    /// <summary>When she approved it, and the version she ticked (guardrail 3: only that version may be sent).</summary>
    public DateTimeOffset? ApprovedAt { get; set; }

    public Guid? ApprovedByTeacherId { get; set; }
    public int? ApprovedScriptVersion { get; set; }

    /// <summary>When the families' links went out (the 6 pm job, or at once in demo mode).</summary>
    public DateTimeOffset? SentAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
