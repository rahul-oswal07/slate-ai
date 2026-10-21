using SlateAi.Api.Features.Attendance;

namespace SlateAi.Api.Tests.Features.Attendance;

public sealed class RegisterMergeTests
{
    private static readonly Guid A = Guid.NewGuid();
    private static readonly Guid B = Guid.NewGuid();
    private static readonly Guid[] Roster = [A, B];
    private static readonly DateTimeOffset Arrived = new(2026, 10, 21, 12, 0, 0, SchoolClock.Ist);
    private static readonly DateTimeOffset Later = Arrived.AddHours(1);

    [Fact]
    public void Sats_with_nothing_stored_becomes_the_register()
    {
        var rows = RegisterMerge.FromSource([], Roster, new HashSet<Guid> { A }, Arrived);

        Assert.Equal(new AttendanceRow(A, false, "sats", false, Arrived, null), Row(rows, A));
        Assert.Equal(new AttendanceRow(B, true, "sats", true, Arrived, null), Row(rows, B));
    }

    [Fact]
    public void Late_sats_keeps_the_teachers_own_marking()
    {
        var manual = RegisterMerge.FromTeacher([], Roster, new HashSet<Guid> { A }, Arrived);

        var rows = RegisterMerge.FromSource(manual, Roster, new HashSet<Guid> { B }, Later);

        Assert.Equal(new AttendanceRow(A, false, "manual", true, Later, Arrived), Row(rows, A));
        Assert.Equal(new AttendanceRow(B, true, "manual", false, Later, Arrived), Row(rows, B));
    }

    [Fact]
    public void Teacher_marking_with_nothing_stored_is_manual()
    {
        var rows = RegisterMerge.FromTeacher([], Roster, new HashSet<Guid> { B }, Arrived);

        Assert.Equal(new AttendanceRow(B, false, "manual", null, null, Arrived), Row(rows, B));
    }

    [Fact]
    public void A_correction_marks_only_the_rows_that_differ_from_sats()
    {
        var sats = RegisterMerge.FromSource([], Roster, new HashSet<Guid> { A }, Arrived);

        // A was absent per SATS but is in class; B unchanged.
        var rows = RegisterMerge.FromTeacher(sats, Roster, new HashSet<Guid>(), Later);

        Assert.Equal(new AttendanceRow(A, true, "sats", false, Arrived, Later), Row(rows, A));
        Assert.Null(Row(rows, B).CorrectedAt);
    }

    [Fact]
    public void Putting_a_row_back_to_the_sats_value_clears_the_correction()
    {
        var sats = RegisterMerge.FromSource([], Roster, new HashSet<Guid> { A }, Arrived);
        var corrected = RegisterMerge.FromTeacher(sats, Roster, new HashSet<Guid>(), Later);

        var rows = RegisterMerge.FromTeacher(corrected, Roster, new HashSet<Guid> { A }, Later.AddMinutes(5));

        Assert.All(rows, r => Assert.Null(r.CorrectedAt));
    }

    [Fact]
    public void Saving_again_without_changes_keeps_the_first_correction_time()
    {
        var sats = RegisterMerge.FromSource([], Roster, new HashSet<Guid> { A }, Arrived);
        var corrected = RegisterMerge.FromTeacher(sats, Roster, new HashSet<Guid>(), Later);

        var rows = RegisterMerge.FromTeacher(corrected, Roster, new HashSet<Guid>(), Later.AddMinutes(5));

        Assert.Equal(Later, Row(rows, A).CorrectedAt);
    }

    private static AttendanceRow Row(IReadOnlyList<AttendanceRow> rows, Guid childId) =>
        rows.Single(r => r.ChildId == childId);
}
