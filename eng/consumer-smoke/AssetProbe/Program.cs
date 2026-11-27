using System.Globalization;
using System.Reflection;
using System.Runtime.Versioning;
using CalendarGenerator;
using ConsoleCalendarGenerator;

// With the argument "culture-audit" the application checks the legacy title under every culture the system has
// and reports what it found; see CultureAudit.cs. That output differs between systems and is not compared.
if (args.Length == 1 && args[0] == "culture-audit")
{
    return CultureAudit.Run();
}

// Reports which build of the library this application was given, then uses both APIs from it.
string? libraryBuild = typeof(ConsoleCalendar).Assembly.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName;
Console.WriteLine("library build: " + libraryBuild);

// Reports which version that build says it is: the package version it was built as, which is its informational
// version without the commit, then its assembly version and its file version.
Assembly library = typeof(ConsoleCalendar).Assembly;
string? builtAs = library.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0];
Console.WriteLine("library version: " + builtAs + ", assembly " + library.GetName().Version
    + ", file " + library.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version);

// The modern API: the layout of the same month, with weeks that start on Sunday.
MonthLayout layout = MonthLayout.Create(2023, 2, DayOfWeek.Sunday);
Console.WriteLine("layout: " + layout.Weeks.Count + " weeks, first week "
    + string.Join(" ", layout.Weeks[0].Cells.Select(cell => cell.Date?.Day.ToString(CultureInfo.InvariantCulture) ?? ".")));

// The modern API: options, rendering, the segments, and the text with a line terminator chosen here.
MonthRenderOptions options = MonthRenderOptions.Default
    .WithFirstDayOfWeek(DayOfWeek.Sunday)
    .WithCellWidth(3)
    .WithIndent("  ")
    .WithTitleFormat("MMMM yyyy")
    .WithTitleSuffix(" (rendered)");
RenderedMonth rendered = MonthRenderer.Render(2023, 2, options);
RenderedSegment[] segments = rendered.Lines.SelectMany(line => line.Segments).ToArray();
Console.WriteLine("rendered: " + rendered.Lines.Count + " lines, "
    + segments.Count(segment => segment.Kind == RenderedSegmentKind.Day) + " days, "
    + segments.Count(segment => segment.Kind == RenderedSegmentKind.EmptyDay) + " empty cells, first day on a "
    + segments.First(segment => segment.Date.HasValue).DayOfWeek);
Console.WriteLine(rendered.ToText("\n"));

// The modern API: a day state with a marker, found again on its segment, a theme that gives it a color, and
// the two writers. The console writer is used with no colors, so that the output does not depend on a terminal.
var chosen = new DayState("chosen", "*");
RenderedMonth marked = MonthRenderer.Render(2023, 2, options.WithTitleSuffix(" (* = chosen)"), new[] { new DateTime(2023, 2, 14, 9, 30, 0) }, chosen);
RenderedSegment markedDay = marked.Lines.SelectMany(line => line.Segments).Single(segment => segment.State != null);
CalendarTheme theme = CalendarTheme.None.WithDayColor(ConsoleColor.White).WithStateColor(chosen.Name, ConsoleColor.Green);
Console.WriteLine("state: " + markedDay.State!.Name + " on day " + markedDay.Date!.Value.Day.ToString(CultureInfo.InvariantCulture)
    + ", cell \"" + markedDay.Text + "\", theme color " + theme.GetColor(markedDay)
    + ", other days " + theme.GetColor(segments.First(segment => segment.Date.HasValue)));
var text = new StringWriter { NewLine = "\n" };
MonthWriter.Write(marked, text);
Console.Write(text.ToString());
ConsoleMonthWriter.Write(marked, CalendarTheme.None);

// The legacy API: draws the month.
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
new ConsoleCalendar(ConsoleColor.Red).GenerateMonth(2, 2023, new List<DateTime> { new DateTime(2023, 2, 14) });
return 0;
