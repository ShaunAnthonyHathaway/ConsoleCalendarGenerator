using System.Globalization;
using System.Reflection;
using System.Runtime.Versioning;
using CalendarGenerator;
using ConsoleCalendarGenerator;

// Reports which build of the library this application was given, then uses both APIs from it.
string? libraryBuild = typeof(ConsoleCalendar).Assembly.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName;
Console.WriteLine("library build: " + libraryBuild);

// The modern API: the layout of the same month, with weeks that start on Sunday.
MonthLayout layout = MonthLayout.Create(2023, 2, DayOfWeek.Sunday);
Console.WriteLine("layout: " + layout.Weeks.Count + " weeks, first week "
    + string.Join(" ", layout.Weeks[0].Cells.Select(cell => cell.Date?.Day.ToString(CultureInfo.InvariantCulture) ?? ".")));

// The legacy API: draws the month.
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
new ConsoleCalendar(ConsoleColor.Red).GenerateMonth(2, 2023, new List<DateTime> { new DateTime(2023, 2, 14) });
