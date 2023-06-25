var CalendarExample1 = new CalendarGenerator.ConsoleCalendar();
CalendarExample1.GenerateMonth(1, 2023, new List<DateTime>() { new DateTime(2023, 1, 1) });
CalendarExample1.SetTitleColor(ConsoleColor.Red);
CalendarExample1.GenerateMonth(2, 2023, new List<DateTime>() { new DateTime(2023, 2, 1), new DateTime(2023, 2, 4) });
CalendarExample1.SetSelectedDateColor(ConsoleColor.Magenta);
CalendarExample1.GenerateMonth(3, 2023, new List<DateTime>() { new DateTime(2023, 3, 1) });
CalendarExample1.SetDayOfWeekColor(ConsoleColor.Green);
CalendarExample1.GenerateMonth(4, 2023, new List<DateTime>() { new DateTime(2023, 4, 1) });
CalendarExample1.SetDateColor(ConsoleColor.DarkYellow);
CalendarExample1.GenerateMonth(5, 2023, new List<DateTime>() { new DateTime(2023, 5, 1), new DateTime(2023, 5, 8), new DateTime(2023, 5, 11) });
Pause();

var CalendarExample2 = new CalendarGenerator.ConsoleCalendar(ConsoleColor.Red);
List<DateTime> HitDates = new List<DateTime>()
{
    new DateTime(2023, 1, 1),
    new DateTime(2023, 2, 1), 
    new DateTime(2023, 2, 4),
    new DateTime(2023, 3, 1),
    new DateTime(2023, 4, 1),
    new DateTime(2023, 5, 1), 
    new DateTime(2023, 5, 8), 
    new DateTime(2023, 5, 11)
};
CalendarExample2.GenerateMonth(1, 2023, HitDates);
CalendarExample2.GenerateMonth(2, 2023, HitDates);
CalendarExample2.GenerateMonth(3, 2023, HitDates);
CalendarExample2.GenerateMonth(4, 2023, HitDates);
CalendarExample2.GenerateMonth(5, 2023, HitDates);
Pause();

CalendarExample2.SetTitleColor(ConsoleColor.DarkMagenta);
CalendarExample2.GenerateYear(2023);
Pause();

var CalendarExample4 = new CalendarGenerator.ConsoleCalendar(ConsoleColor.Red, ConsoleColor.Yellow, ConsoleColor.Magenta, ConsoleColor.Blue);
CalendarExample4.GenerateYear(2023, HitDates);
Console.WriteLine("\n\nPress any key to terminate...");
Console.ReadKey();
void Pause()
{
    Console.WriteLine("\n\nPress any key for next example...");
    Console.ReadKey();
    Console.Write("\f\u001bc\x1b[3J");
}