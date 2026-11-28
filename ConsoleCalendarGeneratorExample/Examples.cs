using System.Globalization;
using ConsoleCalendarGenerator;

namespace ConsoleCalendarGeneratorExample;

/// <summary>
/// The examples of README.md, one method each. An example is the code between a "// &lt;Name&gt;" comment and
/// its "// &lt;/Name&gt;" comment; the README shows that code as it is written here.
/// </summary>
internal static class Examples
{
    /// <summary>A month as text, with an explicit culture and weekday labels.</summary>
    public static MonthRenderOptions QuickStart()
    {
        // <QuickStart>
        MonthRenderOptions options = MonthRenderOptions.Default
            .WithCulture(CultureInfo.GetCultureInfo("en-US"))
            .WithWeekdayLabels(new[] { "Su", "Mo", "Tu", "We", "Th", "Fr", "Sa" });

        RenderedMonth month = MonthRenderer.Render(2027, 3, options);

        Console.WriteLine(month.ToText());
        // </QuickStart>
        return options;
    }

    /// <summary>Weeks that start on Sunday.</summary>
    public static void FirstDayOfWeek(MonthRenderOptions options)
    {
        // <FirstDayOfWeek>
        MonthRenderOptions sundayFirst = options.WithFirstDayOfWeek(DayOfWeek.Sunday);

        Console.WriteLine(MonthRenderer.Render(2027, 3, sundayFirst).ToText());
        // </FirstDayOfWeek>
    }

    /// <summary>A French title with French weekday labels.</summary>
    public static void Localized()
    {
        // <Localized>
        MonthRenderOptions french = MonthRenderOptions.Default
            .WithCulture(CultureInfo.GetCultureInfo("fr-FR"))
            .WithWeekdayLabels(new[] { "di", "lu", "ma", "me", "je", "ve", "sa" });

        Console.WriteLine(MonthRenderer.Render(2027, 3, french).ToText());
        // </Localized>
    }

    /// <summary>Dates marked in the text, with a legend in the title.</summary>
    public static RenderedMonth MarkedDates(MonthRenderOptions options)
    {
        // <MarkedDates>
        var holiday = new DayState("holiday", "*");
        DateTime[] holidays = { new DateTime(2027, 3, 5), new DateTime(2027, 3, 17), new DateTime(2027, 3, 26) };

        MonthRenderOptions withMarkers = options
            .WithCellWidth(3)
            .WithTitleSuffix(" (* = holiday)");

        RenderedMonth month = MonthRenderer.Render(2027, 3, withMarkers, holidays, holiday);

        Console.WriteLine(month.ToText());
        // </MarkedDates>
        return month;
    }

    /// <summary>The marked month on the console, in the colors of a theme.</summary>
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

    /// <summary>The marked month written to a <see cref="TextWriter"/>.</summary>
    public static void AnyTextWriter(RenderedMonth month)
    {
        // <AnyTextWriter>
        using var writer = new StringWriter();

        MonthWriter.Write(month, writer);

        string text = writer.ToString();
        // </AnyTextWriter>
        Console.Write(text);
    }

    /// <summary>The marked days, read from the segments of the rendered month.</summary>
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

    /// <summary>The weeks and weekday columns of a month, without any text.</summary>
    public static void Layout()
    {
        // <Layout>
        MonthLayout layout = MonthLayout.Create(2027, 4, DayOfWeek.Monday);

        Console.WriteLine($"April 2027 needs {layout.Weeks.Count} weeks. Its first week:");
        foreach (MonthLayoutCell cell in layout.Weeks[0].Cells)
        {
            Console.WriteLine($"  {cell.DayOfWeek}: {(cell.Date is DateTime date ? $"day {date.Day}" : "empty")}");
        }
        // </Layout>
    }

    /// <summary>A month drawn on the console by <see cref="CalendarGenerator.ConsoleCalendar"/>.</summary>
    public static void OriginalApi()
    {
        // <OriginalApi>
        // ConsoleCalendar takes the month name from the current culture.
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");

        var calendar = new CalendarGenerator.ConsoleCalendar();
        calendar.SetTitleColor(ConsoleColor.Yellow);
        calendar.SetSelectedDateColor(ConsoleColor.Green);

        calendar.GenerateMonth(3, 2027, new List<DateTime> { new DateTime(2027, 3, 17) });
        // </OriginalApi>
    }
}
