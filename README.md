# ConsoleCalendarGenerator

Month calendars for .NET applications: deterministic text, structured output you can style yourself, and color
output for the console.

This documentation covers ConsoleCalendarGenerator 2.0.1.

## Installation

```text
dotnet add package ConsoleCalendarGenerator --version 2.0.1
```

## Quick start

```csharp
using System.Globalization;
using ConsoleCalendarGenerator;

MonthRenderOptions options = MonthRenderOptions.Default
    .WithCulture(CultureInfo.GetCultureInfo("en-US"))
    .WithWeekdayLabels(new[] { "Su", "Mo", "Tu", "We", "Th", "Fr", "Sa" });

RenderedMonth month = MonthRenderer.Render(2027, 3, options);

Console.WriteLine(month.ToText());
```

```text
     March 2027
Mo Tu We Th Fr Sa Su
 1  2  3  4  5  6  7
 8  9 10 11 12 13 14
15 16 17 18 19 20 21
22 23 24 25 26 27 28
29 30 31
```

The examples on this page are from a console project with implicit usings. Options are immutable: start from
`MonthRenderOptions.Default` and chain `With` methods, each of which returns a new instance.

## Features

- **Gregorian month layout** for any month, with any first day of the week.
- **Deterministic text rendering.** The text depends only on the options you pass. Nothing is read from the
  current culture, the console or the clock.
- **Semantic lines and segments.** Every piece of text identifies itself as a title, a weekday label, a day
  and so on, with its date and state, so you can style or draw a month without parsing text.
- **Culture and first-day configuration.** Titles and weekday labels in the culture you choose, or labels of
  your own.
- **Visible date markers.** Marked dates show in the text itself, so they never depend on color alone.
- **Optional `ConsoleColor` themes** for the title, the weekday labels, the days and each kind of marked date.
- **`TextWriter` output** to any destination.
- **Direct console output** that puts the caller's colors back when it is done.
- **`CalendarGenerator.ConsoleCalendar`,** the original API, for existing applications.

## Culture and first day of the week

The culture sets the language of the title. The first day of the week is a separate option: weeks start on
Monday unless you choose another day. Starting from the `options` of the quick start:

```csharp
MonthRenderOptions sundayFirst = options.WithFirstDayOfWeek(DayOfWeek.Sunday);

Console.WriteLine(MonthRenderer.Render(2027, 3, sundayFirst).ToText());
```

```text
     March 2027
Su Mo Tu We Th Fr Sa
    1  2  3  4  5  6
 7  8  9 10 11 12 13
14 15 16 17 18 19 20
21 22 23 24 25 26 27
28 29 30 31
```

To follow a culture's own convention, pass `culture.DateTimeFormat.FirstDayOfWeek`.

Weekday labels are always seven, Sunday first, whichever day the week starts on:

```csharp
MonthRenderOptions french = MonthRenderOptions.Default
    .WithCulture(CultureInfo.GetCultureInfo("fr-FR"))
    .WithWeekdayLabels(new[] { "di", "lu", "ma", "me", "je", "ve", "sa" });

Console.WriteLine(MonthRenderer.Render(2027, 3, french).ToText());
```

```text
     mars 2027
lu ma me je ve sa di
 1  2  3  4  5  6  7
 8  9 10 11 12 13 14
15 16 17 18 19 20 21
22 23 24 25 26 27 28
29 30 31
```

- **The default culture is the invariant culture,** which keeps the output independent of the machine. Pass the
  culture you want for the title, as both examples do.
- **Labels you supply are identical on every operating system.** Without them, the labels are the culture's
  day names as .NET provides them, and those vary between operating systems: for English, two letters on Linux
  and macOS and one letter on Windows.
- **The calendar is Gregorian.** The title names the Gregorian month and year shown, in the language of the
  culture.

## Marking dates

A `DayState` is a name and a marker. Days in that state show the marker directly before the day number:

```csharp
var holiday = new DayState("holiday", "*");
DateTime[] holidays = { new DateTime(2027, 3, 5), new DateTime(2027, 3, 17), new DateTime(2027, 3, 26) };

MonthRenderOptions withMarkers = options
    .WithCellWidth(3)
    .WithTitleSuffix(" (* = holiday)");

RenderedMonth month = MonthRenderer.Render(2027, 3, withMarkers, holidays, holiday);

Console.WriteLine(month.ToText());
```

```text
 March 2027 (* = holiday)
 Mo  Tu  We  Th  Fr  Sa  Su
  1   2   3   4  *5   6   7
  8   9  10  11  12  13  14
 15  16 *17  18  19  20  21
 22  23  24  25 *26  27  28
 29  30  31
```

- **Give the marker room.** A marker has to fit in the cell beside a two-digit day, so a one-character marker
  needs `WithCellWidth(3)`. A marker that does not fit is reported as an error.
- **Add a legend.** `WithTitleSuffix` is a simple place to say what a marker means.
- **Dates match by year, month and day.** The time of day is ignored, and dates in other months are skipped.
- **Use several states** by passing dates with their states, for example a `Dictionary<DateTime, DayState>`.
  Each day has one state.

## Styling and console output

A `CalendarTheme` assigns console colors to the parts of a month, and `ConsoleMonthWriter` writes the month in
those colors. Colors add to the markers; the text stays the same.

```csharp
CalendarTheme theme = CalendarTheme.None
    .WithTitleColor(ConsoleColor.Yellow)
    .WithWeekdayLabelColor(ConsoleColor.Cyan)
    .WithStateColor("holiday", ConsoleColor.Green);

ConsoleMonthWriter.Write(month, theme);
```

![March 2027 on a terminal: a yellow title, cyan weekday labels, and the 5th, 17th and 26th in green, each with a star before the number](https://raw.githubusercontent.com/ShaunAnthonyHathaway/ConsoleCalendarGenerator/v2.0.1/docs/images/month-with-theme.png)

- A theme has an optional color for the title, the title suffix, the weekday labels, the days, and each state
  by name. Text without a color uses the color the console already has.
- State names are matched exactly, including case.
- Colors are `ConsoleColor` foreground colors. `CalendarTheme.None` writes without colors.
- `ConsoleMonthWriter` writes through `Console.Out` and, before it returns, puts back the colors the console
  had when it started. When output is redirected to a file or a pipe, the text arrives plain.

To write anywhere else, use `MonthWriter` with any `TextWriter`. It writes each line followed by the writer's
own line terminator and never uses the console:

```csharp
using var writer = new StringWriter();

MonthWriter.Write(month, writer);

string text = writer.ToString();
```

`ToText()` returns the month as a string with `"\n"` between the lines on every platform; `ToText("\r\n")`
uses the terminator you pass.

## Structured rendering

A rendered month is a list of lines, and each line is a list of segments. A segment has its text, a kind, and
for day cells the date, the weekday and the state. Read the segments to work with a month without parsing its
text:

```csharp
foreach (RenderedSegment segment in month.Lines.SelectMany(line => line.Segments))
{
    if (segment.State != null && segment.Date is DateTime date)
    {
        Console.WriteLine($"{date.DayOfWeek} {date.Day}: {segment.State.Name}, drawn as \"{segment.Text}\"");
    }
}
```

```text
Friday 5: holiday, drawn as " *5"
Wednesday 17: holiday, drawn as "*17"
Friday 26: holiday, drawn as "*26"
```

The kinds are `Indent`, `Padding`, `Title`, `TitleSuffix`, `WeekdayLabel`, `Gap`, `EmptyDay` and `Day`. The
segments of a line, joined in order, are exactly the text of the line. `CalendarTheme.GetColor(segment)`
returns the color a theme assigns to a segment, which is what you need to draw a month in a user interface of
your own.

## Layout API

`MonthLayout` places the days of a month in weeks and weekday columns, with no text involved:

```csharp
MonthLayout layout = MonthLayout.Create(2027, 4, DayOfWeek.Monday);

Console.WriteLine($"April 2027 needs {layout.Weeks.Count} weeks. Its first week:");
foreach (MonthLayoutCell cell in layout.Weeks[0].Cells)
{
    Console.WriteLine($"  {cell.DayOfWeek}: {(cell.Date is DateTime date ? $"the {date.Day}" : "empty")}");
}
```

```text
April 2027 needs 5 weeks. Its first week:
  Monday: empty
  Tuesday: empty
  Wednesday: empty
  Thursday: the 1
  Friday: the 2
  Saturday: the 3
  Sunday: the 4
```

`MonthRenderer` uses the same layout. Use `MonthLayout` directly when you draw the month yourself.

## Options

| Option | Default | Notes |
|---|---|---|
| `WithCulture` | invariant culture | Language of the title and of culture-provided labels |
| `WithFirstDayOfWeek` | Monday | Independent of the culture |
| `WithWeekdayLabels` | none | Seven labels of your own, Sunday first |
| `WithWeekdayLabelStyle` | `Shortest` | Or `Abbreviated`; applies to culture-provided labels |
| `WithCellWidth` | 2 | 2 to 100; days and labels are right-aligned |
| `WithColumnGap` | 1 | 0 to 100 spaces between columns |
| `WithIndent` | none | Text before every line |
| `WithTitleFormat` | `"Y"` | Any .NET date format string, such as `"MMMM yyyy"` |
| `WithTitleSuffix` | none | Text directly after the title |
| `WithTitleAlignment` | `Center` | Or `Left` |

A label longer than its cell is shortened to fit, so the grid keeps its shape in every language. Widths are
counted in text elements (user-perceived characters). `MonthRenderer` renders one month per call.

## Using ConsoleCalendar

`CalendarGenerator.ConsoleCalendar` is the original API of the package, and it remains available with the same
constructors, methods and parameter names. Existing applications can continue using it. It generates
individual months and full years, writes directly to the console, and supports configurable colors and
selected dates.

```csharp
// ConsoleCalendar takes the month name from the current culture.
CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");

var calendar = new CalendarGenerator.ConsoleCalendar();
calendar.SetTitleColor(ConsoleColor.Yellow);
calendar.SetSelectedDateColor(ConsoleColor.Green);

calendar.GenerateMonth(3, 2027, new List<DateTime> { new DateTime(2027, 3, 17) });
```

```text
    March 2027
Mo Tu We Th Fr Sa Su
 1  2  3  4  5  6  7
 8  9 10 11 12 13 14
15 16 17 18 19 20 21
22 23 24 25 26 27 28
29 30 31
```

- `GenerateMonth` draws one month, and `GenerateYear` draws the twelve months of a year.
- The title, the weekday row, the dates and the selected dates each have a color, set through the
  constructors or the four `Set` methods.
- Weeks start on Monday, and the weekday row is `Mo Tu We Th Fr Sa Su`.
- The console's colors are reset after each month is drawn.

The structured rendering API described above is there when you need layout data, semantic segments,
deterministic text, markers or your own output handling. Both can be used in the same application.

## Moving from 1.0.0 to 2.x

Version 2 keeps the original public signatures and updates five observable behaviors of
`CalendarGenerator.ConsoleCalendar`. Code written for 1.0.0 compiles as it is, and an application compiled
against 1.0.0 runs with version 2 without being rebuilt.

1. **Constructor colors.** Constructor color arguments now apply to the elements named by those arguments:
   `TitleColor` to the title, `DateColor` to the dates, `SelectedDateColor` to the selected dates and
   `DayOfWeekColor` to the weekday row. In 1.0.0, `TitleColor` applied to the weekday row and, with four
   arguments, `DayOfWeekColor` applied to the title. An application that chose its four arguments for that
   assignment keeps its appearance by swapping the first and fourth values; with fewer arguments,
   `SetDayOfWeekColor` and `SetTitleColor` give the same result. The parameterless constructor and the `Set`
   methods behave the same in both versions.
2. **Null date lists.** `GenerateMonth` and `GenerateYear` throw `ArgumentNullException` with the parameter
   name `dates` when the list is `null`. In 1.0.0 they threw `NullReferenceException`. Catch
   `ArgumentNullException`, or pass an empty list to select no dates.
3. **Line endings.** Console output uses consistent line endings: every line ends with the console's line
   terminator. In 1.0.0 the last row of a month ended with a line feed. Output on Linux and macOS is the same
   as before; on Windows every line now ends with CRLF.
4. **Spacing after a month.** Each generated month ends with one blank line. In 1.0.0 a month whose last day
   is a Sunday was followed by two.
5. **Month titles.** Localized month titles correspond to the Gregorian grid: the title is the current
   culture's name for the Gregorian month shown. This affects cultures whose default calendar is not
   Gregorian, such as `fa-IR`, where 1.0.0 took the name from the culture's default calendar.

Snapshot tests and code that counts output lines may need updates for items 3 to 5. Everything else stays as
it was: `GenerateYear`, the `Get` and `Set` methods, date selection by year, month and day, Monday-first
weeks, the weekday row and direct console output.

## Supported frameworks

The package contains two builds of the library, and NuGet selects the right one for your project:

| Build | Used by |
|---|---|
| `net10.0` | Projects that target .NET 10 or later |
| `netstandard2.1` | Projects that target another framework implementing .NET Standard 2.1, such as .NET 8 |

Both builds have the same API and the same behavior. The library has no dependencies, supports trimming and
Native AOT in its `net10.0` build, and is built and tested on Linux, Windows and macOS.

## Building and testing

Building requires the .NET SDK named in
[global.json](https://github.com/ShaunAnthonyHathaway/ConsoleCalendarGenerator/blob/v2.0.1/global.json).

```text
dotnet build ConsoleCalendarGenerator.sln --configuration Release
dotnet test ConsoleCalendarGenerator.Tests/ConsoleCalendarGenerator.Tests.csproj --configuration Release --filter "TestCategory!=Terminal"
dotnet run --project ConsoleCalendarGeneratorExample
```

The last command runs the
[example program](https://github.com/ShaunAnthonyHathaway/ConsoleCalendarGenerator/tree/v2.0.1/ConsoleCalendarGeneratorExample),
the source of every example on this page.
[CONTRIBUTING.md](https://github.com/ShaunAnthonyHathaway/ConsoleCalendarGenerator/blob/master/CONTRIBUTING.md)
covers packaging and the contribution workflow.

## Support and security

- Questions and bug reports: [GitHub issues](https://github.com/ShaunAnthonyHathaway/ConsoleCalendarGenerator/issues)
- Security reports: [SECURITY.md](https://github.com/ShaunAnthonyHathaway/ConsoleCalendarGenerator/blob/master/SECURITY.md)
- Release history: [CHANGELOG.md](https://github.com/ShaunAnthonyHathaway/ConsoleCalendarGenerator/blob/master/CHANGELOG.md)

## License

[MIT](https://github.com/ShaunAnthonyHathaway/ConsoleCalendarGenerator/blob/master/LICENSE)
