using CalendarGenerator;

namespace ConsoleCalendarGenerator.Tests.Support;

/// <summary>
/// The one calendar that the child process renders to its real console (see Program.cs): February 2023 with the
/// 14th selected, through the four-argument constructor so that every color differs from the defaults.
/// </summary>
internal static class ChildScenario
{
    public static void Render() =>
        new ConsoleCalendar(ConsoleColor.Red, ConsoleColor.Green, ConsoleColor.Magenta, ConsoleColor.DarkYellow)
            .GenerateMonth(2, 2023, new List<DateTime> { new DateTime(2023, 2, 14) });

    /// <summary>
    /// The text of that calendar. <paramref name="newLine"/> is the console's newline; the line after the last
    /// day additionally ends with the bare "\n" the library writes as a literal.
    /// </summary>
    public static string PlainText(string newLine) =>
        "   February 2023" + newLine +
        "Mo Tu We Th Fr Sa Su" + newLine +
        "       1  2  3  4  5" + newLine +
        " 6  7  8  9 10 11 12" + newLine +
        "13 14 15 16 17 18 19" + newLine +
        "20 21 22 23 24 25 26" + newLine +
        "27 28" + "\n" +
        newLine;
}
