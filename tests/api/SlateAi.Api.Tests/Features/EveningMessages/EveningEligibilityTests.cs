using SlateAi.Api.Features.EveningMessages;

namespace SlateAi.Api.Tests.Features.EveningMessages;

public sealed class EveningEligibilityTests
{
    [Theory]
    [InlineData(false, true, true, EveningBlock.TemplateNotApproved)]
    [InlineData(true, false, true, EveningBlock.LessonNotConfirmed)]
    [InlineData(true, true, false, EveningBlock.NoAttendance)]
    [InlineData(true, true, true, null)]
    public void Nothing_goes_without_an_approved_template_a_confirmed_lesson_and_attendance(
        bool template, bool lesson, bool attendance, string? expected)
    {
        Assert.Equal(expected, EveningEligibility.Block(template, lesson, attendance));
    }

    [Theory]
    [InlineData("yes", false, false, true, EveningOutcome.WillSend)]
    [InlineData("no", false, false, true, EveningOutcome.NoConsent)]
    [InlineData("", false, false, true, EveningOutcome.NoConsent)] // a form not back counts as No (D-39)
    [InlineData("nophone", false, false, true, EveningOutcome.NoPhone)]
    [InlineData("yes", true, false, true, EveningOutcome.Stopped)]
    [InlineData("yes", false, false, false, EveningOutcome.NoWording)]
    [InlineData("yes", false, true, true, EveningOutcome.Sent)]
    public void Only_families_with_consent_who_have_not_stopped_get_it(
        string consent, bool stopped, bool sent, bool wording, string expected)
    {
        Assert.Equal(expected, EveningEligibility.Decide(consent, stopped, sent, wording));
    }
}
