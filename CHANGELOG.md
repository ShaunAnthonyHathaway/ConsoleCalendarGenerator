# Changelog

Notable changes to ConsoleCalendarGenerator, newest first. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and versions follow
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

Development toward version 2.0. Nothing here is in a published package: 2.0 is not released, and no release
date has been set. The package built from this repository is numbered `2.0.0-dev` and is not published.

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
- The assembly version is `2.0.0.0`. It was `1.0.0.0` in 1.0.0.

### Breaking changes

Version 2.0 corrects a small number of behaviors of `CalendarGenerator.ConsoleCalendar` that were wrong in
1.0.0. Each one is listed here when it is made, with what to change in a program that relied on the old
behavior. The public API of the class does not change, so programs compile and load as before.

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

### Compatibility

- Apart from the breaking changes above, `CalendarGenerator.ConsoleCalendar` is unchanged for callers: the same
  public API, the same output and the same use of the console as in 1.0.0. The public API is compared with the
  published 1.0.0 package every time the package is built, and tests compare everything else with the source
  of 1.0.0.
- A program compiled against 1.0.0 runs with the new library without being rebuilt. It then draws with the
  corrected behavior.
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
