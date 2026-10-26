namespace SlateAi.Api.Data.Entities;

/// <summary>
/// One playable audio segment (D-18: the player plays greeting → disclosure → script in order; no server-side
/// stitching). Stored in Postgres for the MVP (a few MB a week); a blob store can replace it behind IAudioStore.
/// </summary>
public sealed class AudioClip
{
    public Guid Id { get; set; }

    /// <summary>"script", "disclosure", "greeting" or "lesson" (the optional daily lesson audio, T-62).</summary>
    public required string Kind { get; set; }

    /// <summary>"kn", "te", "ta", "en" or "hi".</summary>
    public required string Language { get; set; }

    /// <summary>The catch-up a script clip belongs to; deleting the package deletes its audio.</summary>
    public Guid? PackageId { get; set; }

    /// <summary>The teacher whose own recorded greeting this is (T0 step 1); null for every other kind.</summary>
    public Guid? TeacherId { get; set; }

    /// <summary>SHA-256 of the spoken wording, for clips made once and reused (the disclosure).</summary>
    public string? TextHash { get; set; }

    /// <summary>The words spoken, kept for a <c>lesson</c> clip so she can read what she approves; null otherwise.</summary>
    public string? Transcript { get; set; }

    /// <summary>"audio/ogg" (Opus, 24 kbps).</summary>
    public required string ContentType { get; set; }
    public required byte[] Bytes { get; set; }
    public double Seconds { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
