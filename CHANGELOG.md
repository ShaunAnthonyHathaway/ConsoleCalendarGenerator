# Changelog

Notable changes to ConsoleCalendarGenerator, newest first. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and versions follow
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.0.1] - Unreleased

### Changed

- **Documentation and package presentation.** The README, the package description and tags, the XML
  documentation and the example program are rewritten.
- **Installation instructions** name the package version.
- **File version.** It is `2.0.1.0`. The assembly version stays `2.0.0.0` for every 2.x release.

Runtime behavior and the public API are the same as in 2.0.0.

## [2.0.0] - 2026-11-27

Distributed through GitHub Releases. This version was not published to NuGet.

### Added

- **A structured rendering API** in the `ConsoleCalendarGenerator` namespace, alongside
  `CalendarGenerator.ConsoleCalendar`. It renders one month per call.
  - **Layout.** `MonthLayout` places the days of a month in weeks and weekday columns, for any first day of the
    week.
  - **Deterministic rendering.** `MonthRenderer` renders a month as text that depends only on the options it
    is given.
  - **Options.** `MonthRenderOptions` sets the culture, the first day of the week, weekday labels, the cell
    width, the column gap, an indent, and the format, suffix and alignment of the title.
  - **Semantic segments.** A `RenderedMonth` is lines made of segments that identify themselves: title,
    weekday label, day, marked day and the spacing between them.
  - **Day states with visible markers.** `DayState` marks dates in the text itself.
  - **Themes.** `CalendarTheme` assigns optional console colors to the title, the weekday labels, the days and
    each state.
  - **Writers.** `MonthWriter` writes a rendered month to any `TextWriter`. `ConsoleMonthWriter` writes it to
    the console in the colors of a theme and puts the caller's colors back.
- A `net10.0` build of the library, compatible with trimming and Native AOT.
- XML documentation for every public type and member.
- A README and complete metadata in the package.
- A symbol package with Source Link.

### Changed

`CalendarGenerator.ConsoleCalendar` keeps every public signature. Version 2 updates five of its observable
behaviors:

- **Constructor colors.** Constructor color arguments apply to the elements named by those arguments:
  `TitleColor` to the title, `DateColor` to the dates, `SelectedDateColor` to the selected dates and
  `DayOfWeekColor` to the weekday row. In 1.0.0, `TitleColor` applied to the weekday row and, with four
  arguments, `DayOfWeekColor` applied to the title. The parameterless constructor and the `Set` methods behave
  the same in both versions.
- **Null date lists.** `GenerateMonth` and `GenerateYear` throw `ArgumentNullException` with the parameter
  name `dates` when the list is `null`. In 1.0.0 they threw `NullReferenceException`.
- **Line endings.** Every line of console output ends with the console's line terminator. In 1.0.0 the last
  row of a month ended with a line feed.
- **Spacing after a month.** Each generated month ends with one blank line. In 1.0.0 a month whose last day is
  a Sunday was followed by two.
- **Month titles.** The title is the current culture's name for the Gregorian month shown, including for
  cultures whose default calendar is not Gregorian, such as `fa-IR`.

Also changed:

- **Supported frameworks.** The package has two builds, `net10.0` and `netstandard2.1`.
- **Assembly version.** It is `2.0.0.0`.

### Compatibility

- Every public signature, parameter name, namespace and type name of `CalendarGenerator.ConsoleCalendar` is the
  same as in 1.0.0.
- An application compiled against 1.0.0 runs with version 2 without being rebuilt.
- Apart from the five behaviors listed above, `CalendarGenerator.ConsoleCalendar` draws and uses the console
  as it did in 1.0.0.
- The package has no dependencies.

## [1.0.0] - 2023-06-25

The first release.

### Added

- `CalendarGenerator.ConsoleCalendar`, which draws a month (`GenerateMonth`) or the twelve months of a year
  (`GenerateYear`) on the console, with colors for the title, the weekday row, the dates and the selected
  dates.
- A `netstandard2.1` build.

[2.0.1]: https://github.com/ShaunAnthonyHathaway/ConsoleCalendarGenerator/releases/tag/v2.0.1
[2.0.0]: https://github.com/ShaunAnthonyHathaway/ConsoleCalendarGenerator/releases/tag/v2.0.0
[1.0.0]: https://www.nuget.org/packages/ConsoleCalendarGenerator/1.0.0
