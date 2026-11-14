namespace SlateAi.Api.Features.TodaysMessage;

/// <summary>
/// The learner level from the picture question (D-95), worked out by code: right first time is "got it", right on the
/// second try "getting there", wrong twice "needs help", no tap yet "not answered". Pure.
/// </summary>
public static class TodaysLevel
{
    public const string GotIt = "gotIt";
    public const string GettingThere = "gettingThere";
    public const string NeedsHelp = "needsHelp";
    public const string NotAnswered = "notAnswered";

    public static readonly string[] Signs = [">", "<", "="];

    public static string For(string? first, string? second, string answer) =>
        first is null ? NotAnswered
        : first == answer ? GotIt
        : second is null ? NotAnswered
        : second == answer ? GettingThere
        : NeedsHelp;

    /// <summary>Whether the child is done with the question: right, or two taps.</summary>
    public static bool Done(string? first, string? second, string answer) => first == answer || second is not null;
}
