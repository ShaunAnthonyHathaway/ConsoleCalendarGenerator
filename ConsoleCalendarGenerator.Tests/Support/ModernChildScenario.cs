namespace ConsoleCalendarGenerator.Tests.Support;

/// <summary>
/// The calendar that the child process writes with the modern console writer (see Program.cs): February 2023
/// with the 14th in a state, cells three wide, and a theme that gives the title, the labels, the days and the
/// state a color each.
/// </summary>
internal static class ModernChildScenario
{
    public static readonly DayState Chosen = new("chosen", "*");

    public static readonly CalendarTheme Theme = CalendarTheme.None
        .WithTitleColor(ConsoleColor.Yellow)
        .WithWeekdayLabelColor(ConsoleColor.Cyan)
        .WithDayColor(ConsoleColor.Green)
        .WithStateColor("chosen", ConsoleColor.Magenta);

    public static RenderedMonth Rendered() => MonthRenderer.Render(
        2023, 2,
        MonthRenderOptions.Default.WithCellWidth(3).WithTitleFormat("MMMM yyyy").WithTitleSuffix(" (* = chosen)"),
        new[] { new DateTime(2023, 2, 14) },
        Chosen);

    public static void Write() => ConsoleMonthWriter.Write(Rendered(), Theme);

    /// <summary>The text of that calendar, every line ended by <paramref name="newLine"/>.</summary>
    public static string PlainText(string newLine) =>
        "February 2023 (* = chosen)" + newLine +
        " Mo  Tu  We  Th  Fr  Sa  Su" + newLine +
        "          1   2   3   4   5" + newLine +
        "  6   7   8   9  10  11  12" + newLine +
        " 13 *14  15  16  17  18  19" + newLine +
        " 20  21  22  23  24  25  26" + newLine +
        " 27  28" + newLine;
}
