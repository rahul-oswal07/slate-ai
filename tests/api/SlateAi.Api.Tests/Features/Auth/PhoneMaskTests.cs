using SlateAi.Api.Features.Auth;

namespace SlateAi.Api.Tests.Features.Auth;

public sealed class PhoneMaskTests
{
    [Theory]
    [InlineData("9000003210", "3210")]
    [InlineData("+91 90000 03210", "3210")]
    [InlineData("3210", "3210")]
    public void Keeps_only_the_last_4_digits(string phone, string expected) =>
        Assert.Equal(expected, PhoneMask.Last4(phone));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("321")]
    [InlineData("no phone")]
    public void Shows_nothing_when_there_are_fewer_than_4_digits(string? phone) =>
        Assert.Null(PhoneMask.Last4(phone));
}
