namespace SlateAi.Api.Features.TodaysMessage;

/// <summary>
/// The learner level from the picture question, worked out by code from the family's latest submit (D-108, changing
/// D-95): right is "got it", wrong "needs help", none yet "not answered". The family may submit twice and never sees
/// right or wrong. Pure.
/// </summary>
public static class TodaysLevel
{
    public const string GotIt = "gotIt";
    public const string NeedsHelp = "needsHelp";
    public const string NotAnswered = "notAnswered";

    public static readonly string[] Signs = [">", "<", "="];

    /// <summary>At most this many submits; the latest counts.</summary>
    public const int MaxTries = 2;

    public static string For(string? first, string? second, string answer) =>
        (second ?? first) is not { } latest ? NotAnswered
        : latest == answer ? GotIt
        : NeedsHelp;

    /// <summary>Both tries used: the page shows their choice with Next only.</summary>
    public static bool Locked(string? first, string? second) => second is not null;
}
