# Changelog

Notable changes to ConsoleCalendarGenerator, newest first. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and versions follow
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

Work in the repository that is not in any published package. No version number or release date has been
decided for it.

### Added

- A modern API in the `ConsoleCalendarGenerator` namespace, beside the existing class:
  - `MonthLayout` places the days of a month in weeks and weekday columns, for any first day of the week.
  - `MonthRenderer` and `MonthRenderOptions` render a month as text that is the same on every machine, with
    the title and the weekday labels in a chosen culture, and with configurable cell width, column gap, indent,
    title format, title suffix and title alignment.
  - The result, `RenderedMonth`, is made of lines and segments that say what each piece of text is, so a month
    can be drawn elsewhere without parsing text.
  - `DayState` marks dates with a visible marker, so that a marked date does not depend on color.
  - `CalendarTheme` maps the title, the weekday labels, the days and each state to optional console colors.
  - `MonthWriter` writes a rendered month to any `TextWriter`. `ConsoleMonthWriter` writes it to the console in
    the colors of a theme, and puts the caller's colors back.
- A `net10.0` build of the library in the package, beside the `netstandard2.1` build. It is marked compatible
  with trimming and Native AOT.
- XML documentation for every public type and member, included in the package.
- A symbol package with Source Link.

### Changed

- The README, the example program and the screenshot are new and describe the modern API.

### Compatibility

- `CalendarGenerator.ConsoleCalendar` is unchanged for callers: the same public API, the same output and the
  same use of the console as in 1.0.0. This includes the constructors whose `TitleColor` argument sets the
  color of the weekday row. The package is checked against the published 1.0.0 package on every build.
- Projects that used the `netstandard2.1` build keep getting it. Projects that target .NET 10 or later get the
  `net10.0` build.

## [1.0.0] - 2023-06-25

The first release, and the only version published on NuGet so far.

### Added

- `CalendarGenerator.ConsoleCalendar`, which draws a month (`GenerateMonth`) or the twelve months of a year
  (`GenerateYear`) on the console, with colors for the title, the weekday row, the days and the selected
  dates.
- One `netstandard2.1` build.

[Unreleased]: https://github.com/ShaunAnthonyHathaway/ConsoleCalendarGenerator/commits/master
[1.0.0]: https://www.nuget.org/packages/ConsoleCalendarGenerator/1.0.0
