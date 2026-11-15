namespace SlateAi.Api.Features.TodaysMessage;

/// <summary>Today's message settings, from the <c>TodaysMessage</c> section.</summary>
public sealed class TodaysMessageOptions
{
    public const string Section = "TodaysMessage";

    /// <summary>
    /// Today's message for add and subtract lessons (D-116, T-128): a sum picture question with three choices, written by
    /// <c>prompts/a9-todays-message-sums.md</c>. On by default since its eval run passed (15 Nov); set it to <c>false</c> to turn sums
    /// off without a release. Compare lessons never depend on it.
    /// </summary>
    public bool ArithmeticExercises { get; set; } = true;
}
