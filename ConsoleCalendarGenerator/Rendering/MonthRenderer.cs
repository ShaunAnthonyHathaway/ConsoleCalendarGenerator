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
    private const int MaximumDaysInMonth = 31;
    private const int WidestDayNumber = 2;

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
        return Render(year, month, options, statesByDay: null);
    }

    /// <summary>Renders a month of the Gregorian calendar with states on some of its days.</summary>
    /// <param name="year">The year, from 1 through 9999.</param>
    /// <param name="month">The month, from 1 (January) through 12 (December).</param>
    /// <param name="options">The settings to render with.</param>
    /// <param name="dayStates">
    /// The dates that have a state, each with its state. A date selects the day with the same year, month and
    /// day; its time of day and its <see cref="DateTime.Kind"/> are ignored. Dates outside the rendered month are
    /// ignored. When several entries select the same day, the first one decides its state and the others are
    /// ignored. The collection is read once, from beginning to end, and is not changed.
    /// </param>
    /// <returns>
    /// The rendered month. A day with a state is still one <see cref="RenderedSegmentKind.Day"/> segment as wide
    /// as every other cell: its text is the state's marker directly before the day number, and its
    /// <see cref="RenderedSegment.State"/> is the state. Every other segment, and the shape of every line, is
    /// the same as without states. An empty collection gives exactly what
    /// <see cref="Render(int, int, MonthRenderOptions)"/> gives.
    /// </returns>
    /// <remarks>
    /// A day has at most one state. Markers are not explained in the output; a caller who wants a legend can put
    /// one in <see cref="MonthRenderOptions.TitleSuffix"/>.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="year"/> is less than 1 or greater than 9999, or <paramref name="month"/> is less than 1
    /// or greater than 12. The year is checked first.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="options"/> or <paramref name="dayStates"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="dayStates"/> contains an entry whose state is <see langword="null"/>; or a state whose
    /// marker, together with a two-digit day, is wider than <see cref="MonthRenderOptions.CellWidth"/>; or two
    /// states with the same name and different markers. Every entry is checked, also those for dates outside
    /// the month, so the same input is accepted or refused whichever month is rendered. Nothing is returned when
    /// an entry is refused. The exception is also thrown as described for
    /// <see cref="Render(int, int, MonthRenderOptions)"/>.
    /// </exception>
    public static RenderedMonth Render(
        int year, int month, MonthRenderOptions options, IEnumerable<KeyValuePair<DateTime, DayState>> dayStates)
    {
        MonthLayout.ValidateYearAndMonth(year, month);
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }
        if (dayStates == null)
        {
            throw new ArgumentNullException(nameof(dayStates));
        }

        var statesByDay = new DayState?[MaximumDaysInMonth + 1];
        var statesByName = new Dictionary<string, DayState>(StringComparer.Ordinal);
        foreach (KeyValuePair<DateTime, DayState> entry in dayStates)
        {
            DayState state = entry.Value;
            if (state == null)
            {
                throw new ArgumentException("A date has no state: the state of an entry is null.", nameof(dayStates));
            }
            if (!statesByName.TryGetValue(state.Name, out DayState? known))
            {
                RequireMarkerToFit(state, options, nameof(dayStates));
                statesByName.Add(state.Name, state);
            }
            else if (!ReferenceEquals(known, state) && !string.Equals(known.Marker, state.Marker, StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Two states are named '" + state.Name + "' but have different markers.", nameof(dayStates));
            }
            Assign(statesByDay, entry.Key, state, year, month);
        }
        return Render(year, month, options, statesByDay);
    }

    /// <summary>Renders a month of the Gregorian calendar with one state on some of its days.</summary>
    /// <param name="year">The year, from 1 through 9999.</param>
    /// <param name="month">The month, from 1 (January) through 12 (December).</param>
    /// <param name="options">The settings to render with.</param>
    /// <param name="dates">
    /// The dates that are in the state. A date selects the day with the same year, month and day; its time of
    /// day and its <see cref="DateTime.Kind"/> are ignored. Dates outside the rendered month are ignored, and a
    /// date given more than once counts once. The collection is read once, from beginning to end, and is not
    /// changed.
    /// </param>
    /// <param name="state">The state of those dates.</param>
    /// <returns>
    /// The rendered month, as described for
    /// <see cref="Render(int, int, MonthRenderOptions, IEnumerable{KeyValuePair{DateTime, DayState}})"/>.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="year"/> is less than 1 or greater than 9999, or <paramref name="month"/> is less than 1
    /// or greater than 12. The year is checked first.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="options"/>, <paramref name="dates"/> or <paramref name="state"/> is
    /// <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// The marker of <paramref name="state"/>, together with a two-digit day, is wider than
    /// <see cref="MonthRenderOptions.CellWidth"/>, whether or not any date is in the month. The exception is also
    /// thrown as described for <see cref="Render(int, int, MonthRenderOptions)"/>.
    /// </exception>
    public static RenderedMonth Render(int year, int month, MonthRenderOptions options, IEnumerable<DateTime> dates, DayState state)
    {
        MonthLayout.ValidateYearAndMonth(year, month);
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }
        if (dates == null)
        {
            throw new ArgumentNullException(nameof(dates));
        }
        if (state == null)
        {
            throw new ArgumentNullException(nameof(state));
        }
        RequireMarkerToFit(state, options, nameof(state));

        var statesByDay = new DayState?[MaximumDaysInMonth + 1];
        foreach (DateTime date in dates)
        {
            Assign(statesByDay, date, state, year, month);
        }
        return Render(year, month, options, statesByDay);
    }

    /// <summary>Renders with arguments that have been checked. <paramref name="statesByDay"/> is indexed by day of the month.</summary>
    private static RenderedMonth Render(int year, int month, MonthRenderOptions options, DayState?[]? statesByDay)
    {
        MonthLayout layout = MonthLayout.Create(year, month, options.FirstDayOfWeek);
        var lines = new RenderedLine[2 + layout.Weeks.Count];
        lines[0] = TitleLine(layout, options);
        lines[1] = WeekdayLabelsLine(layout, options);
        for (int week = 0; week < layout.Weeks.Count; week++)
        {
            lines[2 + week] = WeekLine(layout.Weeks[week], options, statesByDay);
        }
        return new RenderedMonth(layout, Array.AsReadOnly(lines));
    }

    /// <summary>Gives the day its state unless an earlier entry already did, or the date is in another month.</summary>
    private static void Assign(DayState?[] statesByDay, DateTime date, DayState state, int year, int month)
    {
        if (date.Year == year && date.Month == month && statesByDay[date.Day] == null)
        {
            statesByDay[date.Day] = state;
        }
    }

    /// <summary>A marked cell holds the marker and the day number; it must hold them for a two-digit day too.</summary>
    private static void RequireMarkerToFit(DayState state, MonthRenderOptions options, string parameterName)
    {
        int needed = TextElements.Count(state.Marker) + WidestDayNumber;
        if (needed > options.CellWidth)
        {
            throw new ArgumentException(
                "The marker of the state '" + state.Name + "' needs cells at least "
                + needed.ToString(CultureInfo.InvariantCulture) + " wide, and the cell width is "
                + options.CellWidth.ToString(CultureInfo.InvariantCulture) + ".",
                parameterName);
        }
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

    private static RenderedLine WeekLine(MonthLayoutWeek week, MonthRenderOptions options, DayState?[]? statesByDay)
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
                DayState? state = statesByDay?[date.Day];
                // The marker goes directly before the number, and the two are right-aligned together.
                string text = state == null ? day : state.Marker + day;
                segments.Add(new RenderedSegment(
                    RenderedSegmentKind.Day, TextElements.FitRight(text, options.CellWidth), cell.DayOfWeek, date, state));
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
