namespace SlateAi.Api.Data.Entities;

/// <summary>
/// A server-wide switch set on an admin page, by key (T-134). Today only <c>demo.school-date</c> ("on"/"off"): whether
/// the demo teacher can act as a past school day (D-120). A missing row is the default, read by the feature's store.
/// </summary>
public sealed class AppSetting
{
    public required string Key { get; set; }
    public required string Value { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
