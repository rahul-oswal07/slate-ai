using SlateAi.Api.Data.Entities;

namespace SlateAi.Api.Features.Demo;

/// <summary>
/// The numbers saved on the admin page (T-118), kept across the demo Reset: number 1 and number 2 (T-140); a null
/// <c>SetAt</c> means that one was not saved there.
/// </summary>
public sealed record SavedPhone(string? Phone, DateTimeOffset? SetAt, string? Phone2 = null, DateTimeOffset? SetAt2 = null);

public static class SavedPhones
{
    /// <summary>
    /// Puts each child's saved number back on the re-created rows (D-110: saved numbers survive Reset, so the test
    /// phones are set once). Children without number 1 keep the seed's fictional number, which is never dialled; the
    /// seed has no number 2.
    /// </summary>
    public static void Apply(IEnumerable<Child> children, IReadOnlyDictionary<Guid, SavedPhone> saved)
    {
        foreach (var child in children)
        {
            if (!saved.TryGetValue(child.Id, out var phone))
            {
                continue;
            }

            if (phone is { Phone: not null, SetAt: not null })
            {
                child.ParentPhone = phone.Phone;
                child.ParentPhoneSetAt = phone.SetAt;
            }

            if (phone is { Phone2: not null, SetAt2: not null })
            {
                child.ParentPhone2 = phone.Phone2;
                child.ParentPhone2SetAt = phone.SetAt2;
            }
        }
    }
}
