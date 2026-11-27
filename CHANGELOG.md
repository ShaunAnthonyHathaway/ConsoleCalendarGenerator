# Changelog

Notable changes to ConsoleCalendarGenerator, newest first. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and versions follow
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.0.0] - not yet published

Version 2.0.0 is final in this repository. It is not on NuGet yet, so this entry has no date: the date is put
in the heading when the version is published, and nothing else about the entry changes then.

### Added

- **A modern API** in the `ConsoleCalendarGenerator` namespace, beside the existing class. It renders one
  month at a time.
  - **Layout.** `MonthLayout` places the days of a month in weeks and weekday columns, for any first day of the
    week, with no text involved.
  - **Deterministic rendering.** `MonthRenderer` renders a month as text that depends only on the options it
    is given. Nothing is read from the current culture, the console or the clock.
  - **Options.** `MonthRenderOptions` sets the culture of the title and the weekday labels, the first day of
    the week, weekday labels of your own, the cell width, the column gap, an indent, and the format, suffix
    and alignment of the title.
  - **Segments that say what they are.** The result, `RenderedMonth`, is lines made of segments: title, weekday
    label, day, marked day and the spacing between them. A month can be styled or drawn elsewhere without
    parsing text.
  - **Day states with visible markers.** `DayState` marks dates with a marker in the text itself, so that a
    marked date does not depend on color.
  - **Themes.** `CalendarTheme` maps the title, the weekday labels, the days and each state to optional
    console colors.
  - **Writers.** `MonthWriter` writes a rendered month to any `TextWriter`. `ConsoleMonthWriter` writes it to
    the console in the colors of a theme and puts the caller's colors back.
- A `net10.0` build of the library, marked compatible with trimming and Native AOT.
- XML documentation for every public type and member, in the package.
- A readme in the package, and complete package metadata: description, license, repository and commit, and
  this changelog as the release notes.
- A symbol package with Source Link.

### Changed

- **Supported frameworks.** The package has two builds, `net10.0` and `netstandard2.1`. Version 1.0.0 had the
  `netstandard2.1` build only; projects that used it keep getting it.
- **Assembly version.** It is `2.0.0.0`. It was `1.0.0.0` in 1.0.0.
- **Documentation and example.** The README, the example program and the screenshot are new and describe the
  modern API. The example program runs without input and prints the same text on Linux, Windows and macOS,
  apart from one example that shows a culture's own day names on purpose.
- **How the package is checked.** Every change is built and tested on Linux, Windows and macOS against both
  builds. Each time the package is packed its public API is compared with 1.0.0, and it is installed into
  separate applications, a Native AOT one among them.

### Breaking changes

Version 2.0 corrects five behaviors of `CalendarGenerator.ConsoleCalendar` that were wrong in 1.0.0. Each is
listed here with what to do in a program that relied on the old behavior. The public API of the class does not
change, so programs compile and load as before.

- **The constructors that take colors give each color to the element it is named for.** `TitleColor` colors
  the title, `DateColor` the days, `SelectedDateColor` the selected days and `DayOfWeekColor` the weekday row.
  In 1.0.0, `TitleColor` colored the weekday row, the title stayed yellow, and with four arguments
  `DayOfWeekColor` colored the title.
  - With one, two or three arguments, the title is now drawn in the first color instead of yellow, and the
    weekday row in cyan instead of the first color.
  - With four arguments, the first and the fourth color trade places. To keep what a program drew with 1.0.0,
    pass those two in each other's place.
  - The parameterless constructor and the `Set` methods are unchanged. A program that uses them draws the
    same with both versions.
  - A value that is not a defined `ConsoleColor` is still accepted and still fails with `ArgumentException`
    only when its element is drawn. For the first and the fourth argument that element changed, so the
    failure now comes at another point: an invalid `TitleColor` fails before anything is written, where 1.0.0
    wrote the title first, and an invalid `DayOfWeekColor` fails after the title is written, where 1.0.0
    wrote nothing.
- **A null list of dates throws `ArgumentNullException` with the parameter name `dates`.** That applies to
  `GenerateMonth(Month, Year, dates)` and `GenerateYear(Year, dates)`. In 1.0.0 they threw
  `NullReferenceException`.
  - The list is still checked before the month and the year, and still before anything is written or any
    color is set.
  - A program that catches `NullReferenceException` around these calls no longer catches this. Catch
    `ArgumentNullException`, or pass an empty list, which selects no dates.
  - Nothing changes for a list that is not `null`.
- **Legacy output uses the console writer's line terminator for every line.** The last row of days of a month
  now ends with it, like every other line. In 1.0.0 that row ended with a bare line feed whatever the
  terminator was.
  - On Windows, where the terminator is CRLF, the output no longer mixes LF and CRLF.
  - Where the terminator is a line feed, as on Linux and macOS, the text is unchanged.
  - A program that replaces `Console.Out` with a writer of its own sees `WriteLine()` where it saw
    `Write("\n")`.
- **Every generated month ends with exactly one blank line.** In 1.0.0 a month whose last day is a Sunday was
  followed by two.
  - Such a month is one line shorter, and so is the output of `GenerateYear` for each such month in the year.
  - A program that counts lines or compares the output with stored text has to allow for it.
- **The title of a legacy month names the Gregorian month it shows.** The name is the current culture's name
  for that month of the Gregorian calendar. In 1.0.0 it was the culture's name for the month of the same number
  in the culture's own calendar.
  - Under a culture whose calendar is the Gregorian one, which is nearly every culture, nothing changes.
  - Under a culture whose calendar is another one, such as `fa-IR` where the system gives it the Persian
    calendar, the title named a different month than the days below it. It now names the month that is drawn,
    in the same language. Which cultures have such a calendar is decided by the operating system's data, so it
    can differ from one system to another.
  - The year in the title is still the number passed in and is never converted. The weekday row, the first day
    of the week and the colors are unchanged, and the culture is only read.
  - The title is still indented by the length of the name, in the same three steps, so it can start a column
    or two further left or right where the two names differ in length.

### Compatibility

- **The legacy API is kept and supported.** No signature, parameter name, namespace or type name of
  `CalendarGenerator.ConsoleCalendar` changes, nothing is removed, and nothing is marked obsolete.
- **Apart from the five breaking changes above, the legacy class behaves as in 1.0.0:** the same output and
  the same use of the console. Tests compare it with the source of 1.0.0.
- **A program compiled against 1.0.0 runs with the new library without being rebuilt.** It then draws with the
  corrected behavior.
- **The modern API is new in 2.0,** so nothing existing depends on it. It is the API to use for new code.
- **The package still has no dependencies.**

## [1.0.0] - 2023-06-25

The first release.

### Added

- `CalendarGenerator.ConsoleCalendar`, which draws a month (`GenerateMonth`) or the twelve months of a year
  (`GenerateYear`) on the console, with colors for the title, the weekday row, the days and the selected
  dates.
- One `netstandard2.1` build.

[2.0.0]: https://github.com/ShaunAnthonyHathaway/ConsoleCalendarGenerator/releases/tag/v2.0.0
[1.0.0]: https://www.nuget.org/packages/ConsoleCalendarGenerator/1.0.0
