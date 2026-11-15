using SlateAi.Api.Features.Admin;

namespace SlateAi.Api.Tests.Features.Admin;

/// <summary>What a number typed on the admin children page may be (T-118).</summary>
public sealed class ParentPhoneRulesTests
{
    [Theory]
    [InlineData("9876543210", "+919876543210")]
    [InlineData("98765 43210", "+919876543210")]
    [InlineData(" 98765-43210 ", "+919876543210")]
    [InlineData("09876543210", "+919876543210")]
    [InlineData("919876543210", "+919876543210")]
    [InlineData("+91 98765 43210", "+919876543210")]
    [InlineData("+91 (98765) 43210", "+919876543210")]
    [InlineData("6000000000", "+916000000000")]
    [InlineData("+44 7700 900123", "+447700900123")]
    [InlineData("+1 415.555.0100", "+14155550100")]
    public void Valid_numbers_are_stored_as_E164(string typed, string stored)
    {
        Assert.Equal((stored, null), ParentPhoneRules.Normalise(typed));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Blank_clears_the_number(string? typed)
    {
        Assert.Equal((null, null), ParentPhoneRules.Normalise(typed));
    }

    [Theory]
    [InlineData("5876543210")] // Indian mobiles start 6–9
    [InlineData("987654321")] // 9 digits
    [InlineData("98765432101")] // 11 digits
    [InlineData("+91 5876543210")] // +91 but not a mobile
    [InlineData("+9198765")] // +91 too short
    [InlineData("98765abcde")]
    [InlineData("+0 1234 5678")]
    [InlineData("+12345")]
    [InlineData("call me")]
    public void Anything_else_is_refused(string typed)
    {
        Assert.Equal((null, ParentPhoneRules.Invalid), ParentPhoneRules.Normalise(typed));
    }
}
