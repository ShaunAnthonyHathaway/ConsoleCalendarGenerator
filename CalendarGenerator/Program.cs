
using System.Globalization;
using System;
using System.Text;

GenerateCalendar(1, 2023);
GenerateCalendar(2, 2023);
GenerateCalendar(3, 2023);
GenerateCalendar(4, 2023);
GenerateCalendar(5, 2023);
GenerateCalendar(6, 2023);
GenerateCalendar(7, 2023);
GenerateCalendar(8, 2023);
GenerateCalendar(9, 2023);
GenerateCalendar(10, 2023);
GenerateCalendar(11, 2023);
GenerateCalendar(12, 2023);

Console.WriteLine("\n\nPress any key to terminate");
Console.ReadKey();
void GenerateCalendar(int Month, int Year)
{
    int days = DateTime.DaysInMonth(Year, Month);
    String StartDay = ConvertToShortDay(new DateTime(Year, Month, 1).DayOfWeek);
    Console.ForegroundColor = ConsoleColor.Yellow;
    String MonthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(Month);
    Console.WriteLine(GetSpaces(MonthName) + MonthName + " " + Year);
    Console.ResetColor();
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("Mo Tu We Th Fr Sa Su");
    Console.ResetColor();
    int Counter = 1;
    bool Started = false;
    StringBuilder CalendarSB = new StringBuilder();
    String ThisDayOfWeek = "Mo";
    days++;
    while (Counter < days)
    {        
        if (!Started)
        {
            if(ThisDayOfWeek == StartDay)
            {                
                Started = true;
                CalendarSB.Append(" " + Counter);
                Counter++;
            }
            else
            {
                CalendarSB.Append("   ");                
            }
        }
        else
        {
            if(Counter < 10)
            {
                if (ThisDayOfWeek == "Mo")
                {
                    CalendarSB.Append(" " + Counter);
                }
                else
                {
                    CalendarSB.Append("  " + Counter);
                }                    
            }
            else
            {
                if (ThisDayOfWeek == "Mo")
                {
                    CalendarSB.Append(Counter);
                }
                else
                {
                    CalendarSB.Append(" " + Counter);
                }
            }
            Counter++;
        }
        if (ThisDayOfWeek == "Su")
        {
            Console.WriteLine(CalendarSB.ToString());
            CalendarSB = new StringBuilder();
        }
        if(Counter == days)
        {
            Console.WriteLine(CalendarSB.ToString() + "\n");            
        }
        ThisDayOfWeek = GetNextShortDay(ThisDayOfWeek);
    }
}
String GetSpaces(String MonthName)
{
    String ReturnStr = String.Empty;

    if(MonthName.Length <= 4)
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
String GetNextShortDay(String CurrentShortDay)
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
String ConvertToShortDay(DayOfWeek CompareDay)
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