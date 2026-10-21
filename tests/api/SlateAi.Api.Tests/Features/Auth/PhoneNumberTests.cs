using SlateAi.Api.Features.Auth;

namespace SlateAi.Api.Tests.Features.Auth;

public sealed class PhoneNumberTests
{
    [Theory]
    [InlineData("9845012345", "9845012345")]
    [InlineData("+91 98450 12345", "9845012345")]
    [InlineData("919845012345", "9845012345")]
    [InlineData("098450-12345", "9845012345")]
    public void Accepts_indian_mobile_formats(string input, string expected) =>
        Assert.Equal(expected, PhoneNumber.Normalise(input));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("12345")]
    [InlineData("98450123456")]
    [InlineData("+1 415 555 01234")]
    public void Rejects_anything_else(string? input) => Assert.Null(PhoneNumber.Normalise(input));
}
