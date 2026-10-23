using SlateAi.Api.Features.CatchUps;

namespace SlateAi.Api.Features.Parent;

public static class CheckOutcome
{
    public const string CaughtUp = "caughtUp";
    public const string NeedsBuddy = "needsBuddy";
}

/// <summary>Scores the picture tap (FR-11). Pure code, never an LLM.</summary>
public static class AnswerScoring
{
    /// <returns>The outcome, or null when <paramref name="choiceId"/> isn't one of the pictures shown.</returns>
    public static (bool Correct, string Outcome)? Score(CheckData check, int choiceId)
    {
        if (choiceId < 0 || choiceId >= check.Options.Count)
        {
            return null;
        }

        var correct = choiceId == check.CorrectIndex;
        return (correct, correct ? CheckOutcome.CaughtUp : CheckOutcome.NeedsBuddy);
    }
}

/// <summary>When a parent link may show its lesson (guardrail 3, FR-19). Pure.</summary>
public static class ParentRules
{
    /// <summary>
    /// Only a catch-up she approved, at the version she approved. Sending moves it to "sent", which keeps the approved
    /// version; anything else (back to ready, regenerating) shows nothing.
    /// </summary>
    public static bool CanServe(string status, int? approvedScriptVersion, int scriptVersion) =>
        status is PackageStatus.Approved or PackageStatus.Sent && approvedScriptVersion == scriptVersion;
}
