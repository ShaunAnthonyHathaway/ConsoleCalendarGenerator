using System;
using System.Collections.Generic;
using System.Globalization;

namespace ConsoleCalendarGenerator;

/// <summary>Renders a month as lines of text made of segments that say what each piece of text is.</summary>
/// <remarks>
/// Rendering is a pure function of its arguments. It performs no input or output, does not use the console, and
/// reads neither the current culture nor anything else from the process. It can be called from several threads
/// at once.
/// </remarks>
public static class MonthRenderer
{
    /// <summary>Renders a month of the Gregorian calendar.</summary>
    /// <param name="year">The year, from 1 through 9999.</param>
    /// <param name="month">The month, from 1 (January) through 12 (December).</param>
    /// <param name="options">
    /// The settings to render with, including the first day of the week. Use
    /// <see cref="MonthRenderOptions.Default"/> for the defaults.
    /// </param>
    /// <returns>The rendered month.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="year"/> is less than 1 or greater than 9999, or <paramref name="month"/> is less than 1
    /// or greater than 12. The year is checked first.
    /// </exception>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// The culture of <paramref name="options"/> supplies a month name, a day name or a pattern that puts a line
    /// break into the title or into a weekday label. No culture that comes with the runtime does.
    /// </exception>
    public static RenderedMonth Render(int year, int month, MonthRenderOptions options)
    {
        MonthLayout.ValidateYearAndMonth(year, month);
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        MonthLayout layout = MonthLayout.Create(year, month, options.FirstDayOfWeek);
        var lines = new RenderedLine[2 + layout.Weeks.Count];
        lines[0] = TitleLine(layout, options);
        lines[1] = WeekdayLabelsLine(layout, options);
        for (int week = 0; week < layout.Weeks.Count; week++)
        {
            lines[2 + week] = WeekLine(layout.Weeks[week], options);
        }
        return new RenderedMonth(layout, Array.AsReadOnly(lines));
    }

    private static RenderedLine TitleLine(MonthLayout layout, MonthRenderOptions options)
    {
        string title = MonthRenderOptions.FormatTitle(layout.Year, layout.Month, options.TitleFormat, options.GregorianFormat);
        RejectLineBreak(title, "The title");

        var segments = new List<RenderedSegment>(4);
        if (title.Length > 0 || options.TitleSuffix.Length > 0)
        {
            AddIndent(segments, options);
            if (options.TitleAlignment == TitleAlignment.Center)
            {
                int spare = options.GridWidth - TextElements.Count(title) - TextElements.Count(options.TitleSuffix);
                if (spare >= 2)
                {
                    segments.Add(new RenderedSegment(RenderedSegmentKind.Padding, new string(' ', spare / 2)));
                }
            }
            if (title.Length > 0)
            {
                segments.Add(new RenderedSegment(RenderedSegmentKind.Title, title));
            }
            if (options.TitleSuffix.Length > 0)
            {
                segments.Add(new RenderedSegment(RenderedSegmentKind.TitleSuffix, options.TitleSuffix));
            }
        }
        return new RenderedLine(RenderedLineKind.Title, segments.AsReadOnly());
    }

    private static RenderedLine WeekdayLabelsLine(MonthLayout layout, MonthRenderOptions options)
    {
        var segments = new List<RenderedSegment>(14);
        AddIndent(segments, options);
        foreach (DayOfWeek dayOfWeek in layout.DaysOfWeek)
        {
            AddGapUnlessFirst(segments, options, dayOfWeek == layout.FirstDayOfWeek);
            string label = LabelOf(dayOfWeek, options);
            segments.Add(new RenderedSegment(
                RenderedSegmentKind.WeekdayLabel, TextElements.FitRight(label, options.CellWidth), dayOfWeek));
        }
        return new RenderedLine(RenderedLineKind.WeekdayLabels, segments.AsReadOnly());
    }

    private static RenderedLine WeekLine(MonthLayoutWeek week, MonthRenderOptions options)
    {
        // The empty cells after the last day of the month are not rendered, so the line does not end with spaces.
        int cellCount = week.Cells.Count;
        while (week.Cells[cellCount - 1].IsEmpty)
        {
            cellCount--;
        }

        var segments = new List<RenderedSegment>(14);
        AddIndent(segments, options);
        for (int column = 0; column < cellCount; column++)
        {
            MonthLayoutCell cell = week.Cells[column];
            AddGapUnlessFirst(segments, options, column == 0);
            if (cell.Date is DateTime date)
            {
                string day = date.Day.ToString(CultureInfo.InvariantCulture);
                segments.Add(new RenderedSegment(
                    RenderedSegmentKind.Day, TextElements.FitRight(day, options.CellWidth), cell.DayOfWeek, date));
            }
            else
            {
                segments.Add(new RenderedSegment(
                    RenderedSegmentKind.EmptyDay, new string(' ', options.CellWidth), cell.DayOfWeek));
            }
        }
        return new RenderedLine(RenderedLineKind.Week, segments.AsReadOnly());
    }

    private static string LabelOf(DayOfWeek dayOfWeek, MonthRenderOptions options)
    {
        if (options.WeekdayLabels != null)
        {
            return options.WeekdayLabels[(int)dayOfWeek];
        }
        string label = options.WeekdayLabelStyle == WeekdayLabelStyle.Abbreviated
            ? options.GregorianFormat.GetAbbreviatedDayName(dayOfWeek)
            : options.GregorianFormat.GetShortestDayName(dayOfWeek);
        RejectLineBreak(label, "A weekday label");
        return label;
    }

    private static void AddIndent(List<RenderedSegment> segments, MonthRenderOptions options)
    {
        if (options.Indent.Length > 0)
        {
            segments.Add(new RenderedSegment(RenderedSegmentKind.Indent, options.Indent));
        }
    }

    private static void AddGapUnlessFirst(List<RenderedSegment> segments, MonthRenderOptions options, bool isFirstColumn)
    {
        if (!isFirstColumn && options.ColumnGap > 0)
        {
            segments.Add(new RenderedSegment(RenderedSegmentKind.Gap, new string(' ', options.ColumnGap)));
        }
    }

    private static void RejectLineBreak(string text, string what)
    {
        if (TextElements.ContainsLineBreak(text))
        {
            throw new ArgumentException(what + " produced by the culture of the options contains a line break.", "options");
        }
    }
}
