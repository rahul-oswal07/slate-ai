namespace SlateAi.Api.Features.EveningMessages;

/// <summary>What happens tonight for one absent child.</summary>
public static class EveningOutcome
{
    /// <summary>Goes after 6 pm (or already went: see <see cref="Sent"/>).</summary>
    public const string WillSend = "willSend";

    public const string Sent = "sent";

    /// <summary>Paper consent form is No or not back (D-39).</summary>
    public const string NoConsent = "noConsent";

    /// <summary>The family has no phone (D-39): buddy card only.</summary>
    public const string NoPhone = "noPhone";

    /// <summary>The family tapped "Stop these messages" (D-19).</summary>
    public const string Stopped = "stopped";

    /// <summary>No template or lesson name in the family's language: refused and logged, never half-filled.</summary>
    public const string NoWording = "noWording";
}

/// <summary>Why nothing goes tonight for the whole class (the send endpoint answers 409).</summary>
public static class EveningBlock
{
    public const string TemplateNotApproved = "templateNotApproved";
    public const string LessonNotConfirmed = "lessonNotConfirmed";

    /// <summary>No register yet, or no school today: nobody counts as absent.</summary>
    public const string NoAttendance = "noAttendance";
}

/// <summary>The send rules (FR-20, D-19, D-39, guardrail 3). Pure; the sender and Today's card both use it.</summary>
public static class EveningEligibility
{
    public static string? Block(bool templateApproved, bool lessonConfirmed, bool attendanceIn) =>
        !templateApproved ? EveningBlock.TemplateNotApproved
        : !lessonConfirmed ? EveningBlock.LessonNotConfirmed
        : !attendanceIn ? EveningBlock.NoAttendance
        : null;

    /// <param name="consent">"yes", "no" or "nophone" (anything else counts as no).</param>
    public static string Decide(string consent, bool stopped, bool alreadySent, bool hasWording) =>
        alreadySent ? EveningOutcome.Sent
        : consent == ConsentValues.NoPhone ? EveningOutcome.NoPhone
        : consent != ConsentValues.Yes ? EveningOutcome.NoConsent
        : stopped ? EveningOutcome.Stopped
        : !hasWording ? EveningOutcome.NoWording
        : EveningOutcome.WillSend;
}

/// <summary><c>Child.Consent</c> values (D-39).</summary>
public static class ConsentValues
{
    public const string Yes = "yes";
    public const string No = "no";
    public const string NoPhone = "nophone";
}
