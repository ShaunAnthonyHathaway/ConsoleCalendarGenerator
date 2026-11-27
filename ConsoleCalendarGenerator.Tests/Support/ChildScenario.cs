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
    /// The same calendar drawn by the frozen source of version 1.0.0, for the tests that keep on record what
    /// 1.0.0 sent to the real console streams (Historical/Published100RedirectedOutputTests.cs).
    /// </summary>
    public static void RenderWithPublished100() =>
        new Historical.Published100.ConsoleCalendar(ConsoleColor.Red, ConsoleColor.Green, ConsoleColor.Magenta, ConsoleColor.DarkYellow)
            .GenerateMonth(2, 2023, new List<DateTime> { new DateTime(2023, 2, 14) });

    /// <summary>
    /// The text of that calendar as 1.0.0 wrote it. <paramref name="newLine"/> is the console's newline; the
    /// line after the last day ends with the bare "\n" that 1.0.0 wrote as a literal, and then with the newline.
    /// </summary>
    public static string PlainTextOfPublished100(string newLine) =>
        "   February 2023" + newLine +
        "Mo Tu We Th Fr Sa Su" + newLine +
        "       1  2  3  4  5" + newLine +
        " 6  7  8  9 10 11 12" + newLine +
        "13 14 15 16 17 18 19" + newLine +
        "20 21 22 23 24 25 26" + newLine +
        "27 28" + "\n" +
        newLine;

    /// <summary>
    /// The text of that calendar. <paramref name="newLine"/> is the console's newline, which ends every line:
    /// the last row of days and the one blank line after it as well.
    /// </summary>
    public static string PlainText(string newLine) =>
        "   February 2023" + newLine +
        "Mo Tu We Th Fr Sa Su" + newLine +
        "       1  2  3  4  5" + newLine +
        " 6  7  8  9 10 11 12" + newLine +
        "13 14 15 16 17 18 19" + newLine +
        "20 21 22 23 24 25 26" + newLine +
        "27 28" + newLine +
        newLine;
}
