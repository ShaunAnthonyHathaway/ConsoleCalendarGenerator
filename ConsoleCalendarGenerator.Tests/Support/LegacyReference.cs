using System.Text;

namespace ConsoleCalendarGenerator.Tests.Support;

/// <summary>
/// The text ConsoleCalendar 1.0.0 writes for a month, built from the layout rules instead of copied from the
/// library, so that the library can be compared with it for every month.
/// </summary>
internal static class LegacyReference
{
    public const string WeekdayHeader = "Mo Tu We Th Fr Sa Su";

    /// <summary>Five spaces for names of up to 4 characters, four for up to 7, three for anything longer.</summary>
    public static string TitleIndent(string monthName) =>
        monthName.Length <= 4 ? "     " : monthName.Length <= 7 ? "    " : "   ";

    public static string Title(string monthName, int year) => TitleIndent(monthName) + monthName + " " + year;

    /// <param name="newLine">
    /// The console writer's newline. The line after the last day always ends with a bare "\n" as well, because
    /// the library writes that one as a literal character.
    /// </param>
    public static string Month(int year, int month, string monthName, string newLine = "\n")
    {
        var text = new StringBuilder();
        text.Append(Title(monthName, year)).Append(newLine);
        text.Append(WeekdayHeader).Append(newLine);

        int days = DateTime.DaysInMonth(year, month);
        // Monday is column 0 and Sunday column 6; every column is two characters wide with one space between.
        int column = ((int)new DateTime(year, month, 1).DayOfWeek + 6) % 7;
        text.Append(' ', column * 3);
        for (int day = 1; day <= days; day++)
        {
            if (day > 1 && column > 0)
            {
                text.Append(' ');
            }
            text.Append(day.ToString(System.Globalization.CultureInfo.InvariantCulture).PadLeft(2));
            if (column == 6)
            {
                text.Append(newLine);
            }
            if (day == days)
            {
                text.Append('\n').Append(newLine);
            }
            column = (column + 1) % 7;
        }
        return text.ToString();
    }

    public static string Year(int year, Func<int, string> monthName, string newLine = "\n") =>
        string.Concat(Enumerable.Range(1, 12).Select(month => Month(year, month, monthName(month), newLine)));

    /// <summary>The invariant culture's month name, which is the English one.</summary>
    public static string EnglishMonthName(int month) =>
        System.Globalization.CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(month);
}
