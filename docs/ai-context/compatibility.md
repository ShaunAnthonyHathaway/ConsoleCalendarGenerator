# Compatibility

The baseline is the package that is actually on nuget.org: **`ConsoleCalendarGenerator` 1.0.0**, published
2023-06-25, containing one `netstandard2.1` assembly. Everything on this page describes that package and is
verified by tests.

## Policy

- **1.x is additive.** Every 1.x version must be a drop-in replacement for 1.0.0: same public API, same output,
  same side effects, for source that is recompiled and for binaries that are not.
- **Known defects stay in 1.x.** Programs have been written against the behavior that exists, and the README's
  screenshots show it. Changing it silently would change those programs' output.
- **New capability arrives as new API,** in the `ConsoleCalendarGenerator` namespace, beside the legacy class.
- **The legacy class stays available.** `CalendarGenerator.ConsoleCalendar` is not removed or renamed in 1.x.
- **Breaking corrections belong in 2.0,** and each one is a deliberate, listed decision. The list is not final;
  see [open-questions.md](open-questions.md#q-08-what-exactly-changes-in-20).

## The published API

```text
namespace CalendarGenerator
public class ConsoleCalendar                     (not sealed, no interfaces, no virtual or static members)

ConsoleCalendar()
ConsoleCalendar(ConsoleColor TitleColor)
ConsoleCalendar(ConsoleColor TitleColor, ConsoleColor DateColor)
ConsoleCalendar(ConsoleColor TitleColor, ConsoleColor DateColor, ConsoleColor SelectedDateColor)
ConsoleCalendar(ConsoleColor TitleColor, ConsoleColor DateColor, ConsoleColor SelectedDateColor, ConsoleColor DayOfWeekColor)

void GenerateMonth(int Month, int Year)
void GenerateMonth(int Month, int Year, List<DateTime> dates)
void GenerateYear(int Year)
void GenerateYear(int Year, List<DateTime> dates)

ConsoleColor GetTitleColor()            void SetTitleColor(ConsoleColor TitleColor)
ConsoleColor GetDayOfWeekColor()        void SetDayOfWeekColor(ConsoleColor TitleColor)
ConsoleColor GetDateColor()             void SetDateColor(ConsoleColor TitleColor)
ConsoleColor GetSelectedDateColor()     void SetSelectedDateColor(ConsoleColor TitleColor)
```

Details that are part of the contract:

- The namespace is `CalendarGenerator`, which is not the package ID. That mismatch is published and stays.
- The date parameters are the concrete `List<DateTime>`, not an interface.
- Parameter names are part of the contract, because callers can use named arguments: `Month`, `Year`, `dates`,
  the constructor names above, and `TitleColor` on all four setters.
- The class is not sealed. Consumers may have derived from it.
- The assembly is named `ConsoleCalendarGenerator` and is not strong-named. Giving it a strong name would change
  its identity; do not do that without real consumer evidence and Shaun's approval.

## Behavior that must not change by accident

Each line is covered by tests in `ConsoleCalendarGenerator.Tests/Legacy/`.

**Colors and constructors**

- Defaults: title Yellow, weekday row Cyan, dates White, selected dates Blue.
- **The constructor defect.** In the one-, two- and three-argument constructors, the `TitleColor` argument colors
  the weekday row and the title stays Yellow. In the four-argument constructor, `TitleColor` colors the weekday
  row and `DayOfWeekColor` colors the title. The setters are correct.
- Setters and constructors accept values that are not real `ConsoleColor`s. The failure (`ArgumentException`)
  comes later, when the console is asked to use the color, and whatever was written before it stays written.

**Layout and text**

- Weeks start on Monday. The weekday row is always the English `Mo Tu We Th Fr Sa Su`, whatever the culture.
- Day cells are two characters wide with one space between them. No line has trailing spaces.
- The title is indented five spaces for month names of up to 4 characters, four for up to 7, three otherwise,
  followed by the name, a space and the year as a plain number (year 1 is `1`).
- A month is followed by one blank line, or by two when its last day is a Sunday.
- `GenerateYear` is exactly twelve `GenerateMonth` calls. There is no year heading.

**Line endings**

- Every line ends with `Console.WriteLine`, so with the console writer's newline, except the last row of days,
  which ends with a literal `\n`. Where the writer's newline is `\r\n`, as on Windows, the output mixes both.

**Culture**

- The month name comes from `CultureInfo.CurrentCulture` (not `CurrentUICulture`). Nothing else is localized.
- The month number is looked up in the culture's own calendar while the grid is always Gregorian. In a culture
  whose calendar is not Gregorian (for example `fa-IR`), the title names a different month than the grid shows.
- The year is the argument, never converted to the culture's calendar.

**Selected dates**

- A date selects a day when its year, month and day match. Time of day and `DateTimeKind` are ignored.
- Duplicates, order and dates outside the rendered month or year make no difference.
- The caller's list is not modified.
- Selection changes color only. The text is identical with and without it.
- The space before a day belongs to that day's write and takes its color. The empty cells before the 1st are
  written in the color of the 1st.

**Exceptions**

- Month outside 1 to 12, or year outside 1 to 9999: `ArgumentOutOfRangeException`, before anything is written.
- A null list: `NullReferenceException`, before anything is written, and before the month and year are checked.

**Console side effects**

- Output goes straight to the console: text through `Console.Out`, colors through `Console.ForegroundColor`.
- `Console.ResetColor()` is called after the title, after the weekday row and at the end. The caller's
  foreground and background colors are reset, not restored.
- Colors are changed even when `Console.Out` has been replaced.
- The library writes no escape sequences of its own.

## What protects it

Three independent layers. A change has to pass all of them.

1. **Characterization tests** (`ConsoleCalendarGenerator.Tests/Legacy/`), including digests recorded from the
   published 1.0.0 assembly. They run against both library builds. See [testing.md](testing.md).
2. **`PublicApiTests`,** which compares the public surface of the `CalendarGenerator` namespace, by reflection,
   with the list above. The modern API is listed the same way in `Layout/LayoutApiTests`, which also fails if
   the assembly exports a type outside the two namespaces.
3. **Package validation** on every `dotnet pack`: the `netstandard2.1` build is compared with the one in the
   1.0.0 package, the `net10.0` build with the `netstandard2.1` build, and renamed parameters count as breaks.
   See [packaging-and-releases.md](packaging-and-releases.md#package-validation).

If a legacy test fails, the default assumption is that the change is wrong, not the test.

## Versions

- **`AssemblyVersion` is the major version only:** `1.0.0.0` for every 1.x release, `2.0.0.0` when the project
  reaches 2.0, and so on, always matching the package's major version. It must not stay at `1.0.0.0` after 1.x.
- **The package version** is SemVer and is what identifies a release to consumers.
- **The file version** follows the package version.
- **The informational version** carries the package version and the commit, added by the SDK's Source Link
  support. Keep it.

## Adding API safely

- Adding a type or a member is allowed in 1.x. Removing, renaming or changing one is not.
- Do not add an overload that makes an existing call ambiguous. Example: adding
  `GenerateMonth(int, int, IEnumerable<DateTime>)` beside the `List<DateTime>` overload makes
  `GenerateMonth(1, 2023, null)` fail to compile. Analyze `null` arguments and implicit conversions before adding
  any overload to an existing method group. New capability normally belongs on new types instead.
- Do not add `[Obsolete]` to legacy members until a replacement exists to point to. When that happens is an open
  question; see [open-questions.md](open-questions.md#q-05-when-do-legacy-members-become-obsolete).
- Both library builds must expose the same API. Package validation enforces this.
