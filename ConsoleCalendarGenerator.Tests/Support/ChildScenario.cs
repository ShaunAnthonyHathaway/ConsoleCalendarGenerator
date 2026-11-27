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
    /// <summary>
    /// September 2023 with the 14th selected, under whatever the current culture is, in four colors set with the
    /// setters, which did the same in 1.0.0 as they do now. The month ends on a Saturday, so on a terminal whose
    /// line end is a line feed the two versions write the same bytes for everything but the title.
    /// </summary>
    public static void RenderSeptember()
    {
        var calendar = new ConsoleCalendar();
        calendar.SetTitleColor(ConsoleColor.Red);
        calendar.SetDayOfWeekColor(ConsoleColor.DarkYellow);
        calendar.SetDateColor(ConsoleColor.Green);
        calendar.SetSelectedDateColor(ConsoleColor.Magenta);
        calendar.GenerateMonth(9, 2023, new List<DateTime> { new DateTime(2023, 9, 14) });
    }

    /// <summary>The same with the frozen source of version 1.0.0.</summary>
    public static void RenderSeptemberWithPublished100()
    {
        var calendar = new Historical.Published100.ConsoleCalendar();
        calendar.SetTitleColor(ConsoleColor.Red);
        calendar.SetDayOfWeekColor(ConsoleColor.DarkYellow);
        calendar.SetDateColor(ConsoleColor.Green);
        calendar.SetSelectedDateColor(ConsoleColor.Magenta);
        calendar.GenerateMonth(9, 2023, new List<DateTime> { new DateTime(2023, 9, 14) });
    }

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
