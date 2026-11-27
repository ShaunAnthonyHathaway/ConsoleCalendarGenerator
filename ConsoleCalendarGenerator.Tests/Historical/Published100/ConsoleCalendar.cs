// HISTORICAL REFERENCE. DO NOT EDIT, REFORMAT, MODERNIZE OR "FIX" ANYTHING BELOW THE MARKER LINE.
//
// What this is
//   The source of CalendarGenerator.ConsoleCalendar as it was when ConsoleCalendarGenerator 1.0.0 was published
//   on nuget.org (2023-06-25). The tests compile it into the test assembly and run it beside the class in the
//   library, to show what 1.0.0 did and exactly where a later version differs. It is evidence, not code to
//   maintain: everything it does wrong is kept on purpose, because the wrong behavior is the thing recorded.
//   Among it: the constructor arguments that color another element than their name says, the
//   NullReferenceException for a null list, the bare "\n" after the last row of days, the second blank line
//   after a month that ends on a Sunday, and the month name taken from the current culture's own calendar.
//
// Where it comes from
//   Repository  https://github.com/ShaunAnthonyHathaway/ConsoleCalendarGenerator
//   File        ConsoleCalendarGenerator/ConsoleCalendar.cs
//   Commit      93f2d5d32537676b01806b2b6995a5628bd5c7dc ("added library and examples", 2023-06-24), and
//               unchanged in every commit after it until def8a3b (2023-08-16)
//   Git blob    02b3c1cf556e7b25c9b8cbb06ae4505cdb296796
//   SHA-256     d79695038673e2576b2e439bcb618cec663ced701c5bdbdb0c46b838d477de3e (8,906 bytes)
//   More, including why this blob and not the one of commit 64995b6: Historical/Published100Source.cs.
//
// What differs from that blob, and nothing else
//   1. This comment, up to and including the marker line, was put in front.
//   2. The byte order mark that began the file is gone, because the file no longer begins with the source.
//   3. The namespace is ConsoleCalendarGenerator.Tests.Historical.Published100 instead of CalendarGenerator, so
//      that the class cannot be confused with, or collide with, the one in the library.
//   4. The file ends with a line feed, which the original did not.
//   Published100SourceTests undoes these four changes and compares the result with the SHA-256 above, so any
//   other change to this file, a single space included, fails the tests.
//
// It is test code only. It is not part of the library, is never packed, and has no public production API.
//
// Copyright (c) 2023 Shaun Hathaway. Licensed under the MIT License; see LICENSE at the root of the repository.
//
// ---- the frozen source of 1.0.0 begins on the next line ----
using System;
using System.Collections.Generic;
using System.Globalization;

namespace ConsoleCalendarGenerator.Tests.Historical.Published100
{
    public class ConsoleCalendar
    {
        private ConsoleColor _dayOfWeekColor { get; set; }
        private ConsoleColor _dateColor { get; set; }
        private ConsoleColor _selectedDateColor { get; set; }
        private ConsoleColor _titleColor { get; set; }
        public void GenerateYear(int Year)
        {
            GenerateYear(Year, new List<DateTime>());
        }
        public void GenerateYear(int Year, List<DateTime> dates)
        {
            int MonthCounter = 0;
            while (MonthCounter < 12)
            {
                MonthCounter++;
                GenerateMonth(MonthCounter, Year, dates);
            }
        }
        public void GenerateMonth(int Month, int Year)
        {
            GenerateMonth(Month, Year, new List<DateTime>());
        }
        public void GenerateMonth(int Month, int Year, List<DateTime> dates)
        {
            List<Int32> HitDays = new List<Int32>();
            foreach (DateTime dt in dates)
            {
                if (dt.Month == Month && dt.Year == Year && HitDays.Contains(dt.Day) == false)
                {
                    HitDays.Add(dt.Day);
                }
            }
            int days = DateTime.DaysInMonth(Year, Month);
            String StartDay = ConvertToShortDay(new DateTime(Year, Month, 1).DayOfWeek);
            Console.ForegroundColor = _titleColor;
            String MonthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(Month);
            Console.WriteLine(GetSpaces(MonthName) + MonthName + " " + Year);
            Console.ResetColor();
            Console.ForegroundColor = _dayOfWeekColor;
            Console.WriteLine("Mo Tu We Th Fr Sa Su");
            Console.ResetColor();
            int Counter = 1;
            bool Started = false;
            String ThisDayOfWeek = "Mo";
            days++;
            while (Counter < days)
            {
                if (!Started)
                {
                    if (ThisDayOfWeek == StartDay)
                    {
                        Started = true;
                        SetColor(Counter, ref HitDays);
                        Console.Write(" " + Counter);
                        Counter++;
                    }
                    else
                    {
                        SetColor(Counter, ref HitDays);
                        Console.Write("   ");
                    }
                }
                else
                {
                    if (Counter < 10)
                    {
                        if (ThisDayOfWeek == "Mo")
                        {
                            SetColor(Counter, ref HitDays);
                            Console.Write(" " + Counter);
                        }
                        else
                        {
                            SetColor(Counter, ref HitDays);
                            Console.Write("  " + Counter);
                        }
                    }
                    else
                    {
                        if (ThisDayOfWeek == "Mo")
                        {
                            SetColor(Counter, ref HitDays);
                            Console.Write(Counter);
                        }
                        else
                        {
                            SetColor(Counter, ref HitDays);
                            Console.Write(" " + Counter);
                        }
                    }
                    Counter++;
                }
                if (ThisDayOfWeek == "Su")
                {
                    Console.WriteLine();
                }
                if (Counter == days)
                {
                    Console.Write("\n");
                    Console.WriteLine();
                }
                ThisDayOfWeek = GetNextShortDay(ThisDayOfWeek);
            }
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
        private void SetColor(int Counter, ref List<Int32> HitDays)
        {
            if (HitDays.Contains(Counter))
            {
                Console.ForegroundColor = _selectedDateColor;
            }
            else
            {
                Console.ForegroundColor = _dateColor;
            }
        }
        private String GetSpaces(String MonthName)
        {
            String ReturnStr = String.Empty;
            if (MonthName.Length <= 4)
            {
                ReturnStr = "     ";
            }
            else if (MonthName.Length <= 7)
            {
                ReturnStr = "    ";
            }
            else
            {
                ReturnStr = "   ";
            }
            return ReturnStr;
        }
        private String GetNextShortDay(String CurrentShortDay)
        {
            String ReturnStr = String.Empty;
            switch (CurrentShortDay)
            {
                case "Mo":
                    ReturnStr = "Tu";
                    break;
                case "Tu":
                    ReturnStr = "We";
                    break;
                case "We":
                    ReturnStr = "Th";
                    break;
                case "Th":
                    ReturnStr = "Fr";
                    break;
                case "Fr":
                    ReturnStr = "Sa";
                    break;
                case "Sa":
                    ReturnStr = "Su";
                    break;
                case "Su":
                    ReturnStr = "Mo";
                    break;
            }
            return ReturnStr;
        }
        private String ConvertToShortDay(DayOfWeek CompareDay)
        {
            String ReturnStr = String.Empty;

            switch (CompareDay)
            {
                case DayOfWeek.Monday:
                    ReturnStr = "Mo";
                    break;
                case DayOfWeek.Tuesday:
                    ReturnStr = "Tu";
                    break;
                case DayOfWeek.Wednesday:
                    ReturnStr = "We";
                    break;
                case DayOfWeek.Thursday:
                    ReturnStr = "Th";
                    break;
                case DayOfWeek.Friday:
                    ReturnStr = "Fr";
                    break;
                case DayOfWeek.Saturday:
                    ReturnStr = "Sa";
                    break;
                case DayOfWeek.Sunday:
                    ReturnStr = "Su";
                    break;
            }
            return ReturnStr;
        }
    }
}
