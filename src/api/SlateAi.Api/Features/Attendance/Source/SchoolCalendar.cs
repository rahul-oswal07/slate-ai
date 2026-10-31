namespace SlateAi.Api.Features.Attendance.Source;

/// <summary>When the school is open (D-69): every day but Sundays, second Saturdays and configured holidays.</summary>
public static class SchoolCalendar
{
    public static bool IsSchoolDay(DateOnly date, IEnumerable<DateOnly> holidays) =>
        date.DayOfWeek != DayOfWeek.Sunday
        && !(date.DayOfWeek == DayOfWeek.Saturday && (date.Day - 1) / 7 == 1)
        && !holidays.Contains(date);
}
