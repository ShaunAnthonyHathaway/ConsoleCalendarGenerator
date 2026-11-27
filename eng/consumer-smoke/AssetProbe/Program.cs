using System.Globalization;
using System.Reflection;
using System.Runtime.Versioning;
using CalendarGenerator;

// Reports which build of the library this application was given, then draws one month with it.
string? libraryBuild = typeof(ConsoleCalendar).Assembly.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName;
Console.WriteLine("library build: " + libraryBuild);

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
new ConsoleCalendar(ConsoleColor.Red).GenerateMonth(2, 2023, new List<DateTime> { new DateTime(2023, 2, 14) });
