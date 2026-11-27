using System.Globalization;

namespace ConsoleCalendarGenerator.Tests.Support;

/// <summary>
/// Writes a <see cref="MonthLayout"/> out as text, so that a test can spell out the grid it expects and a failure
/// shows the grid it got. This is a way of reading a layout in the tests; it is not how the library renders one.
/// </summary>
internal static class LayoutPicture
{
    /// <summary>
    /// One line for the weekday columns and one per week. Every cell is two characters wide with one space
    /// between cells; an empty cell is shown as " .".
    /// </summary>
    /// <example>
    /// <code>
    /// "Mo Tu We Th Fr Sa Su"
    /// " .  .  1  2  3  4  5"
    /// </code>
    /// </example>
    public static string[] Of(MonthLayout layout)
    {
        var lines = new List<string> { string.Join(" ", layout.DaysOfWeek.Select(TwoLetters)) };
        lines.AddRange(layout.Weeks.Select(week => string.Join(" ", week.Cells.Select(cell =>
            cell.Date is DateTime date ? date.Day.ToString(CultureInfo.InvariantCulture).PadLeft(2) : " ."))));
        return lines.ToArray();
    }

    /// <summary>
    /// Everything a layout exposes, as one string: the year, month and first day of the week, the weekday of
    /// each column, and for every cell its weekday and its complete date. Two layouts with the same description
    /// are indistinguishable through the public API.
    /// </summary>
    public static string Describe(MonthLayout layout)
    {
        string weeks = string.Join("\n", layout.Weeks.Select(week => string.Join(" ", week.Cells.Select(cell =>
            TwoLetters(cell.DayOfWeek) + ":" + (cell.IsEmpty ? "empty" : "date") + ":" + (cell.Date is DateTime date
                ? date.ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff", CultureInfo.InvariantCulture) + "/" + date.Kind
                : "none")))));
        return FormattableString.Invariant($"{layout.Year:0000}-{layout.Month:00} {layout.FirstDayOfWeek}\n")
            + string.Join(" ", layout.DaysOfWeek) + "\n" + weeks;
    }

    private static string TwoLetters(DayOfWeek day) => day.ToString().Substring(0, 2);
}
