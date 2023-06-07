
using System.Globalization;

GenerateCalendar(1, 2023, new List<DateTime>() { new DateTime(2023, 1, 1) });
GenerateCalendar(2, 2023, new List<DateTime>() { new DateTime(2023, 2, 1) });
GenerateCalendar(3, 2023, new List<DateTime>() { new DateTime(2023, 3, 1) });
GenerateCalendar(4, 2023, new List<DateTime>() { new DateTime(2023, 4, 1) });
GenerateCalendar(5, 2023, new List<DateTime>() { new DateTime(2023, 5, 1) });
GenerateCalendar(6, 2023, new List<DateTime>() { new DateTime(2023, 6, 1) });
GenerateCalendar(7, 2023, new List<DateTime>() { new DateTime(2023, 7, 1) });
GenerateCalendar(8, 2023, new List<DateTime>() { new DateTime(2023, 8, 1) });
GenerateCalendar(9, 2023, new List<DateTime>() { new DateTime(2023, 9, 1) });
GenerateCalendar(10, 2023, new List<DateTime>() { new DateTime(2023, 10, 1) });
GenerateCalendar(11, 2023, new List<DateTime>() { new DateTime(2023, 11, 1) });
GenerateCalendar(12, 2023, new List<DateTime>() { new DateTime(2023, 12, 1) });

Console.WriteLine("\n\nPress any key to terminate");
Console.ReadKey();
void GenerateCalendar(int Month, int Year, List<DateTime> dates)
{
    List<Int32> HitDays = new List<Int32>();
    foreach (DateTime dt in dates)
    {
        if (HitDays.Contains(dt.Day) == false)
        {
            HitDays.Add(dt.Day);
        }
    }
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
    String ThisDayOfWeek = "Mo";
    days++;
    while (Counter < days)
    {        
        if (!Started)
        {
            if(ThisDayOfWeek == StartDay)
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
            if(Counter < 10)
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
        if(Counter == days)
        {
            Console.Write("\n");
            Console.WriteLine();            
        }
        ThisDayOfWeek = GetNextShortDay(ThisDayOfWeek);
    }
}
static void SetColor(int Counter, ref List<Int32> HitDays)
{
    if(HitDays.Contains(Counter))
    {
        Console.ForegroundColor = ConsoleColor.Blue;         
    }
    else
    {
        Console.ResetColor();
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