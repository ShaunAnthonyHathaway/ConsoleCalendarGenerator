using System;
using System.Collections.Generic;
using System.Globalization;
using ConsoleCalendarGenerator;

namespace CalendarGenerator
{
    // The API published in ConsoleCalendarGenerator 1.0.0, kept and supported in 2.0 for existing callers. Its
    // public surface, its output, every single call it makes on the console and its exceptions are a
    // compatibility contract, including what a new design would not choose; see
    // docs/ai-context/compatibility.md. Version 2.0 corrects five listed behaviors, all of which are made; that
    // file lists them. Nothing else may be "corrected".
    //
    // Where each day goes comes from MonthLayout, the modern layout model. Everything else about how 1.0.0 draws
    // a month is kept in this class on purpose, because the modern renderer and writers deliberately behave
    // differently: they format the title, restore the caller's colors, write whole lines, and reject invalid
    // colors early.
    /// <summary>
    /// Draws a month, or the twelve months of a year, directly on the console, in four configurable colors. This
    /// is the original API of the package.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A month is drawn as a title with the name of the month and the year, the weekday row
    /// <c>Mo Tu We Th Fr Sa Su</c>, and one line for each week. Weeks start on Monday. The title, the weekday row,
    /// the days and the selected days each have a color. A selected day is shown in its own color; the text is the
    /// same.
    /// </para>
    /// <para>
    /// The name of the month follows the culture: it is the name <see cref="CultureInfo.CurrentCulture"/> has for
    /// the month of the Gregorian calendar, read each time a month is drawn. The weekday row is always in English.
    /// </para>
    /// <para>
    /// A color can be any <see cref="ConsoleColor"/> value, also one that is not defined. Such a value is accepted
    /// and stored; drawing fails later, when the console is asked to use it.
    /// </para>
    /// <para>
    /// Drawing is not synchronized. Drawing from several threads at the same time mixes their output.
    /// </para>
    /// <para>
    /// The constructors, methods and parameter names are those of version 1.0.0, so code written for that version
    /// compiles and runs without being changed. Version 2 updates five observable behaviors: which element a
    /// constructor argument colors, the exception for a list of dates that is <see langword="null"/>, line ends,
    /// the empty line after a month, and the month title under a culture whose default calendar is not Gregorian.
    /// The members describe the current behavior, and the README of the package lists the differences.
    /// </para>
    /// <para>
    /// The types of the <c>ConsoleCalendarGenerator</c> namespace provide layout data, text that does not depend
    /// on the console, visible date markers and custom output: see <see cref="MonthRenderer"/> and
    /// <see cref="ConsoleMonthWriter"/>.
    /// </para>
    /// </remarks>
    public class ConsoleCalendar
    {
        // Always English and always from Monday, whatever the culture.
        private const string WeekdayRow = "Mo Tu We Th Fr Sa Su";

        private ConsoleColor _dayOfWeekColor;
        private ConsoleColor _dateColor;
        private ConsoleColor _selectedDateColor;
        private ConsoleColor _titleColor;

        /// <summary>Draws the twelve months of a year on the console, one below the other, with no selected days.</summary>
        /// <param name="Year">The year, from 1 to 9999.</param>
        /// <remarks>
        /// This is the same as calling <see cref="GenerateMonth(int, int)"/> for the months 1 to 12. There is no heading
        /// for the year. Every month, December included, is followed by one empty line, and nothing more is added
        /// between the months or at the end. Every title names the Gregorian month below it, in the language of
        /// <see cref="CultureInfo.CurrentCulture"/>.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="Year"/> is less than 1 or greater than 9999. Nothing is written.
        /// </exception>
        public void GenerateYear(int Year)
        {
            GenerateYear(Year, new List<DateTime>());
        }
        /// <summary>
        /// Draws the twelve months of a year on the console, one below the other, with the given dates as selected
        /// days.
        /// </summary>
        /// <param name="Year">The year, from 1 to 9999.</param>
        /// <param name="dates">
        /// The dates to show as selected, for the whole year in one list. See
        /// <see cref="GenerateMonth(int, int, List{DateTime})"/> for how a date selects a day. The list is not changed.
        /// It may be empty and must not be <see langword="null"/>.
        /// </param>
        /// <remarks>
        /// This is the same as calling <see cref="GenerateMonth(int, int, List{DateTime})"/> for the months 1 to 12
        /// with the same list. There is no heading for the year. Every month, December included, is followed by one
        /// empty line, and nothing more is added between the months or at the end. Every title names the Gregorian
        /// month below it, in the language of <see cref="CultureInfo.CurrentCulture"/>.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="dates"/> is <see langword="null"/>. This is checked first, also when
        /// <paramref name="Year"/> is invalid as well, and nothing is written.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="Year"/> is less than 1 or greater than 9999. Nothing is written.
        /// </exception>
        public void GenerateYear(int Year, List<DateTime> dates)
        {
            // Checked here as well as in GenerateMonth, so that this method answers for its own argument.
            if (dates == null)
            {
                throw new ArgumentNullException(nameof(dates));
            }

            // Twelve months one after the other, with no heading for the year.
            for (int month = 1; month <= 12; month++)
            {
                GenerateMonth(month, Year, dates);
            }
        }
        /// <summary>Draws one month on the console, with no selected days.</summary>
        /// <param name="Month">The month, from 1 to 12. Note that the month comes before the year.</param>
        /// <param name="Year">The year, from 1 to 9999.</param>
        /// <remarks>
        /// See <see cref="GenerateMonth(int, int, List{DateTime})"/> for what is written and how the console is used.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="Month"/> is less than 1 or greater than 12, or <paramref name="Year"/> is less than 1 or
        /// greater than 9999. Nothing is written.
        /// </exception>
        public void GenerateMonth(int Month, int Year)
        {
            GenerateMonth(Month, Year, new List<DateTime>());
        }
        /// <summary>Draws one month on the console, with the given dates as selected days.</summary>
        /// <param name="Month">The month, from 1 to 12. Note that the month comes before the year.</param>
        /// <param name="Year">The year, from 1 to 9999.</param>
        /// <param name="dates">
        /// The dates to show as selected. A date selects a day of the month when its year, month and day are the same;
        /// its time of day and its <see cref="DateTime.Kind"/> are ignored. Dates in other months or years, repeated
        /// dates and the order of the list make no difference. The list is not changed. It may be empty and must not be
        /// <see langword="null"/>.
        /// </param>
        /// <remarks>
        /// <para>
        /// <b>What is written.</b> The title is the name of the month, a space and the year as given, after three to
        /// five spaces: fewer for a longer name. The weekday row follows, then one line for each week, with the days
        /// right-aligned in columns two characters wide and one space between columns. No line ends with spaces. The
        /// month is followed by exactly one empty line.
        /// </para>
        /// <para>
        /// <b>Culture.</b> The days are always those of the Gregorian month, and the title names that month: it is the
        /// name <see cref="CultureInfo.CurrentCulture"/> has for the month of the Gregorian calendar, also when the
        /// culture's default calendar is another one. The culture is only read, and
        /// <see cref="CultureInfo.CurrentUICulture"/> is not used. The weekday row is the English one and weeks start
        /// on Monday whatever the culture is. The year is written as given and is never converted.
        /// </para>
        /// <para>
        /// <b>The console.</b> Text is written to <see cref="Console.Out"/> in the configured colors, and the console's
        /// colors are reset when the month is drawn. Colors are set even when <see cref="Console.Out"/> has been
        /// replaced or redirected.
        /// </para>
        /// <para>
        /// <b>Line ends.</b> Every line, the last line of days and the empty line after it included, ends with the
        /// line terminator of <see cref="Console.Out"/>.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="dates"/> is <see langword="null"/>. This is checked first, also when
        /// <paramref name="Month"/> or <paramref name="Year"/> is invalid as well, and nothing is written.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="Month"/> is less than 1 or greater than 12, or <paramref name="Year"/> is less than 1 or
        /// greater than 9999. Nothing is written.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// One of the colors is not a defined <see cref="ConsoleColor"/> value. The exception is thrown when that color
        /// is first used, and what was written before it stays written.
        /// </exception>
        public void GenerateMonth(int Month, int Year, List<DateTime> dates)
        {
            // The order of these first steps is what callers observe when arguments are invalid. The list comes
            // first: a null list is refused even when the month or the year is invalid as well. Version 1.0.0 had
            // no check and failed with NullReferenceException when it began to read the list; version 2.0 throws
            // ArgumentNullException at the same point (D-58). Then the runtime checks the month and the year, and
            // its exception is the one that reaches the caller. All of it happens before anything is written or
            // any color is set.
            if (dates == null)
            {
                throw new ArgumentNullException(nameof(dates));
            }

            bool[] isSelected = SelectedDays(Month, Year, dates);
            int lastDay = DateTime.DaysInMonth(Year, Month);

            MonthLayout layout = MonthLayout.Create(Year, Month, DayOfWeek.Monday);

            WriteTitle(Month, Year);
            WriteWeekdayRow();
            WriteDays(layout, lastDay, isSelected);
            // Reset, not restored: the colors the caller had are gone.
            Console.ResetColor();
        }
        /// <summary>Gets the color of the weekday row.</summary>
        /// <returns>The color. For a calendar made by the parameterless constructor it is cyan.</returns>
        public ConsoleColor GetDayOfWeekColor()
        {
            return _dayOfWeekColor;
        }
        /// <summary>Sets the color of the weekday row, for the months drawn after this call.</summary>
        /// <param name="TitleColor">
        /// The color of the weekday row. All four <c>Set</c> methods name their parameter <c>TitleColor</c>.
        /// </param>
        public void SetDayOfWeekColor(ConsoleColor TitleColor)
        {
            _dayOfWeekColor = TitleColor;
        }
        /// <summary>Gets the color of the days that are not selected.</summary>
        /// <returns>The color. For a calendar made by the parameterless constructor it is white.</returns>
        public ConsoleColor GetDateColor()
        {
            return _dateColor;
        }
        /// <summary>Sets the color of the days that are not selected, for the months drawn after this call.</summary>
        /// <param name="TitleColor">
        /// The color of the days. All four <c>Set</c> methods name their parameter <c>TitleColor</c>.
        /// </param>
        public void SetDateColor(ConsoleColor TitleColor)
        {
            _dateColor = TitleColor;
        }
        /// <summary>Gets the color of the selected days.</summary>
        /// <returns>The color. For a calendar made by the parameterless constructor it is blue.</returns>
        public ConsoleColor GetSelectedDateColor()
        {
            return _selectedDateColor;
        }
        /// <summary>Sets the color of the selected days, for the months drawn after this call.</summary>
        /// <param name="TitleColor">
        /// The color of the selected days. All four <c>Set</c> methods name their parameter <c>TitleColor</c>.
        /// </param>
        public void SetSelectedDateColor(ConsoleColor TitleColor)
        {
            _selectedDateColor = TitleColor;
        }
        /// <summary>Gets the color of the title, the line with the name of the month and the year.</summary>
        /// <returns>The color. For a calendar made by the parameterless constructor it is yellow.</returns>
        public ConsoleColor GetTitleColor()
        {
            return _titleColor;
        }
        /// <summary>Sets the color of the title, for the months drawn after this call.</summary>
        /// <param name="TitleColor">The color of the title.</param>
        public void SetTitleColor(ConsoleColor TitleColor)
        {
            _titleColor = TitleColor;
        }
        /// <summary>
        /// Creates a calendar with the default colors: a yellow title, a cyan weekday row, white days and blue
        /// selected days.
        /// </summary>
        /// <remarks>
        /// Change a color afterwards with <see cref="SetTitleColor(ConsoleColor)"/>,
        /// <see cref="SetDayOfWeekColor(ConsoleColor)"/>, <see cref="SetDateColor(ConsoleColor)"/> or
        /// <see cref="SetSelectedDateColor(ConsoleColor)"/>.
        /// </remarks>
        public ConsoleCalendar()
        {
            _dayOfWeekColor = ConsoleColor.Cyan;
            _dateColor = ConsoleColor.White;
            _titleColor = ConsoleColor.Yellow;
            _selectedDateColor = ConsoleColor.Blue;
        }

        // In the constructors below each argument sets the color it is named for, and a color with no argument
        // keeps its default. Version 1.0.0 gave the TitleColor argument to the weekday row, left the title yellow,
        // and with four arguments gave the DayOfWeekColor argument to the title. Version 2.0 corrects that (D-57).
        /// <summary>Creates a calendar with a color for the title and the default colors for everything else.</summary>
        /// <param name="TitleColor">The color of the title.</param>
        /// <remarks>
        /// The weekday row keeps its default color, cyan. In version 1.0.0 this argument applied to the weekday row.
        /// </remarks>
        public ConsoleCalendar(ConsoleColor TitleColor)
        {
            _dayOfWeekColor = ConsoleColor.Cyan;
            _dateColor = ConsoleColor.White;
            _titleColor = TitleColor;
            _selectedDateColor = ConsoleColor.Blue;
        }
        /// <summary>
        /// Creates a calendar with colors for the title and the days, and the default colors for the weekday row and
        /// the selected days.
        /// </summary>
        /// <param name="TitleColor">The color of the title.</param>
        /// <param name="DateColor">The color of the days that are not selected.</param>
        /// <remarks>
        /// The weekday row keeps its default color, cyan. In version 1.0.0 the first argument applied to the weekday
        /// row.
        /// </remarks>
        public ConsoleCalendar(ConsoleColor TitleColor, ConsoleColor DateColor)
        {
            _dayOfWeekColor = ConsoleColor.Cyan;
            _dateColor = DateColor;
            _titleColor = TitleColor;
            _selectedDateColor = ConsoleColor.Blue;
        }
        /// <summary>
        /// Creates a calendar with colors for the title, the days and the selected days, and the default color for
        /// the weekday row.
        /// </summary>
        /// <param name="TitleColor">The color of the title.</param>
        /// <param name="DateColor">The color of the days that are not selected.</param>
        /// <param name="SelectedDateColor">The color of the selected days.</param>
        /// <remarks>
        /// The weekday row keeps its default color, cyan. In version 1.0.0 the first argument applied to the weekday
        /// row.
        /// </remarks>
        public ConsoleCalendar(ConsoleColor TitleColor, ConsoleColor DateColor, ConsoleColor SelectedDateColor)
        {
            _dayOfWeekColor = ConsoleColor.Cyan;
            _dateColor = DateColor;
            _titleColor = TitleColor;
            _selectedDateColor = SelectedDateColor;
        }
        /// <summary>Creates a calendar with all four colors.</summary>
        /// <param name="TitleColor">The color of the title.</param>
        /// <param name="DateColor">The color of the days that are not selected.</param>
        /// <param name="SelectedDateColor">The color of the selected days.</param>
        /// <param name="DayOfWeekColor">The color of the weekday row.</param>
        /// <remarks>
        /// Each argument applies to the element it is named for. In version 1.0.0 the first argument applied to the
        /// weekday row and the fourth to the title.
        /// </remarks>
        public ConsoleCalendar(ConsoleColor TitleColor, ConsoleColor DateColor, ConsoleColor SelectedDateColor, ConsoleColor DayOfWeekColor)
        {
            _dayOfWeekColor = DayOfWeekColor;
            _dateColor = DateColor;
            _titleColor = TitleColor;
            _selectedDateColor = SelectedDateColor;
        }

        // Which days of the month are selected, indexed by day. A date selects a day when its year, month and
        // day match; its time of day and its kind play no part. The list is only read.
        private static bool[] SelectedDays(int Month, int Year, List<DateTime> dates)
        {
            bool[] isSelected = new bool[32];
            foreach (DateTime date in dates)
            {
                if (date.Month == Month && date.Year == Year)
                {
                    isSelected[date.Day] = true;
                }
            }
            return isSelected;
        }

        // The title names the Gregorian month, the one the grid shows, in the language of the current culture.
        // The year is the argument, never converted. The indent has three steps by the length of the name; it
        // is not centering.
        private void WriteTitle(int Month, int Year)
        {
            Console.ForegroundColor = _titleColor;
            string monthName = GregorianMonthName(Month);
            string indent = monthName.Length <= 4 ? "     " : monthName.Length <= 7 ? "    " : "   ";
            Console.WriteLine(indent + monthName + " " + Year);
            Console.ResetColor();
        }

        // The current culture's name for a month of the Gregorian calendar (D-61). Version 1.0.0 asked the
        // culture's date formats for month number Month whatever their calendar was, and so named a month of
        // another calendar under a culture whose dates do not use the Gregorian one: the ninth Persian month
        // above the grid of September.
        //
        // Nearly every culture's dates use the Gregorian calendar, and for those this is the call 1.0.0 made,
        // names set by the program included. Any other culture offers the Gregorian calendar among its optional
        // calendars, and its names for that calendar are used: the culture's own kind of Gregorian calendar
        // first, because the other kinds that some systems offer name the months in English or French. A
        // culture that offers no Gregorian calendar at all, of which none is known, gets the invariant names:
        // the right month in English, never a month of another calendar.
        private static string GregorianMonthName(int Month)
        {
            CultureInfo culture = CultureInfo.CurrentCulture;
            DateTimeFormatInfo format = culture.DateTimeFormat;
            if (format.Calendar is GregorianCalendar)
            {
                return format.GetMonthName(Month);
            }

            Calendar[] offered = culture.OptionalCalendars;
            return GregorianMonthName(Month, format, offered, localized: true)
                ?? GregorianMonthName(Month, format, offered, localized: false)
                ?? CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(Month);
        }

        // The name that the formats have for the month once they use a Gregorian calendar among those offered,
        // of the culture's own kind or of the other kinds, or null when there is none they take. The calendar
        // is set on a copy that is made here and thrown away, so neither the culture nor its formats, which may
        // be shared and read-only, are changed. Formats that are not the culture's own may refuse a calendar
        // the culture offers; that is not allowed to become an exception which 1.0.0 did not throw.
        private static string? GregorianMonthName(int Month, DateTimeFormatInfo format, Calendar[] offered, bool localized)
        {
            foreach (Calendar calendar in offered)
            {
                if (calendar is GregorianCalendar gregorian
                    && (gregorian.CalendarType == GregorianCalendarTypes.Localized) == localized)
                {
                    var copy = (DateTimeFormatInfo)format.Clone();
                    try
                    {
                        copy.Calendar = gregorian;
                    }
                    catch (ArgumentException)
                    {
                        continue;
                    }
                    return copy.GetMonthName(Month);
                }
            }
            return null;
        }

        private void WriteWeekdayRow()
        {
            Console.ForegroundColor = _dayOfWeekColor;
            Console.WriteLine(WeekdayRow);
            Console.ResetColor();
        }

        // One call on the console per cell, each after setting its color, exactly as 1.0.0 makes them.
        private void WriteDays(MonthLayout layout, int lastDay, bool[] isSelected)
        {
            foreach (MonthLayoutWeek week in layout.Weeks)
            {
                foreach (MonthLayoutCell cell in week.Cells)
                {
                    if (!(cell.Date is DateTime date))
                    {
                        // Only cells before the 1st get here, because the method returns at the last day. An
                        // empty cell is written three wide, its own two columns and the space after it, in the
                        // color of the 1st.
                        SetDayColor(isSelected[1]);
                        Console.Write("   ");
                        continue;
                    }

                    int day = date.Day;
                    SetDayColor(isSelected[day]);
                    // The space between two columns belongs to the write of the day after it, and takes that
                    // day's color. Monday has no column before it, and the 1st needs none either, because the
                    // empty cells before it already include it.
                    bool spaceAlreadyWritten = cell.DayOfWeek == DayOfWeek.Monday || day == 1;
                    if (day < 10)
                    {
                        Console.Write(spaceAlreadyWritten ? " " + day : "  " + day);
                    }
                    else if (spaceAlreadyWritten)
                    {
                        // A number, not a string: 1.0.0 calls Console.Write(int) here and nowhere else.
                        Console.Write(day);
                    }
                    else
                    {
                        Console.Write(" " + day);
                    }

                    // A row ends after its Sunday, and the last row ends after the last day, once and with the
                    // console's line terminator either way. Version 1.0.0 ended the last row with a literal "\n"
                    // instead, which mixed two kinds of line end where the terminator is "\r\n" (corrected in
                    // 2.0, D-59), and ended it a second time when the last day was a Sunday, which put two blank
                    // lines after such a month (corrected in 2.0, D-60).
                    if (cell.DayOfWeek == DayOfWeek.Sunday || day == lastDay)
                    {
                        Console.WriteLine();
                    }
                    if (day == lastDay)
                    {
                        // The one blank line after the month.
                        Console.WriteLine();
                        return;
                    }
                }
            }
        }

        private void SetDayColor(bool selected)
        {
            Console.ForegroundColor = selected ? _selectedDateColor : _dateColor;
        }
    }
}
