namespace SlateAi.Api.Data.Entities;

public sealed class Teacher
{
    public Guid Id { get; set; }
    public required string DisplayName { get; set; }
    public string? Phone { get; set; }
    public string? PinHash { get; set; }
    /// <summary><c>/api/audio/{id}</c> of the teacher's own greeting clip (AudioClip kind "greeting"), or null.</summary>
    public string? GreetingAudioUrl { get; set; }

    /// <summary>
    /// Set once by the greeting step (the voice-consent tick plus the recording), or by "Continue without a greeting"
    /// when the microphone is blocked (D-34). It is the first-run gate: null sends the teacher to the Welcome.
    /// </summary>
    public DateTimeOffset? ConsentAt { get; set; }

    /// <summary>Wrong PINs since the last success or lockout.</summary>
    public int FailedPinAttempts { get; set; }

    public DateTimeOffset? PinLockedUntil { get; set; }
}
