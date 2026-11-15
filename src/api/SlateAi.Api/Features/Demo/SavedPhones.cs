using SlateAi.Api.Data.Entities;

namespace SlateAi.Api.Features.Demo;

/// <summary>A number saved on the admin page (T-118), kept across the demo Reset.</summary>
public sealed record SavedPhone(string Phone, DateTimeOffset SetAt);

public static class SavedPhones
{
    /// <summary>
    /// Puts each child's saved number back on the re-created rows (D-110: saved numbers survive Reset, so the test
    /// phones are set once). Children without one keep the seed's fictional number, which is never dialled.
    /// </summary>
    public static void Apply(IEnumerable<Child> children, IReadOnlyDictionary<Guid, SavedPhone> saved)
    {
        foreach (var child in children)
        {
            if (saved.TryGetValue(child.Id, out var phone))
            {
                child.ParentPhone = phone.Phone;
                child.ParentPhoneSetAt = phone.SetAt;
            }
        }
    }
}
