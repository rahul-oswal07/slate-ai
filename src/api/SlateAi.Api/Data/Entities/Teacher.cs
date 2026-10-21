namespace SlateAi.Api.Data.Entities;

public sealed class Teacher
{
    public Guid Id { get; set; }
    public required string DisplayName { get; set; }
    public string? Phone { get; set; }
    public string? PinHash { get; set; }
    public string? GreetingAudioUrl { get; set; }

    /// <summary>Set by the first-run welcome for now (T-47); moves to the voice-consent tick with T0 step 1.</summary>
    public DateTimeOffset? ConsentAt { get; set; }

    /// <summary>Wrong PINs since the last success or lockout.</summary>
    public int FailedPinAttempts { get; set; }

    public DateTimeOffset? PinLockedUntil { get; set; }
}
