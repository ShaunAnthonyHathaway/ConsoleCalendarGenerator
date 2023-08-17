# ConsoleCalendarGenerator

A simple calendar generator for dotnet console applications

## Installing

Just install the [ConsoleCalendarGenerator NuGet package](https://www.nuget.org/packages/ConsoleCalendarGenerator/):

```
PM> Install-Package ConsoleCalendarGenerator
```

### A few examples

```c#
//generate a month at a time with the selected dates passed to each month
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
```
![ss1](https://github.com/ShaunAnthonyHathaway/ConsoleCalendarGenerator/blob/master/ConsoleCalendarGenerator/docs/ss1.png?raw=true)

```c#
//generate a month at a time with a selected date list
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
```
![ss2](https://github.com/ShaunAnthonyHathaway/ConsoleCalendarGenerator/blob/master/ConsoleCalendarGenerator/docs/ss2.png?raw=true)

```c#
//Generate a year with no selected dates
CalendarExample2.SetTitleColor(ConsoleColor.DarkMagenta);
CalendarExample2.GenerateYear(2023);
```
![ss3](https://github.com/ShaunAnthonyHathaway/ConsoleCalendarGenerator/blob/master/ConsoleCalendarGenerator/docs/ss3.png?raw=true)
