using System;
using System.Collections.Generic;
using System.Globalization;
using ConsoleCalendarGenerator;

namespace CalendarGenerator
{
    // The API published in ConsoleCalendarGenerator 1.0.0, kept for existing callers. Its public surface, its
    // output, every single call it makes on the console and its exceptions are a compatibility contract,
    // including what is plainly wrong; see docs/ai-context/compatibility.md. Version 2.0 corrects five listed
    // behaviors, each in a change of its own; that file says which are made. Nothing else may be "corrected".
    //
    // Where each day goes comes from MonthLayout, the modern layout model. Everything else about how 1.0.0 draws
    // a month is kept in this class on purpose, because the modern renderer and writers deliberately behave
    // differently: they take the title from the Gregorian calendar, restore the caller's colors, write whole
    // lines with one line terminator, and reject invalid colors early.
    /// <summary>
    /// The calendar of version 1.0.0. It draws a month, or the twelve months of a year, directly on the console,
    /// in four colors.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class is kept so that programs written for version 1.0.0 keep working. What it writes, how it uses the
    /// console and which exceptions it throws are the same in every 1.x version, including the behavior described
    /// here that a new design would not choose. For new code, use the types of the <c>ConsoleCalendarGenerator</c>
    /// namespace instead: <see cref="MonthRenderer"/> renders a month as text without using the console, and
    /// <see cref="ConsoleMonthWriter"/> writes it in color.
    /// </para>
    /// <para>
    /// A month is drawn as a title with the name of the month and the year, the weekday row
    /// <c>Mo Tu We Th Fr Sa Su</c>, and one line for each week. Weeks start on Monday. The title, the weekday row,
    /// the days and the selected days each have a color. A selected day differs from the other days by its color
    /// only; the text is the same.
    /// </para>
    /// <para>
    /// Only the name of the month follows a culture: it is read from <see cref="CultureInfo.CurrentCulture"/> each
    /// time a month is drawn. The weekday row is always in English.
    /// </para>
    /// <para>
    /// A color can be any <see cref="ConsoleColor"/> value, also one that is not defined. Such a value is accepted
    /// and stored; drawing fails later, when the console is asked to use it.
    /// </para>
    /// <para>
    /// Drawing is not synchronized. Drawing from several threads at the same time mixes their output.
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
        /// for the year.
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
        /// with the same list. There is no heading for the year.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="dates"/> is <see langword="null"/>. This is checked first, also when
        /// <paramref name="Year"/> is invalid as well, and nothing is written. Version 1.0.0 threw
        /// <see cref="NullReferenceException"/> here; version 2.0 corrects that.
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
        /// month is followed by one empty line, or by two when its last day is a Sunday.
        /// </para>
        /// <para>
        /// <b>Culture.</b> The name of the month is what the calendar of <see cref="CultureInfo.CurrentCulture"/> calls
        /// its month number <paramref name="Month"/>. The days are always those of the Gregorian month. Under a culture
        /// whose own calendar is not the Gregorian one, the title can therefore name a different month than the one
        /// drawn. The year is never converted.
        /// </para>
        /// <para>
        /// <b>The console.</b> Text is written to <see cref="Console.Out"/> piece by piece, and the color of each piece
        /// is set through <see cref="Console.ForegroundColor"/>. A selected day is written in the color for selected
        /// days, together with the space before it. <see cref="Console.ResetColor"/> is called after the title, after
        /// the weekday row and at the end, so the colors the caller had set are reset, not put back. Colors are set
        /// even when <see cref="Console.Out"/> has been replaced or redirected.
        /// </para>
        /// <para>
        /// <b>Line ends.</b> Every line ends with the line terminator of <see cref="Console.Out"/>, except the last
        /// line of days, which ends with a line feed. Where the terminator is a carriage return and a line feed, as
        /// on Windows, the output contains both forms.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="dates"/> is <see langword="null"/>. This is checked first, also when
        /// <paramref name="Month"/> or <paramref name="Year"/> is invalid as well, and nothing is written. Version 1.0.0
        /// threw <see cref="NullReferenceException"/> here; version 2.0 corrects that.
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
        /// The color of the weekday row. The parameter has this name on all four <c>Set</c> methods; here it has
        /// nothing to do with the title.
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
        /// The color of the days. The parameter has this name on all four <c>Set</c> methods; here it has nothing to
        /// do with the title.
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
        /// The color of the selected days. The parameter has this name on all four <c>Set</c> methods; here it has
        /// nothing to do with the title.
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
        /// This is the constructor to prefer. Change a color afterwards with <see cref="SetTitleColor(ConsoleColor)"/>,
        /// <see cref="SetDayOfWeekColor(ConsoleColor)"/>, <see cref="SetDateColor(ConsoleColor)"/> or
        /// <see cref="SetSelectedDateColor(ConsoleColor)"/>, each of which sets the color it is named for.
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
        /// Version 1.0.0 gave this color to the weekday row and left the title yellow. Version 2.0 corrects that:
        /// the argument colors the title, and the weekday row is cyan. For a program that must draw the same with
        /// both versions, use the parameterless constructor and the <c>Set</c> methods.
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
        /// Version 1.0.0 gave the first color to the weekday row and left the title yellow. Version 2.0 corrects
        /// that: the first argument colors the title, and the weekday row is cyan. For a program that must draw the
        /// same with both versions, use the parameterless constructor and the <c>Set</c> methods.
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
        /// Version 1.0.0 gave the first color to the weekday row and left the title yellow. Version 2.0 corrects
        /// that: the first argument colors the title, and the weekday row is cyan. For a program that must draw the
        /// same with both versions, use the parameterless constructor and the <c>Set</c> methods.
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
        /// Version 1.0.0 gave the first color to the weekday row and the last to the title. Version 2.0 corrects
        /// that: each argument colors what it is named for. A program written for 1.0.0 keeps its appearance by
        /// passing its first and its last color in each other's place, or, to draw the same with both versions, by
        /// using the parameterless constructor and the <c>Set</c> methods.
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

        // The month name comes from the current culture's own calendar, while the grid and the year are Gregorian.
        // Under a culture whose calendar is not Gregorian the title therefore names a different month than the
        // one drawn. The indent has three steps by the length of the name; it is not centering.
        private void WriteTitle(int Month, int Year)
        {
            Console.ForegroundColor = _titleColor;
            string monthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(Month);
            string indent = monthName.Length <= 4 ? "     " : monthName.Length <= 7 ? "    " : "   ";
            Console.WriteLine(indent + monthName + " " + Year);
            Console.ResetColor();
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

                    if (cell.DayOfWeek == DayOfWeek.Sunday)
                    {
                        Console.WriteLine();
                    }
                    if (day == lastDay)
                    {
                        // The last row ends with a literal line feed, not with the console's line terminator,
                        // followed by a blank line. Where the console's terminator is "\r\n" the output mixes
                        // both. A month whose last day is a Sunday has just ended its row above as well, so it
                        // is followed by two blank lines.
                        Console.Write("\n");
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
