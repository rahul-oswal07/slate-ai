namespace SlateAi.Api.Data.Entities;

/// <summary>
/// One row per child per school day. A date with no rows for a class is not a school day
/// (holiday, break), so it never counts as an absence.
/// </summary>
public sealed class Attendance
{
    public Guid ClassId { get; set; }
    public DateOnly Date { get; set; }
    public Guid ChildId { get; set; }
    public bool Present { get; set; }
}
