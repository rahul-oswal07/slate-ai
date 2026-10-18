namespace SlateAi.Api.Data.Entities;

public sealed class Teacher
{
    public Guid Id { get; set; }
    public required string DisplayName { get; set; }
    public string? Phone { get; set; }
    public string? PinHash { get; set; }
    public string? GreetingAudioUrl { get; set; }
    public DateTimeOffset? ConsentAt { get; set; }
}
