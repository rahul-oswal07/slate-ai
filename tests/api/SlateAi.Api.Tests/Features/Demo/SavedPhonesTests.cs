using SlateAi.Api.Data.Entities;
using SlateAi.Api.Features.Demo;

namespace SlateAi.Api.Tests.Features.Demo;

/// <summary>Numbers saved on the admin page survive the demo Reset (T-118, D-110).</summary>
public sealed class SavedPhonesTests
{
    [Fact]
    public void Reset_puts_each_saved_number_back_and_leaves_the_others_on_the_seed()
    {
        var setAt = new DateTimeOffset(2026, 11, 15, 10, 0, 0, TimeSpan.Zero);
        var manjunath = Child("Manjunath", "+919000000015");
        var ravi = Child("Ravi", "+919000000024");

        // T-140: both numbers come back; a family with only Number 2 keeps the seed's (never dialled) Number 1.
        SavedPhones.Apply([manjunath, ravi], new Dictionary<Guid, SavedPhone>
        {
            [manjunath.Id] = new("+919876543210", setAt, "+919845012345", setAt),
            [ravi.Id] = new(null, null, "+919812345678", setAt),
        });

        Assert.Equal(("+919876543210", setAt), (manjunath.ParentPhone, manjunath.ParentPhoneSetAt));
        Assert.Equal(("+919845012345", setAt), (manjunath.ParentPhone2, manjunath.ParentPhone2SetAt));
        Assert.Equal(("+919000000024", (DateTimeOffset?)null), (ravi.ParentPhone, ravi.ParentPhoneSetAt));
        Assert.Equal(("+919812345678", setAt), (ravi.ParentPhone2, ravi.ParentPhone2SetAt));
    }

    private static Child Child(string name, string seedPhone) =>
        new() { Id = Guid.NewGuid(), FirstName = name, HomeLanguage = "kn", ParentPhone = seedPhone, HasPhone = true };
}
