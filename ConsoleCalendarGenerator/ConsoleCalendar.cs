using System;
using System.Collections.Generic;
using System.Globalization;
using ConsoleCalendarGenerator;

namespace CalendarGenerator
{
    // The API published in ConsoleCalendarGenerator 1.0.0, kept for existing callers. Its public surface, its
    // output, every single call it makes on the console and its exceptions are a compatibility contract for the
    // whole 1.x line, including what is plainly wrong; see docs/ai-context/compatibility.md. Nothing here may be
    // "corrected".
    //
    // Where each day goes comes from MonthLayout, the modern layout model. Everything else about how 1.0.0 draws
    // a month is kept in this class on purpose, because the modern renderer and writers deliberately behave
    // differently: they take the title from the Gregorian calendar, restore the caller's colors, write whole
    // lines with one line terminator, and reject invalid colors early.
    public class ConsoleCalendar
    {
        // Always English and always from Monday, whatever the culture.
        private const string WeekdayRow = "Mo Tu We Th Fr Sa Su";

        private ConsoleColor _dayOfWeekColor;
        private ConsoleColor _dateColor;
        private ConsoleColor _selectedDateColor;
        private ConsoleColor _titleColor;

        public void GenerateYear(int Year)
        {
            GenerateYear(Year, new List<DateTime>());
        }
        public void GenerateYear(int Year, List<DateTime> dates)
        {
            // Twelve months one after the other, with no heading for the year.
            for (int month = 1; month <= 12; month++)
            {
                GenerateMonth(month, Year, dates);
            }
        }
        public void GenerateMonth(int Month, int Year)
        {
            GenerateMonth(Month, Year, new List<DateTime>());
        }
        public void GenerateMonth(int Month, int Year, List<DateTime> dates)
        {
            // The order of these first two steps is what callers observe when arguments are invalid. The list is
            // read first, so a null list fails with NullReferenceException even when the month or the year is
            // invalid as well. Then the runtime checks the month and the year, and its exception is the one that
            // reaches the caller. Both happen before anything is written or any color is set.
            bool[] isSelected = SelectedDays(Month, Year, dates);
            int lastDay = DateTime.DaysInMonth(Year, Month);

            MonthLayout layout = MonthLayout.Create(Year, Month, DayOfWeek.Monday);

            WriteTitle(Month, Year);
            WriteWeekdayRow();
            WriteDays(layout, lastDay, isSelected);
            // Reset, not restored: the colors the caller had are gone.
            Console.ResetColor();
        }
        public ConsoleColor GetDayOfWeekColor()
        {
            return _dayOfWeekColor;
        }
        public void SetDayOfWeekColor(ConsoleColor TitleColor)
        {
            _dayOfWeekColor = TitleColor;
        }
        public ConsoleColor GetDateColor()
        {
            return _dateColor;
        }
        public void SetDateColor(ConsoleColor TitleColor)
        {
            _dateColor = TitleColor;
        }
        public ConsoleColor GetSelectedDateColor()
        {
            return _selectedDateColor;
        }
        public void SetSelectedDateColor(ConsoleColor TitleColor)
        {
            _selectedDateColor = TitleColor;
        }
        public ConsoleColor GetTitleColor()
        {
            return _titleColor;
        }
        public void SetTitleColor(ConsoleColor TitleColor)
        {
            _titleColor = TitleColor;
        }
        public ConsoleCalendar()
        {
            _dayOfWeekColor = ConsoleColor.Cyan;
            _dateColor = ConsoleColor.White;
            _titleColor = ConsoleColor.Yellow;
            _selectedDateColor = ConsoleColor.Blue;
        }

        // In the constructors below the argument named TitleColor colors the weekday row, and in the last one the
        // argument named DayOfWeekColor colors the title. That is the published behavior and it stays for 1.x.
        public ConsoleCalendar(ConsoleColor TitleColor)
        {
            _dayOfWeekColor = TitleColor;
            _dateColor = ConsoleColor.White;
            _titleColor = ConsoleColor.Yellow;
            _selectedDateColor = ConsoleColor.Blue;
        }
        public ConsoleCalendar(ConsoleColor TitleColor, ConsoleColor DateColor)
        {
            _dayOfWeekColor = TitleColor;
            _dateColor = DateColor;
            _titleColor = ConsoleColor.Yellow;
            _selectedDateColor = ConsoleColor.Blue;
        }
        public ConsoleCalendar(ConsoleColor TitleColor, ConsoleColor DateColor, ConsoleColor SelectedDateColor)
        {
            _dayOfWeekColor = TitleColor;
            _dateColor = DateColor;
            _titleColor = ConsoleColor.Yellow;
            _selectedDateColor = SelectedDateColor;
        }
        public ConsoleCalendar(ConsoleColor TitleColor, ConsoleColor DateColor, ConsoleColor SelectedDateColor, ConsoleColor DayOfWeekColor)
        {
            _dayOfWeekColor = TitleColor;
            _dateColor = DateColor;
            _titleColor = DayOfWeekColor;
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
