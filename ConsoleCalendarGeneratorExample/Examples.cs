using System.Globalization;
using ConsoleCalendarGenerator;

namespace ConsoleCalendarGeneratorExample;

/// <summary>
/// One method for each thing the library does. The code between a "// &lt;Name&gt;" comment and its
/// "// &lt;/Name&gt;" comment is an example in README.md, word for word. The tests compare the two, compile this
/// file against both builds of the library, and compare what the program prints with expected-output.txt.
/// </summary>
internal static class Examples
{
    /// <summary>A month as text, with the title and the weekday labels in a chosen culture.</summary>
    public static void QuickStart()
    {
        // <QuickStart>
        MonthRenderOptions options = MonthRenderOptions.Default
            .WithCulture(CultureInfo.GetCultureInfo("en-US"));

        RenderedMonth month = MonthRenderer.Render(2027, 3, options);

        Console.WriteLine(month.ToText());
        // </QuickStart>
    }

    /// <summary>The culture decides the language, and nothing else.</summary>
    public static void Culture()
    {
        // <Culture>
        MonthRenderOptions french = MonthRenderOptions.Default
            .WithCulture(CultureInfo.GetCultureInfo("fr-FR"));

        Console.WriteLine(MonthRenderer.Render(2027, 3, french).ToText());
        // </Culture>
    }

    /// <summary>The first day of the week is chosen separately from the culture.</summary>
    public static void FirstDayOfWeek()
    {
        // <FirstDayOfWeek>
        MonthRenderOptions sundayFirst = MonthRenderOptions.Default
            .WithCulture(CultureInfo.GetCultureInfo("en-US"))
            .WithFirstDayOfWeek(DayOfWeek.Sunday);

        Console.WriteLine(MonthRenderer.Render(2027, 3, sundayFirst).ToText());
        // </FirstDayOfWeek>
    }

    /// <summary>Days in a state show its marker, and the title says what the marker means.</summary>
    public static RenderedMonth MarkedDates()
    {
        // <MarkedDates>
        var holiday = new DayState("holiday", "*");
        DateTime[] holidays = { new DateTime(2027, 3, 5), new DateTime(2027, 3, 17), new DateTime(2027, 3, 26) };

        MonthRenderOptions options = MonthRenderOptions.Default
            .WithCulture(CultureInfo.GetCultureInfo("en-US"))
            .WithCellWidth(3)
            .WithTitleSuffix(" (* = holiday)");

        RenderedMonth month = MonthRenderer.Render(2027, 3, options, holidays, holiday);

        Console.WriteLine(month.ToText());
        // </MarkedDates>
        return month;
    }

    /// <summary>The same month on the console, in the colors of a theme.</summary>
    public static void Colors(RenderedMonth month)
    {
        // <Colors>
        CalendarTheme theme = CalendarTheme.None
            .WithTitleColor(ConsoleColor.Yellow)
            .WithWeekdayLabelColor(ConsoleColor.Cyan)
            .WithStateColor("holiday", ConsoleColor.Green);

        ConsoleMonthWriter.Write(month, theme);
        // </Colors>
    }

    /// <summary>The same month written to a writer that is not the console.</summary>
    public static void AnyTextWriter(RenderedMonth month)
    {
        // <AnyTextWriter>
        using var writer = new StringWriter();

        MonthWriter.Write(month, writer);

        string text = writer.ToString();
        // </AnyTextWriter>
        Console.Write(text);
    }

    /// <summary>The marked days, found from what the segments say and not from their text.</summary>
    public static void Segments(RenderedMonth month)
    {
        // <Segments>
        foreach (RenderedSegment segment in month.Lines.SelectMany(line => line.Segments))
        {
            if (segment.State != null && segment.Date is DateTime date)
            {
                Console.WriteLine($"{date.DayOfWeek} {date.Day}: {segment.State.Name}, drawn as \"{segment.Text}\"");
            }
        }
        // </Segments>
    }

    /// <summary>Where the days of a month go, without any text.</summary>
    public static void Layout()
    {
        // <Layout>
        MonthLayout layout = MonthLayout.Create(2027, 4, DayOfWeek.Monday);

        Console.WriteLine($"April 2027 needs {layout.Weeks.Count} weeks. Its first week:");
        foreach (MonthLayoutCell cell in layout.Weeks[0].Cells)
        {
            Console.WriteLine($"  {cell.DayOfWeek}: {(cell.Date is DateTime date ? $"the {date.Day}" : "empty")}");
        }
        // </Layout>
    }

    /// <summary>The API of version 1.0.0, which draws straight on the console.</summary>
    public static void Legacy()
    {
        // <Legacy>
        // The legacy class takes the month name from the current culture of the process.
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");

        var calendar = new CalendarGenerator.ConsoleCalendar();
        calendar.SetTitleColor(ConsoleColor.Yellow);
        calendar.SetSelectedDateColor(ConsoleColor.Green);

        calendar.GenerateMonth(3, 2027, new List<DateTime> { new DateTime(2027, 3, 17) });
        // </Legacy>
    }
}
