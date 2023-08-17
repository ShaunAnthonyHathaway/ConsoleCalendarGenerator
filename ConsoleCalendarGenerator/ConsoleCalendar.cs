using System;
using System.Collections.Generic;
using System.Globalization;

namespace CalendarGenerator
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
            String StartDay = (new DateTime(Year, Month, 1).DayOfWeek).ToString().Substring(0,2);
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
    }
}