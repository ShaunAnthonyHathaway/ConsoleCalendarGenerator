using System.Globalization;
using System.Text;

namespace ConsoleCalendarGenerator.Tests.Historical;

/// <summary>
/// The text version 1.0.0 wrote for a month, built from its layout rules and not copied from any
/// implementation, so that the frozen source of 1.0.0 can be compared with it for every month. It differs from
/// the text of version 2.0 (Support/LegacyReference.cs) in how a month ends, and in nothing else:
///
/// <list type="bullet">
/// <item>The last row of days ends with a literal "\n", whatever the console's line terminator is.</item>
/// <item>A month whose last day is a Sunday is followed by two blank lines, every other month by one.</item>
/// </list>
///
/// This describes history and is never changed to match the library.
/// </summary>
internal static class Published100Text
{
    public const string WeekdayHeader = "Mo Tu We Th Fr Sa Su";

    /// <summary>Five spaces for names of up to 4 characters, four for up to 7, three for anything longer.</summary>
    public static string Title(string monthName, int year) =>
        (monthName.Length <= 4 ? "     " : monthName.Length <= 7 ? "    " : "   ") + monthName + " " + year.ToString(CultureInfo.InvariantCulture);

    /// <param name="newLine">
    /// The console writer's line terminator. The line after the last day always ends with a bare "\n" as well,
    /// because 1.0.0 wrote that one as a literal character.
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
            text.Append(day.ToString(CultureInfo.InvariantCulture).PadLeft(2));
            if (column == 6)
            {
                // A full week is ended with the console's terminator, also when it is the last row.
                text.Append(newLine);
            }
            if (day == days)
            {
                // Then the literal line feed that ends the last row, and the blank line.
                text.Append('\n').Append(newLine);
            }
            column = (column + 1) % 7;
        }
        return text.ToString();
    }

    public static string Year(int year, Func<int, string> monthName, string newLine = "\n") =>
        string.Concat(Enumerable.Range(1, 12).Select(month => Month(year, month, monthName(month), newLine)));

    /// <summary>The invariant culture's month name, which is the English one.</summary>
    public static string EnglishMonthName(int month) => CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(month);
}
