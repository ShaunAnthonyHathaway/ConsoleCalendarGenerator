using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ConsoleCalendarGenerator;

/// <summary>
/// The settings <see cref="MonthRenderer"/> uses to turn a month into lines of text: language, first day of the
/// week, weekday labels, cell size, spacing, indent and title.
/// </summary>
/// <remarks>
/// <para>
/// Instances are immutable. Start from <see cref="Default"/> and call the <c>With</c> methods; each returns a new
/// instance and leaves the one it was called on unchanged.
/// </para>
/// <para>
/// Nothing here is read from the process: not the current culture, not the console. The same options always
/// produce the same text on the same system. The names and patterns of a culture come from the culture data of
/// the runtime and the operating system, which is not the same on every system: the shortest day names of a
/// culture, for example, are two letters on some systems and one letter on others. The invariant culture, and
/// labels set with <see cref="WithWeekdayLabels"/>, give the same text everywhere.
/// </para>
/// <para>
/// Widths are counted in text elements, the runtime's unit for one user-perceived character
/// (<see cref="StringInfo"/>). A text element is never split. How many columns a terminal uses to display a
/// text element is outside what this library can know.
/// </para>
/// <para>
/// Text that is supplied here, weekday labels, an indent, a title suffix and the literal text of a title format,
/// is put into the rendered month as it is. A line break in it is refused. Nothing else is examined, removed
/// or escaped, control characters included, so text from an untrusted source should be checked before it is
/// passed in. The same holds for the marker of a <see cref="DayState"/>.
/// </para>
/// </remarks>
public sealed class MonthRenderOptions
{
    private const int DaysPerWeek = 7;
    private const int MinimumCellWidth = 2;
    private const int MaximumCellWidth = 100;
    private const int MaximumColumnGap = 100;

    private MonthRenderOptions(
        CultureInfo culture,
        DateTimeFormatInfo gregorianFormat,
        DayOfWeek firstDayOfWeek,
        WeekdayLabelStyle weekdayLabelStyle,
        IReadOnlyList<string>? weekdayLabels,
        int cellWidth,
        int columnGap,
        string indent,
        string titleFormat,
        string titleSuffix,
        TitleAlignment titleAlignment)
    {
        Culture = culture;
        GregorianFormat = gregorianFormat;
        FirstDayOfWeek = firstDayOfWeek;
        WeekdayLabelStyle = weekdayLabelStyle;
        WeekdayLabels = weekdayLabels;
        CellWidth = cellWidth;
        ColumnGap = columnGap;
        Indent = indent;
        TitleFormat = titleFormat;
        TitleSuffix = titleSuffix;
        TitleAlignment = titleAlignment;
    }

    /// <summary>
    /// Gets the default options: the invariant culture, weeks that start on Monday, the culture's shortest day
    /// names as labels, cells two wide with one space between columns, no indent, and a centered title in the
    /// culture's year-and-month pattern with no suffix.
    /// </summary>
    public static MonthRenderOptions Default { get; } = new MonthRenderOptions(
        CultureInfo.InvariantCulture,
        CultureInfo.InvariantCulture.DateTimeFormat,
        DayOfWeek.Monday,
        WeekdayLabelStyle.Shortest,
        weekdayLabels: null,
        cellWidth: 2,
        columnGap: 1,
        indent: string.Empty,
        titleFormat: "Y",
        titleSuffix: string.Empty,
        TitleAlignment.Center);

    /// <summary>
    /// Gets the culture that supplies the language of the title and of the weekday labels. The default is
    /// <see cref="CultureInfo.InvariantCulture"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is a read-only copy taken when the options were created; later changes to the culture that was
    /// passed in, or to the current culture, have no effect.
    /// </para>
    /// <para>
    /// The culture supplies names and patterns only. The month is always a month of the Gregorian calendar, and
    /// the title names that month and year: for a culture whose own calendar is a different one, the culture's
    /// Gregorian names and patterns are used. The culture's first day of the week is not used either; see
    /// <see cref="FirstDayOfWeek"/>.
    /// </para>
    /// </remarks>
    public CultureInfo Culture { get; }

    /// <summary>Gets the day of the week in the first column. The default is <see cref="DayOfWeek.Monday"/>.</summary>
    /// <remarks>This is never taken from <see cref="Culture"/>.</remarks>
    public DayOfWeek FirstDayOfWeek { get; }

    /// <summary>
    /// Gets which of the culture's day names label the columns. The default is
    /// <see cref="WeekdayLabelStyle.Shortest"/>. It is not used while <see cref="WeekdayLabels"/> is set.
    /// </summary>
    public WeekdayLabelStyle WeekdayLabelStyle { get; }

    /// <summary>
    /// Gets the labels supplied with <see cref="WithWeekdayLabels"/>, or <see langword="null"/> when the labels
    /// come from the culture. The default is <see langword="null"/>.
    /// </summary>
    /// <remarks>
    /// There are seven, indexed by <see cref="DayOfWeek"/>: Sunday first, the order of
    /// <see cref="DateTimeFormatInfo.DayNames"/>. That order does not depend on <see cref="FirstDayOfWeek"/>.
    /// </remarks>
    public IReadOnlyList<string>? WeekdayLabels { get; }

    /// <summary>Gets the width of every cell, in text elements. The default is 2.</summary>
    /// <remarks>
    /// A day number or a label shorter than the cell is right-aligned in it. A label longer than the cell is cut
    /// to its first <see cref="CellWidth"/> text elements.
    /// </remarks>
    public int CellWidth { get; }

    /// <summary>Gets the number of spaces between two neighboring columns. The default is 1.</summary>
    public int ColumnGap { get; }

    /// <summary>
    /// Gets the width of the grid of seven columns, in text elements: seven cells and the six gaps between
    /// them. The indent is not included.
    /// </summary>
    public int GridWidth => (DaysPerWeek * CellWidth) + ((DaysPerWeek - 1) * ColumnGap);

    /// <summary>Gets the text that starts every line. The default is an empty string.</summary>
    public string Indent { get; }

    /// <summary>
    /// Gets the date format string that produces the title. The default is <c>"Y"</c>, the culture's
    /// year-and-month pattern.
    /// </summary>
    /// <remarks>
    /// The format is applied to the first day of the month with the names and patterns of
    /// <see cref="Culture"/>, in the Gregorian calendar.
    /// </remarks>
    public string TitleFormat { get; }

    /// <summary>Gets the text placed directly after the title. The default is an empty string.</summary>
    public string TitleSuffix { get; }

    /// <summary>
    /// Gets where the title and its suffix are placed relative to the grid. The default is
    /// <see cref="TitleAlignment.Center"/>.
    /// </summary>
    public TitleAlignment TitleAlignment { get; }

    /// <summary>The culture's names and patterns for the Gregorian calendar. Read-only.</summary>
    internal DateTimeFormatInfo GregorianFormat { get; }

    /// <summary>Returns options that take the language of the title and the weekday labels from a culture.</summary>
    /// <param name="culture">
    /// The culture. A read-only copy is taken, so later changes to this object do not affect the options.
    /// </param>
    /// <returns>New options with <see cref="Culture"/> set.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="culture"/> is <see langword="null"/>.</exception>
    public MonthRenderOptions WithCulture(CultureInfo culture)
    {
        if (culture == null)
        {
            throw new ArgumentNullException(nameof(culture));
        }
        CultureInfo snapshot = CultureInfo.ReadOnly(culture);
        return new MonthRenderOptions(
            snapshot, GregorianFormatOf(snapshot), FirstDayOfWeek, WeekdayLabelStyle, WeekdayLabels,
            CellWidth, ColumnGap, Indent, TitleFormat, TitleSuffix, TitleAlignment);
    }

    /// <summary>Returns options with a different day of the week in the first column.</summary>
    /// <param name="firstDayOfWeek">The day of the week to place in the first column.</param>
    /// <returns>New options with <see cref="FirstDayOfWeek"/> set.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="firstDayOfWeek"/> is not one of the seven <see cref="DayOfWeek"/> values.
    /// </exception>
    public MonthRenderOptions WithFirstDayOfWeek(DayOfWeek firstDayOfWeek)
    {
        if (firstDayOfWeek < DayOfWeek.Sunday || firstDayOfWeek > DayOfWeek.Saturday)
        {
            throw new ArgumentOutOfRangeException(
                nameof(firstDayOfWeek), firstDayOfWeek, "The first day of the week must be a valid day of the week.");
        }
        return new MonthRenderOptions(
            Culture, GregorianFormat, firstDayOfWeek, WeekdayLabelStyle, WeekdayLabels,
            CellWidth, ColumnGap, Indent, TitleFormat, TitleSuffix, TitleAlignment);
    }

    /// <summary>
    /// Returns options that label the columns with day names from the culture, replacing any labels set with
    /// <see cref="WithWeekdayLabels"/>.
    /// </summary>
    /// <param name="weekdayLabelStyle">Which of the culture's day names to use.</param>
    /// <returns>
    /// New options with <see cref="WeekdayLabelStyle"/> set and <see cref="WeekdayLabels"/>
    /// <see langword="null"/>.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="weekdayLabelStyle"/> is not a defined <see cref="ConsoleCalendarGenerator.WeekdayLabelStyle"/>
    /// value.
    /// </exception>
    public MonthRenderOptions WithWeekdayLabelStyle(WeekdayLabelStyle weekdayLabelStyle)
    {
        if (weekdayLabelStyle != WeekdayLabelStyle.Shortest && weekdayLabelStyle != WeekdayLabelStyle.Abbreviated)
        {
            throw new ArgumentOutOfRangeException(
                nameof(weekdayLabelStyle), weekdayLabelStyle, "The weekday label style is not a defined value.");
        }
        return new MonthRenderOptions(
            Culture, GregorianFormat, FirstDayOfWeek, weekdayLabelStyle, weekdayLabels: null,
            CellWidth, ColumnGap, Indent, TitleFormat, TitleSuffix, TitleAlignment);
    }

    /// <summary>Returns options that label the columns with the given text instead of the culture's day names.</summary>
    /// <param name="weekdayLabels">
    /// Seven labels indexed by <see cref="DayOfWeek"/>: the label for Sunday first, then Monday, through
    /// Saturday, whatever <see cref="FirstDayOfWeek"/> is. Each must contain at least one character and no line
    /// break. The labels are copied, so later changes to the collection do not affect the options.
    /// </param>
    /// <returns>New options with <see cref="WeekdayLabels"/> set.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="weekdayLabels"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="weekdayLabels"/> does not contain exactly seven labels, or one of them is
    /// <see langword="null"/>, is empty or contains a line break.
    /// </exception>
    public MonthRenderOptions WithWeekdayLabels(IEnumerable<string> weekdayLabels)
    {
        if (weekdayLabels == null)
        {
            throw new ArgumentNullException(nameof(weekdayLabels));
        }
        string[] copy = weekdayLabels.ToArray();
        if (copy.Length != DaysPerWeek)
        {
            throw new ArgumentException("Exactly seven weekday labels are required, Sunday first.", nameof(weekdayLabels));
        }
        foreach (string label in copy)
        {
            if (string.IsNullOrEmpty(label))
            {
                throw new ArgumentException("A weekday label must not be null or empty.", nameof(weekdayLabels));
            }
            if (TextElements.ContainsLineBreak(label))
            {
                throw new ArgumentException("A weekday label must not contain a line break.", nameof(weekdayLabels));
            }
        }
        return new MonthRenderOptions(
            Culture, GregorianFormat, FirstDayOfWeek, WeekdayLabelStyle, Array.AsReadOnly(copy),
            CellWidth, ColumnGap, Indent, TitleFormat, TitleSuffix, TitleAlignment);
    }

    /// <summary>Returns options with a different cell width.</summary>
    /// <param name="cellWidth">The width of every cell in text elements, from 2 through 100.</param>
    /// <returns>New options with <see cref="CellWidth"/> set.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="cellWidth"/> is less than 2, which could not hold a two-digit day, or greater than 100.
    /// </exception>
    public MonthRenderOptions WithCellWidth(int cellWidth)
    {
        if (cellWidth < MinimumCellWidth || cellWidth > MaximumCellWidth)
        {
            throw new ArgumentOutOfRangeException(nameof(cellWidth), cellWidth, "The cell width must be from 2 through 100.");
        }
        return new MonthRenderOptions(
            Culture, GregorianFormat, FirstDayOfWeek, WeekdayLabelStyle, WeekdayLabels,
            cellWidth, ColumnGap, Indent, TitleFormat, TitleSuffix, TitleAlignment);
    }

    /// <summary>Returns options with a different gap between columns.</summary>
    /// <param name="columnGap">The number of spaces between two neighboring columns, from 0 through 100.</param>
    /// <returns>New options with <see cref="ColumnGap"/> set.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="columnGap"/> is less than 0 or greater than 100.
    /// </exception>
    public MonthRenderOptions WithColumnGap(int columnGap)
    {
        if (columnGap < 0 || columnGap > MaximumColumnGap)
        {
            throw new ArgumentOutOfRangeException(nameof(columnGap), columnGap, "The column gap must be from 0 through 100.");
        }
        return new MonthRenderOptions(
            Culture, GregorianFormat, FirstDayOfWeek, WeekdayLabelStyle, WeekdayLabels,
            CellWidth, columnGap, Indent, TitleFormat, TitleSuffix, TitleAlignment);
    }

    /// <summary>Returns options that start every line with the given text.</summary>
    /// <param name="indent">The text to start every line with, usually spaces. It must not contain a line break.</param>
    /// <returns>New options with <see cref="Indent"/> set.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="indent"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="indent"/> contains a line break.</exception>
    public MonthRenderOptions WithIndent(string indent)
    {
        if (indent == null)
        {
            throw new ArgumentNullException(nameof(indent));
        }
        if (TextElements.ContainsLineBreak(indent))
        {
            throw new ArgumentException("The indent must not contain a line break.", nameof(indent));
        }
        return new MonthRenderOptions(
            Culture, GregorianFormat, FirstDayOfWeek, WeekdayLabelStyle, WeekdayLabels,
            CellWidth, ColumnGap, indent, TitleFormat, TitleSuffix, TitleAlignment);
    }

    /// <summary>Returns options that produce the title with a different date format string.</summary>
    /// <param name="titleFormat">
    /// A standard or custom .NET date format string, such as <c>"Y"</c> or <c>"MMMM yyyy"</c>. Text in quotation
    /// marks is copied to the title unchanged, so <c>"'Bookings for' MMMM"</c> is valid. The string must not be
    /// empty or contain a line break.
    /// </param>
    /// <returns>New options with <see cref="TitleFormat"/> set.</returns>
    /// <remarks>
    /// The format is applied to midnight on the first day of the month with a zero offset from UTC, so the
    /// result never depends on the time zone of the machine. The standard format <c>"U"</c> is not supported.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="titleFormat"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="titleFormat"/> is empty or contains a line break.
    /// </exception>
    /// <exception cref="FormatException"><paramref name="titleFormat"/> is not a valid date format string.</exception>
    public MonthRenderOptions WithTitleFormat(string titleFormat)
    {
        if (titleFormat == null)
        {
            throw new ArgumentNullException(nameof(titleFormat));
        }
        if (titleFormat.Length == 0)
        {
            throw new ArgumentException("The title format must not be empty.", nameof(titleFormat));
        }
        if (TextElements.ContainsLineBreak(titleFormat))
        {
            throw new ArgumentException("The title format must not contain a line break.", nameof(titleFormat));
        }
        try
        {
            // Whether a format string is valid does not depend on the culture or on the date.
            _ = FormatTitle(2000, 1, titleFormat, CultureInfo.InvariantCulture.DateTimeFormat);
        }
        catch (FormatException exception)
        {
            throw new FormatException("The title format is not a valid date format string.", exception);
        }
        return new MonthRenderOptions(
            Culture, GregorianFormat, FirstDayOfWeek, WeekdayLabelStyle, WeekdayLabels,
            CellWidth, ColumnGap, Indent, titleFormat, TitleSuffix, TitleAlignment);
    }

    /// <summary>Returns options that place the given text directly after the title.</summary>
    /// <param name="titleSuffix">
    /// The text to append. Nothing is inserted between the title and the suffix, so a suffix that should be
    /// separated from the title starts with a space. It must not contain a line break.
    /// </param>
    /// <returns>New options with <see cref="TitleSuffix"/> set.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="titleSuffix"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="titleSuffix"/> contains a line break.</exception>
    public MonthRenderOptions WithTitleSuffix(string titleSuffix)
    {
        if (titleSuffix == null)
        {
            throw new ArgumentNullException(nameof(titleSuffix));
        }
        if (TextElements.ContainsLineBreak(titleSuffix))
        {
            throw new ArgumentException("The title suffix must not contain a line break.", nameof(titleSuffix));
        }
        return new MonthRenderOptions(
            Culture, GregorianFormat, FirstDayOfWeek, WeekdayLabelStyle, WeekdayLabels,
            CellWidth, ColumnGap, Indent, TitleFormat, titleSuffix, TitleAlignment);
    }

    /// <summary>Returns options that place the title differently relative to the grid.</summary>
    /// <param name="titleAlignment">Where to place the title and its suffix.</param>
    /// <returns>New options with <see cref="TitleAlignment"/> set.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="titleAlignment"/> is not a defined <see cref="ConsoleCalendarGenerator.TitleAlignment"/>
    /// value.
    /// </exception>
    public MonthRenderOptions WithTitleAlignment(TitleAlignment titleAlignment)
    {
        if (titleAlignment != TitleAlignment.Left && titleAlignment != TitleAlignment.Center)
        {
            throw new ArgumentOutOfRangeException(
                nameof(titleAlignment), titleAlignment, "The title alignment is not a defined value.");
        }
        return new MonthRenderOptions(
            Culture, GregorianFormat, FirstDayOfWeek, WeekdayLabelStyle, WeekdayLabels,
            CellWidth, ColumnGap, Indent, TitleFormat, TitleSuffix, titleAlignment);
    }

    /// <summary>
    /// Formats the first day of a month. The value has a zero offset from UTC, so no format specifier can bring
    /// in the time zone of the machine.
    /// </summary>
    internal static string FormatTitle(int year, int month, string titleFormat, DateTimeFormatInfo format) =>
        new DateTimeOffset(year, month, 1, 0, 0, 0, TimeSpan.Zero).ToString(titleFormat, format);

    /// <summary>
    /// The culture's names and patterns for the Gregorian calendar. A culture whose own calendar is another one
    /// (Persian or Thai Buddhist, for example) also carries Gregorian data, and that is what is returned for it,
    /// so that a title always names the Gregorian month the grid shows.
    /// </summary>
    private static DateTimeFormatInfo GregorianFormatOf(CultureInfo culture)
    {
        DateTimeFormatInfo format = culture.DateTimeFormat;
        if (format.Calendar is GregorianCalendar)
        {
            return format;
        }
        foreach (Calendar calendar in culture.OptionalCalendars)
        {
            if (calendar is GregorianCalendar)
            {
                var gregorian = (DateTimeFormatInfo)format.Clone();
                gregorian.Calendar = calendar;
                return DateTimeFormatInfo.ReadOnly(gregorian);
            }
        }
        // A culture that offers no Gregorian calendar at all. None is known; if one exists, its title falls back
        // to the invariant culture's names rather than naming a month of another calendar.
        return CultureInfo.InvariantCulture.DateTimeFormat;
    }
}
